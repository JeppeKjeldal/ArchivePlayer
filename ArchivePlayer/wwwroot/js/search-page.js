class SearchPageController {
    constructor() {
        this.config = this.loadConfig();
        this.currentPage = 1;
        this.currentQuery = '';
        this.isSearching = false;

        this.initializeEventListeners();
    }

    loadConfig() {
        const configElement = document.getElementById('searchConfig');
        return configElement ? JSON.parse(configElement.textContent) : {};
    }

    initializeEventListeners() {
        const searchInput = document.getElementById('searchInput');
        if (searchInput) {
            searchInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.performSearch(true);
                }
            });
        }

        const searchButton = document.getElementById('searchButton');
        if (searchButton) {
            searchButton.addEventListener('click', (e) => {
                e.preventDefault();
                this.performSearch(true);
            });
        }

        // Modal event listeners
        const modal = document.getElementById('categoryModal');
        if (modal) {
            modal.addEventListener('click', (e) => {
                if (e.target === modal) this.closeCategoryModal();
            });
        }

        const closeModalBtn = document.getElementById('closeModalBtn');
        if (closeModalBtn) {
            closeModalBtn.addEventListener('click', () => this.closeCategoryModal());
        }

        const confirmCategoryBtn = document.getElementById('confirmCategoryBtn');
        if (confirmCategoryBtn) {
            confirmCategoryBtn.addEventListener('click', () => this.submitFinalForm());
        }
    }

    async performSearch(reset = true) {
        if (this.isSearching) return;

        const queryInput = document.getElementById('searchInput');
        if (!queryInput) return;

        if (reset) {
            this.currentQuery = queryInput.value.trim();
            this.currentPage = 1;
            if (!this.currentQuery) return;

            const resultsContainer = document.getElementById('resultsContainer');
            if (resultsContainer) {
                resultsContainer.innerHTML = '';
            }

            const searchResultsDiv = document.getElementById('searchResults');
            if (searchResultsDiv) {
                searchResultsDiv.classList.remove('hidden');
            }
        } else {
            this.currentQuery = queryInput.value.trim();
            if (!this.currentQuery) return;
        }

        this.isSearching = true;
        const searchButton = document.getElementById('searchButton');
        if (searchButton) {
            searchButton.disabled = true;
            searchButton.textContent = 'Searching...';
        }

        try {
            const url = `${this.config.liveSearchUrl}?query=${encodeURIComponent(this.currentQuery)}&page=${this.currentPage}`;
            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

            const data = await response.json();

            if (searchButton) {
                searchButton.disabled = false;
                searchButton.textContent = 'Search';
            }
            this.isSearching = false;

            const resultsContainer = document.getElementById('resultsContainer');

            if (data.results && data.results.length === 0 && this.currentPage === 1) {
                resultsContainer.innerHTML = '<div class="text-center py-8 text-gray-500">No video results found. Try a different search term.</div>';
                return;
            }

            this.renderResults(data.results, resultsContainer);

        } catch (error) {
            if (searchButton) {
                searchButton.disabled = false;
                searchButton.textContent = 'Search';
            }
            this.isSearching = false;
            const resultsContainer = document.getElementById('resultsContainer');
            if (resultsContainer && this.currentPage === 1) {
                resultsContainer.innerHTML = '<div class="text-center py-8 text-red-500">An error occurred while searching.</div>';
            }
            console.error('Search error:', error);
        }
    }

    renderResults(results, container) {
        results.forEach(item => {
            const card = document.createElement('div');
            card.className = 'bg-gray-900 rounded-lg overflow-hidden border border-gray-800 hover:border-netflix-red transition group';

            const safeTitle = (item.title || 'Unknown').replace(/'/g, "\\'").replace(/"/g, '&quot;');
            const thumbnail = item.thumbnailUrl || 'https://via.placeholder.com/300x450?text=No+Image';

            card.innerHTML = `
                <div class="aspect-[2/3] bg-black relative">
                    <img src="${thumbnail}" alt="${safeTitle}" class="w-full h-full object-cover" onerror="this.src='https://via.placeholder.com/300x450?text=No+Image'">
                    <div class="absolute top-2 right-2 bg-black/70 text-white text-xs px-2 py-1 rounded uppercase">${item.mediaType || 'Video'}</div>
                </div>
                <div class="p-3">
                    <h3 class="text-white font-semibold text-sm line-clamp-2 mb-3" title="${safeTitle}">${safeTitle}</h3>
                    <button type="button" class="add-to-library-btn w-full bg-netflix-red hover:bg-red-700 text-white text-sm font-semibold py-2 rounded transition flex items-center justify-center gap-2" 
                            data-identifier="${item.identifier}" 
                            data-title="${safeTitle}">
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
                        </svg>
                        Add to Library
                    </button>
                </div>
            `;

            const btn = card.querySelector('.add-to-library-btn');
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                this.openCategoryModal(item.identifier, item.title);
            });

            container.appendChild(card);
        });
    }

    openCategoryModal(identifier, title) {
        const modal = document.getElementById('categoryModal');
        const loading = document.getElementById('suggestionLoading');
        const content = document.getElementById('suggestionContent');
        const chipsContainer = document.getElementById('suggestionChips');
        const inputEl = document.getElementById('customCategoryInput');

        const finalInputString = document.getElementById('finalInputString');
        const finalCategoryName = document.getElementById('finalCategoryName');

        if (!modal) return;

        modal.classList.remove('hidden');
        modal.classList.add('flex');
        loading.classList.remove('hidden');
        content.classList.add('hidden');
        chipsContainer.innerHTML = '';
        inputEl.value = 'Uncategorized';

        if (finalInputString) finalInputString.value = identifier;
        if (finalCategoryName) finalCategoryName.value = 'Uncategorized';

        fetch(`${this.config.getCategorySuggestionsUrl}?identifier=${encodeURIComponent(identifier)}`)
            .then(response => response.json())
            .then(suggestions => {
                loading.classList.add('hidden');
                content.classList.remove('hidden');

                if (suggestions && suggestions.length > 0) {
                    const uniqueGenres = [...new Set(suggestions.map(s => s.name))].slice(0, 5);

                    uniqueGenres.forEach(genre => {
                        const chip = document.createElement('button');
                        chip.type = 'button';
                        chip.className = 'bg-gray-800 hover:bg-netflix-red hover:text-white text-gray-300 text-sm px-3 py-1.5 rounded-full border border-gray-700 transition';
                        chip.textContent = genre;
                        chip.addEventListener('click', () => {
                            inputEl.value = genre;
                            if (finalCategoryName) finalCategoryName.value = genre; // Opdater skjult form

                            document.querySelectorAll('#suggestionChips button').forEach(b => {
                                b.classList.remove('bg-netflix-red', 'text-white');
                                b.classList.add('bg-gray-800', 'text-gray-300');
                            });
                            chip.classList.remove('bg-gray-800', 'text-gray-300');
                            chip.classList.add('bg-netflix-red', 'text-white');
                        });
                        chipsContainer.appendChild(chip);
                    });
                } else {
                    chipsContainer.innerHTML = '<p class="text-gray-500 text-sm italic">No specific suggestions found. Feel free to type your own!</p>';
                }
            })
            .catch(() => {
                loading.classList.add('hidden');
                content.classList.remove('hidden');
                chipsContainer.innerHTML = '<p class="text-red-400 text-sm">Could not load suggestions.</p>';
            });
    }

    submitFinalForm() {
        const inputEl = document.getElementById('customCategoryInput');
        const finalCategoryName = document.getElementById('finalCategoryName');

        if (inputEl && finalCategoryName) {
            finalCategoryName.value = inputEl.value || 'Uncategorized';
        }

        const form = document.getElementById('finalAddForm');
        if (form) {
            form.submit();
        }
    }

    closeCategoryModal() {
        const modal = document.getElementById('categoryModal');
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.searchController = new SearchPageController();
});
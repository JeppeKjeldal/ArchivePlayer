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
        document.getElementById('tabSearch')?.addEventListener('click', () => this.switchTab('search'));
        document.getElementById('tabUrl')?.addEventListener('click', () => this.switchTab('url'));

        const searchInput = document.getElementById('searchQuery');
        searchInput?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') this.performSearch(true);
        });

        document.querySelector('button[onclick="performSearch()"]')?.addEventListener('click', () => this.performSearch(true));

        document.getElementById('loadMoreBtn')?.addEventListener('click', () => this.loadMore());

        const modal = document.getElementById('categoryModal');
        modal?.addEventListener('click', (e) => {
            if (e.target === modal) this.closeCategoryModal();
        });
    }

    switchTab(tab) {
        const tabSearch = document.getElementById('tabSearch');
        const tabUrl = document.getElementById('tabUrl');
        const contentSearch = document.getElementById('contentSearch');
        const contentUrl = document.getElementById('contentUrl');

        if (tab === 'search') {
            tabSearch.className = 'flex-1 py-3 text-center text-lg font-semibold text-netflix-red border-b-2 border-netflix-red transition';
            tabUrl.className = 'flex-1 py-3 text-center text-lg font-semibold text-gray-400 hover:text-white transition';
            contentSearch.classList.remove('hidden');
            contentUrl.classList.add('hidden');
        } else {
            tabUrl.className = 'flex-1 py-3 text-center text-lg font-semibold text-netflix-red border-b-2 border-netflix-red transition';
            tabSearch.className = 'flex-1 py-3 text-center text-lg font-semibold text-gray-400 hover:text-white transition';
            contentUrl.classList.remove('hidden');
            contentSearch.classList.add('hidden');
        }
    }

    async performSearch(reset = true) {
        if (this.isSearching) return;

        const queryInput = document.getElementById('searchQuery');
        if (reset) {
            this.currentQuery = queryInput.value.trim();
            this.currentPage = 1;
            if (!this.currentQuery) return;

            const resultsContainer = document.getElementById('searchResults');
            const placeholder = document.getElementById('searchPlaceholder');
            if (placeholder) placeholder.remove();
            resultsContainer.innerHTML = '';
            document.getElementById('loadMoreContainer').classList.add('hidden');
        } else {
            this.currentQuery = queryInput.value.trim();
            if (!this.currentQuery) return;
        }

        this.isSearching = true;
        const loading = document.getElementById('searchLoading');
        const loadMoreBtn = document.getElementById('loadMoreBtn');

        loading.classList.remove('hidden');
        if (loadMoreBtn) loadMoreBtn.disabled = true;

        try {
            const url = `${this.config.liveSearchUrl}?query=${encodeURIComponent(this.currentQuery)}&page=${this.currentPage}`;
            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

            const data = await response.json();
            loading.classList.add('hidden');
            this.isSearching = false;
            if (loadMoreBtn) loadMoreBtn.disabled = false;

            if (data.results && data.results.length === 0 && this.currentPage === 1) {
                document.getElementById('searchResults').innerHTML = '<div class="col-span-full text-center py-12 text-gray-500">No video results found. Try a different search term.</div>';
                return;
            }

            const resultsContainer = document.getElementById('searchResults');
            this.renderResults(data.results, resultsContainer);

            const loadMoreContainer = document.getElementById('loadMoreContainer');
            if (data.hasMoreResults) {
                loadMoreContainer.classList.remove('hidden');
            } else {
                loadMoreContainer.classList.add('hidden');
            }

        } catch (error) {
            loading.classList.add('hidden');
            this.isSearching = false;
            if (loadMoreBtn) loadMoreBtn.disabled = false;

            if (this.currentPage === 1) {
                document.getElementById('searchResults').innerHTML = '<div class="col-span-full text-center py-12 text-red-500">An error occurred while searching.</div>';
            }
            console.error('Search error:', error);
        }
    }

    renderResults(results, container) {
        results.forEach(item => {
            const card = document.createElement('div');
            card.className = 'bg-gray-900 rounded-lg overflow-hidden border border-gray-800 hover:border-netflix-red transition group';
            
            const safeTitle = item.title.replace(/'/g, "\\'").replace(/"/g, '&quot;');
            
            card.innerHTML = `
                <div class="aspect-[2/3] bg-black relative">
                    <img src="${item.thumbnailUrl}" alt="${item.title}" class="w-full h-full object-cover" onerror="this.src='https://via.placeholder.com/300x450?text=No+Image'">
                    <div class="absolute top-2 right-2 bg-black/70 text-white text-xs px-2 py-1 rounded uppercase">${item.mediaType}</div>
                </div>
                <div class="p-3">
                    <h3 class="text-white font-semibold text-sm line-clamp-2 mb-3" title="${item.title}">${item.title}</h3>
                    <button class="add-to-library-btn w-full bg-netflix-red hover:bg-red-700 text-white text-sm font-semibold py-2 rounded transition flex items-center justify-center gap-2" 
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
            btn.addEventListener('click', () => this.openCategoryModal(item.identifier, item.title));
            
            container.appendChild(card);
        });
    }

    loadMore() {
        this.currentPage++;
        this.performSearch(false);
    }

    openCategoryModal(identifier, title) {
        const modal = document.getElementById('categoryModal');
        const loading = document.getElementById('modalLoading');
        const content = document.getElementById('modalContent');
        const titleEl = document.getElementById('modalItemTitle');
        const chipsContainer = document.getElementById('suggestionChips');
        const inputEl = document.getElementById('modalCategoryInput');
        const form = document.getElementById('categoryForm');

        modal.classList.remove('hidden');
        loading.classList.remove('hidden');
        content.classList.add('hidden');
        chipsContainer.innerHTML = '';
        inputEl.value = 'Uncategorized';
        titleEl.textContent = title;
        document.getElementById('modalIdentifier').value = identifier;
        form.action = this.config.addSourceUrl;

        fetch(`${this.config.getCategorySuggestionsUrl}?identifier=${encodeURIComponent(identifier)}`)
            .then(res => res.json())
            .then(genres => {
                loading.classList.add('hidden');
                content.classList.remove('hidden');

                if (genres && genres.length > 0) {
                    const uniqueGenres = [...new Set(genres)].slice(0, 5);
                    uniqueGenres.forEach(genre => {
                        const chip = document.createElement('button');
                        chip.type = 'button';
                        chip.className = 'bg-gray-800 hover:bg-netflix-red hover:text-white text-gray-300 text-sm px-3 py-1.5 rounded-full border border-gray-700 transition';
                        chip.textContent = genre;
                        chip.addEventListener('click', () => {
                            inputEl.value = genre;
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

    closeCategoryModal() {
        document.getElementById('categoryModal').classList.add('hidden');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.searchController = new SearchPageController();
});
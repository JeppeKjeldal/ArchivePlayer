/**
 * ArchivePlayer - Home Page Logic
 * Fetches library data from the API and renders the UI dynamically.
 */

class HomePageController {
    constructor() {
        this.libraryApiBaseUrl = '/api/v1/library';
    }

    async initialize() {
        this.initializeEventListeners();
        await this.refreshLibrarySections();
    }

    initializeEventListeners() {
        document.addEventListener('click', event => this.handlePageClick(event));
    }

    async refreshLibrarySections() {
        await Promise.all([
            this.loadContinueWatching(),
            this.loadLibrary()
        ]);
    }

    async handlePageClick(event) {
        const actionButton = event.target.closest('[data-library-action]');
        if (!actionButton) return;

        event.preventDefault();
        event.stopPropagation();

        const userMediaItemId = Number(actionButton.dataset.userMediaItemId);
        if (!Number.isFinite(userMediaItemId)) return;

        const action = actionButton.dataset.libraryAction;
        const title = actionButton.dataset.title || 'this item';

        if (action === 'remove-from-library') {
            await this.removeLibraryItem(userMediaItemId, title, actionButton);
        }

        if (action === 'clear-recent-history') {
            await this.clearRecentHistory(userMediaItemId, title, actionButton);
        }
    }

    async loadContinueWatching() {
        const section = document.getElementById('continueWatchingSection');
        const grid = document.getElementById('continueWatchingGrid');

        if (!section || !grid) return;

        try {
            const response = await fetch(`${this.libraryApiBaseUrl}/recently-watched?limit=10`);
            if (!response.ok) throw new Error('Failed to fetch recently watched');

            const items = await response.json();

            grid.innerHTML = '';

            if (items.length === 0) {
                section.classList.add('hidden');
                return;
            }

            section.classList.remove('hidden');

            items.forEach(item => {
                const card = this.createContinueWatchingCard(item);
                grid.appendChild(card);
            });
        } catch (error) {
            console.error('Error loading continue watching:', error);
            section.classList.add('hidden');
        }
    }

    async loadLibrary() {
        const librarySection = document.getElementById('librarySection');
        const emptyState = document.getElementById('emptyState');
        const container = document.getElementById('libraryContainer');

        if (!librarySection || !emptyState || !container) return;

        try {
            const response = await fetch(this.libraryApiBaseUrl);
            if (!response.ok) throw new Error('Failed to fetch library');

            const data = await response.json();
            const categoryNames = Object.keys(data.categories || {});

            container.innerHTML = '';

            if (categoryNames.length === 0) {
                librarySection.classList.add('hidden');
                emptyState.classList.remove('hidden');
                return;
            }

            librarySection.classList.remove('hidden');
            emptyState.classList.add('hidden');

            const sortedCategories = this.sortCategoryNames(categoryNames);

            sortedCategories.forEach(categoryName => {
                const items = data.categories[categoryName];
                const section = this.createCategorySection(categoryName, items);
                container.appendChild(section);
            });
        } catch (error) {
            console.error('Error loading library:', error);
            librarySection.classList.add('hidden');
            emptyState.classList.remove('hidden');
        }
    }

    sortCategoryNames(categoryNames) {
        return categoryNames.sort((firstCategory, secondCategory) => {
            if (firstCategory === 'Uncategorized') return 1;
            if (secondCategory === 'Uncategorized') return -1;
            return firstCategory.localeCompare(secondCategory);
        });
    }

    createContinueWatchingCard(item) {
        return this.createMediaCard(item, {
            showProgress: true,
            showClearRecentHistory: true
        });
    }

    createLibraryCard(item) {
        return this.createMediaCard(item, {
            showProgress: false,
            showClearRecentHistory: false
        });
    }

    createMediaCard(item, options) {
        const card = document.createElement('article');
        card.className = 'relative bg-gray-900 rounded-lg overflow-hidden border border-gray-800 hover:border-netflix-red transition group';

        const title = this.escapeHtml(item.title);
        const category = this.escapeHtml(item.categoryName);
        const posterUrl = this.escapeAttribute(item.tmdbPosterUrl || item.thumbnailUrl || '');
        const watchUrl = `/Watch/${encodeURIComponent(item.identifier)}`;
        const progressPercent = this.calculateProgressPercent(item.watchProgressSeconds);

        card.innerHTML = `
            <div class="absolute top-2 right-2 z-20 flex gap-2 opacity-100 sm:opacity-0 sm:group-hover:opacity-100 focus-within:opacity-100 transition">
                ${options.showClearRecentHistory ? this.createClearRecentHistoryButton(item) : ''}
                ${this.createRemoveLibraryButton(item)}
            </div>

            <a href="${watchUrl}" class="block">
                <div class="aspect-[2/3] bg-black relative">
                    <img src="${posterUrl}" alt="${title}" class="w-full h-full object-cover">
                    ${options.showProgress ? `
                        <div class="absolute bottom-0 left-0 right-0 h-1 bg-gray-700">
                            <div class="h-full bg-netflix-red" style="width: ${progressPercent}%"></div>
                        </div>
                    ` : ''}
                </div>

                <div class="p-3">
                    <h3 class="text-white font-semibold text-sm line-clamp-2 mb-1" title="${title}">${title}</h3>
                    <p class="text-gray-400 text-xs">${category}</p>
                </div>
            </a>
        `;

        this.attachImageFallback(card);

        return card;
    }

    createCategorySection(categoryName, items) {
        const section = document.createElement('div');
        section.className = 'mb-8';

        const header = document.createElement('h3');
        header.className = 'text-xl font-bold text-white mb-3';
        header.textContent = categoryName;

        const grid = document.createElement('div');
        grid.className = 'grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4';

        items.forEach(item => {
            const card = this.createLibraryCard(item);
            grid.appendChild(card);
        });

        section.appendChild(header);
        section.appendChild(grid);

        return section;
    }

    createRemoveLibraryButton(item) {
        return `
            <button type="button"
                    data-library-action="remove-from-library"
                    data-user-media-item-id="${item.id}"
                    data-title="${this.escapeAttribute(item.title)}"
                    class="bg-black/80 hover:bg-netflix-red text-white rounded-full p-2 border border-gray-700 transition"
                    title="Remove from library"
                    aria-label="Remove ${this.escapeAttribute(item.title)} from library">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 pointer-events-none" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm4 0a1 1 0 10-2 0v6a1 1 0 102 0V8z" clip-rule="evenodd" />
                </svg>
            </button>
        `;
    }

    createClearRecentHistoryButton(item) {
        return `
            <button type="button"
                    data-library-action="clear-recent-history"
                    data-user-media-item-id="${item.id}"
                    data-title="${this.escapeAttribute(item.title)}"
                    class="bg-black/80 hover:bg-gray-700 text-white rounded-full p-2 border border-gray-700 transition"
                    title="Remove from Continue Watching"
                    aria-label="Remove ${this.escapeAttribute(item.title)} from Continue Watching">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 pointer-events-none" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-12.75a.75.75 0 00-1.5 0v5c0 .27.14.52.37.65l3.5 2a.75.75 0 10.76-1.3l-3.13-1.79V5.25z" clip-rule="evenodd" />
                </svg>
            </button>
        `;
    }

    async removeLibraryItem(userMediaItemId, title, button) {
        const confirmed = confirm(`Remove "${title}" from your library?`);
        if (!confirmed) return;

        await this.deleteLibraryResource(
            `${this.libraryApiBaseUrl}/${userMediaItemId}`,
            button,
            'Failed to remove item from library'
        );
    }

    async clearRecentHistory(userMediaItemId, title, button) {
        const confirmed = confirm(`Remove "${title}" from Continue Watching?`);
        if (!confirmed) return;

        await this.deleteLibraryResource(
            `${this.libraryApiBaseUrl}/${userMediaItemId}/recent-history`,
            button,
            'Failed to remove item from Continue Watching'
        );
    }

    async deleteLibraryResource(url, button, errorMessage) {
        const originalDisabledState = button.disabled;
        button.disabled = true;

        try {
            const response = await fetch(url, {
                method: 'DELETE'
            });

            if (!response.ok) {
                throw new Error(`${errorMessage}. Status: ${response.status}`);
            }

            await this.refreshLibrarySections();
        } catch (error) {
            console.error(error);
            alert(errorMessage);
            button.disabled = originalDisabledState;
        }
    }

    calculateProgressPercent(watchProgressSeconds) {
        if (!watchProgressSeconds || watchProgressSeconds <= 0) return 0;

        const assumedEpisodeDurationSeconds = 1800;
        return Math.min((watchProgressSeconds / assumedEpisodeDurationSeconds) * 100, 100);
    }

    attachImageFallback(card) {
        const image = card.querySelector('img');
        if (!image) return;

        image.addEventListener('error', () => {
            image.src = 'https://placehold.co/300x450/111827/9CA3AF?text=No+Image';
        }, { once: true });
    }

    escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }

    escapeAttribute(value) {
        return this.escapeHtml(value);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new HomePageController().initialize();
});
/**
 * ArchivePlayer - Enhanced video player with Netflix-style features
 */

class VideoPlayer {
    constructor(container) {
        this.container = container;
        this.videoElement = document.getElementById('videoPlayer');
        this.identifier = container.dataset.identifier;
        this.savedProgress = parseInt(container.dataset.savedProgress) || 0;
        this.showDescription = container.dataset.showDescription || 'No description available.';
        this.hideOverlayTimeout = null;
        this.lastTapTime = 0;
    }

    initialize() {
        this.setupVideoSource();
        this.setupVolumeMemory();
        this.setupProgressTracking();
        this.setupControlOverlay();
        this.setupPlaybackSpeed();
        this.setupPictureInPicture();
        this.setupSkipButtons();
        this.setupKeyboardShortcuts();
        this.setupMobileGestures();
        this.videoElement.play().catch(() => {});
    }

    setupVideoSource() {
        const videoFile = this.container.dataset.videoFile;
        if (!videoFile) return;
        this.videoElement.src = `https://archive.org/download/${this.identifier}/${encodeURIComponent(videoFile)}`;
        if (this.savedProgress > 0) {
            this.videoElement.addEventListener('loadedmetadata', () => {
                this.videoElement.currentTime = this.savedProgress;
            }, { once: true });
        }
    }

    setupVolumeMemory() {
        const savedVolume = localStorage.getItem('videoVolume');
        if (savedVolume !== null) this.videoElement.volume = parseFloat(savedVolume);
        this.videoElement.addEventListener('volumechange', () => {
            localStorage.setItem('videoVolume', this.videoElement.volume);
        });
    }

    setupProgressTracking() {
        setInterval(() => this.saveProgress(), 10000);
        window.addEventListener('beforeunload', () => this.saveProgress());
    }

    saveProgress() {
        const progressSeconds = Math.floor(this.videoElement.currentTime);
        const currentEpisode = localStorage.getItem('currentEpisode') || '';
        fetch('/Watch/SaveProgress', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `identifier=${encodeURIComponent(this.identifier)}&progressSeconds=${progressSeconds}&episodeFileName=${encodeURIComponent(currentEpisode)}`
        });
    }

    setupControlOverlay() {
        const overlay = document.getElementById('controlOverlay');
        if (!overlay) return;
        this.container.addEventListener('mousemove', () => {
            overlay.style.opacity = '1';
            clearTimeout(this.hideOverlayTimeout);
            this.hideOverlayTimeout = setTimeout(() => {
                if (!this.videoElement.paused) overlay.style.opacity = '0';
            }, 3000);
        });
        this.container.addEventListener('mouseleave', () => {
            if (!this.videoElement.paused) overlay.style.opacity = '0';
        });
    }

    setupPlaybackSpeed() {
        const speedSelect = document.getElementById('playbackSpeed');
        if (!speedSelect) return;
        speedSelect.addEventListener('change', () => {
            this.videoElement.playbackRate = parseFloat(speedSelect.value);
        });
    }

    setupPictureInPicture() {
        const pipButton = document.getElementById('pipButton');
        if (!pipButton) return;
        pipButton.addEventListener('click', async () => {
            try {
                if (document.pictureInPictureElement) await document.exitPictureInPicture();
                else await this.videoElement.requestPictureInPicture();
            } catch (error) { console.error('PiP error:', error); }
        });
    }

    setupSkipButtons() {
        const skipBack = document.getElementById('skipBackButton');
        const skipForward = document.getElementById('skipForwardButton');
        if (skipBack) skipBack.addEventListener('click', () => this.skipBackward());
        if (skipForward) skipForward.addEventListener('click', () => this.skipForward());
    }

    skipBackward() {
        this.videoElement.currentTime = Math.max(0, this.videoElement.currentTime - 10);
    }

    skipForward() {
        this.videoElement.currentTime = Math.min(this.videoElement.duration || 0, this.videoElement.currentTime + 10);
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => this.handleKeyPress(e));
    }

    handleKeyPress(e) {
        if (['INPUT', 'TEXTAREA', 'SELECT'].includes(e.target.tagName)) return;
        switch (e.key.toLowerCase()) {
            case ' ': case 'k': e.preventDefault(); this.videoElement.paused ? this.videoElement.play() : this.videoElement.pause(); break;
            case 'arrowleft': case 'j': e.preventDefault(); this.skipBackward(); break;
            case 'arrowright': case 'l': e.preventDefault(); this.skipForward(); break;
            case 'arrowup': e.preventDefault(); this.videoElement.volume = Math.min(1, this.videoElement.volume + 0.1); break;
            case 'arrowdown': e.preventDefault(); this.videoElement.volume = Math.max(0, this.videoElement.volume - 0.1); break;
            case 'm': e.preventDefault(); this.videoElement.muted = !this.videoElement.muted; break;
            case 'f': e.preventDefault(); document.fullscreenElement ? document.exitFullscreen() : this.container.requestFullscreen(); break;
        }
    }

    setupMobileGestures() {
        this.container.addEventListener('touchend', (e) => {
            const currentTime = new Date().getTime();
            const tapLength = currentTime - this.lastTapTime;
            if (tapLength < 300 && tapLength > 0) {
                const rect = this.container.getBoundingClientRect();
                const tapX = e.changedTouches[0].clientX - rect.left;
                if (tapX < rect.width / 3) {
                    this.skipBackward();
                    this.showDoubleTapIndicator('left');
                } else if (tapX > rect.width * 2 / 3) {
                    this.skipForward();
                    this.showDoubleTapIndicator('right');
                }
            } else {
                this.lastTapTime = currentTime;
            }
        });
    }

    showDoubleTapIndicator(side) {
        const indicator = document.getElementById(`doubleTap${side.charAt(0).toUpperCase() + side.slice(1)}`);
        if (!indicator) return;
        indicator.style.opacity = '1';
        setTimeout(() => { indicator.style.opacity = '0'; }, 500);
    }
}

class EpisodePlayer extends VideoPlayer {
    constructor(container) {
        super(container);
        this.seasonMap = {};
        this.savedEpisode = container.dataset.savedEpisode || '';
        this.countdownInterval = null;

        const jsonDataElement = document.getElementById('videoPlayerData');
        if (jsonDataElement) {
            try {
                const jsonData = JSON.parse(jsonDataElement.textContent);
                this.episodes = jsonData.episodes || [];
                this.episodeThumbnails = jsonData.episodeThumbnails || [];
            } catch (error) {
                console.error('Failed to parse episode data:', error);
                this.episodes = [];
                this.episodeThumbnails = [];
            }
        } else {
            this.episodes = [];
            this.episodeThumbnails = [];
        }
    }

    initialize() {
        this.buildSeasonMap();
        this.populateSeasonDropdown();
        this.selectInitialSeason();
        this.renderEpisodeCards();
        
        this.setupVolumeMemory();
        this.setupProgressTracking();
        this.setupControlOverlay();
        this.setupPlaybackSpeed();
        this.setupPictureInPicture();
        this.setupSkipButtons();
        this.setupKeyboardShortcuts();
        this.setupMobileGestures();
        this.setupAutoplay();
        this.setupCountdownCancel();
        
        this.resumeFromSavedProgress();
    }

    buildSeasonMap() {
        this.episodes.forEach(file => {
            const seasonInfo = this.extractSeasonInfo(file);
            const episodeInfo = this.extractEpisodeInfo(file);
            const tmdbData = this.findTmdbDataForEpisode(seasonInfo.number, episodeInfo.number);

            if (!this.seasonMap[seasonInfo.label]) {
                this.seasonMap[seasonInfo.label] = [];
            }

            this.seasonMap[seasonInfo.label].push({
                filename: file,
                thumbnail: tmdbData ? tmdbData.stillUrl : null,
                name: tmdbData ? tmdbData.name : file,
                episodeNum: episodeInfo.number,
                seasonNum: seasonInfo.number,
                overview: tmdbData ? tmdbData.overview : null
            });
        });
    }

    extractSeasonInfo(filename) {
        const match = filename.match(/S(\d+)/i) || filename.match(/Season\s*(\d+)/i) || filename.match(/(\d+)x\d+/i);
        if (match) {
            return { number: parseInt(match[1]), label: `Season ${String(parseInt(match[1])).padStart(2, '0')}` };
        }
        return { number: null, label: 'Other' };
    }

    extractEpisodeInfo(filename) {
        const match = filename.match(/E(\d+)/i) || filename.match(/Ep(?:isode)?\s*(\d+)/i) || filename.match(/\d+x(\d+)/i);
        return { number: match ? parseInt(match[1]) : null };
    }

    findTmdbDataForEpisode(seasonNumber, episodeNumber) {
        if (!seasonNumber || !episodeNumber || !this.episodeThumbnails.length) return null;
        return this.episodeThumbnails.find(ep => ep.seasonNumber === seasonNumber && ep.episodeNumber === episodeNumber);
    }

    populateSeasonDropdown() {
        const dropdown = document.getElementById('seasonSelect');
        if (!dropdown) return;
        Object.keys(this.seasonMap).sort().forEach(season => {
            const option = document.createElement('option');
            option.value = season;
            option.textContent = `${season} (${this.seasonMap[season].length} episodes)`;
            dropdown.appendChild(option);
        });
    }

    setupSeasonSelector() {
        const dropdown = document.getElementById('seasonSelect');
        if (dropdown) {
            dropdown.addEventListener('change', () => this.renderEpisodeCards());
        }
    }

    selectInitialSeason() {
        if (!this.savedEpisode) return;
        const season = this.findSeasonForEpisode(this.savedEpisode);
        const dropdown = document.getElementById('seasonSelect');
        if (dropdown) dropdown.value = season;
    }

    findSeasonForEpisode(filename) {
        for (const [season, episodes] of Object.entries(this.seasonMap)) {
            if (episodes.some(ep => ep.filename === filename)) return season;
        }
        return 'all';
    }

    renderEpisodeCards() {
        const dropdown = document.getElementById('seasonSelect');
        const container = document.getElementById('episodesContainer');
        if (!container || !dropdown) return;

        container.innerHTML = '';
        const selectedSeason = dropdown.value;
        const episodesToShow = selectedSeason === 'all' ? Object.values(this.seasonMap).flat() : (this.seasonMap[selectedSeason] || []);
        const currentPlaying = localStorage.getItem('currentEpisode') || this.savedEpisode;

        episodesToShow.forEach(ep => {
            const card = this.createEpisodeCard(ep, ep.filename === currentPlaying);
            card.addEventListener('click', () => this.playEpisode(ep.filename, 0));
            container.appendChild(card);
        });

        setTimeout(() => {
            if (currentPlaying) this.highlightCurrentEpisode(currentPlaying);
        }, 100);
    }

    createEpisodeCard(episode, isCurrent) {
        const card = document.createElement('div');
        const borderClass = isCurrent ? 'border-netflix-red ring-2 ring-netflix-red' : 'border-gray-700 hover:border-netflix-red';
        card.className = `flex-shrink-0 w-64 bg-gray-800 rounded-lg overflow-hidden hover:scale-105 cursor-pointer transition border ${borderClass}`;
        card.setAttribute('data-filename', episode.filename);
        const displayName = episode.episodeNum ? `Episode ${episode.episodeNum}` : episode.name;

        card.innerHTML = `
            ${episode.thumbnail ? 
                `<div class="aspect-video bg-black"><img src="${episode.thumbnail}" alt="${episode.name}" class="w-full h-full object-cover" /></div>` : 
                `<div class="aspect-video bg-gradient-to-br from-gray-700 to-gray-900 flex items-center justify-center"><svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 text-gray-600" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm3 2h6v4H7V5zm8 8v2h1v-2h-1zm-2-2H7v4h6v-4zm2 0h1V9h-1v2zm1-4V5h-1v2h1zM5 5v2H4V5h1zm-1 4h1v2H4V9zm0 4h1v2H4v-2z" clip-rule="evenodd" /></svg></div>`
            }
            <div class="p-3 bg-gray-900">
                <p class="text-white font-semibold text-sm truncate">${displayName}</p>
                <p class="text-gray-400 text-xs mt-1 truncate" title="${episode.filename}">${episode.name}</p>
            </div>
        `;
        return card;
    }

    playEpisode(filename, startTime = 0) {
        const newSrc = `https://archive.org/download/${this.identifier}/${encodeURIComponent(filename)}`;
        if (this.videoElement.src !== newSrc) {
            this.videoElement.src = newSrc;
            this.videoElement.load();
        }
        
        const attemptPlay = () => {
            this.videoElement.currentTime = startTime;
            this.videoElement.play().catch(err => console.warn("Play interrupted:", err));
        };

        if (this.videoElement.readyState >= 2) attemptPlay();
        else this.videoElement.addEventListener('loadeddata', attemptPlay, { once: true });
        
        localStorage.setItem('currentEpisode', filename);
        this.highlightCurrentEpisode(filename);
        this.updateEpisodeInfo(filename);
        this.hideCountdown();
    }

    highlightCurrentEpisode(filename) {
        const container = document.getElementById('episodesContainer');
        if (!container) return;
        
        container.querySelectorAll('div').forEach(card => {
            card.classList.remove('border-netflix-red', 'ring-2', 'ring-netflix-red');
            card.classList.add('border-gray-700');
        });
        const currentCard = container.querySelector(`div[data-filename="${filename}"]`);
        if (currentCard) {
            currentCard.classList.remove('border-gray-700');
            currentCard.classList.add('border-netflix-red', 'ring-2', 'ring-netflix-red');
            currentCard.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        }
    }

    updateEpisodeInfo(filename) {
        const allEpisodes = Object.values(this.seasonMap).flat();
        const episode = allEpisodes.find(ep => ep.filename === filename);
        if (!episode) return;

        const episodeInfoElement = document.getElementById('episodeInfo');
        const descriptionElement = document.getElementById('episodeDescription');

        if (episode.seasonNum && episode.episodeNum) {
            const episodeTitle = episode.name ? ` - ${episode.name}` : '';
            episodeInfoElement.textContent = `Season ${episode.seasonNum}, Episode ${episode.episodeNum}${episodeTitle}`;
        } else {
            episodeInfoElement.textContent = '';
        }
        descriptionElement.textContent = episode.overview || this.showDescription;
    }

    setupAutoplay() {
        this.videoElement.addEventListener('ended', () => this.handleEpisodeEnded());
    }

    handleEpisodeEnded() {
        const currentEpisode = localStorage.getItem('currentEpisode') || '';
        const nextEpisode = this.getNextEpisode(currentEpisode);
        if (nextEpisode) this.showCountdown(nextEpisode);
    }

    getNextEpisode(currentFilename) {
        const allEpisodes = Object.values(this.seasonMap).flat();
        const currentIndex = allEpisodes.findIndex(ep => ep.filename === currentFilename);
        if (currentIndex >= 0 && currentIndex < allEpisodes.length - 1) {
            return allEpisodes[currentIndex + 1];
        }
        return null;
    }

    showCountdown(nextEpisode) {
        const countdown = document.getElementById('nextEpisodeCountdown');
        const countdownNumber = document.getElementById('countdownNumber');
        if (!countdown || !countdownNumber) return;

        countdown.classList.remove('hidden');
        let remaining = 10;
        countdownNumber.textContent = remaining;

        this.countdownInterval = setInterval(() => {
            remaining--;
            countdownNumber.textContent = remaining;
            if (remaining <= 0) {
                this.hideCountdown();
                this.playEpisode(nextEpisode.filename, 0);
            }
        }, 1000);
    }

    hideCountdown() {
        if (this.countdownInterval) {
            clearInterval(this.countdownInterval);
            this.countdownInterval = null;
        }
        const countdown = document.getElementById('nextEpisodeCountdown');
        if (countdown) countdown.classList.add('hidden');
    }

    setupCountdownCancel() {
        const cancelButton = document.getElementById('cancelCountdown');
        if (cancelButton) cancelButton.addEventListener('click', () => this.hideCountdown());
        this.videoElement.addEventListener('play', () => this.hideCountdown());
    }

    resumeFromSavedProgress() {
        const allEpisodes = Object.values(this.seasonMap).flat();
        if (this.savedEpisode && allEpisodes.some(ep => ep.filename === this.savedEpisode)) {
            this.playEpisode(this.savedEpisode, this.savedProgress);
            return;
        }
        if (allEpisodes.length > 0) {
            this.playEpisode(allEpisodes[0].filename, 0);
        }
    }
}

// Safe Initialization (Handles both early and late script loading)
function initPlayer() {
    const container = document.getElementById('videoPlayerContainer');
    if (!container) return;
    
    const hasEpisodes = container.dataset.hasEpisodes === 'true';
    if (hasEpisodes) {
        new EpisodePlayer(container).initialize();
    } else {
        new VideoPlayer(container).initialize();
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initPlayer);
} else {
    initPlayer();
}
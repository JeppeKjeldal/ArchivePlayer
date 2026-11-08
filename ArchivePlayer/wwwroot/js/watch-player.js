class VideoPlayer {
    constructor(container) {
        this.container = container;
        this.videoElement = document.getElementById('videoPlayer');
        this.identifier = container.dataset.identifier;
        this.apiBaseUrl = '/api/v1/playback';
        
        this.hideOverlayTimeout = null;
        this.lastTapTime = 0;
        this.playbackData = null;
    }

    async initialize() {
        await this.fetchPlaybackData();
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

    async fetchPlaybackData() {
        try {
            const response = await fetch(`${this.apiBaseUrl}/${this.identifier}`);
            if (!response.ok) throw new Error('Failed to fetch playback data');
            this.playbackData = await response.json();
            
            document.getElementById('videoTitle').textContent = this.playbackData.title || 'Unknown Title';
            document.getElementById('episodeDescription').textContent = this.playbackData.description || 'No description available.';
        } catch (error) {
            console.error('Error fetching playback data:', error);
        }
    }

    setupVideoSource() {
        if (!this.playbackData || !this.playbackData.videoFiles || this.playbackData.videoFiles.length === 0) return;
        
        const videoFile = this.playbackData.currentEpisodeFileName || this.playbackData.videoFiles[0];
        this.videoElement.src = `https://archive.org/download/${this.identifier}/${encodeURIComponent(videoFile)}`;
        
        if (this.playbackData.watchProgressSeconds > 0) {
            this.videoElement.addEventListener('loadedmetadata', () => {
                this.videoElement.currentTime = this.playbackData.watchProgressSeconds;
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
        const currentEpisode = this.getCurrentEpisode();
        
        fetch(`${this.apiBaseUrl}/progress`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                identifier: this.identifier,
                progressSeconds: progressSeconds,
                episodeFileName: currentEpisode
            })
        }).catch(err => console.error('Failed to save progress:', err));
    }

    getCurrentEpisode() {
        return localStorage.getItem('currentEpisode') || (this.playbackData?.currentEpisodeFileName || '');
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
        this.countdownInterval = null;
    }

    async initialize() {
        await this.fetchPlaybackData();
        
        this.buildSeasonMap();
        this.populateSeasonDropdown();
        this.setupSeasonSelector();
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
        if (!this.playbackData || !this.playbackData.episodes) {
            console.log("No episodes found in API response");
            return;
        }

        this.playbackData.episodes.forEach(ep => {
            const seasonLabel = `Season ${String(ep.seasonNumber).padStart(2, '0')}`;
            if (!this.seasonMap[seasonLabel]) {
                this.seasonMap[seasonLabel] = [];
            }
            this.seasonMap[seasonLabel].push(ep);
        });
        console.log("Season map built:", this.seasonMap);
    }

    populateSeasonDropdown() {
        const dropdown = document.getElementById('seasonSelect');
        if (!dropdown) return;
        
        document.getElementById('episodesSection').classList.remove('hidden');
        
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
        const currentEpisode = this.getCurrentEpisode();
        if (!currentEpisode) return;
        
        const season = this.findSeasonForEpisode(currentEpisode);
        const dropdown = document.getElementById('seasonSelect');
        if (dropdown) dropdown.value = season;
    }

    findSeasonForEpisode(filename) {
        for (const [season, episodes] of Object.entries(this.seasonMap)) {
            if (episodes.some(ep => ep.fileName === filename)) return season;
        }
        return 'all';
    }

    renderEpisodeCards() {
        const dropdown = document.getElementById('seasonSelect');
        const container = document.getElementById('episodesContainer');
        if (!container || !dropdown) return;

        container.innerHTML = '';
        const selectedSeason = dropdown.value;
        console.log("Rendering episodes for season:", selectedSeason);
        const episodesToShow = selectedSeason === 'all' ? Object.values(this.seasonMap).flat() : (this.seasonMap[selectedSeason] || []);
        const currentPlaying = this.getCurrentEpisode();

        episodesToShow.forEach(ep => {
            const card = this.createEpisodeCard(ep, ep.fileName === currentPlaying);
            card.addEventListener('click', () => this.playEpisode(ep.fileName, 0));
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
        card.setAttribute('data-filename', episode.fileName);
        const displayName = episode.episodeNumber ? `Episode ${episode.episodeNumber}` : (episode.name || 'Unknown');

        card.innerHTML = `
            ${episode.stillUrl ? 
                `<div class="aspect-video bg-black"><img src="${episode.stillUrl}" alt="${episode.name}" class="w-full h-full object-cover" /></div>` : 
                `<div class="aspect-video bg-gradient-to-br from-gray-700 to-gray-900 flex items-center justify-center"><svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 text-gray-600" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm3 2h6v4H7V5zm8 8v2h1v-2h-1zm-2-2H7v4h6v-4zm2 0h1V9h-1v2zm1-4V5h-1v2h1zM5 5v2H4V5h1zm-1 4h1v2H4V9zm0 4h1v2H4v-2z" clip-rule="evenodd" /></svg></div>`
            }
            <div class="p-3 bg-gray-900">
                <p class="text-white font-semibold text-sm truncate">${displayName}</p>
                <p class="text-gray-400 text-xs mt-1 truncate" title="${episode.fileName}">${episode.name || episode.fileName}</p>
            </div>
        `;
        return card;
    }

    playEpisode(filename, startTime = 0) {
        if (!filename) {
            console.error("Attempted to play episode with empty filename!");
            return;
        }
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
        if (!this.playbackData || !this.playbackData.episodes) return;
        const episode = this.playbackData.episodes.find(ep => ep.fileName === filename);
        if (!episode) return;

        const episodeInfoElement = document.getElementById('episodeInfo');
        const descriptionElement = document.getElementById('episodeDescription');

        if (episode.seasonNumber && episode.episodeNumber) {
            const episodeTitle = episode.name ? ` - ${episode.name}` : '';
            episodeInfoElement.textContent = `Season ${episode.seasonNumber}, Episode ${episode.episodeNumber}${episodeTitle}`;
        } else {
            episodeInfoElement.textContent = '';
        }
        descriptionElement.textContent = episode.overview || this.playbackData.description || 'No description available.';
    }

    setupAutoplay() {
        this.videoElement.addEventListener('ended', () => this.handleEpisodeEnded());
    }

    handleEpisodeEnded() {
        const currentEpisode = this.getCurrentEpisode();
        const nextEpisode = this.getNextEpisode(currentEpisode);
        if (nextEpisode) this.showCountdown(nextEpisode);
    }

    getNextEpisode(currentFilename) {
        if (!this.playbackData || !this.playbackData.episodes) return null;
        const allEpisodes = Object.values(this.seasonMap).flat();
        const currentIndex = allEpisodes.findIndex(ep => ep.fileName === currentFilename);
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
                this.playEpisode(nextEpisode.fileName, 0);
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
        if (!this.playbackData || !this.playbackData.videoFiles || this.playbackData.videoFiles.length === 0) {
            console.error("No video files available");
            return;
        }
        
        const savedEpisode = this.playbackData.currentEpisodeFileName || this.getCurrentEpisode();
        const allEpisodes = this.playbackData.episodes ? Object.values(this.seasonMap).flat() : [];
        
        let fileToPlay = null;

        if (savedEpisode && this.playbackData.videoFiles.includes(savedEpisode)) {
            fileToPlay = savedEpisode;
        } else {
            const firstValidEpisode = allEpisodes.find(ep => ep.fileName && this.playbackData.videoFiles.includes(ep.fileName));
            if (firstValidEpisode) {
                fileToPlay = firstValidEpisode.fileName;
            } else {
                fileToPlay = this.playbackData.videoFiles[0];
            }
        }
        
        console.log("Resuming playback with file:", fileToPlay);
        this.playEpisode(fileToPlay, this.playbackData.watchProgressSeconds);
    }
}

function initPlayer() {
    const container = document.getElementById('videoPlayerContainer');
    if (!container) return;
    
    new EpisodePlayer(container).initialize();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initPlayer);
} else {
    initPlayer();
}
// Music Player JavaScript - Advanced Sticky Player Implementation

class MusicPlayer {    constructor() {
        this.audio = document.getElementById('music-audio');
        this.player = document.getElementById('sticky-music-player');
        this.isPlaying = false;
        this.currentSong = null;
        this.playlist = [];
        this.currentIndex = 0;
        this.isRepeat = false;
        this.isShuffle = false;
        this.volume = 0.7;
        
        // Queue system for playlist functionality
        this.queue = [];
        this.currentTrackIndex = -1;
        this.currentTrack = null;
        
        // Listening history tracking
        this.playStartTime = null;
        this.totalListenTime = 0;
        this.minimumListenDuration = 30; // seconds before counting as a play
        this.currentPlaySession = null;
        
        // Initialize player if elements exist
        if (this.audio && this.player) {
            this.initializePlayer();
            // Restore previous state if any
            this.restorePlayerState();
        }

        this.defaultAlbumArt = '/assets/default-album-art.png'; // Or your actual default image path
    }    initializePlayer() {
        // Get DOM elements
        this.elements = {
            playPauseBtn: document.getElementById('player-play-pause'),
            prevBtn: document.getElementById('player-prev'),
            nextBtn: document.getElementById('player-next'),
            progressBar: document.getElementById('player-progress'),
            volumeBar: document.getElementById('player-volume'),
            volumeBtn: document.getElementById('player-volume-btn'),
            currentTime: document.getElementById('player-current-time'),
            duration: document.getElementById('player-duration'),
            songTitle: document.getElementById('player-song-title'),
            artistName: document.getElementById('player-artist-name'),
            albumArt: document.getElementById('player-album-art'),
            repeatBtn: document.getElementById('player-repeat'),
            shuffleBtn: document.getElementById('player-shuffle'),
            queueBtn: document.getElementById('player-queue'),
            minimizeBtn: document.getElementById('player-minimize')
        };

        // Set up convenience references for clearPlayerUI method
        this.songTitleElement = this.elements.songTitle;
        this.songArtistElement = this.elements.artistName;
        this.albumArtElement = this.elements.albumArt;
        this.currentTimeElement = this.elements.currentTime;
        this.durationElement = this.elements.duration;
        this.seekBarElement = this.elements.progressBar;

        this.bindEvents();
        this.setupAudioEvents();
        this.setVolume(this.volume);
    }

    bindEvents() {
        // Play/Pause button
        this.elements.playPauseBtn?.addEventListener('click', () => {
            this.togglePlayPause();
        });

        // Previous/Next buttons
        this.elements.prevBtn?.addEventListener('click', () => {
            this.previousTrack();
        });

        this.elements.nextBtn?.addEventListener('click', () => {
            this.nextTrack();
        });

        // Progress bar
        this.elements.progressBar?.addEventListener('input', (e) => {
            this.seekTo(e.target.value);
        });

        // Volume controls
        this.elements.volumeBar?.addEventListener('input', (e) => {
            this.setVolume(e.target.value / 100);
        });

        this.elements.volumeBtn?.addEventListener('click', () => {
            this.toggleMute();
        });

        // Repeat and shuffle
        this.elements.repeatBtn?.addEventListener('click', () => {
            this.toggleRepeat();
        });

        this.elements.shuffleBtn?.addEventListener('click', () => {
            this.toggleShuffle();
        });        // Queue button
        this.elements.queueBtn?.addEventListener('click', () => {
            this.showQueue();
        });

        // Minimize player
        this.elements.minimizeBtn?.addEventListener('click', () => {
            this.hidePlayer();
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
            
            switch(e.code) {
                case 'Space':
                    e.preventDefault();
                    this.togglePlayPause();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    this.nextTrack();
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    this.previousTrack();
                    break;
            }
        });
    }    setupAudioEvents() {
        // Time update
        this.audio.addEventListener('timeupdate', () => {
            this.updateProgress();
            // Save state periodically during playback
            if (this.isPlaying && this.currentSong) {
                this.savePlayerState();
                
                // Update listening session every 10 seconds during playback
                if (this.currentPlaySession && this.playStartTime && 
                    (Date.now() - this.playStartTime) > 10000) {
                    this.updateListeningSession();
                }
            }
        });

        // Metadata loaded
        this.audio.addEventListener('loadedmetadata', () => {
            this.updateDuration();
        });

        // Track ended
        this.audio.addEventListener('ended', () => {
            this.handleTrackEnd();
        });

        // Play/pause events
        this.audio.addEventListener('play', () => {
            this.isPlaying = true;
            this.updatePlayButton();
            this.savePlayerState();
            
            // Resume listening session
            if (this.currentPlaySession && !this.playStartTime) {
                this.playStartTime = Date.now();
            }
        });

        this.audio.addEventListener('pause', () => {
            this.isPlaying = false;
            this.updatePlayButton();
            this.savePlayerState();
            
            // Update listening session when paused
            this.updateListeningSession();
        });

        // Error handling
        this.audio.addEventListener('error', (e) => {
            console.error('Audio error:', e);
            this.showError('Error playing audio');
        });

        // Can play
        this.audio.addEventListener('canplay', () => {
            this.elements.playPauseBtn?.classList.remove('loading');
        });

        // Loading
        this.audio.addEventListener('loadstart', () => {
            this.elements.playPauseBtn?.classList.add('loading');
        });
    }

    // Public methods for controlling the player
    playSong(song, playlist = null, startIndex = 0) {
        // End current listening session before starting new song
        this.endListeningSession();
        
        this.currentSong = song;
        
        if (playlist) {
            this.playlist = playlist;
            this.currentIndex = startIndex;
        } else {
            this.playlist = [song];
            this.currentIndex = 0;
        }
        
        this.loadCurrentSong();
        this.showPlayer();
        this.play();
        
        // Save state when a new song is loaded
        this.savePlayerState();
    }loadCurrentSong() {
        if (!this.currentSong) return;

        // Update audio source
        this.audio.src = this.currentSong.audioUrl || this.currentSong.AudioUrl || '';
        
        // Update UI
        this.updateSongInfo();
        
        // Reset progress
        this.elements.progressBar.value = 0;
        this.elements.progressBar.style.setProperty('--progress', '0%');
        this.elements.currentTime.textContent = '0:00';
    }

    updateSongInfo() {
        if (!this.currentSong) return;

        const title = this.currentSong.title || this.currentSong.Title || 'Unknown Title';
        const artist = this.currentSong.artistName || this.currentSong.ArtistName || 'Unknown Artist';
        const albumArt = this.currentSong.coverImageUrl || this.currentSong.CoverImageUrl || 
                        'https://placehold.co/300x300/212121/AAAAAA?text=Song';

        this.elements.songTitle.textContent = title;
        this.elements.artistName.textContent = artist;
        this.elements.albumArt.src = albumArt;
        this.elements.albumArt.alt = `${title} cover`;

        // Update page title
        document.title = `${title} - ${artist} - Music App`;
    }

    play() {
        if (this.audio.src) {
            this.audio.play().then(() => {
                this.isPlaying = true;
                this.updatePlayButton();
                
                // Start tracking listen time
                this.startListeningSession();
            }).catch(e => {
                console.error('Play failed:', e);
                this.showError('Failed to play audio');
            });
        }
    }

    pause() {
        this.audio.pause();
        this.isPlaying = false;
        this.updatePlayButton();
        
        // Track listen time when pausing
        this.updateListeningSession();
    }

    togglePlayPause() {
        if (this.isPlaying) {
            this.pause();
        } else {
            this.play();
        }
    }    nextTrack() {
        if (this.playlist.length <= 1) return;

        // End current listening session
        this.endListeningSession();

        if (this.isShuffle) {
            this.currentIndex = Math.floor(Math.random() * this.playlist.length);
        } else {
            this.currentIndex = (this.currentIndex + 1) % this.playlist.length;
        }

        this.currentSong = this.playlist[this.currentIndex];
        this.loadCurrentSong();
        
        if (this.isPlaying) {
            this.play();
        }
        
        // Save state after track change
        this.savePlayerState();
    }

    previousTrack() {
        if (this.playlist.length <= 1) return;

        if (this.audio.currentTime > 3) {
            // If more than 3 seconds played, restart current track
            this.audio.currentTime = 0;
            this.savePlayerState();
            return;
        }

        // End current listening session
        this.endListeningSession();

        this.currentIndex = this.currentIndex === 0 ? 
            this.playlist.length - 1 : this.currentIndex - 1;

        this.currentSong = this.playlist[this.currentIndex];
        this.loadCurrentSong();
        
        if (this.isPlaying) {
            this.play();
        }
        
        // Save state after track change
        this.savePlayerState();
    }seekTo(percentage) {
        if (this.audio.duration) {
            this.audio.currentTime = (percentage / 100) * this.audio.duration;
            // Update visual progress immediately
            this.elements.progressBar.style.setProperty('--progress', `${percentage}%`);
        }
    }    setVolume(volume) {
        this.volume = Math.max(0, Math.min(1, volume));
        this.audio.volume = this.volume;
        
        if (this.elements.volumeBar) {
            this.elements.volumeBar.value = this.volume * 100;
        }
        
        this.updateVolumeIcon();
        this.savePlayerState();
    }

    toggleMute() {
        if (this.audio.volume > 0) {
            this.audio.volume = 0;
        } else {
            this.audio.volume = this.volume;
        }
        this.updateVolumeIcon();
        this.savePlayerState();
    }

    toggleRepeat() {
        this.isRepeat = !this.isRepeat;
        this.elements.repeatBtn?.classList.toggle('active', this.isRepeat);
        this.savePlayerState();
    }

    toggleShuffle() {
        this.isShuffle = !this.isShuffle;
        this.elements.shuffleBtn?.classList.toggle('active', this.isShuffle);
        this.savePlayerState();
    }

    // UI update methods
    updatePlayButton() {
        const icon = this.elements.playPauseBtn?.querySelector('i');
        if (icon) {
            icon.className = this.isPlaying ? 'bi bi-pause-fill' : 'bi bi-play-fill';
        }
    }    updateProgress() {
        if (this.audio.duration && this.elements.progressBar) {
            const percentage = (this.audio.currentTime / this.audio.duration) * 100;
            this.elements.progressBar.value = percentage;
            
            // Update CSS custom property for progress fill
            this.elements.progressBar.style.setProperty('--progress', `${percentage}%`);
            
            this.elements.currentTime.textContent = this.formatTime(this.audio.currentTime);
        }
    }

    updateDuration() {
        if (this.audio.duration && this.elements.duration) {
            this.elements.duration.textContent = this.formatTime(this.audio.duration);
        }
    }

    updateVolumeIcon() {
        const icon = this.elements.volumeBtn?.querySelector('i');
        if (!icon) return;

        if (this.audio.volume === 0) {
            icon.className = 'bi bi-volume-mute-fill';
        } else if (this.audio.volume < 0.5) {
            icon.className = 'bi bi-volume-down-fill';
        } else {
            icon.className = 'bi bi-volume-up-fill';
        }
    }

    handleTrackEnd() {
        // End current listening session when track ends
        this.endListeningSession();
        
        if (this.isRepeat) {
            this.audio.currentTime = 0;
            this.play();
        } else if (this.playlist.length > 1) {
            this.nextTrack();
        } else {
            this.isPlaying = false;
            this.updatePlayButton();
        }
    }

    showPlayer() {
        this.player?.classList.remove('d-none');
        // Add bottom padding to content to prevent overlap
        document.body.style.paddingBottom = '80px';
    }    hidePlayer() {
        this.pause();
        this.player?.classList.add('d-none');
        document.body.style.paddingBottom = '';
        
        // Reset page title
        document.title = document.title.replace(/^.+ - .+ - /, '');
        
        // End listening session when hiding player
        this.endListeningSession();
        
        // Clear saved state when player is hidden
        this.clearPlayerState();
    }

    showQueue() {
        // Remove any existing queue toast
        const existingToast = document.getElementById('queue-toast');
        if (existingToast) {
            existingToast.remove();
            return; // Toggle behavior - close if already open
        }

        if (!this.playlist || this.playlist.length === 0) {
            this.showNotification('No songs in queue');
            return;
        }

        // Create the queue toast container
        const queueToast = document.createElement('div');
        queueToast.id = 'queue-toast';
        queueToast.className = 'queue-toast';        // Create header
        const header = document.createElement('div');
        header.className = 'queue-header';
        header.innerHTML = `
            <h5>Current Queue</h5>
            <button class="queue-close-btn" onclick="document.getElementById('queue-toast').remove()">
                <i class="bi bi-x-lg"></i>
            </button>
        `;

        // Create scrollable content area
        const content = document.createElement('div');
        content.className = 'queue-content';        // Generate playlist items
        this.playlist.forEach((song, index) => {
            const songItem = document.createElement('div');
            songItem.className = `queue-item ${index === this.currentIndex ? 'current' : ''}`;
            songItem.dataset.index = index;            // Get the correct image URL with proper fallbacks
            const imageUrl = song.coverImageUrl || song.CoverImageUrl || 'https://placehold.co/40x40/212121/AAAAAA?text=Song';
            const title = song.title || song.Title || 'Unknown Title';
            const artist = song.artistName || song.ArtistName || 'Unknown Artist';

            songItem.innerHTML = `                <div class="queue-item-artwork">
                    <img src="${imageUrl}" alt="${title}" onerror="this.src='https://placehold.co/40x40/212121/AAAAAA?text=Song'">
                    ${index === this.currentIndex ? '<i class="bi bi-volume-up-fill current-indicator"></i>' : ''}
                </div>
                <div class="queue-item-info">
                    <div class="queue-item-title">${title}</div>
                    <div class="queue-item-artist">${artist}</div>
                </div>
                <div class="queue-item-duration">${this.formatTime(song.duration || 0)}</div>
            `;

            // Add click handler to play specific song while keeping the playlist intact
            songItem.addEventListener('click', () => {
                if (index !== this.currentIndex) {
                    this.currentIndex = index;
                    this.currentSong = this.playlist[index];
                    this.loadCurrentSong();
                    if (this.isPlaying) {
                        this.play();
                    }
                }
                queueToast.remove();
            });

            content.appendChild(songItem);
        });

        // Assemble the toast
        queueToast.appendChild(header);
        queueToast.appendChild(content);
        document.body.appendChild(queueToast);

        // Auto-close after 10 seconds of inactivity
        setTimeout(() => {
            if (document.getElementById('queue-toast')) {
                queueToast.remove();
            }
        }, 10000);
    }

    showNotification(message) {
        // Simple notification method
        const notification = document.createElement('div');
        notification.className = 'music-notification';
        notification.textContent = message;
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // State persistence methods for continuous playback
    savePlayerState() {
        if (!this.currentSong) return;
        
        const state = {
            currentSong: this.currentSong,
            playlist: this.playlist,
            currentIndex: this.currentIndex,
            currentTime: this.audio.currentTime || 0,
            isPlaying: this.isPlaying,
            volume: this.volume,
            isRepeat: this.isRepeat,
            isShuffle: this.isShuffle,
            timestamp: Date.now()
        };
        
        try {
            localStorage.setItem('musicPlayerState', JSON.stringify(state));
        } catch (e) {
            console.warn('Failed to save player state:', e);
        }
    }

    restorePlayerState() {
        try {
            const savedState = localStorage.getItem('musicPlayerState');
            if (!savedState) return;
            
            const state = JSON.parse(savedState);
            
            // Check if state is not too old (max 24 hours)
            const maxAge = 24 * 60 * 60 * 1000; // 24 hours in milliseconds
            if (Date.now() - state.timestamp > maxAge) {
                localStorage.removeItem('musicPlayerState');
                return;
            }
            
            // Restore state
            this.currentSong = state.currentSong;
            this.playlist = state.playlist || [];
            this.currentIndex = state.currentIndex || 0;
            this.volume = state.volume || 0.7;
            this.isRepeat = state.isRepeat || false;
            this.isShuffle = state.isShuffle || false;
            
            if (this.currentSong) {
                this.loadCurrentSong();
                this.showPlayer();
                
                // Set the saved time position
                if (state.currentTime > 0) {
                    this.audio.addEventListener('loadedmetadata', () => {
                        this.audio.currentTime = state.currentTime;
                        this.updateProgress();
                    }, { once: true });
                }
                
                // Restore volume and UI states
                this.setVolume(this.volume);
                this.elements.repeatBtn?.classList.toggle('active', this.isRepeat);
                this.elements.shuffleBtn?.classList.toggle('active', this.isShuffle);
                
                // If was playing, resume playback after a short delay
                if (state.isPlaying) {
                    // Show a brief visual indicator that playback was restored
                    this.showRestoreIndicator();
                    
                    // Add a small delay to ensure audio is ready
                    setTimeout(() => {
                        this.play();
                    }, 500);
                }
            }
        } catch (e) {
            console.warn('Failed to restore player state:', e);
            localStorage.removeItem('musicPlayerState');
        }
    }

    showRestoreIndicator() {
        // Create a temporary indicator to show that playback was restored
        const indicator = document.createElement('div');
        indicator.textContent = '🎵 Playback restored';
        indicator.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: var(--bs-success);
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            font-size: 14px;
            z-index: 9999;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
            transition: all 0.3s ease;
            transform: translateX(100%);
        `;
        
        document.body.appendChild(indicator);
        
        // Animate in
        requestAnimationFrame(() => {
            indicator.style.transform = 'translateX(0)';
        });
        
        // Remove after 3 seconds
        setTimeout(() => {
            indicator.style.transform = 'translateX(100%)';
            setTimeout(() => {
                document.body.removeChild(indicator);
            }, 300);
        }, 3000);
    }

    clearPlayerState() {
        try {
            localStorage.removeItem('musicPlayerState');
        } catch (e) {
            console.warn('Failed to clear player state:', e);
        }
    }

    // Utility methods
    formatTime(seconds) {
        if (isNaN(seconds)) return '0:00';
        
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
    }

    showError(message) {
        // You can implement a toast notification system here
        console.error('Music Player Error:', message);
    }

    // Get current song info
    getCurrentSong() {
        return this.currentSong;
    }

    isPlayerVisible() {
        return !this.player?.classList.contains('d-none');
    }

    // Listening History Tracking Methods
    startListeningSession() {
        if (!this.currentSong) return;
        
        this.playStartTime = Date.now();
        this.totalListenTime = 0;
        
        // Create a new play session
        this.currentPlaySession = {
            songId: this.currentSong.id || this.currentSong.Id,
            startTime: this.playStartTime,
            totalListenTime: 0,
            hasBeenLogged: false
        };
        
        console.log('Started listening session for:', this.currentSong.title || this.currentSong.Title);
    }
    
    updateListeningSession() {
        if (!this.currentPlaySession || !this.playStartTime) return;
        
        const now = Date.now();
        const sessionTime = Math.floor((now - this.playStartTime) / 1000);
        this.currentPlaySession.totalListenTime += sessionTime;
        
        // Reset start time for next segment
        this.playStartTime = this.isPlaying ? now : null;
        
        // Log play if minimum duration reached
        if (!this.currentPlaySession.hasBeenLogged && 
            this.currentPlaySession.totalListenTime >= this.minimumListenDuration) {
            this.logSongPlay();
        }
    }
    
    endListeningSession() {
        if (!this.currentPlaySession) return;
        
        // Update one final time
        this.updateListeningSession();
        
        // Log the play if it hasn't been logged yet and meets minimum duration
        if (!this.currentPlaySession.hasBeenLogged && 
            this.currentPlaySession.totalListenTime >= this.minimumListenDuration) {
            this.logSongPlay();
        }
        
        console.log(`Ended listening session. Total time: ${this.currentPlaySession.totalListenTime}s`);
        
        // Clear session
        this.currentPlaySession = null;
        this.playStartTime = null;
    }
    
    async logSongPlay() {
        if (!this.currentPlaySession || this.currentPlaySession.hasBeenLogged) return;
        
        try {
            const response = await fetch(`/api/Songs/${this.currentPlaySession.songId}/play`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    duration: this.formatDurationForAPI(this.currentPlaySession.totalListenTime)
                })
            });
            
            if (response.ok) {
                const result = await response.json();
                this.currentPlaySession.hasBeenLogged = true;
                console.log(`Song play logged successfully. Total plays: ${result.playCount}`);
                
                // Update play count in UI if available
                this.updatePlayCountDisplay(result.playCount);
            } else {
                console.warn('Failed to log song play:', response.statusText);
            }
        } catch (error) {
            console.error('Error logging song play:', error);
        }
    }
    
    formatDurationForAPI(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    
    updatePlayCountDisplay(playCount) {
        // Update play count in any visible elements
        const playCountElements = document.querySelectorAll(`[data-song-id="${this.currentPlaySession?.songId}"] .play-count`);
        playCountElements.forEach(element => {
            element.textContent = `${playCount} plays`;
        });
    }

    // New methods for playlist functionality
    playPlaylist(songsArray, playNow = true) {
        if (!songsArray || songsArray.length === 0) {
            console.warn("playPlaylist called with empty or invalid songsArray");
            this.queue = [];
            this.currentTrackIndex = -1;
            this.clearPlayerUI();
            if (this.showNotification) this.showNotification("Playlist is empty or could not be loaded.", "info");
            return;
        }

        this.queue = songsArray.map(song => ({
            id: song.id,
            title: song.title,
            artistName: song.artistName,
            albumName: song.albumName,
            coverImageUrl: song.coverImageUrl,
            audioUrl: song.audioUrl
        }));

        this.currentTrackIndex = 0;        if (playNow && this.queue.length > 0) {
            // Use playSong method instead of playTrack
            this.playSong(this.queue[this.currentTrackIndex], this.queue, 0);        } else if (this.queue.length > 0) {
            const firstTrack = this.queue[this.currentTrackIndex];
            if (this.songTitleElement) this.songTitleElement.textContent = firstTrack.title || 'Unknown Song';
            if (this.songArtistElement) this.songArtistElement.textContent = firstTrack.artistName || 'Unknown Artist';
            if (this.albumArtElement) this.albumArtElement.src = firstTrack.coverImageUrl || this.defaultAlbumArt;
            if (this.audio) this.audio.src = firstTrack.audioUrl; // Load the audio src but don't play
            if (this.durationElement) this.durationElement.textContent = '0:00'; // Reset duration until metadata loads
            if (this.currentTimeElement) this.currentTimeElement.textContent = '0:00';
            if (this.seekBarElement) this.seekBarElement.value = 0;
        } else {
            this.clearPlayerUI();
        }
        this.updateQueueDisplay();
        this.updatePlayPauseButton();
        if (this.showNotification) this.showNotification("Playlist loaded.", "success");
    }    clearPlayerUI() {
        if (this.songTitleElement) this.songTitleElement.textContent = 'Music Player'; // Default text
        if (this.songArtistElement) this.songArtistElement.textContent = 'Select a song to play';
        if (this.albumArtElement) this.albumArtElement.src = this.defaultAlbumArt;
        if (this.audio) {
            this.audio.src = '';
            this.audio.pause();
        }
        if (this.currentTimeElement) this.currentTimeElement.textContent = '0:00';
        if (this.durationElement) this.durationElement.textContent = '0:00';
        if (this.seekBarElement) this.seekBarElement.value = 0;        this.updatePlayPauseButton(); // Ensure play button shows "play" icon
        this.currentTrack = null; // Clear current track
    }

    updatePlayPauseButton() {
        // Alias for updatePlayButton to maintain compatibility
        this.updatePlayButton();
    }

    updateQueueDisplay() {
        // Placeholder: Implement if you have a UI for the queue
        console.log("Queue updated. Current queue:", this.queue);
        // Example: if you have a queue list element:
        // const queueListElement = document.getElementById('player-queue-list');
        // if (queueListElement) {
        //     queueListElement.innerHTML = ''; // Clear existing
        //     this.queue.forEach((track, index) => {
        //         const item = document.createElement('li');
        //         item.textContent = `${index + 1}. ${track.title} - ${track.artistName}`;
        //         if (index === this.currentTrackIndex) {
        //             item.classList.add('active');
        //         }
        //         item.onclick = () => {
        //             this.currentTrackIndex = index;
        //             this.playTrack(this.queue[this.currentTrackIndex]);
        //         };
        //         queueListElement.appendChild(item);
        //     });
        // }
    }
    // End of new methods

    // Ensure showNotification is available or add it if it's part of this player
    // For example, if it's a method of this class:
    // showNotification(message, type = 'info') {
    //     // Basic console log, replace with actual UI notification
    //     console.log(`[${type.toUpperCase()}] ${message}`);
    //     // If you have a global notification function, you might not need it here.
    // }    // Make sure the constructor initializes this.currentTrack = null;
    // ... existing code like updatePlayPauseButton, setVolume etc.
    // Ensure playNextInQueue and playPreviousInQueue correctly use this.currentTrackIndex and this.queue
    
    playNextInQueue() {
        if (this.queue.length > 0) {
            this.currentTrackIndex = (this.currentTrackIndex + 1) % this.queue.length;
            this.playSong(this.queue[this.currentTrackIndex], this.queue, this.currentTrackIndex);
        }
    }

    playPreviousInQueue() {
        if (this.queue.length > 0) {
            this.currentTrackIndex = (this.currentTrackIndex - 1 + this.queue.length) % this.queue.length;
            this.playSong(this.queue[this.currentTrackIndex], this.queue, this.currentTrackIndex);
        }
    }
}

document.addEventListener('DOMContentLoaded', () => {
    // Initialize global music player instance
    window.musicPlayer = new MusicPlayer();
    
    // Save state before page unloads (for manual navigation)
    window.addEventListener('beforeunload', function() {
        if (window.musicPlayer && window.musicPlayer.currentSong) {
            window.musicPlayer.savePlayerState();
        }
    });
    
    // Save state when page becomes hidden (for tab switching, etc.)
    document.addEventListener('visibilitychange', function() {
        if (document.hidden && window.musicPlayer && window.musicPlayer.currentSong) {
            window.musicPlayer.savePlayerState();
        }
    });
    
    // Handle browser back/forward navigation
    window.addEventListener('pageshow', function(event) {
        // If page is loaded from cache (back/forward), restore state
        if (event.persisted && window.musicPlayer) {
            window.musicPlayer.restorePlayerState();
        }
    });
});

// Helper function to play a song from anywhere in the app
window.playSong = function(song, playlist = null, startIndex = 0) {
    if (window.musicPlayer) {
        window.musicPlayer.playSong(song, playlist, startIndex);
    }
};

// Helper function to add click handlers to play buttons throughout the app
document.addEventListener('DOMContentLoaded', function() {
    // Add event listeners to existing play buttons
    document.addEventListener('click', function(e) {
        const playBtn = e.target.closest('[data-play-song]');
        const playAlbumBtn = e.target.closest('[data-play-album]');
        
        if (playBtn) {
            e.preventDefault();
            
            try {
                const songAttr = playBtn.getAttribute('data-play-song');
                let songData;
                
                // Check if it's JSON data or just an ID
                if (songAttr.startsWith('{') || songAttr.startsWith('[')) {
                    // It's JSON data
                    songData = JSON.parse(songAttr);
                } else {
                    // It's just an ID, construct song object from data attributes
                    songData = {
                        id: parseInt(songAttr),
                        title: playBtn.getAttribute('data-song-title') || 'Unknown Title',
                        artistName: playBtn.getAttribute('data-artist-name') || 'Unknown Artist',
                        audioUrl: playBtn.getAttribute('data-audio-url') || '',
                        coverImageUrl: playBtn.getAttribute('data-cover-image-url') || 'https://placehold.co/300x300/212121/AAAAAA?text=Song'
                    };
                }
                
                const playlistData = playBtn.getAttribute('data-playlist');
                const startIndex = parseInt(playBtn.getAttribute('data-index')) || 0;
                
                let playlist = null;
                if (playlistData) {
                    playlist = JSON.parse(playlistData);
                }
                
                window.playSong(songData, playlist, startIndex);
            } catch (error) {
                console.error('Error parsing song data:', error);
                console.log('Song attribute:', playBtn.getAttribute('data-play-song'));
            }
        }
        
        if (playAlbumBtn) {
            e.preventDefault();
            
            try {
                const albumData = JSON.parse(playAlbumBtn.getAttribute('data-play-album'));
                
                if (albumData && albumData.length > 0) {
                    // Play the first song from the album with the full album as playlist
                    window.playSong(albumData[0], albumData, 0);
                }
            } catch (error) {
                console.error('Error parsing album data:', error);
            }
        }
    });
});

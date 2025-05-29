// Music Player JavaScript - Advanced Sticky Player Implementation

class MusicPlayer {
    constructor() {
        this.audio = document.getElementById('music-audio');
        this.player = document.getElementById('sticky-music-player');
        this.isPlaying = false;
        this.currentSong = null;
        this.playlist = [];
        this.currentIndex = 0;
        this.isRepeat = false;
        this.isShuffle = false;
        this.volume = 0.7;
        
        // Initialize player if elements exist
        if (this.audio && this.player) {
            this.initializePlayer();
        }
    }

    initializePlayer() {
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
    }

    setupAudioEvents() {
        // Time update
        this.audio.addEventListener('timeupdate', () => {
            this.updateProgress();
        });

        // Metadata loaded
        this.audio.addEventListener('loadedmetadata', () => {
            this.updateDuration();
        });

        // Track ended
        this.audio.addEventListener('ended', () => {
            this.handleTrackEnd();
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
    }    loadCurrentSong() {
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
    }

    togglePlayPause() {
        if (this.isPlaying) {
            this.pause();
        } else {
            this.play();
        }
    }

    nextTrack() {
        if (this.playlist.length <= 1) return;

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
    }

    previousTrack() {
        if (this.playlist.length <= 1) return;

        if (this.audio.currentTime > 3) {
            // If more than 3 seconds played, restart current track
            this.audio.currentTime = 0;
            return;
        }

        this.currentIndex = this.currentIndex === 0 ? 
            this.playlist.length - 1 : this.currentIndex - 1;

        this.currentSong = this.playlist[this.currentIndex];
        this.loadCurrentSong();
        
        if (this.isPlaying) {
            this.play();
        }
    }    seekTo(percentage) {
        if (this.audio.duration) {
            this.audio.currentTime = (percentage / 100) * this.audio.duration;
            // Update visual progress immediately
            this.elements.progressBar.style.setProperty('--progress', `${percentage}%`);
        }
    }

    setVolume(volume) {
        this.volume = Math.max(0, Math.min(1, volume));
        this.audio.volume = this.volume;
        
        if (this.elements.volumeBar) {
            this.elements.volumeBar.value = this.volume * 100;
        }
        
        this.updateVolumeIcon();
    }

    toggleMute() {
        if (this.audio.volume > 0) {
            this.audio.volume = 0;
        } else {
            this.audio.volume = this.volume;
        }
        this.updateVolumeIcon();
    }

    toggleRepeat() {
        this.isRepeat = !this.isRepeat;
        this.elements.repeatBtn?.classList.toggle('active', this.isRepeat);
    }

    toggleShuffle() {
        this.isShuffle = !this.isShuffle;
        this.elements.shuffleBtn?.classList.toggle('active', this.isShuffle);
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
    }

    hidePlayer() {
        this.pause();
        this.player?.classList.add('d-none');
        document.body.style.paddingBottom = '';
        
        // Reset page title
        document.title = document.title.replace(/^.+ - .+ - /, '');
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
}

// Global music player instance
let musicPlayer;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    musicPlayer = new MusicPlayer();
    
    // Make it globally accessible
    window.musicPlayer = musicPlayer;
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

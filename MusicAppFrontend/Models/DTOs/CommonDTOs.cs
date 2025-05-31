using System;
using System.Collections.Generic;

namespace MusicApp.Models.DTOs
{
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
    
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
      public class ArtistDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ImageUrl { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool IsActive { get; set; } // Added for admin edit
        public bool IsPrimaryArtist { get; set; } = false; // Added for multiple artist support
        public List<AlbumDto> Albums { get; set; } = new List<AlbumDto>();
        public List<SongDto> Songs { get; set; } = new List<SongDto>();
        public int FollowersCount { get; set; }
    }
    
    public class ArtistDetailDto : ArtistDto
    {
        public new List<AlbumDto> Albums { get; set; } = new List<AlbumDto>();
    }    public class AlbumDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int ArtistId { get; set; }
        public string? ArtistName { get; set; } // Made nullable
        public string? CoverImageUrl { get; set; }
        public int? Year { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TotalTracks { get; set; }
        public string? Description { get; set; } // Added
        public TimeSpan? Duration { get; set; } // Added
        public bool IsLiked { get; set; } // Added to store like status
        public List<SongDto> Songs { get; set; } = new List<SongDto>();
        // Additional properties for compatibility
        public int TrackCount => TotalTracks ?? 0;
        public double TotalDuration => Duration?.TotalMinutes ?? 0.0;
    }
      public class AlbumUpdateDTO
    {
        public string? Title { get; set; }
        public int? ArtistId { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? TotalTracks { get; set; }
        public TimeSpan? Duration { get; set; }
    }public class SongDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? ArtistId { get; set; } // Keep for backward compatibility
        public string? ArtistName { get; set; } // Keep for backward compatibility
        public List<ArtistDto>? Artists { get; set; } = new List<ArtistDto>(); // New field for multiple artists
        public int? AlbumId { get; set; }
        public string? AlbumTitle { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? AudioUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public int? TrackNumber { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int PlayCount { get; set; }
        public bool IsFavorited { get; set; } = false;
    }    public class SongUpdateDTO
    {
        public string? Title { get; set; }
        public int? ArtistId { get; set; } // Keep for backward compatibility
        public List<int>? ArtistIds { get; set; } // New field for multiple artists
        public int? PrimaryArtistId { get; set; } // To designate primary artist
        public int? AlbumId { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? AudioUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public int? TrackNumber { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int? PlayCount { get; set; }
    }

    public class SongCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public int? ArtistId { get; set; } // Keep for backward compatibility
        public List<int>? ArtistIds { get; set; } // New field for multiple artists
        public int? PrimaryArtistId { get; set; } // To designate primary artist
        public int? AlbumId { get; set; }
        public TimeSpan Duration { get; set; }
        public int? TrackNumber { get; set; }
        public string? Genre { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
      public class PlaylistDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int SongCount { get; set; }
        public bool IsPublic { get; set; }
        public List<SongDto> Songs { get; set; } = new List<SongDto>();
    }
    
    public class PlaylistDetailDto : PlaylistDto
    {
        public new List<PlaylistSongDto> Songs { get; set; } = new List<PlaylistSongDto>();
    }      public class PlaylistSongDto
    {
        public int SongId { get; set; }
        public string? Title { get; set; }
        public string? ArtistName { get; set; }
        public string? AlbumTitle { get; set; }
        public TimeSpan? Duration { get; set; }
        public string DurationString => Duration.HasValue ? $"{(int)Duration.Value.TotalMinutes}:{Duration.Value.Seconds:D2}" : "--:--";
        public string? CoverImageUrl { get; set; }
        public string? AudioUrl { get; set; }
        public int Order { get; set; }
        public DateTime AddedAt { get; set; }
    }
    
    public class SearchResultsDto
    {
        public List<SongDto> Songs { get; set; } = new List<SongDto>();
        public List<ArtistDto> Artists { get; set; } = new List<ArtistDto>();
        public List<AlbumDto> Albums { get; set; } = new List<AlbumDto>();
        public List<PlaylistDto> Playlists { get; set; } = new List<PlaylistDto>();
    }
      public class UserDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PlaylistsCount { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string? Bio { get; set; } // <-- Add this line
    }
    
    public class SongListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ArtistName { get; set; }
        public string AlbumTitle { get; set; }
        public int Duration { get; set; }
        public string CoverImageUrl { get; set; }
    }
      public class ArtistCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ArtistUpdateDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? Country { get; set; }
        public string? Genre { get; set; }
        public DateTime? FormedDate { get; set; }
        public int? MonthlyListeners { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsActive { get; set; }
    }
}
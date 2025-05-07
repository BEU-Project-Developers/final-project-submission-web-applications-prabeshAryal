# Music App Data Structure Overview

## Entity Relationships Diagram

```
User ───┬─── Playlist (1:N)
        │
        ├─── UserRole ─── Role (N:M)
        │
        ├─── UserFollower (self-referencing N:M)
        │
        └─── UserFavorite ─── Song (N:M)
            
Artist ─── Album (1:N) ─── Song (1:N)
                              │
                              └─── PlaylistSong ─── Playlist (N:M)
```

## Detailed Model Structure

### User Model
```csharp
public class User
{
    // Primary Key
    public int Id { get; set; }
    
    // Account Information
    public string Username { get; set; } // Required, max 50 chars
    public string Email { get; set; }    // Required, max 100 chars
    public string FirstName { get; set; } // Max 50 chars
    public string LastName { get; set; }  // Max 50 chars
    public string PasswordHash { get; set; } // Required, BCrypt hashed
    public string? ProfileImageUrl { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } // Defaults to UTC now
    public DateTime UpdatedAt { get; set; } // Updated when user is modified
    public DateTime LastLoginAt { get; set; } // Updated on login
    
    // Navigation Collections
    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ICollection<Playlist> Playlists { get; set; }
    public virtual ICollection<UserFollower> Followers { get; set; }
    public virtual ICollection<UserFollower> Following { get; set; }
    public virtual ICollection<UserFavorite> Favorites { get; set; }
}
```

### Role Model
```csharp
public class Role
{
    // Primary Key
    public int Id { get; set; }
    
    // Role Info
    public string Name { get; set; } // Required, max 50 chars
    public string? Description { get; set; } // Max 255 chars
    
    // Navigation Collection
    public virtual ICollection<UserRole> UserRoles { get; set; }
}
```

### Artist Model
```csharp
public class Artist
{
    // Primary Key
    public int Id { get; set; }
    
    // Artist Information
    public string Name { get; set; } // Required, max 100 chars
    public string? Bio { get; set; } // Max 1000 chars
    public string? ImageUrl { get; set; }
    public string? Country { get; set; } // Max 100 chars
    public string? Genre { get; set; } // Max 50 chars
    public DateTime? FormedDate { get; set; }
    public int? MonthlyListeners { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } // Defaults to UTC now
    public DateTime UpdatedAt { get; set; } // Updated when artist is modified
    
    // Navigation Collection
    public virtual ICollection<Album> Albums { get; set; }
}
```

### Album Model
```csharp
public class Album
{
    // Primary Key
    public int Id { get; set; }
    
    // Album Information
    public string Title { get; set; } // Required, max 100 chars
    public int ArtistId { get; set; } // Foreign key to Artist
    public string? CoverImageUrl { get; set; }
    public int? Year { get; set; }
    public string? Description { get; set; } // Max 1000 chars
    public string? Genre { get; set; } // Max 50 chars
    public DateTime? ReleaseDate { get; set; }
    public int? TotalTracks { get; set; }
    public TimeSpan? Duration { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } // Defaults to UTC now
    public DateTime UpdatedAt { get; set; } // Updated when album is modified
    
    // Navigation Properties
    public virtual Artist Artist { get; set; } // Parent Artist
    public virtual ICollection<Song> Songs { get; set; } // Child Songs
}
```

### Song Model
```csharp
public class Song
{
    // Primary Key
    public int Id { get; set; }
    
    // Song Information
    public string Title { get; set; } // Required, max 100 chars
    public int? ArtistId { get; set; } // Foreign key to Artist
    public int? AlbumId { get; set; } // Foreign key to Album
    public TimeSpan Duration { get; set; }
    public string? AudioUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int? TrackNumber { get; set; }
    public string? Genre { get; set; } // Max 50 chars
    public DateTime? ReleaseDate { get; set; }
    public int PlayCount { get; set; } = 0;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } // Defaults to UTC now
    public DateTime UpdatedAt { get; set; } // Updated when song is modified
    
    // Navigation Properties
    public virtual Artist? Artist { get; set; }
    public virtual Album? Album { get; set; }
    
    // Navigation Collections
    public virtual ICollection<Playlist> Playlists { get; set; }
    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; }
    public virtual ICollection<UserFavorite> UserFavorites { get; set; }
}
```

### Playlist Model
```csharp
public class Playlist
{
    // Primary Key
    public int Id { get; set; }
    
    // Playlist Information
    public string Name { get; set; } // Required, max 100 chars
    public string? Description { get; set; } // Max 500 chars
    public int UserId { get; set; } // Foreign key to User
    public string? CoverImageUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    
    // Timestamps
    public DateTime CreatedAt { get; set; } // Defaults to UTC now
    public DateTime UpdatedAt { get; set; } // Updated when playlist is modified
    
    // Navigation Properties
    public virtual User User { get; set; } // Parent User
    
    // Navigation Collections
    public virtual ICollection<Song> Songs { get; set; }
    public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; }
}
```

### Junction Tables

#### UserRole
```csharp
public class UserRole
{
    // Composite Primary Key
    public int UserId { get; set; }
    public int RoleId { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; }
    public virtual Role Role { get; set; }
}
```

#### UserFollower
```csharp
public class UserFollower
{
    // Composite Primary Key
    public int UserId { get; set; } // The user being followed
    public int FollowerId { get; set; } // The user who is following
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual User User { get; set; } // The followed user
    public virtual User Follower { get; set; } // The follower
}
```

#### UserFavorite
```csharp
public class UserFavorite
{
    // Composite Primary Key
    public int UserId { get; set; }
    public int SongId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual User User { get; set; }
    public virtual Song Song { get; set; }
}
```

#### PlaylistSong
```csharp
public class PlaylistSong
{
    // Composite Primary Key
    public int PlaylistId { get; set; }
    public int SongId { get; set; }
    public int Order { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Playlist Playlist { get; set; }
    public virtual Song Song { get; set; }
}
```

#### RefreshToken
```csharp
public class RefreshToken
{
    // Primary Key
    public int Id { get; set; }
    
    // Token Information
    public string Token { get; set; } = string.Empty; // Required
    public int UserId { get; set; } // Foreign key to User
    public DateTime ExpiryDate { get; set; }
    public DateTime IssuedAt { get; set; }
    public bool IsRevoked { get; set; }
    
    // Navigation Property
    public virtual User User { get; set; }
}
```

## Database Configuration

The `MusicDbContext` configures these relationships with the following key features:

1. **Many-to-Many Relationships**:
   - User ↔ Role via UserRole (composite key)
   - User ↔ User via UserFollower (self-referencing with Restrict delete behavior)
   - User ↔ Song via UserFavorite (favorites)
   - Playlist ↔ Song via PlaylistSong (ordered songs in playlists)

2. **One-to-Many Relationships**:
   - Artist → Albums (cascade delete)
   - Album → Songs (SetNull delete behavior - songs survive album deletion)
   - User → Playlists (cascade delete)
   - User → RefreshTokens (cascade delete)

3. **Foreign Key Constraints**:
   - Songs can exist without an Album (nullable AlbumId)
   - Songs can exist without an Artist (nullable ArtistId)
   - All other relationships are required

4. **Indexes**:
   - All foreign keys are indexed for performance
   - Composite keys have clustered indexes

This data structure enables:
- User authentication with roles and token-based security
- Social features (following, favorites)
- Complete music management (artists, albums, songs)
- Playlist creation and management
- Tracking of user activity and engagement

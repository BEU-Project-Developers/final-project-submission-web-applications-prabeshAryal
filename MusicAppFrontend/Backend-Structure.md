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



# JSON Response Examples for Each Endpoint

## Authentication Endpoints

### POST /api/Auth/register
```json
{
  "message": "Registration successful",
  "user": {
    "id": 3,
    "username": "newuser",
    "email": "newuser@example.com",
    "firstName": "New",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["User"]
  }
}
```

### POST /api/Auth/login
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "NwRIJGR5OsJc+g7xmtSfLHxyGwguckmhM4JO4BdSRR4=",
  "user": {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "firstName": "Admin",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["Admin", "User"]
  }
}
```

### POST /api/Auth/refresh-token
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "TIuTfPVEV+QDnvlO1Xul7d4OjDRmJXD5tqz/Qnhr0Ko="
}
```

### POST /api/Auth/revoke-token
```json
{
  "message": "Token revoked successfully"
}
```

## User Endpoints

### GET /api/Users
```json
[
  {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "firstName": "Admin",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["Admin", "User"]
  },
  {
    "id": 2,
    "username": "user",
    "email": "user@example.com",
    "firstName": "Regular",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["User"]
  }
]
```

### GET /api/Users/{id}
```json
{
  "id": 1,
  "username": "admin",
  "email": "admin@example.com",
  "firstName": "Admin",
  "lastName": "User",
  "profileImageUrl": null,
  "roles": ["Admin", "User"]
}
```

### GET /api/Users/profile
```json
{
  "id": 2,
  "username": "user",
  "email": "user@example.com",
  "firstName": "Regular",
  "lastName": "User",
  "profileImageUrl": null,
  "roles": ["User"]
}
```

### PUT /api/Users/{id}
```json
// No content response (204) if successful
```

### POST /api/Users/profile-image
```json
{
  "filePath": "/uploads/profiles/a1b2c3d4-user-profile.jpg"
}
```

### GET /api/Users/{id}/followers
```json
[
  {
    "id": 2,
    "username": "user",
    "email": "user@example.com",
    "firstName": "Regular",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["User"]
  }
]
```

### GET /api/Users/{id}/following
```json
[
  {
    "id": 1,
    "username": "admin",
    "email": "admin@example.com",
    "firstName": "Admin",
    "lastName": "User",
    "profileImageUrl": null,
    "roles": ["Admin", "User"]
  }
]
```

### POST /api/Users/{id}/follow
```json
// No content response (204) if successful
```

### DELETE /api/Users/{id}/unfollow
```json
// No content response (204) if successful
```

## Artist Endpoints

### GET /api/Artists
```json
{
  "totalCount": 3,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "name": "The Beatles",
      "country": "United Kingdom",
      "genre": "Rock",
      "formedDate": "1960-08-01T00:00:00",
      "monthlyListeners": 15000000,
      "isActive": false,
      "imageUrl": "/uploads/artists/the-beatles.jpg"
    },
    {
      "id": 2,
      "name": "Queen",
      "country": "United Kingdom",
      "genre": "Rock",
      "formedDate": "1970-01-01T00:00:00",
      "monthlyListeners": 12000000,
      "isActive": false,
      "imageUrl": "/uploads/artists/queen.jpg"
    },
    {
      "id": 3,
      "name": "Michael Jackson",
      "country": "USA",
      "genre": "Pop",
      "formedDate": null,
      "monthlyListeners": 10000000,
      "isActive": false,
      "imageUrl": "/uploads/artists/michael-jackson.jpg"
    }
  ]
}
```

### GET /api/Artists/{id}
```json
{
  "id": 1,
  "name": "The Beatles",
  "bio": "English rock band formed in Liverpool in 1960...",
  "imageUrl": "/uploads/artists/the-beatles.jpg",
  "country": "United Kingdom",
  "genre": "Rock",
  "formedDate": "1960-08-01T00:00:00",
  "monthlyListeners": 15000000,
  "isActive": false,
  "albums": [
    {
      "id": 1,
      "title": "Abbey Road",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "year": 1969,
      "releaseDate": "1969-09-26T00:00:00"
    },
    {
      "id": 2,
      "title": "Let It Be",
      "coverImageUrl": "/uploads/albums/let-it-be.jpg",
      "year": 1970,
      "releaseDate": "1970-05-08T00:00:00"
    }
  ]
}
```

### POST /api/Artists
```json
{
  "id": 4,
  "name": "Pink Floyd",
  "bio": "English rock band formed in London in 1965...",
  "country": "United Kingdom",
  "genre": "Progressive Rock",
  "formedDate": "1965-01-01T00:00:00",
  "monthlyListeners": 8000000,
  "isActive": false
}
```

### PUT /api/Artists/{id}
```json
// No content response (204) if successful
```

### DELETE /api/Artists/{id}
```json
// No content response (204) if successful
```

### POST /api/Artists/{id}/image
```json
{
  "imageUrl": "/uploads/artists/a1b2c3d4-artist.jpg"
}
```

### GET /api/Artists/genres
```json
[
  "Pop",
  "Rock",
  "Progressive Rock"
]
```

### GET /api/Artists/countries
```json
[
  "United Kingdom",
  "USA"
]
```

## Album Endpoints

### GET /api/Albums
```json
{
  "totalCount": 4,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "title": "Abbey Road",
      "artistId": 1,
      "artistName": "The Beatles",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "year": 1969,
      "genre": "Rock",
      "releaseDate": "1969-09-26T00:00:00",
      "totalTracks": 17,
      "duration": "00:47:23"
    },
    {
      "id": 2,
      "title": "Let It Be",
      "artistId": 1,
      "artistName": "The Beatles",
      "coverImageUrl": "/uploads/albums/let-it-be.jpg",
      "year": 1970,
      "genre": "Rock",
      "releaseDate": "1970-05-08T00:00:00",
      "totalTracks": 12,
      "duration": "00:35:10"
    },
    {
      "id": 3,
      "title": "A Night at the Opera",
      "artistId": 2,
      "artistName": "Queen",
      "coverImageUrl": "/uploads/albums/a-night-at-the-opera.jpg",
      "year": 1975,
      "genre": "Rock",
      "releaseDate": "1975-11-21T00:00:00",
      "totalTracks": 12,
      "duration": "00:43:08"
    },
    {
      "id": 4,
      "title": "Thriller",
      "artistId": 3,
      "artistName": "Michael Jackson",
      "coverImageUrl": "/uploads/albums/thriller.jpg",
      "year": 1982,
      "genre": "Pop",
      "releaseDate": "1982-11-30T00:00:00",
      "totalTracks": 9,
      "duration": "00:42:19"
    }
  ]
}
```

### GET /api/Albums/{id}
```json
{
  "id": 1,
  "title": "Abbey Road",
  "artistId": 1,
  "artistName": "The Beatles",
  "coverImageUrl": "/uploads/albums/abbey-road.jpg",
  "year": 1969,
  "description": "Abbey Road is the eleventh studio album by the English rock band the Beatles...",
  "genre": "Rock",
  "releaseDate": "1969-09-26T00:00:00",
  "totalTracks": 17,
  "duration": "00:47:23",
  "songs": [
    {
      "id": 1,
      "title": "Come Together",
      "trackNumber": 1,
      "duration": "00:04:19",
      "audioUrl": "/uploads/songs/come-together.mp3",
      "playCount": 102
    },
    {
      "id": 2,
      "title": "Something",
      "trackNumber": 2,
      "duration": "00:03:02",
      "audioUrl": "/uploads/songs/something.mp3",
      "playCount": 85
    }
  ]
}
```

### POST /api/Albums
```json
{
  "id": 5,
  "title": "The Dark Side of the Moon",
  "artistId": 4,
  "year": 1973,
  "description": "The Dark Side of the Moon is the eighth studio album by the English rock band Pink Floyd...",
  "genre": "Progressive Rock",
  "releaseDate": "1973-03-01T00:00:00",
  "totalTracks": 10,
  "duration": "00:42:49"
}
```

### PUT /api/Albums/{id}
```json
// No content response (204) if successful
```

### DELETE /api/Albums/{id}
```json
// No content response (204) if successful
```

### POST /api/Albums/{id}/cover
```json
{
  "coverImageUrl": "/uploads/albums/a1b2c3d4-album-cover.jpg"
}
```

## Song Endpoints

### GET /api/Songs
```json
{
  "totalCount": 8,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "title": "Come Together",
      "artistId": 1,
      "artistName": "The Beatles",
      "albumId": 1,
      "albumTitle": "Abbey Road",
      "duration": "00:04:19",
      "audioUrl": "/uploads/songs/come-together.mp3",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "trackNumber": 1,
      "genre": "Rock",
      "releaseDate": "1969-09-26T00:00:00",
      "playCount": 102
    },
    {
      "id": 3,
      "title": "Let It Be",
      "artistId": 1,
      "artistName": "The Beatles",
      "albumId": 2,
      "albumTitle": "Let It Be",
      "duration": "00:04:03",
      "audioUrl": "/uploads/songs/let-it-be.mp3",
      "coverImageUrl": "/uploads/albums/let-it-be.jpg",
      "trackNumber": 1,
      "genre": "Rock",
      "releaseDate": "1970-05-08T00:00:00",
      "playCount": 143
    }
  ]
}
```

### GET /api/Songs/{id}
```json
{
  "id": 1,
  "title": "Come Together",
  "artistId": 1,
  "artistName": "The Beatles",
  "albumId": 1,
  "albumTitle": "Abbey Road",
  "duration": "00:04:19",
  "audioUrl": "/uploads/songs/come-together.mp3",
  "coverImageUrl": "/uploads/albums/abbey-road.jpg",
  "trackNumber": 1,
  "genre": "Rock",
  "releaseDate": "1969-09-26T00:00:00",
  "playCount": 102,
  "isFavorite": true
}
```

### POST /api/Songs
```json
{
  "id": 9,
  "title": "Money",
  "artistId": 4,
  "albumId": 5,
  "duration": "00:06:22",
  "trackNumber": 6,
  "genre": "Progressive Rock",
  "releaseDate": "1973-03-01T00:00:00",
  "playCount": 0
}
```

### PUT /api/Songs/{id}
```json
// No content response (204) if successful
```

### DELETE /api/Songs/{id}
```json
// No content response (204) if successful
```

### POST /api/Songs/{id}/audio
```json
{
  "audioUrl": "/uploads/songs/a1b2c3d4-song.mp3"
}
```

### POST /api/Songs/{id}/cover
```json
{
  "coverImageUrl": "/uploads/songs/a1b2c3d4-song-cover.jpg"
}
```

### POST /api/Songs/{id}/play
```json
{
  "playCount": 103
}
```

### POST /api/Songs/{id}/favorite
```json
// No content response (204) if successful
```

### DELETE /api/Songs/{id}/favorite
```json
// No content response (204) if successful
```

### GET /api/Songs/favorites
```json
{
  "totalCount": 3,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "title": "Come Together",
      "artistId": 1,
      "artistName": "The Beatles",
      "albumId": 1,
      "albumTitle": "Abbey Road",
      "duration": "00:04:19",
      "audioUrl": "/uploads/songs/come-together.mp3",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "trackNumber": 1,
      "genre": "Rock",
      "releaseDate": "1969-09-26T00:00:00",
      "playCount": 102,
      "addedAt": "2023-05-06T10:15:30Z"
    }
  ]
}
```

### GET /api/Songs/genres
```json
[
  "Pop",
  "Rock",
  "Progressive Rock"
]
```

## Playlist Endpoints

### GET /api/Playlists
```json
{
  "totalCount": 1,
  "totalPages": 1,
  "currentPage": 1,
  "pageSize": 20,
  "data": [
    {
      "id": 1,
      "name": "Rock Classics",
      "description": "The best rock songs of all time",
      "userId": 2,
      "username": "user",
      "coverImageUrl": null,
      "isPublic": true,
      "createdAt": "2023-05-06T09:30:00Z",
      "songCount": 6
    }
  ]
}
```

### GET /api/Playlists/user
```json
[
  {
    "id": 1,
    "name": "Rock Classics",
    "description": "The best rock songs of all time",
    "coverImageUrl": null,
    "isPublic": true,
    "createdAt": "2023-05-06T09:30:00Z",
    "songCount": 6
  }
]
```

### GET /api/Playlists/{id}
```json
{
  "id": 1,
  "name": "Rock Classics",
  "description": "The best rock songs of all time",
  "userId": 2,
  "username": "user",
  "coverImageUrl": null,
  "isPublic": true,
  "createdAt": "2023-05-06T09:30:00Z",
  "updatedAt": "2023-05-06T10:15:00Z",
  "songs": [
    {
      "id": 1,
      "title": "Come Together",
      "artistId": 1,
      "artistName": "The Beatles",
      "albumId": 1,
      "albumTitle": "Abbey Road",
      "duration": "00:04:19",
      "audioUrl": "/uploads/songs/come-together.mp3",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "order": 1,
      "addedAt": "2023-05-06T09:30:10Z"
    },
    {
      "id": 2,
      "title": "Something",
      "artistId": 1,
      "artistName": "The Beatles",
      "albumId": 1,
      "albumTitle": "Abbey Road",
      "duration": "00:03:02",
      "audioUrl": "/uploads/songs/something.mp3",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "order": 2,
      "addedAt": "2023-05-06T09:30:15Z"
    }
  ]
}
```

### POST /api/Playlists
```json
{
  "id": 2,
  "name": "Workout Mix",
  "description": "Energetic songs for the gym",
  "userId": 2,
  "isPublic": true,
  "createdAt": "2023-05-07T08:30:00Z",
  "updatedAt": "2023-05-07T08:30:00Z"
}
```

### PUT /api/Playlists/{id}
```json
// No content response (204) if successful
```

### DELETE /api/Playlists/{id}
```json
// No content response (204) if successful
```

### POST /api/Playlists/{id}/songs
```json
// No content response (204) if successful
```

### DELETE /api/Playlists/{id}/songs/{songId}
```json
// No content response (204) if successful
```

### PUT /api/Playlists/{id}/songs/reorder
```json
// No content response (204) if successful
```

### POST /api/Playlists/{id}/cover
```json
{
  "coverImageUrl": "/uploads/playlists/a1b2c3d4-playlist-cover.jpg"
}
```

## Search Endpoints

### GET /api/Search?query=beatles
```json
{
  "songs": [
    {
      "id": 1,
      "title": "Come Together",
      "artistName": "The Beatles",
      "albumTitle": "Abbey Road",
      "duration": "00:04:19",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg"
    },
    {
      "id": 3,
      "title": "Let It Be",
      "artistName": "The Beatles",
      "albumTitle": "Let It Be",
      "duration": "00:04:03",
      "coverImageUrl": "/uploads/albums/let-it-be.jpg"
    }
  ],
  "artists": [
    {
      "id": 1,
      "name": "The Beatles",
      "imageUrl": "/uploads/artists/the-beatles.jpg",
      "genre": "Rock"
    }
  ],
  "albums": [
    {
      "id": 1,
      "title": "Abbey Road",
      "artistName": "The Beatles",
      "coverImageUrl": "/uploads/albums/abbey-road.jpg",
      "year": 1969
    },
    {
      "id": 2,
      "title": "Let It Be",
      "artistName": "The Beatles",
      "coverImageUrl": "/uploads/albums/let-it-be.jpg",
      "year": 1970
    }
  ],
  "playlists": [
    {
      "id": 1,
      "name": "Rock Classics",
      "username": "user",
      "coverImageUrl": null,
      "songCount": 6
    }
  ]
}
```

### GET /api/Search/songs?query=come
```json
[
  {
    "id": 1,
    "title": "Come Together",
    "artistId": 1,
    "artistName": "The Beatles",
    "albumId": 1,
    "albumTitle": "Abbey Road",
    "duration": "00:04:19",
    "coverImageUrl": "/uploads/albums/abbey-road.jpg",
    "genre": "Rock",
    "playCount": 102
  }
]
```

### GET /api/Search/artists?query=queen
```json
[
  {
    "id": 2,
    "name": "Queen",
    "imageUrl": "/uploads/artists/queen.jpg",
    "country": "United Kingdom",
    "genre": "Rock",
    "monthlyListeners": 12000000
  }
]
```

### GET /api/Search/albums?query=opera
```json
[
  {
    "id": 3,
    "title": "A Night at the Opera",
    "artistId": 2,
    "artistName": "Queen",
    "coverImageUrl": "/uploads/albums/a-night-at-the-opera.jpg",
    "year": 1975,
    "genre": "Rock"
  }
]
```

### GET /api/Search/playlists?query=rock
```json
[
  {
    "id": 1,
    "name": "Rock Classics",
    "userId": 2,
    "username": "user",
    "coverImageUrl": null,
    "isPublic": true,
    "songCount": 6
  }
]
```

These examples cover all endpoints in the API, showing the data structure for each response. This should help you understand how to integrate with the backend when developing your frontend application.

[
  {
    "Controller": "ApiExplorer",
    "Action": "GetAllRoutes",
    "Path": "api",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "GetAlbums",
    "Path": "api/Albums",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "CreateAlbum",
    "Path": "api/Albums",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "GetAlbum",
    "Path": "api/Albums/{id}",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "UpdateAlbum",
    "Path": "api/Albums/{id}",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "DeleteAlbum",
    "Path": "api/Albums/{id}",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Albums",
    "Action": "UploadCoverImage",
    "Path": "api/Albums/{id}/cover",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "GetArtists",
    "Path": "api/Artists",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "CreateArtist",
    "Path": "api/Artists",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "GetArtist",
    "Path": "api/Artists/{id}",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "UpdateArtist",
    "Path": "api/Artists/{id}",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "DeleteArtist",
    "Path": "api/Artists/{id}",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "UploadImage",
    "Path": "api/Artists/{id}/image",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "GetCountries",
    "Path": "api/Artists/countries",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Artists",
    "Action": "GetGenres",
    "Path": "api/Artists/genres",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Auth",
    "Action": "Login",
    "Path": "api/Auth/login",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Auth",
    "Action": "RefreshToken",
    "Path": "api/Auth/refresh-token",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Auth",
    "Action": "Register",
    "Path": "api/Auth/register",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Auth",
    "Action": "RevokeToken",
    "Path": "api/Auth/revoke-token",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "GetPlaylists",
    "Path": "api/Playlists",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "CreatePlaylist",
    "Path": "api/Playlists",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "GetPlaylist",
    "Path": "api/Playlists/{id}",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "UpdatePlaylist",
    "Path": "api/Playlists/{id}",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "DeletePlaylist",
    "Path": "api/Playlists/{id}",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "UploadCover",
    "Path": "api/Playlists/{id}/cover",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "AddSong",
    "Path": "api/Playlists/{id}/songs",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "RemoveSong",
    "Path": "api/Playlists/{id}/songs/{songId}",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "ReorderSongs",
    "Path": "api/Playlists/{id}/songs/reorder",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Playlists",
    "Action": "GetUserPlaylists",
    "Path": "api/Playlists/user",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Search",
    "Action": "Search",
    "Path": "api/Search",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Search",
    "Action": "SearchAlbums",
    "Path": "api/Search/albums",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Search",
    "Action": "SearchArtists",
    "Path": "api/Search/artists",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Search",
    "Action": "SearchPlaylists",
    "Path": "api/Search/playlists",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Search",
    "Action": "SearchSongs",
    "Path": "api/Search/songs",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "GetSongs",
    "Path": "api/Songs",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "CreateSong",
    "Path": "api/Songs",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "GetSong",
    "Path": "api/Songs/{id}",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "UpdateSong",
    "Path": "api/Songs/{id}",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "DeleteSong",
    "Path": "api/Songs/{id}",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "UploadAudio",
    "Path": "api/Songs/{id}/audio",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "UploadCover",
    "Path": "api/Songs/{id}/cover",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "FavoriteSong",
    "Path": "api/Songs/{id}/favorite",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "UnfavoriteSong",
    "Path": "api/Songs/{id}/favorite",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "PlaySong",
    "Path": "api/Songs/{id}/play",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "GetFavorites",
    "Path": "api/Songs/favorites",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Songs",
    "Action": "GetGenres",
    "Path": "api/Songs/genres",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "GetUsers",
    "Path": "api/Users",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "GetUser",
    "Path": "api/Users/{id}",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "UpdateUser",
    "Path": "api/Users/{id}",
    "HttpMethods": [
      "PUT"
    ]
  },
  {
    "Controller": "Users",
    "Action": "FollowUser",
    "Path": "api/Users/{id}/follow",
    "HttpMethods": [
      "POST"
    ]
  },
  {
    "Controller": "Users",
    "Action": "GetFollowers",
    "Path": "api/Users/{id}/followers",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "GetFollowing",
    "Path": "api/Users/{id}/following",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "UnfollowUser",
    "Path": "api/Users/{id}/unfollow",
    "HttpMethods": [
      "DELETE"
    ]
  },
  {
    "Controller": "Users",
    "Action": "GetCurrentUser",
    "Path": "api/Users/profile",
    "HttpMethods": [
      "GET"
    ]
  },
  {
    "Controller": "Users",
    "Action": "UploadProfileImage",
    "Path": "api/Users/profile-image",
    "HttpMethods": [
      "POST"
    ]
  }
]
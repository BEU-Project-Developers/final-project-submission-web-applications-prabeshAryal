I'll analyze your model files to provide a comprehensive database schema overview. Let me check the model files to understand the relationships and structure.


Read file: Models/User.cs

Read file: Models/Artist.cs

Read file: Models/Album.cs

Read file: Models/Song.cs

Read file: Models/Playlist.cs
Based on the model files, here's the complete database schema:

### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    PasswordHash NVARCHAR(MAX) NOT NULL,
    ProfileImageUrl NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    LastLoginAt DATETIME2 NOT NULL
)
```

### Roles Table
```sql
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL
)
```

### UserRoles Table (Junction table for User-Role many-to-many)
```sql
CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
)
```

### Artists Table
```sql
CREATE TABLE Artists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Bio NVARCHAR(1000),
    ImageUrl NVARCHAR(MAX),
    Country NVARCHAR(100),
    Genre NVARCHAR(50),
    FormedDate DATETIME2,
    MonthlyListeners INT,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
)
```

### Albums Table
```sql
CREATE TABLE Albums (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(100) NOT NULL,
    ArtistId INT NOT NULL,
    CoverImageUrl NVARCHAR(MAX),
    Year INT,
    Description NVARCHAR(1000),
    Genre NVARCHAR(50),
    ReleaseDate DATETIME2,
    TotalTracks INT,
    Duration TIME,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ArtistId) REFERENCES Artists(Id)
)
```

### Songs Table
```sql
CREATE TABLE Songs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(100) NOT NULL,
    ArtistId INT,
    AlbumId INT,
    Duration TIME NOT NULL,
    AudioUrl NVARCHAR(MAX),
    CoverImageUrl NVARCHAR(MAX),
    TrackNumber INT,
    Genre NVARCHAR(50),
    ReleaseDate DATETIME2,
    PlayCount INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (ArtistId) REFERENCES Artists(Id),
    FOREIGN KEY (AlbumId) REFERENCES Albums(Id)
)
```

### Playlists Table
```sql
CREATE TABLE Playlists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    UserId INT NOT NULL,
    CoverImageUrl NVARCHAR(MAX),
    IsPublic BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
```

### PlaylistSongs Table (Junction table for Playlist-Song many-to-many)
```sql
CREATE TABLE PlaylistSongs (
    PlaylistId INT NOT NULL,
    SongId INT NOT NULL,
    PRIMARY KEY (PlaylistId, SongId),
    FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id),
    FOREIGN KEY (SongId) REFERENCES Songs(Id)
)
```

### UserFollowers Table (Junction table for User-User many-to-many)
```sql
CREATE TABLE UserFollowers (
    FollowerId INT NOT NULL,
    FollowingId INT NOT NULL,
    PRIMARY KEY (FollowerId, FollowingId),
    FOREIGN KEY (FollowerId) REFERENCES Users(Id),
    FOREIGN KEY (FollowingId) REFERENCES Users(Id)
)
```

### UserFavorites Table (Junction table for User-Song many-to-many)
```sql
CREATE TABLE UserFavorites (
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    PRIMARY KEY (UserId, SongId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (SongId) REFERENCES Songs(Id)
)
```

### RefreshTokens Table
```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Token NVARCHAR(MAX) NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
```

Key Features of the Schema:
1. All tables have proper foreign key constraints
2. Many-to-many relationships are handled through junction tables
3. Appropriate data types and lengths for each field
4. Timestamps for creation and updates
5. Nullable fields where appropriate
6. Default values for boolean fields
7. Proper indexing through primary and foreign keys

The schema supports:
- User authentication and authorization
- Artist and album management
- Song cataloging
- Playlist creation and management
- User following system
- Favorites system
- Refresh token management for authentication

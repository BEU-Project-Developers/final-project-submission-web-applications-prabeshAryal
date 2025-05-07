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



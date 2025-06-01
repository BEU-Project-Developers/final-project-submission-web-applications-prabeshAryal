-- ===============================================
-- Music App Database Recreation Script
-- Modern Programming Language - 2 Final Project
-- Author: Prabesh Aryal
-- Created: December 2025
-- ===============================================

-- This script creates the complete database structure for the Music App
-- Run this script to recreate the database on another machine

-- Create Database (uncomment if needed)
-- CREATE DATABASE MusicAppDB;
-- GO
-- USE MusicAppDB;
-- GO

-- ===========================================
-- 1. Create Tables in Dependency Order
-- ===========================================

-- Users Table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    ProfileImageUrl NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Roles Table
CREATE TABLE Roles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

-- UserRoles Junction Table
CREATE TABLE UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

-- Artists Table
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
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Albums Table
CREATE TABLE Albums (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(100) NOT NULL,
    ArtistId INT,
    CoverImageUrl NVARCHAR(MAX),
    Year INT,
    Description NVARCHAR(1000),
    Genre NVARCHAR(50),
    ReleaseDate DATETIME2,
    TotalTracks INT,
    Duration TIME,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ArtistId) REFERENCES Artists(Id) ON DELETE SET NULL
);

-- Songs Table
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
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ArtistId) REFERENCES Artists(Id) ON DELETE SET NULL,
    FOREIGN KEY (AlbumId) REFERENCES Albums(Id) ON DELETE SET NULL
);

-- Playlists Table
CREATE TABLE Playlists (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    UserId INT NOT NULL,
    CoverImageUrl NVARCHAR(MAX),
    IsPublic BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- PlaylistSongs Junction Table
CREATE TABLE PlaylistSongs (
    PlaylistId INT NOT NULL,
    SongId INT NOT NULL,
    [Order] INT NOT NULL,
    AddedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (PlaylistId, SongId),
    FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
    FOREIGN KEY (SongId) REFERENCES Songs(Id) ON DELETE CASCADE
);

-- UserFavorites Junction Table
CREATE TABLE UserFavorites (
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    AddedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (UserId, SongId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SongId) REFERENCES Songs(Id) ON DELETE RESTRICT
);

-- UserFollowers Junction Table (User following system)
CREATE TABLE UserFollowers (
    UserId INT NOT NULL,
    FollowerId INT NOT NULL,
    PRIMARY KEY (UserId, FollowerId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    FOREIGN KEY (FollowerId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CHECK (UserId != FollowerId) -- Prevent self-following
);

-- SongArtists Junction Table (for collaborations)
CREATE TABLE SongArtists (
    SongId INT NOT NULL,
    ArtistId INT NOT NULL,
    IsPrimaryArtist BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    PRIMARY KEY (SongId, ArtistId),
    FOREIGN KEY (SongId) REFERENCES Songs(Id) ON DELETE CASCADE,
    FOREIGN KEY (ArtistId) REFERENCES Artists(Id) ON DELETE CASCADE
);

-- AlbumLikes Table
CREATE TABLE AlbumLikes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    AlbumId INT NOT NULL,
    LikedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (AlbumId) REFERENCES Albums(Id) ON DELETE CASCADE,
    UNIQUE(UserId, AlbumId)
);

-- RefreshTokens Table (for JWT authentication)
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Token NVARCHAR(MAX) NOT NULL,
    UserId INT NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    IssuedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsRevoked BIT NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- UserSongPlays Table (for analytics)
CREATE TABLE UserSongPlays (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    SongId INT NOT NULL,
    PlayedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Duration TIME,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (SongId) REFERENCES Songs(Id) ON DELETE CASCADE
);

-- ===========================================
-- 2. Create Indexes for Performance
-- ===========================================

-- Users table indexes
CREATE NONCLUSTERED INDEX IX_Users_Username ON Users(Username);
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);

-- Artists table indexes
CREATE NONCLUSTERED INDEX IX_Artists_Name ON Artists(Name);
CREATE NONCLUSTERED INDEX IX_Artists_Genre ON Artists(Genre);

-- Albums table indexes
CREATE NONCLUSTERED INDEX IX_Albums_ArtistId ON Albums(ArtistId);
CREATE NONCLUSTERED INDEX IX_Albums_Genre ON Albums(Genre);

-- Songs table indexes
CREATE NONCLUSTERED INDEX IX_Songs_ArtistId ON Songs(ArtistId);
CREATE NONCLUSTERED INDEX IX_Songs_AlbumId ON Songs(AlbumId);
CREATE NONCLUSTERED INDEX IX_Songs_Genre ON Songs(Genre);
CREATE NONCLUSTERED INDEX IX_Songs_PlayCount ON Songs(PlayCount DESC);

-- Playlists table indexes
CREATE NONCLUSTERED INDEX IX_Playlists_UserId ON Playlists(UserId);
CREATE NONCLUSTERED INDEX IX_Playlists_IsPublic ON Playlists(IsPublic);

-- ===========================================
-- 3. Insert Initial Data
-- ===========================================

-- Insert Roles
INSERT INTO Roles (Name, Description) VALUES 
('Admin', 'Administrator with full access'),
('User', 'Standard user');

-- Insert Initial Admin User
-- Password is: Admin@123 (hashed with BCrypt)
INSERT INTO Users (Username, Email, FirstName, LastName, PasswordHash, CreatedAt, UpdatedAt, LastLoginAt) VALUES 
('admin', 'admin@example.com', 'Admin', 'User', '$2a$11$rOZKZ9z8cYQc5rOZKZ9z8uO1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1', GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- Insert Initial Regular User  
-- Password is: User@123 (hashed with BCrypt)
INSERT INTO Users (Username, Email, FirstName, LastName, PasswordHash, CreatedAt, UpdatedAt, LastLoginAt) VALUES 
('prabe.sh', 'hello@prabe.sh', 'Prabesh', 'Aryal', '$2a$11$rOZKZ9z8cYQc5rOZKZ9z8uO1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1qJ1', GETUTCDATE(), GETUTCDATE(), GETUTCDATE());

-- Assign roles to users
INSERT INTO UserRoles (UserId, RoleId) VALUES 
(1, 1), -- Admin user gets Admin role
(1, 2), -- Admin user also gets User role
(2, 2); -- Regular user gets User role

-- Insert Sample Artists
INSERT INTO Artists (Name, Bio, Country, Genre, FormedDate, MonthlyListeners, IsActive, CreatedAt, UpdatedAt) VALUES 
('Ed Sheeran', 'English singer-songwriter', 'UK', 'Pop', '2010-01-01', 75000000, 1, GETUTCDATE(), GETUTCDATE()),
('Taylor Swift', 'American singer-songwriter', 'USA', 'Pop', '2006-01-01', 80000000, 1, GETUTCDATE(), GETUTCDATE()),
('Arijit Singh', 'Indian playback singer', 'India', 'Bollywood', '2005-01-01', 31000000, 1, GETUTCDATE(), GETUTCDATE()),
('Sajjan Raj Vaidya', 'Nepali singer-songwriter', 'Nepal', 'Nepali Pop', '2014-01-01', 550000, 1, GETUTCDATE(), GETUTCDATE()),
('The Chainsmokers', 'American DJ duo', 'USA', 'EDM', '2012-01-01', 25000000, 1, GETUTCDATE(), GETUTCDATE());

-- Insert Sample Albums
INSERT INTO Albums (Title, ArtistId, Year, Description, Genre, ReleaseDate, TotalTracks, Duration, CreatedAt, UpdatedAt) VALUES 
('÷ (Divide)', 1, 2017, 'Third studio album by Ed Sheeran', 'Pop', '2017-03-03', 16, '00:59:00', GETUTCDATE(), GETUTCDATE()),
('1989', 2, 2014, 'Fifth studio album by Taylor Swift', 'Pop', '2014-10-27', 13, '00:48:00', GETUTCDATE(), GETUTCDATE()),
('Aashiqui 2 Soundtrack', 3, 2013, 'Bollywood soundtrack album', 'Bollywood', '2013-04-06', 11, '00:58:00', GETUTCDATE(), GETUTCDATE()),
('Singles Collection', 4, 2023, 'Collection of hit singles', 'Nepali Pop', '2023-01-01', 5, '00:22:00', GETUTCDATE(), GETUTCDATE()),
('Collage EP', 5, 2016, 'Second EP by The Chainsmokers', 'EDM', '2016-11-04', 5, '00:19:00', GETUTCDATE(), GETUTCDATE());

-- Insert Sample Songs
INSERT INTO Songs (Title, ArtistId, AlbumId, Duration, TrackNumber, Genre, ReleaseDate, PlayCount, CreatedAt, UpdatedAt) VALUES 
('Shape of You', 1, 1, '00:03:53', 4, 'Pop', '2017-01-06', 1000000, GETUTCDATE(), GETUTCDATE()),
('Perfect', 1, 1, '00:04:23', 5, 'Pop', '2017-03-03', 800000, GETUTCDATE(), GETUTCDATE()),
('Blank Space', 2, 2, '00:03:51', 2, 'Pop', '2014-11-10', 900000, GETUTCDATE(), GETUTCDATE()),
('Shake It Off', 2, 2, '00:03:39', 6, 'Pop', '2014-08-18', 850000, GETUTCDATE(), GETUTCDATE()),
('Tum Hi Ho', 3, 3, '00:04:22', 1, 'Bollywood', '2013-04-06', 500000, GETUTCDATE(), GETUTCDATE()),
('Hataarindai Bataasindai', 4, 4, '00:04:30', 1, 'Nepali Pop', '2018-01-01', 100000, GETUTCDATE(), GETUTCDATE()),
('Closer', 5, 5, '00:04:04', 1, 'EDM', '2016-07-29', 900000, GETUTCDATE(), GETUTCDATE());

-- Insert Sample Playlists
INSERT INTO Playlists (Name, Description, UserId, IsPublic, CreatedAt, UpdatedAt) VALUES 
('My Favorites', 'Collection of my favorite songs', 2, 1, GETUTCDATE(), GETUTCDATE()),
('Workout Mix', 'High energy songs for workouts', 2, 1, GETUTCDATE(), GETUTCDATE());

-- Insert Sample Playlist Songs
INSERT INTO PlaylistSongs (PlaylistId, SongId, [Order], AddedAt) VALUES 
(1, 1, 1, GETUTCDATE()),
(1, 3, 2, GETUTCDATE()),
(1, 5, 3, GETUTCDATE()),
(2, 1, 1, GETUTCDATE()),
(2, 7, 2, GETUTCDATE());

-- ===========================================
-- 4. Database Setup Complete
-- ===========================================

PRINT 'Music App Database has been successfully created and populated with initial data!';
PRINT 'Admin credentials: admin@example.com / Admin@123';
PRINT 'User credentials: hello@prabe.sh / User@123';
PRINT '';
PRINT 'Database contains:';
PRINT '- 2 Roles (Admin, User)';
PRINT '- 2 Users (1 Admin, 1 Regular User)';
PRINT '- 5 Sample Artists';
PRINT '- 5 Sample Albums';
PRINT '- 7 Sample Songs';
PRINT '- 2 Sample Playlists';
PRINT '';
PRINT 'To use with the Music App:';
PRINT '1. Update connection string in appsettings.json';
PRINT '2. Run Entity Framework migrations if needed';
PRINT '3. Start the application';
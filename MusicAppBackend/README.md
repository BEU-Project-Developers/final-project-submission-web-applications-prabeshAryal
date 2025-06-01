# 🎵 MusicApp Backend

A comprehensive RESTful API backend for a modern music streaming application built with ASP.NET Core. This backend provides secure authentication, music streaming, playlist management, social features, and administrative capabilities.

## Developer Information

**Name:** Prabesh Aryal  
**University:** Baku Engineering University  
**Program:** Computer Engineering BSc, Class 1202i  
**Portfolio:** https://prabe.sh  
**Email:** hello@prabe.sh  
**GitHub:** prabeshAryal

---

## 🎵 Features

### Core Music Features
- **Music Streaming**: Upload, store, and stream audio files
- **Artist Management**: Complete artist profiles with images
- **Album Management**: Album creation with cover art support
- **Song Management**: Detailed song metadata with multiple artist support
- **Search Functionality**: Advanced search across songs, artists, and albums

### User Management
- **Authentication**: Secure JWT-based authentication with refresh tokens
- **User Profiles**: Comprehensive user profile management
- **Role-Based Access**: Admin and User role differentiation
- **Password Security**: Bcrypt password hashing
- **Account Management**: Registration, login, password reset

### Social Features
- **Playlists**: Create, manage, and share custom playlists
- **Favorites**: Mark songs as favorites
- **Following System**: Follow other users
- **Playlist Sharing**: Share playlists with other users
- **Listening History**: Track user's music listening patterns

### Analytics & Statistics
- **Play Tracking**: Monitor song play counts and user engagement
- **User Statistics**: Detailed listening statistics and history
- **Popular Content**: Track most played songs and trending content

### File Management
- **Multi-format Support**: Audio files, images, and cover art
- **Secure Upload**: Protected file upload endpoints
- **Efficient Storage**: Organized file storage system
- **Direct Streaming**: Optimized file serving for audio streaming

### Administrative Features
- **Content Moderation**: Admin controls for managing content
- **User Management**: Administrative user oversight
- **System Statistics**: Platform-wide analytics and insights

---

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 6.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens) with refresh token support
- **Password Hashing**: BCrypt
- **File Storage**: Local file system with organized structure
- **API Documentation**: Swagger/OpenAPI
- **CORS**: Configured for cross-origin requests
- **Logging**: Built-in ASP.NET Core logging

---

## 📊 Database Schema

### Core Entities

#### Users
```sql
Users (Id, Username, Email, PasswordHash, Salt, Role, CreatedAt, IsActive, ...)
```

#### Music Entities
```sql
Artists (Id, Name, Bio, ImagePath, CreatedAt, ...)
Albums (Id, Title, Description, CoverImagePath, ReleaseDate, ...)
Songs (Id, Title, Duration, FilePath, CoverImagePath, ReleaseDate, PlayCount, ...)
```

#### Social & Playlist Entities
```sql
Playlists (Id, Name, Description, CoverImagePath, UserId, IsPublic, CreatedAt, ...)
UserFollows (FollowerId, FollowingId, CreatedAt)
UserFavorites (UserId, SongId, CreatedAt)
```

### Relationships

#### Many-to-Many Relationships
- **Songs ↔ Artists**: `SongArtists` (SongId, ArtistId)
- **Songs ↔ Playlists**: `PlaylistSongs` (PlaylistId, SongId, AddedAt)
- **Users ↔ Songs**: `UserFavorites` (UserId, SongId, CreatedAt)
- **Users ↔ Users**: `UserFollows` (FollowerId, FollowingId, CreatedAt)

#### One-to-Many Relationships
- **Users → Playlists**: One user can have multiple playlists
- **Albums → Songs**: One album can contain multiple songs
- **Users → UserSongHistory**: Track individual play history

---

## 🔐 Authentication System

### JWT Implementation
- **Access Tokens**: Short-lived tokens for API access (15 minutes)
- **Refresh Tokens**: Long-lived tokens for token renewal (7 days)
- **Secure Storage**: Refresh tokens stored securely in database
- **Token Rotation**: Automatic token refresh mechanism

### Authorization Levels
- **Public**: No authentication required
- **User**: Requires valid JWT token
- **Admin**: Requires admin role privileges

### Security Features
- Password hashing with BCrypt and salt
- Token blacklisting for logout
- Role-based access control
- Secure password requirements

---

## 🚀 API Endpoints

### Authentication Endpoints
```
POST /api/auth/register          # User registration
POST /api/auth/login             # User login
POST /api/auth/refresh           # Token refresh
POST /api/auth/revoke            # Token revocation/logout
```

### Song Management
```
GET    /api/songs                # Get all songs (with pagination)
GET    /api/songs/{id}           # Get specific song
POST   /api/songs                # Create new song (Admin)
PUT    /api/songs/{id}           # Update song (Admin)
DELETE /api/songs/{id}           # Delete song (Admin)
POST   /api/songs/{id}/audio     # Upload song audio file
POST   /api/songs/{id}/cover     # Upload song cover image
POST   /api/songs/{id}/play      # Record song play
GET    /api/songs/search         # Search songs
```

### Artist Management
```
GET    /api/artists              # Get all artists
GET    /api/artists/{id}         # Get specific artist
POST   /api/artists              # Create new artist (Admin)
PUT    /api/artists/{id}         # Update artist (Admin)
DELETE /api/artists/{id}         # Delete artist (Admin)
POST   /api/artists/{id}/image   # Upload artist image
GET    /api/artists/{id}/songs   # Get artist's songs
```

### Album Management
```
GET    /api/albums               # Get all albums
GET    /api/albums/{id}          # Get specific album
POST   /api/albums               # Create new album (Admin)
PUT    /api/albums/{id}          # Update album (Admin)
DELETE /api/albums/{id}          # Delete album (Admin)
POST   /api/albums/{id}/cover    # Upload album cover
GET    /api/albums/{id}/songs    # Get album songs
```

### Playlist Management
```
GET    /api/playlists            # Get user's playlists
GET    /api/playlists/{id}       # Get specific playlist
POST   /api/playlists            # Create new playlist
PUT    /api/playlists/{id}       # Update playlist
DELETE /api/playlists/{id}       # Delete playlist
POST   /api/playlists/{id}/songs/{songId}  # Add song to playlist
DELETE /api/playlists/{id}/songs/{songId}  # Remove song from playlist
POST   /api/playlists/{id}/cover # Upload playlist cover
```

### User Management
```
GET    /api/users/{id}           # Get user profile
PUT    /api/users/{id}           # Update user profile
GET    /api/users/{id}/stats     # Get user statistics
GET    /api/users/{id}/history   # Get listening history
GET    /api/users/{id}/favorites # Get favorite songs
POST   /api/users/{id}/favorites/{songId}   # Add to favorites
DELETE /api/users/{id}/favorites/{songId}   # Remove from favorites
POST   /api/users/{id}/follow/{targetId}    # Follow user
DELETE /api/users/{id}/follow/{targetId}    # Unfollow user
```

### File Management
```
POST   /api/filestorage/upload   # Upload files
GET    /api/files/{filename}     # Serve files
```

---

## ⚙️ Setup and Installation

### Prerequisites
- .NET 6.0 SDK or later
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd MusicAppBackend
   ```

2. **Configure Database Connection**
   Update `appsettings.json` with your SQL Server connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MusicAppDb;Trusted_Connection=true;"
     }
   }
   ```

3. **Configure JWT Settings**
   Update JWT configuration in `appsettings.json`:
   ```json
   {
     "Jwt": {
       "Key": "your-super-secret-key-here-make-it-long-and-secure",
       "Issuer": "MusicApp",
       "Audience": "MusicAppUsers",
       "ExpireMinutes": 15,
       "RefreshExpireDays": 7
     }
   }
   ```

4. **Install Dependencies**
   ```bash
   dotnet restore
   ```

5. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

6. **Run the Application**
   ```bash
   dotnet run
   ```

7. **Access the API**
   - API Base URL: `https://localhost:7001` or `http://localhost:5001`
   - Swagger Documentation: `https://localhost:7001/swagger`

### Environment Configuration

Create `appsettings.Development.json` for development-specific settings:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "your-development-connection-string"
  }
}
```

---

## 📁 Project Structure

```
MusicAppBackend/
├── Controllers/              # API Controllers
│   ├── AuthController.cs     # Authentication endpoints
│   ├── SongsController.cs    # Song management
│   ├── ArtistsController.cs  # Artist management
│   ├── AlbumsController.cs   # Album management
│   ├── PlaylistsController.cs # Playlist management
│   ├── UsersController.cs    # User management
│   ├── FilesController.cs    # File serving
│   └── FileStorageController.cs # File uploads
├── Data/                     # Database context and models
│   ├── MusicDbContext.cs     # EF Core DbContext
│   └── DbInitializer.cs      # Database seeding
├── DTOs/                     # Data Transfer Objects
│   ├── AuthDTOs.cs          # Authentication DTOs
│   ├── SongDTOs.cs          # Song-related DTOs
│   ├── PlaylistDTOs.cs      # Playlist DTOs
│   └── UserDTOs.cs          # User DTOs
├── Models/                   # Entity models
│   ├── User.cs              # User entity
│   ├── Song.cs              # Song entity
│   ├── Artist.cs            # Artist entity
│   ├── Album.cs             # Album entity
│   └── Playlist.cs          # Playlist entity
├── Services/                 # Business logic services
│   └── AuthService.cs       # Authentication service
├── Migrations/              # EF Core migrations
├── uploads/                 # File storage directory
├── Program.cs               # Application entry point
└── appsettings.json         # Configuration settings
```

---

## 🔧 Configuration

### CORS Configuration
The application is configured to accept requests from frontend applications running on different ports:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### File Upload Configuration
- **Upload Directory**: `uploads/` (created automatically)
- **Supported Formats**: Audio files (mp3, wav, etc.), Images (jpg, png, gif)
- **File Size Limits**: Configurable through IFormFile constraints

### Database Initialization
The application automatically:
- Creates the database if it doesn't exist
- Applies pending migrations
- Seeds initial data (default admin user, sample content)

---

## 📝 Usage Examples

### Authentication Flow
```csharp
// Register a new user
POST /api/auth/register
{
    "username": "john_doe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
}

// Login
POST /api/auth/login
{
    "email": "john@example.com",
    "password": "SecurePassword123!"
}

// Response includes access token and refresh token
{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "user": { ... }
}
```

### Creating a Playlist
```csharp
// Create playlist (requires authentication)
POST /api/playlists
Authorization: Bearer <access-token>
{
    "name": "My Favorites",
    "description": "Collection of my favorite songs",
    "isPublic": true
}

// Add song to playlist
POST /api/playlists/{playlistId}/songs/{songId}
Authorization: Bearer <access-token>
```

### File Upload
```csharp
// Upload song audio file
POST /api/songs/{songId}/audio
Authorization: Bearer <admin-token>
Content-Type: multipart/form-data

FormData: file = <audio-file>
```

---

## 🧪 Testing

### API Testing with Swagger
1. Navigate to `https://localhost:7001/swagger`
2. Use the interactive documentation to test endpoints
3. Authenticate using the `/api/auth/login` endpoint
4. Copy the returned token to authorize other requests

### Database Testing
The application includes database seeding for testing:
- Default admin user created on first run
- Sample artists, albums, and songs for testing
- Test playlists and user relationships

---

## 🚀 Deployment

### Production Considerations
1. **Security**: Update JWT secrets and connection strings
2. **CORS**: Configure appropriate allowed origins
3. **File Storage**: Consider cloud storage solutions for scalability
4. **Database**: Use production SQL Server instance
5. **Logging**: Configure comprehensive logging for monitoring
6. **HTTPS**: Ensure SSL certificates are properly configured

### Environment Variables
Consider using environment variables for sensitive configuration:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`

---

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Verify SQL Server is running
   - Check connection string format
   - Ensure database permissions

2. **Authentication Issues**
   - Verify JWT configuration
   - Check token expiration
   - Ensure proper Bearer token format

3. **File Upload Issues**
   - Check upload directory permissions
   - Verify file size limits
   - Ensure supported file formats

4. **CORS Issues**
   - Verify allowed origins in CORS policy
   - Check frontend request headers
   - Ensure credentials are included if required

---

## 📄 License

This project is developed as part of academic studies at Baku Engineering University.

---

## 👨‍💻 Developer Contact

**Prabesh Aryal**  
Computer Engineering BSc, Class 1202i  
Baku Engineering University  

📧 **Email:** hello@prabe.sh  
🌐 **Portfolio:** https://prabe.sh  
💻 **GitHub:** prabeshAryal  

For any questions or collaboration opportunities, feel free to reach out through any of the above channels.

# 🗄️ Database Recreation Guide

## Music App Database Setup - Multiple Options

This directory contains resources for recreating the Music App database on any machine. You have **three different options** to set up the database:

> **🎯 New to Databases?** Don't worry! This guide explains everything step-by-step. Choose Option 1 for the simplest setup.

---

## 🤔 **Which Option Should I Choose?**

| If you are... | Choose | Why? |
|---------------|---------|------|
| **New to development** | Option 1 | Simplest - just run one SQL script |
| **Working with the code** | Option 2 | Best for development and testing |
| **Want maximum control** | Option 3 | Combine EF migrations with manual seeding |

---

## 📋 **Prerequisites**
- SQL Server (LocalDB, Express, or Full Version)
- .NET 8.0 SDK
- Visual Studio Code or Visual Studio

---

## 🚀 **Option 1: Manual SQL Script (Recommended for Fresh Setup)**

### Quick Setup
1. **Open SQL Server Management Studio (SSMS)** or **Azure Data Studio**
2. **Connect to your SQL Server instance**
3. **Create a new database** (optional - script can create it):
   ```sql
   CREATE DATABASE MusicAppDB;
   ```
4. **Run the recreation script**:
   ```sql
   -- Execute the complete script
   -- File: Database_Recreation_Script.sql
   ```

### What This Script Does:
- ✅ Creates all 14 database tables with proper relationships
- ✅ Sets up foreign key constraints and indexes
- ✅ Inserts initial roles (Admin, User)
- ✅ Creates default admin and user accounts
- ✅ Populates sample data (Artists, Albums, Songs, Playlists)
- ✅ Ready to use immediately

### Default Credentials:
| Role | Username | Email | Password |
|------|----------|-------|----------|
| Admin | `admin` | `admin@example.com` | `Admin@123` |
| User | `prabe.sh` | `hello@prabe.sh` | `User@123` |

---

## 🔄 **Option 2: Entity Framework Migrations (Recommended for Development)**

### For Developers Working with the Source Code:

> **💡 What are EF Migrations?** Entity Framework (EF) migrations are like version control for your database. They track changes to your database structure and can automatically create/update tables.

1. **Navigate to Backend Directory**:
   ```powershell
   cd MusicAppBackend
   ```

2. **Update Connection String** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MusicAppDB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Install EF Core Tools** (if not already installed):
   ```powershell
   # Install Entity Framework Core tools globally
   dotnet tool install --global dotnet-ef
   
   # Or update if already installed
   dotnet tool update --global dotnet-ef
   
   # Verify installation
   dotnet ef --version
   ```

4. **Understanding EF Commands**:
   ```powershell
   # List all available migrations
   dotnet ef migrations list
   
   # Check current database status
   dotnet ef database get-connection
   
   # Apply migrations to create/update database
   dotnet ef database update
   
   # Create a new migration (if needed)
   dotnet ef migrations add YourMigrationName
   
   # Remove last migration (if not applied to database)
   dotnet ef migrations remove
   
   # Reset database completely (CAUTION: Deletes all data)
   dotnet ef database drop --force
   ```

5. **Run EF Migrations**:
   ```powershell
   # Check if migrations exist (should show InitialMigration)
   dotnet ef migrations list
   
   # Apply database migrations to create tables
   dotnet ef database update
   
   # If migrations don't exist, create them first
   dotnet ef migrations add InitialMigration
   dotnet ef database update
   ```

6. **Database Seeding Options**:
   
   **Option A: Automatic Seeding (Recommended)**
   - The application automatically seeds data when you run it
   - Simply start the application: `dotnet run`
   - Look for seeding messages in the console output
   
   **Option B: Manual Seeding Activation**
   - If seeding doesn't happen automatically, you can force it
   - Open `MusicAppBackend/Program.cs`
   - Find these commented lines (around line 120):
   ```csharp
   // Initialize database and seed data
   /*try
   {
       logger.LogInformation("Initializing database with seed data...");
       await DbInitializer.Initialize(app.Services);
       logger.LogInformation("Database initialization completed successfully.");
   }
   catch (Exception ex)
   {
       logger.LogWarning(ex, "Database initialization failed. The application will start without initial data.");
   }*/
   ```
   - **Uncomment these lines** (remove `/*` and `*/`) to force seeding on application startup
   - This ensures data is populated even if seeding was skipped before
   - Save the file and run `dotnet run` again

### Benefits of EF Migrations:
- 🔄 Version-controlled database changes
- 🛠️ Easy rollback capabilities
- 🔗 Seamless integration with application code
- 📊 Automatic schema updates

---

## 🌱 **Option 3: Seeding Script with EF (Hybrid Approach)**

### Best of Both Worlds:

1. **Use EF to create schema**:
   ```powershell
   cd MusicAppBackend
   dotnet ef database update
   ```

2. **Run seeding manually** (if needed):
   ```powershell
   # The DbInitializer.cs class provides comprehensive seeding
   # It runs automatically when you start the application
   dotnet run
   ```

### Seeding Features:
- 🎵 **Rich Sample Data**: 15+ Artists, 20+ Albums, 50+ Songs
- 👥 **User Accounts**: Admin and regular user accounts
- 🎶 **Playlists**: Pre-created playlists with songs
- 🔐 **Role System**: Admin and User roles with proper permissions
- 📊 **Relationships**: All entity relationships properly established

---

## 🗂️ **Database Schema Overview**

### Core Tables:
| Table | Purpose | Records |
|-------|---------|---------|
| `Users` | User accounts and profiles | 2+ |
| `Roles` | Permission system | 2 |
| `Artists` | Music artists and bands | 15+ |
| `Albums` | Music albums | 20+ |
| `Songs` | Individual tracks | 50+ |
| `Playlists` | User-created playlists | Sample playlists |

### Relationship Tables:
| Table | Relationship | Purpose |
|-------|-------------|---------|
| `UserRoles` | User ↔ Role | Role assignments |
| `PlaylistSongs` | Playlist ↔ Song | Playlist contents |
| `UserFavorites` | User ↔ Song | Favorite songs |
| `UserFollowers` | User ↔ User | Social following |
| `SongArtists` | Song ↔ Artist | Collaborations |
| `AlbumLikes` | User ↔ Album | Album likes |

---

## 🔧 **Connection String Configuration**

### For Local Development:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MusicAppDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### For SQL Server Express:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MusicAppDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### For Remote SQL Server:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=MusicAppDB;User Id=your-username;Password=your-password;MultipleActiveResultSets=true"
  }
}
```

---

## ✅ **Verification Steps**

After setup, verify your database:

1. **Check Table Creation**:
   ```sql
   SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_TYPE = 'BASE TABLE'
   ORDER BY TABLE_NAME;
   ```

2. **Verify Sample Data**:
   ```sql
   SELECT COUNT(*) as UserCount FROM Users;
   SELECT COUNT(*) as ArtistCount FROM Artists;
   SELECT COUNT(*) as SongCount FROM Songs;
   ```

3. **Test Login** with provided credentials

---

## 🚨 **Troubleshooting**

### Common Issues:

**Connection Problems:**
- Ensure SQL Server is running
- Check connection string format
- Verify database permissions

**Migration Errors:**
```powershell
# Reset migrations if needed
dotnet ef database drop --force
dotnet ef database update
```

**Seeding Issues:**
- Check `DbInitializer.cs` logs in application console
- Ensure no duplicate data constraints are violated
- If automatic seeding fails, manually uncomment seeding code in `Program.cs`

**Entity Framework Tool Issues:**
```powershell
# If dotnet ef command not found
dotnet tool install --global dotnet-ef

# If EF tools are outdated
dotnet tool update --global dotnet-ef

# Check if tools are installed correctly
dotnet ef --version
```

**Database Already Exists Error:**
```powershell
# Drop existing database and recreate
dotnet ef database drop --force
dotnet ef database update
```

---

## 📞 **Support**

For questions about database setup:
1. Check application logs in console output
2. Verify connection strings match your SQL Server setup
3. Ensure all prerequisites are installed

---

## 🎯 **Quick Start Summary**

**Fastest Setup** (Option 1):
1. Open SSMS/Azure Data Studio
2. Run `Database_Recreation_Script.sql`
3. Update connection string in `appsettings.json`
4. Start application with `dotnet run`

**Development Setup** (Option 2):
1. Update connection string
2. Run `dotnet ef database update`
3. Start application (auto-seeds data)

That's it! Your Music App database is ready! 🎵
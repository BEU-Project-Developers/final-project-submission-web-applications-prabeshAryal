# Music App Project

A full-stack music streaming platform built with ASP.NET Core (backend), ASP.NET MVC (frontend), and a Python utility for data population.

## Project Components

- **`MusicAppBackend/`**: The ASP.NET Core Web API that serves data to the frontend. It handles business logic, database interactions, and API endpoints for users, music, playlists, etc.
- **`MusicAppFrontend/`**: The ASP.NET MVC application that provides the user interface. Users can browse music, manage playlists, and interact with their profiles.
- **`MusicAppPopulator/`**: A Python utility (`imp.py`) to seed the database with initial data. Includes a `schema.md` detailing the database structure.

## Key Features

- User registration, login, and profile management.
- Browse and search for songs, artists, and albums.
- Create, manage, and share playlists.
- Social features like following users or artists.
- Backend API for data management and frontend interaction.

## Tech Stack

- **Backend**: C# with ASP.NET Core, Entity Framework Core, SQL Server
- **Frontend**: C# with ASP.NET MVC, Razor Views, HTML, CSS, JavaScript (likely with libraries like jQuery/Bootstrap)
- **Populator**: Python

## Getting Started

1.  **Prerequisites**:
    *   .NET SDK (version compatible with the project, likely .NET 6.0 or newer)
    *   SQL Server instance
    *   Python 3.x (for the populator)
2.  **Configuration**:
    *   Update database connection strings in `MusicAppBackend/appsettings.json` and `MusicAppFrontend/appsettings.json`.
3.  **Database Setup**:
    *   Navigate to `MusicAppBackend/` and run Entity Framework migrations: `dotnet ef database update`
4.  **Run Backend**:
    *   Navigate to `MusicAppBackend/` and run: `dotnet run`
5.  **Run Frontend**:
    *   Navigate to `MusicAppFrontend/` and run: `dotnet run`
6.  **Populate Data (Optional)**:
    *   Navigate to `MusicAppPopulator/` and run: `python imp.py` (ensure any dependencies like `pyodbc` are installed).

## Documentation

- **`ProjectMarkdownSummary.md`**: Summary of all Markdown files in the project.
- **`MusicAppBackend/BackendPrompt.md`**: Details about the backend API.
- **`MusicAppFrontend/readme.md`**: Frontend specific information.
- **`MusicAppFrontend/misc/Business_Logic_Summary.md`**: Overview of the business logic.
- **`MusicAppPopulator/schema.md`**: Database schema.
- **`MusicAppPopulator/important.md`**: Important notes for the populator.

---
This project was developed as part of the Modern Programming Language - 2 course.
Developer: Prabesh Aryal

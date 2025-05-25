import uuid

def generate_uuid():
    """Generate a UUID in the format c6ea7f12-7db0-418d-ace2-72a91fe2d8b4."""

    return str(uuid.uuid4())

print(generate_uuid())

import os
import sys
import requests
import random
import uuid
from datetime import datetime, timedelta
from pathlib import Path
import time


class MusicAppPopulator:
    def __init__(self):
        # Paths
        self.project_root = Path(__file__).parent.parent
        self.backend_uploads = (
            self.project_root / "MusicAppBackend" / "wwwroot" / "uploads"
        )

        # Image folders
        self.image_folders = {
            "profiles": self.backend_uploads / "profiles",
            "artists": self.backend_uploads / "artists",
            "albums": self.backend_uploads / "albums",
            "songs": self.backend_uploads / "songs",
            "playlists": self.backend_uploads / "playlists",
        }

        # Create directories
        self.create_directories()

        # Sample data
        self.artists_data = []
        self.albums_data = []
        self.songs_data = []
        self.users_data = []
        self.playlists_data = []

        # Picsum photo IDs (curated list for better quality)
        self.picsum_ids = list(range(1, 1085))

    def create_directories(self):
        """Create all necessary directories for image storage"""
        print("Creating directory structure...")
        for folder_name, folder_path in self.image_folders.items():
            folder_path.mkdir(parents=True, exist_ok=True)
            print(f"  Created: {folder_path}")

    def download_image(self, image_type, width=600, height=600, filename=None):
        """Download an image from Picsum Photos"""
        try:
            # Get random picsum ID
            picsum_id = random.choice(self.picsum_ids)
            url = f"https://picsum.photos/id/{picsum_id}/{width}/{height}"

            # Generate filename if not provided
            if not filename:
                filename = f"{uuid.uuid4()}.jpg"

            # Ensure .jpg extension
            if not filename.endswith(".jpg"):
                filename += ".jpg"

            # Download image
            response = requests.get(url, timeout=10)
            response.raise_for_status()

            # Save image
            file_path = self.image_folders[image_type] / filename
            with open(file_path, "wb") as f:
                f.write(response.content)

            # Return relative path for database storage
            relative_path = f"{image_type}/{filename}"
            print(f"  Downloaded: {relative_path}")
            return relative_path

        except Exception as e:
            print(f"  Error downloading image: {e}")
            return None

    def generate_artists(self, count=25):
        """Generate artist data with images"""
        print(f"\nGenerating {count} artists...")

        artist_names = [
            "The Beatles",
            "Queen",
            "Michael Jackson",
            "Madonna",
            "Elvis Presley",
            "Bob Dylan",
            "The Rolling Stones",
            "Led Zeppelin",
            "Pink Floyd",
            "David Bowie",
            "Beyoncé",
            "Taylor Swift",
            "Ed Sheeran",
            "Adele",
            "Drake",
            "Kanye West",
            "Eminem",
            "Jay-Z",
            "Rihanna",
            "Lady Gaga",
            "Bruno Mars",
            "The Weeknd",
            "Billie Eilish",
            "Ariana Grande",
            "Post Malone",
        ]

        countries = [
            "United States",
            "United Kingdom",
            "Canada",
            "Australia",
            "Germany",
            "France",
            "Italy",
            "Spain",
            "Sweden",
            "Japan",
            "South Korea",
            "Brazil",
        ]

        genres = [
            "Rock",
            "Pop",
            "Hip Hop",
            "R&B",
            "Electronic",
            "Jazz",
            "Blues",
            "Country",
            "Folk",
            "Classical",
            "Reggae",
            "Punk",
            "Metal",
            "Indie",
        ]

        for i in range(count):
            # Download artist image
            image_path = self.download_image("artists", 600, 600)

            artist = {
                "id": i + 1,
                "name": artist_names[i] if i < len(artist_names) else f"Artist {i + 1}",
                "bio": f"Bio for {artist_names[i] if i < len(artist_names) else f'Artist {i + 1}'}. A talented musician with years of experience.",
                "imageUrl": f"/uploads/{image_path}" if image_path else None,
                "country": random.choice(countries),
                "genre": random.choice(genres),
                "formedDate": datetime.now()
                - timedelta(days=random.randint(365, 365 * 30)),
                "monthlyListeners": random.randint(10000, 50000000),
                "isActive": random.choice([True, True, True, False]),  # 75% active
                "createdAt": datetime.now(),
                "updatedAt": datetime.now(),
            }

            self.artists_data.append(artist)
            time.sleep(0.1)  # Rate limiting

    def generate_albums(self, count=50):
        """Generate album data with cover images"""
        print(f"\nGenerating {count} albums...")
        album_titles = [
            "Abbey Road",
            "Dark Side of the Moon",
            "Thriller",
            "Back in Black",
            "The Wall",
            "Rumours",
            "Hotel California",
            "Led Zeppelin IV",
            "Born to Run",
            "Purple Rain",
            "OK Computer",
            "Nevermind",
            "The Joshua Tree",
            "London Calling",
            "Pet Sounds",
            "Revolver",
            "What's Going On",
            "Exile on Main St.",
            "Born in the U.S.A.",
            "Blood on the Tracks",
        ]
        for i in range(count):
            image_path = self.download_image("albums", 600, 600)
            artist_id = random.randint(1, len(self.artists_data))
            release_date = datetime.now() - timedelta(days=random.randint(30, 365 * 10))
            # Generate valid duration for SQL TIME (max 23:59:59)
            total_minutes = random.randint(30, 80)
            hours = total_minutes // 60
            minutes = total_minutes % 60
            duration = f"{hours:02d}:{minutes:02d}:{random.randint(0, 59):02d}"
            album = {
                "id": i + 1,
                "title": album_titles[i] if i < len(album_titles) else f"Album {i + 1}",
                "artistId": artist_id,
                "coverImageUrl": f"/uploads/{image_path}" if image_path else None,
                "year": release_date.year,
                "description": f"Description for album {i + 1}. A collection of amazing tracks.",
                "genre": self.artists_data[artist_id - 1]["genre"],
                "releaseDate": release_date,
                "totalTracks": random.randint(8, 16),
                "duration": duration,
                "createdAt": datetime.now(),
                "updatedAt": datetime.now(),
            }
            self.albums_data.append(album)
            time.sleep(0.05)

    def generate_songs(self, count=200):
        """Generate song data with cover images and valid AlbumId references"""
        print(f"\nGenerating {count} songs...")
        song_titles = [
            "Bohemian Rhapsody",
            "Hey Jude",
            "Like a Rolling Stone",
            "Smells Like Teen Spirit",
            "Hotel California",
            "Stairway to Heaven",
            "Billie Jean",
            "Purple Haze",
            "Good Vibrations",
            "What's Going On",
            "Respect",
            "Johnny B. Goode",
            "Hound Dog",
            "Let It Be",
            "Born to Run",
            "Imagine",
            "My Generation",
            "I Can't Get No Satisfaction",
            "Brown Sugar",
            "Layla",
        ]
        for i in range(count):
            image_path = self.download_image("songs", 600, 600)
            artist_id = random.randint(1, len(self.artists_data))
            album_id = random.randint(1, len(self.albums_data))
            # Valid SQL TIME duration (max 23:59:59)
            minutes = random.randint(2, 6)
            seconds = random.randint(0, 59)
            duration = f"00:{minutes:02d}:{seconds:02d}"
            song = {
                "id": i + 1,
                "title": song_titles[i % len(song_titles)]
                + (f" {i // len(song_titles) + 1}" if i >= len(song_titles) else ""),
                "artistId": artist_id,
                "albumId": album_id,
                "duration": duration,
                "audioUrl": None,
                "coverImageUrl": f"/uploads/{image_path}" if image_path else None,
                "trackNumber": random.randint(1, 16),
                "genre": self.artists_data[artist_id - 1]["genre"],
                "releaseDate": datetime.now()
                - timedelta(days=random.randint(30, 365 * 5)),
                "playCount": random.randint(100, 1000000),
                "createdAt": datetime.now(),
                "updatedAt": datetime.now(),
            }
            self.songs_data.append(song)
            time.sleep(0.02)

    def generate_playlists(self, count=30):
        """Generate playlist data with cover images and valid UserId references"""
        print(f"\nGenerating {count} playlists...")
        playlist_names = [
            "My Favorites",
            "Road Trip Mix",
            "Workout Playlist",
            "Chill Vibes",
            "Party Hits",
            "Study Music",
            "Late Night Jazz",
            "90s Nostalgia",
            "Rock Classics",
            "Pop Hits",
            "Summer Vibes",
            "Rainy Day Blues",
            "Feel Good Songs",
            "Throwback Thursday",
            "Indie Discoveries",
            "Electronic Dreams",
            "Country Roads",
            "Hip Hop Essentials",
        ]
        for i in range(count):
            image_path = self.download_image("playlists", 600, 600)
            user_id = random.randint(1, len(self.users_data))
            playlist = {
                "id": i + 1,
                "name": playlist_names[i % len(playlist_names)]
                + (
                    f" {i // len(playlist_names) + 1}"
                    if i >= len(playlist_names)
                    else ""
                ),
                "description": "A curated playlist of amazing songs for every occasion.",
                "userId": user_id,
                "coverImageUrl": f"/uploads/{image_path}" if image_path else None,
                "isPublic": random.choice([True, True, False]),
                "createdAt": datetime.now() - timedelta(days=random.randint(1, 180)),
                "updatedAt": datetime.now(),
            }
            self.playlists_data.append(playlist)
            time.sleep(0.05)

    def generate_sql_inserts(self):
        """Generate SQL INSERT statements with valid datetimes and durations"""
        print("\nGenerating SQL INSERT statements...")
        output_dir = Path(__file__).parent / "generated_data"
        output_dir.mkdir(exist_ok=True)
        sql_file = output_dir / "insert_data.sql"
        with open(sql_file, "w", encoding="utf-8") as f:
            f.write("-- Generated SQL INSERT statements for Music App\n")
            f.write("-- Run these after your database is created\n\n")
            # Artists
            f.write("-- Insert Artists\n")
            for artist in self.artists_data:
                formed_date = artist["formedDate"].strftime("%Y-%m-%d %H:%M:%S")
                created_at = artist["createdAt"].strftime("%Y-%m-%d %H:%M:%S")
                updated_at = artist["updatedAt"].strftime("%Y-%m-%d %H:%M:%S")
                image_url = f"'{artist['imageUrl']}'" if artist["imageUrl"] else "NULL"
                sql = f"""INSERT INTO Artists (Name, Bio, ImageUrl, Country, Genre, FormedDate, MonthlyListeners, IsActive, CreatedAt, UpdatedAt) \
VALUES ('{artist["name"].replace("'", "''")}', '{artist["bio"].replace("'", "''")}', {image_url}, '{artist["country"]}', '{artist["genre"]}', CAST('{formed_date}' AS DATETIME2), {artist["monthlyListeners"]}, {1 if artist["isActive"] else 0}, CAST('{created_at}' AS DATETIME2), CAST('{updated_at}' AS DATETIME2));\n"""
                f.write(sql)
            f.write("\n-- Insert Albums\n")
            for album in self.albums_data:
                release_date = album["releaseDate"].strftime("%Y-%m-%d %H:%M:%S")
                created_at = album["createdAt"].strftime("%Y-%m-%d %H:%M:%S")
                updated_at = album["updatedAt"].strftime("%Y-%m-%d %H:%M:%S")
                cover_image_url = (
                    f"'{album['coverImageUrl']}'" if album["coverImageUrl"] else "NULL"
                )
                sql = f"""INSERT INTO Albums (Title, ArtistId, CoverImageUrl, Year, Description, Genre, ReleaseDate, TotalTracks, Duration, CreatedAt, UpdatedAt) \
VALUES ('{album["title"].replace("'", "''")}', {album["artistId"]}, {cover_image_url}, {album["year"]}, '{album["description"].replace("'", "''")}', '{album["genre"]}', CAST('{release_date}' AS DATETIME2), {album["totalTracks"]}, '{album["duration"]}', CAST('{created_at}' AS DATETIME2), CAST('{updated_at}' AS DATETIME2));\n"""
                f.write(sql)
            f.write("\n-- Insert Songs\n")
            for song in self.songs_data:
                release_date = song["releaseDate"].strftime("%Y-%m-%d %H:%M:%S")
                created_at = song["createdAt"].strftime("%Y-%m-%d %H:%M:%S")
                updated_at = song["updatedAt"].strftime("%Y-%m-%d %H:%M:%S")
                sql = f"""INSERT INTO Songs (Title, ArtistId, AlbumId, Duration, AudioUrl, CoverImageUrl, TrackNumber, Genre, ReleaseDate, PlayCount, CreatedAt, UpdatedAt) \
VALUES ('{song["title"].replace("'", "''")}', {song["artistId"]}, {song["albumId"]}, '{song["duration"]}', NULL, '{song["coverImageUrl"] or "NULL"}', {song["trackNumber"]}, '{song["genre"]}', CAST('{release_date}' AS DATETIME2), {song["playCount"]}, CAST('{created_at}' AS DATETIME2), CAST('{updated_at}' AS DATETIME2));\n"""
                f.write(sql)
            f.write("\n-- Insert Users\n")
            for user in self.users_data:
                created_at = user["createdAt"].strftime("%Y-%m-%d %H:%M:%S")
                updated_at = user["updatedAt"].strftime("%Y-%m-%d %H:%M:%S")
                last_login = user["lastLoginAt"].strftime("%Y-%m-%d %H:%M:%S")
                sql = f"""INSERT INTO Users (Username, Email, FirstName, LastName, PasswordHash, ProfileImageUrl, CreatedAt, UpdatedAt, LastLoginAt) \
VALUES ('{user["username"]}', '{user["email"]}', '{user["firstName"]}', '{user["lastName"]}', '{user["passwordHash"]}', '{user["profileImageUrl"] or "NULL"}', CAST('{created_at}' AS DATETIME2), CAST('{updated_at}' AS DATETIME2), CAST('{last_login}' AS DATETIME2));\n"""
                f.write(sql)
            f.write("\n-- Insert Playlists\n")
            for playlist in self.playlists_data:
                created_at = playlist["createdAt"].strftime("%Y-%m-%d %H:%M:%S")
                updated_at = playlist["updatedAt"].strftime("%Y-%m-%d %H:%M:%S")
                sql = f"""INSERT INTO Playlists (Name, Description, UserId, CoverImageUrl, IsPublic, CreatedAt, UpdatedAt) \
VALUES ('{playlist["name"].replace("'", "''")}', '{playlist["description"].replace("'", "''")}', {playlist["userId"]}, '{playlist["coverImageUrl"] or "NULL"}', {1 if playlist["isPublic"] else 0}, CAST('{created_at}' AS DATETIME2), CAST('{updated_at}' AS DATETIME2));\n"""
                f.write(sql)
        print(f"  Generated: {sql_file}")

    def run(self):
        print("=== Music App Database Populator ===")
        print(f"Backend uploads directory: {self.backend_uploads}")
        try:
            self.generate_artists(25)
            self.generate_albums(50)
            self.generate_users(20)
            self.generate_playlists(30)
            self.generate_songs(200)
            self.generate_sql_inserts()
            print("\n=== Population Complete! ===")
            print("Generated:")
            print(f"  - {len(self.artists_data)} Artists")
            print(f"  - {len(self.albums_data)} Albums")
            print(f"  - {len(self.songs_data)} Songs")
            print(f"  - {len(self.users_data)} Users")
            print(f"  - {len(self.playlists_data)} Playlists")
            print(f"\nImages saved to: {self.backend_uploads}")
            print(
                f"SQL file saved to: {Path(__file__).parent / 'generated_data' / 'insert_data.sql'}"
            )
        except KeyboardInterrupt:
            print("\n\nProcess interrupted by user.")
        except Exception as e:
            print(f"\nError occurred: {e}")
            import traceback

            traceback.print_exc()

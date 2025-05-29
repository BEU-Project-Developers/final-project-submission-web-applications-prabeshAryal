using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicAppBackend.Models; // Assuming your models are in this namespace
using MusicAppBackend.Services; // Assuming your IAuthService is in this namespace

namespace MusicAppBackend.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<MusicDbContext>>();
            var context = services.GetRequiredService<MusicDbContext>();
            var authService = services.GetRequiredService<IAuthService>();

            logger.LogInformation("Starting database initialization...");

            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // Seed Roles: Only if roles table is empty.
                if (!await context.Roles.AnyAsync())
                {
                    await SeedRoles(context, logger);
                }
                else
                {
                    logger.LogInformation("Roles already exist, skipping seeding Roles.");
                }

                // Seed Users: Only if users table is empty.
                if (!await context.Users.AnyAsync())
                {
                    await SeedUsers(context, authService, logger);
                }
                else
                {
                    logger.LogInformation("Users already exist, skipping seeding Users.");
                }

                // Seed Artists, Albums, Songs:
                // These methods are designed to be idempotent when append = true.
                // They will add items from their predefined lists if they don't already exist.
                logger.LogInformation("Processing Artist seeding (append mode)...");
                await SeedArtists(context, logger, true);

                logger.LogInformation("Processing Album seeding (append mode)...");
                await SeedAlbums(context, logger, true);

                logger.LogInformation("Processing Song seeding (append mode)...");
                await SeedSongs(context, logger, true);

                // Seed Playlists: Only if playlists table is empty.
                if (!await context.Playlists.AnyAsync())
                {
                    await SeedPlaylists(context, logger);
                }
                else
                {
                    logger.LogInformation("Playlists already exist, skipping seeding Playlists.");
                }

                await transaction.CommitAsync();
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Error during database seeding. Transaction rolled back.");
                throw;
            }
        }

        private static async Task SeedRoles(MusicDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding roles...");
            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Administrator with full access" },
                new Role { Name = "User", Description = "Standard user" }
            };
            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();
            logger.LogInformation("Roles seeded.");
        }

        private static async Task SeedUsers(MusicDbContext context, IAuthService authService, ILogger logger)
        {
            logger.LogInformation("Seeding users...");
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");

            if (adminRole == null || userRole == null)
            {
                logger.LogError("Admin or User role not found. Ensure roles are seeded before users.");
                throw new InvalidOperationException("Roles not found for user seeding.");
            }

            var usersToSeed = new List<User>
            {
                new User { Username = "admin", Email = "admin@example.com", FirstName = "Admin", LastName = "User", PasswordHash = authService.HashPassword("Admin@123"), CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow },
                new User { Username = "user", Email = "user@example.com", FirstName = "Regular", LastName = "User", PasswordHash = authService.HashPassword("User@123"), CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow }
            };

            foreach (var userSeed in usersToSeed)
            {
                // Check if user already exists before adding
                if (!await context.Users.AnyAsync(u => u.Username == userSeed.Username))
                {
                    context.Users.Add(userSeed);
                    await context.SaveChangesAsync(); // Save user to get Id

                    var roleToAssign = userSeed.Username == "admin" ? adminRole : userRole;
                    // Check if UserRole mapping already exists (though unlikely if user is new)
                    if (!await context.UserRoles.AnyAsync(ur => ur.UserId == userSeed.Id && ur.RoleId == roleToAssign.Id))
                    {
                        context.UserRoles.Add(new UserRole { UserId = userSeed.Id, RoleId = roleToAssign.Id });
                    }
                }
            }
            await context.SaveChangesAsync(); // Save UserRoles
            logger.LogInformation("Users seeded.");
        }

        private static async Task SeedArtists(MusicDbContext context, ILogger logger, bool append = false)
        {
            // This check ensures that if append is false, we only seed if the table is entirely empty.
            // If append is true, this check is bypassed, and we proceed to add missing items.
            if (!append && await context.Artists.AnyAsync())
            {
                logger.LogInformation("Artists already exist and append mode is off. Skipping artist seeding.");
                return;
            }
            logger.LogInformation("Seeding artists (append mode: {Append})...", append);

            var artistsData = new List<Artist>
            {
                // From previous seeding
                new Artist { Name = "Sajjan Raj Vaidya", Bio = "Nepali singer, songwriter, known for soulful melodies.", Country = "Nepal", Genre = "Nepali Pop, Indie", FormedDate = new DateTime(2014, 1, 1), MonthlyListeners = 550000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Arijit Singh", Bio = "Indian singer and music composer.", Country = "India", Genre = "Bollywood, Indian Pop", FormedDate = new DateTime(2005, 1, 1), MonthlyListeners = 31000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Ed Sheeran", Bio = "English singer-songwriter.", Country = "United Kingdom", Genre = "Pop, Folk-Pop", FormedDate = new DateTime(2004, 1, 1), MonthlyListeners = 72000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                // New artists
                new Artist { Name = "Taylor Swift", Bio = "American singer-songwriter.", Country = "USA", Genre = "Pop, Country, Electropop", FormedDate = new DateTime(2004, 1, 1), MonthlyListeners = 80000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Mahesh Kafle", Bio = "Nepali singer.", Country = "Nepal", Genre = "Nepali Lok Pop", FormedDate = new DateTime(2015, 1, 1), MonthlyListeners = 200000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Melina Rai", Bio = "Nepali singer.", Country = "Nepal", Genre = "Nepali Pop, Playback Singer", FormedDate = new DateTime(2010, 1, 1), MonthlyListeners = 300000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Tribal Rain", Bio = "Nepali band known for their folk rock music.", Country = "Nepal", Genre = "Nepali Folk Rock", FormedDate = new DateTime(2013, 1, 1), MonthlyListeners = 150000, IsActive = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }, // Corrected IsActive based on comment.
                new Artist { Name = "Olivia Rodrigo", Bio = "American singer-songwriter and actress.", Country = "USA", Genre = "Pop, Pop-Rock, Alternative Pop", FormedDate = new DateTime(2020, 1, 1), MonthlyListeners = 40000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Sushant Ghimire (Vyoma)", Bio = "Nepali singer-songwriter and musician.", Country = "Nepal", Genre = "Nepali Pop, Indie Folk", FormedDate = new DateTime(2018, 1, 1), MonthlyListeners = 100000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Sushant KC", Bio = "Nepali singer, songwriter, and composer.", Country = "Nepal", Genre = "Nepali Pop, R&B", FormedDate = new DateTime(2016, 1, 1), MonthlyListeners = 400000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Maroon 5", Bio = "American pop rock band.", Country = "USA", Genre = "Pop Rock, Pop, Funk Rock", FormedDate = new DateTime(1994, 1, 1), MonthlyListeners = 50000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "The Chainsmokers", Bio = "American electronic DJ and production duo.", Country = "USA", Genre = "EDM, Pop, Electropop", FormedDate = new DateTime(2012, 1, 1), MonthlyListeners = 35000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Artist { Name = "Halsey", Bio = "American singer and songwriter.", Country = "USA", Genre = "Pop, Electropop, Alternative Pop", FormedDate = new DateTime(2012, 1, 1), MonthlyListeners = 30000000, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            };

            int newArtistsAdded = 0;
            foreach (var artistData in artistsData)
            {
                if (!await context.Artists.AnyAsync(a => a.Name == artistData.Name))
                {
                    context.Artists.Add(artistData);
                    newArtistsAdded++;
                }
            }

            if (newArtistsAdded > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} new artists seeded.", newArtistsAdded);
            }
            else
            {
                logger.LogInformation("No new artists to seed from the predefined list. All listed artists already exist or list is empty.");
            }
        }

        private static async Task SeedAlbums(MusicDbContext context, ILogger logger, bool append = false)
        {
            if (!append && await context.Albums.AnyAsync())
            {
                logger.LogInformation("Albums already exist and append mode is off. Skipping album seeding.");
                return;
            }
            logger.LogInformation("Seeding albums (append mode: {Append})...", append);

            // Ensure artists are loaded to link albums correctly
            var artists = await context.Artists.ToDictionaryAsync(a => a.Name, a => a.Id);
            var albumsData = new List<Album>();

            void AddAlbum(string artistName, string title, int year, string desc, string genre, DateTime releaseDate, int tracks, double durationMinutes, string albumArtistGenreFallback)
            {
                if (artists.TryGetValue(artistName, out var artistId))
                {
                    albumsData.Add(new Album { Title = title, ArtistId = artistId, Year = year, Description = desc, Genre = genre ?? albumArtistGenreFallback, ReleaseDate = releaseDate, TotalTracks = tracks, Duration = TimeSpan.FromMinutes(durationMinutes), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
                else { logger.LogWarning("Artist '{ArtistName}' not found for album '{Title}'. Album not added.", artistName, title); }
            }

            AddAlbum("Sajjan Raj Vaidya", "Singles Collection (Sajjan Raj Vaidya)", DateTime.UtcNow.Year, "Popular singles by Sajjan Raj Vaidya.", "Nepali Pop, Indie", new DateTime(2023, 1, 1), 5, 22, "Nepali Pop");
            AddAlbum("Arijit Singh", "Aashiqui 2 (Soundtrack)", 2013, "Soundtrack of Bollywood movie Aashiqui 2.", "Bollywood Soundtrack", new DateTime(2013, 4, 6), 11, 58, "Bollywood");
            AddAlbum("Ed Sheeran", "÷ (Divide)", 2017, "Third studio album by Ed Sheeran.", "Pop", new DateTime(2017, 3, 3), 16, 59, "Pop");
            AddAlbum("Taylor Swift", "1989", 2014, "Fifth studio album by Taylor Swift.", "Synth-pop", new DateTime(2014, 10, 27), 13, 48, "Pop");
            AddAlbum("Olivia Rodrigo", "SOUR", 2021, "Debut studio album by Olivia Rodrigo.", "Pop, Pop-Rock", new DateTime(2021, 5, 21), 11, 34, "Pop");
            AddAlbum("Tribal Rain", "Roka Yo Samay", 2017, "Debut album by Tribal Rain.", "Nepali Folk Rock", new DateTime(2017, 4, 1), 10, 45, "Nepali Folk Rock");
            AddAlbum("Maroon 5", "Jordi", 2021, "Seventh studio album by Maroon 5.", "Pop", new DateTime(2021, 6, 11), 14, 43, "Pop Rock");
            AddAlbum("The Chainsmokers", "Collage (EP)", 2016, "Second EP by The Chainsmokers.", "EDM, Future Bass", new DateTime(2016, 11, 4), 5, 19, "EDM");

            int newAlbumsAdded = 0;
            foreach (var albumData in albumsData)
            {
                if (!await context.Albums.AnyAsync(a => a.Title == albumData.Title && a.ArtistId == albumData.ArtistId))
                {
                    context.Albums.Add(albumData);
                    newAlbumsAdded++;
                }
            }
            if (newAlbumsAdded > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} new albums seeded.", newAlbumsAdded);
            }
            else
            {
                logger.LogInformation("No new albums to seed from the predefined list. All listed albums already exist or list is empty/artists not found.");
            }
        }

        private static async Task SeedSongs(MusicDbContext context, ILogger logger, bool append = false)
        {
            if (!append && await context.Songs.AnyAsync())
            {
                logger.LogInformation("Songs already exist and append mode is off. Skipping song seeding.");
                return;
            }
            logger.LogInformation("Seeding songs (append mode: {Append})...", append);
            var random = new Random();

            var artistDict = await context.Artists.ToDictionaryAsync(a => a.Name, a => a.Id);
            // Load albums with their artists to correctly create the lookup key
            var albumList = await context.Albums
                                    .Include(al => al.Artist)
                                    .ToListAsync();
            var albumLookup = albumList.Where(al => al.Artist != null) // Ensure artist is not null
                                     .ToDictionary(al => $"{al.Title} - {al.Artist.Name}", al => al.Id);

            var songsData = new List<Song>();

            void AddSong(string title, string artistName, string albumTitleAndArtistKey, double durationMinutes, int trackNo, string genre, DateTime releaseDate, int playCountMin = 500, int playCountMax = 50000)
            {
                if (artistDict.TryGetValue(artistName, out var artistId))
                {
                    int? albumId = null;
                    if (!string.IsNullOrEmpty(albumTitleAndArtistKey) && albumLookup.TryGetValue(albumTitleAndArtistKey, out var foundAlbumId))
                    {
                        albumId = foundAlbumId;
                    }
                    else if (!string.IsNullOrEmpty(albumTitleAndArtistKey))
                    {
                        logger.LogWarning("Album key '{AlbumKey}' not found for song '{SongTitle}'. Song will be added as a single.", albumTitleAndArtistKey, title);
                    }
                    songsData.Add(new Song { Title = title, ArtistId = artistId, AlbumId = albumId, Duration = TimeSpan.FromMinutes(durationMinutes), TrackNumber = trackNo, Genre = genre, ReleaseDate = releaseDate, PlayCount = random.Next(playCountMin, playCountMax), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
                else { logger.LogWarning("Artist '{ArtistName}' not found for song '{SongTitle}'. Song not added.", artistName, title); }
            }

            AddSong("Hataarindai, Bataasindai", "Sajjan Raj Vaidya", "Singles Collection (Sajjan Raj Vaidya) - Sajjan Raj Vaidya", 4.5, 1, "Nepali Pop", new DateTime(2018, 1, 1), 10000, 70000);
            AddSong("Chitthi Bhitra", "Sajjan Raj Vaidya", "Singles Collection (Sajjan Raj Vaidya) - Sajjan Raj Vaidya", 5.2, 2, "Nepali Pop", new DateTime(2019, 1, 1), 8000, 60000);
            AddSong("Sasto Mutu", "Sajjan Raj Vaidya", "Singles Collection (Sajjan Raj Vaidya) - Sajjan Raj Vaidya", 4.1, 3, "Nepali Pop", new DateTime(2022, 1, 1), 12000, 80000);
            AddSong("Tum Hi Ho", "Arijit Singh", "Aashiqui 2 (Soundtrack) - Arijit Singh", 4.37, 1, "Bollywood", new DateTime(2013, 4, 6), 500000, 2500000);
            AddSong("Channa Mereya", "Arijit Singh", null, 4.82, 1, "Bollywood", new DateTime(2016, 9, 29), 400000, 2000000);
            AddSong("Kesariya", "Arijit Singh", null, 4.47, 1, "Bollywood", new DateTime(2022, 7, 17), 300000, 1800000);
            AddSong("Shape of You", "Ed Sheeran", "÷ (Divide) - Ed Sheeran", 3.89, 4, "Pop", new DateTime(2017, 1, 6), 1000000, 7000000);
            AddSong("Perfect", "Ed Sheeran", "÷ (Divide) - Ed Sheeran", 4.39, 5, "Pop", new DateTime(2017, 3, 3), 800000, 6000000);
            AddSong("Castle on the Hill", "Ed Sheeran", "÷ (Divide) - Ed Sheeran", 4.35, 2, "Pop", new DateTime(2017, 1, 6), 700000, 5000000);
            AddSong("Blank Space", "Taylor Swift", "1989 - Taylor Swift", 3.85, 2, "Electropop", new DateTime(2014, 11, 10), 900000, 6000000);
            AddSong("Nacha Firiri", "Mahesh Kafle", null, 4.5, 1, "Nepali Lok Pop", new DateTime(2021, 3, 1), 50000, 300000);
            AddSong("Junna Hery", "Tribal Rain", "Roka Yo Samay - Tribal Rain", 5.08, 3, "Nepali Folk Rock", new DateTime(2017, 4, 1), 30000, 200000);
            AddSong("Happier", "Olivia Rodrigo", "SOUR - Olivia Rodrigo", 2.92, 6, "Pop Ballad", new DateTime(2021, 5, 21), 700000, 4000000);
            AddSong("Feri Feri", "Sushant Ghimire (Vyoma)", null, 3.5, 1, "Nepali Pop/Indie", new DateTime(2023, 5, 10), 20000, 150000);
            AddSong("Bardali", "Sushant KC", null, 3.75, 1, "Nepali Pop/R&B", new DateTime(2023, 2, 15), 40000, 250000);
            AddSong("Memories", "Maroon 5", "Jordi - Maroon 5", 3.15, 1, "Pop", new DateTime(2019, 9, 20), 800000, 5000000);
            AddSong("Closer", "The Chainsmokers", "Collage (EP) - The Chainsmokers", 4.07, 1, "EDM/Pop", new DateTime(2016, 7, 29), 900000, 6000000);

            int newSongsAdded = 0;
            foreach (var songData in songsData)
            {
                if (!await context.Songs.AnyAsync(s => s.Title == songData.Title && s.ArtistId == songData.ArtistId))
                {
                    context.Songs.Add(songData);
                    newSongsAdded++;
                }
            }

            if (newSongsAdded > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("{Count} new songs seeded.", newSongsAdded);
            }
            else
            {
                logger.LogInformation("No new songs to seed from the predefined list. All listed songs already exist or list is empty/artists or albums not found.");
            }
        }

        private static async Task SeedPlaylists(MusicDbContext context, ILogger logger)
        {
            logger.LogInformation("Seeding playlists...");
            var regularUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "user");

            if (regularUser != null)
            {
                // Check if this specific playlist already exists for this user
                if (!await context.Playlists.AnyAsync(p => p.Name == "My Ultimate Mix" && p.UserId == regularUser.Id))
                {
                    var mixedPlaylist = new Playlist
                    {
                        Name = "My Ultimate Mix",
                        Description = "A collection of Nepali, Hindi, and English favorites.",
                        UserId = regularUser.Id,
                        IsPublic = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Playlists.Add(mixedPlaylist);
                    await context.SaveChangesAsync(); // Save playlist to get Id

                    var songsForPlaylist = await context.Songs
                                                .OrderByDescending(s => s.PlayCount)
                                                .Take(7)
                                                .ToListAsync();

                    if (songsForPlaylist.Any())
                    {
                        int order = 0;
                        var playlistSongsToAdd = new List<PlaylistSong>();
                        foreach (var song in songsForPlaylist)
                        {
                            // Check if song is already in this playlist (though unlikely for a new playlist)
                            if (!await context.PlaylistSongs.AnyAsync(ps => ps.PlaylistId == mixedPlaylist.Id && ps.SongId == song.Id))
                            {
                                playlistSongsToAdd.Add(new PlaylistSong
                                {
                                    PlaylistId = mixedPlaylist.Id,
                                    SongId = song.Id,
                                    Order = order++,
                                    AddedAt = DateTime.UtcNow
                                });
                            }
                        }
                        if (playlistSongsToAdd.Any())
                        {
                            await context.PlaylistSongs.AddRangeAsync(playlistSongsToAdd);
                            await context.SaveChangesAsync();
                        }
                        logger.LogInformation("Playlist 'My Ultimate Mix' seeded with songs for user {UserId}.", regularUser.Id);
                    }
                    else { logger.LogWarning("No songs found to add to 'My Ultimate Mix' playlist."); }
                }
                else
                {
                    logger.LogInformation("Playlist 'My Ultimate Mix' already exists for user {UserId}.", regularUser.Id);
                }
            }            else { logger.LogWarning("Regular user not found when seeding playlists."); }
            logger.LogInformation("Playlists seeding attempt finished.");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicAppBackend.Models;
using MusicAppBackend.Services;

namespace MusicAppBackend.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<MusicDbContext>>();

            try
            {
                var context = services.GetRequiredService<MusicDbContext>();
                var authService = services.GetRequiredService<IAuthService>();

                logger.LogInformation("Starting database initialization...");

                // Use transactions to ensure data integrity
                using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    // Seed roles if they don't exist
                    if (!await context.Roles.AnyAsync())
                    {
                        await SeedRoles(context, logger);
                    }
                    else
                    {
                        logger.LogInformation("Roles already exist, skipping seeding.");
                    }

                    // Seed users if they don't exist
                    if (!await context.Users.AnyAsync())
                    {
                        await SeedUsers(context, authService, logger);
                    }
                    else
                    {
                        logger.LogInformation("Users already exist, skipping seeding.");
                    }

                    // Seed artists if they don't exist
                    if (!await context.Artists.AnyAsync())
                    {
                        await SeedArtists(context, logger);
                    }
                    else
                    {
                        logger.LogInformation("Artists already exist, skipping seeding.");
                    }

                    // Seed albums if they don't exist
                    if (!await context.Albums.AnyAsync())
                    {
                        await SeedAlbums(context, logger);
                    }
                    else
                    {
                        logger.LogInformation("Albums already exist, skipping seeding.");
                    }

                    // Seed songs if they don't exist
                    if (!await context.Songs.AnyAsync())
                    {
                        await SeedSongs(context, logger);
                    }
                    else
                    {
                        logger.LogInformation("Songs already exist, skipping seeding.");
                    }

                    // Seed playlists if they don't exist
                    if (!await context.Playlists.AnyAsync())
                    {
                        await SeedPlaylists(context, logger);
                    }
                    else
                    {
                        logger.LogInformation("Playlists already exist, skipping seeding.");
                    }

                    // Commit the transaction
                    await transaction.CommitAsync();
                    logger.LogInformation("Database initialization completed successfully.");
                }
                catch (Exception ex)
                {
                    // If any seeding operation fails, roll back the entire transaction
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Error during database seeding. Transaction rolled back.");
                    throw; // Re-throw to be caught by the outer try-catch
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                // We allow the application to continue running even if seeding fails
            }
        }

        private static async Task SeedRoles(MusicDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding roles...");

                var roles = new List<Role>
                {
                    new Role { Name = "Admin", Description = "Administrator with full access" },
                    new Role { Name = "User", Description = "Standard user" }
                };

                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();

                logger.LogInformation("Roles seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding roles.");
                throw;
            }
        }

        private static async Task SeedUsers(MusicDbContext context, IAuthService authService, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding users...");

                // Create admin user
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    PasswordHash = authService.HashPassword("Admin@123"),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                // Assign admin role
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    });
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Admin role not found when seeding users.");
                }

                // Create regular user
                var regularUser = new User
                {
                    Username = "user",
                    Email = "user@example.com",
                    FirstName = "Regular",
                    LastName = "User",
                    PasswordHash = authService.HashPassword("User@123"),
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                context.Users.Add(regularUser);
                await context.SaveChangesAsync();

                // Assign user role
                var userRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (userRole != null)
                {
                    context.UserRoles.Add(new UserRole
                    {
                        UserId = regularUser.Id,
                        RoleId = userRole.Id
                    });
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("User role not found when seeding users.");
                }

                logger.LogInformation("Users seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding users.");
                throw;
            }
        }

        private static async Task SeedArtists(MusicDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding artists...");

                var artists = new List<Artist>
                {
                    new Artist
                    {
                        Name = "The Beatles",
                        Bio = "The Beatles were an English rock band formed in Liverpool in 1960.",
                        Country = "United Kingdom",
                        Genre = "Rock",
                        FormedDate = new DateTime(1960, 1, 1),
                        MonthlyListeners = 20000000,
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Artist
                    {
                        Name = "Queen",
                        Bio = "Queen are a British rock band formed in London in 1970.",
                        Country = "United Kingdom",
                        Genre = "Rock",
                        FormedDate = new DateTime(1970, 1, 1),
                        MonthlyListeners = 18000000,
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Artist
                    {
                        Name = "Michael Jackson",
                        Bio = "Michael Joseph Jackson was an American singer, songwriter, and dancer.",
                        Country = "United States",
                        Genre = "Pop",
                        FormedDate = new DateTime(1964, 1, 1),
                        MonthlyListeners = 15000000,
                        IsActive = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                await context.Artists.AddRangeAsync(artists);
                await context.SaveChangesAsync();

                logger.LogInformation("Artists seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding artists.");
                throw;
            }
        }

        private static async Task SeedAlbums(MusicDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding albums...");

                var beatles = await context.Artists.FirstOrDefaultAsync(a => a.Name == "The Beatles");
                var queen = await context.Artists.FirstOrDefaultAsync(a => a.Name == "Queen");
                var michaelJackson = await context.Artists.FirstOrDefaultAsync(a => a.Name == "Michael Jackson");

                if (beatles != null)
                {
                    var beatlesAlbums = new List<Album>
                    {
                        new Album
                        {
                            Title = "Abbey Road",
                            ArtistId = beatles.Id,
                            Year = 1969,
                            Description = "Abbey Road is the eleventh studio album by English rock band the Beatles.",
                            Genre = "Rock",
                            ReleaseDate = new DateTime(1969, 9, 26),
                            TotalTracks = 17,
                            Duration = TimeSpan.FromMinutes(47.5),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Album
                        {
                            Title = "Let It Be",
                            ArtistId = beatles.Id,
                            Year = 1970,
                            Description = "Let It Be is the twelfth and final studio album by English rock band the Beatles.",
                            Genre = "Rock",
                            ReleaseDate = new DateTime(1970, 5, 8),
                            TotalTracks = 12,
                            Duration = TimeSpan.FromMinutes(35.1),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Albums.AddRangeAsync(beatlesAlbums);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("The Beatles artist not found when seeding albums.");
                }

                if (queen != null)
                {
                    var queenAlbums = new List<Album>
                    {
                        new Album
                        {
                            Title = "A Night at the Opera",
                            ArtistId = queen.Id,
                            Year = 1975,
                            Description = "A Night at the Opera is the fourth studio album by the British rock band Queen.",
                            Genre = "Rock",
                            ReleaseDate = new DateTime(1975, 11, 21),
                            TotalTracks = 12,
                            Duration = TimeSpan.FromMinutes(43.08),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Albums.AddRangeAsync(queenAlbums);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Queen artist not found when seeding albums.");
                }

                if (michaelJackson != null)
                {
                    var mjAlbums = new List<Album>
                    {
                        new Album
                        {
                            Title = "Thriller",
                            ArtistId = michaelJackson.Id,
                            Year = 1982,
                            Description = "Thriller is the sixth studio album by American singer Michael Jackson.",
                            Genre = "Pop",
                            ReleaseDate = new DateTime(1982, 11, 30),
                            TotalTracks = 9,
                            Duration = TimeSpan.FromMinutes(42.19),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Albums.AddRangeAsync(mjAlbums);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Michael Jackson artist not found when seeding albums.");
                }

                logger.LogInformation("Albums seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding albums.");
                throw;
            }
        }

        private static async Task SeedSongs(MusicDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding songs...");

                var abbeyRoad = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Abbey Road");
                var letItBe = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Let It Be");
                var nightAtTheOpera = await context.Albums.FirstOrDefaultAsync(a => a.Title == "A Night at the Opera");
                var thriller = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Thriller");

                var beatles = await context.Artists.FirstOrDefaultAsync(a => a.Name == "The Beatles");
                var queen = await context.Artists.FirstOrDefaultAsync(a => a.Name == "Queen");
                var michaelJackson = await context.Artists.FirstOrDefaultAsync(a => a.Name == "Michael Jackson");

                if (abbeyRoad != null && beatles != null)
                {
                    var abbeyRoadSongs = new List<Song>
                    {
                        new Song
                        {
                            Title = "Come Together",
                            ArtistId = beatles.Id,
                            AlbumId = abbeyRoad.Id,
                            Duration = TimeSpan.FromMinutes(4.2),
                            TrackNumber = 1,
                            Genre = "Rock",
                            ReleaseDate = abbeyRoad.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Song
                        {
                            Title = "Something",
                            ArtistId = beatles.Id,
                            AlbumId = abbeyRoad.Id,
                            Duration = TimeSpan.FromMinutes(3.03),
                            TrackNumber = 2,
                            Genre = "Rock",
                            ReleaseDate = abbeyRoad.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Songs.AddRangeAsync(abbeyRoadSongs);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Abbey Road album or The Beatles artist not found when seeding songs.");
                }

                if (letItBe != null && beatles != null)
                {
                    var letItBeSongs = new List<Song>
                    {
                        new Song
                        {
                            Title = "Let It Be",
                            ArtistId = beatles.Id,
                            AlbumId = letItBe.Id,
                            Duration = TimeSpan.FromMinutes(4.03),
                            TrackNumber = 6,
                            Genre = "Rock",
                            ReleaseDate = letItBe.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Song
                        {
                            Title = "The Long and Winding Road",
                            ArtistId = beatles.Id,
                            AlbumId = letItBe.Id,
                            Duration = TimeSpan.FromMinutes(3.38),
                            TrackNumber = 9,
                            Genre = "Rock",
                            ReleaseDate = letItBe.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Songs.AddRangeAsync(letItBeSongs);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Let It Be album or The Beatles artist not found when seeding songs.");
                }

                if (nightAtTheOpera != null && queen != null)
                {
                    var queenSongs = new List<Song>
                    {
                        new Song
                        {
                            Title = "Bohemian Rhapsody",
                            ArtistId = queen.Id,
                            AlbumId = nightAtTheOpera.Id,
                            Duration = TimeSpan.FromMinutes(5.55),
                            TrackNumber = 11,
                            Genre = "Rock",
                            ReleaseDate = nightAtTheOpera.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Song
                        {
                            Title = "Love of My Life",
                            ArtistId = queen.Id,
                            AlbumId = nightAtTheOpera.Id,
                            Duration = TimeSpan.FromMinutes(3.38),
                            TrackNumber = 9,
                            Genre = "Rock",
                            ReleaseDate = nightAtTheOpera.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Songs.AddRangeAsync(queenSongs);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("A Night at the Opera album or Queen artist not found when seeding songs.");
                }

                if (thriller != null && michaelJackson != null)
                {
                    var thrillerSongs = new List<Song>
                    {
                        new Song
                        {
                            Title = "Thriller",
                            ArtistId = michaelJackson.Id,
                            AlbumId = thriller.Id,
                            Duration = TimeSpan.FromMinutes(5.57),
                            TrackNumber = 4,
                            Genre = "Pop",
                            ReleaseDate = thriller.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new Song
                        {
                            Title = "Billie Jean",
                            ArtistId = michaelJackson.Id,
                            AlbumId = thriller.Id,
                            Duration = TimeSpan.FromMinutes(4.54),
                            TrackNumber = 6,
                            Genre = "Pop",
                            ReleaseDate = thriller.ReleaseDate,
                            PlayCount = new Random().Next(1000, 10000),
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        }
                    };

                    await context.Songs.AddRangeAsync(thrillerSongs);
                    await context.SaveChangesAsync();
                }
                else
                {
                    logger.LogWarning("Thriller album or Michael Jackson artist not found when seeding songs.");
                }

                logger.LogInformation("Songs seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding songs.");
                throw;
            }
        }

        private static async Task SeedPlaylists(MusicDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Seeding playlists...");

                var regularUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "user");

                if (regularUser != null)
                {
                    var rockClassics = new Playlist
                    {
                        Name = "Rock Classics",
                        Description = "A collection of classic rock songs",
                        UserId = regularUser.Id,
                        IsPublic = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Playlists.Add(rockClassics);
                    await context.SaveChangesAsync();

                    // Add songs to playlist
                    var songs = await context.Songs
                        .Where(s => s.Genre == "Rock")
                        .ToListAsync();

                    if (songs.Any())
                    {
                        int order = 0;
                        foreach (var song in songs)
                        {
                            context.PlaylistSongs.Add(new PlaylistSong
                            {
                                PlaylistId = rockClassics.Id,
                                SongId = song.Id,
                                Order = order++,
                                AddedAt = DateTime.UtcNow
                            });
                        }

                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        logger.LogWarning("No rock songs found when seeding playlists.");
                    }
                }
                else
                {
                    logger.LogWarning("Regular user not found when seeding playlists.");
                }

                logger.LogInformation("Playlists seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding playlists.");
                throw;
            }
        }
    }
}
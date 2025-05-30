using System;
using System.Collections.Generic;

namespace MusicApp.ViewModels
{
    public class ListeningHistoryViewModel
    {
        public List<ListeningHistoryItem> History { get; set; } = new List<ListeningHistoryItem>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public class ListeningHistoryItem
        {
            public SongInfo Song { get; set; } = new SongInfo();
            public DateTime PlayedAt { get; set; }
            public TimeSpan? ListenDuration { get; set; }
            
            public string FormattedPlayedAt
            {
                get
                {
                    var now = DateTime.UtcNow;
                    var timeAgo = now - PlayedAt;

                    if (timeAgo.TotalMinutes < 1)
                        return "Just now";
                    if (timeAgo.TotalMinutes < 60)
                        return $"{(int)timeAgo.TotalMinutes} minutes ago";
                    if (timeAgo.TotalHours < 24)
                        return $"{(int)timeAgo.TotalHours} hours ago";
                    if (timeAgo.TotalDays < 7)
                        return $"{(int)timeAgo.TotalDays} days ago";
                    
                    return PlayedAt.ToString("MMM dd, yyyy");
                }
            }

            public string FormattedListenDuration
            {
                get
                {
                    if (!ListenDuration.HasValue)
                        return "--:--";
                    
                    var duration = ListenDuration.Value;
                    if (duration.TotalHours >= 1)
                        return duration.ToString(@"h\:mm\:ss");
                    else
                        return duration.ToString(@"mm\:ss");
                }
            }

            public double ListenPercentage
            {
                get
                {
                    if (!ListenDuration.HasValue || !Song.Duration.HasValue || Song.Duration.Value.TotalSeconds == 0)
                        return 0;
                    
                    return Math.Min(100, (ListenDuration.Value.TotalSeconds / Song.Duration.Value.TotalSeconds) * 100);
                }
            }
        }

        public class SongInfo
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;
            public string? Album { get; set; }
            public string? Genre { get; set; }
            public TimeSpan? Duration { get; set; }
            public string CoverImageUrl { get; set; } = "/images/default-cover.png";

            public string FormattedDuration
            {
                get
                {
                    if (!Duration.HasValue)
                        return "--:--";
                    
                    if (Duration.Value.TotalHours >= 1)
                        return Duration.Value.ToString(@"h\:mm\:ss");
                    else
                        return Duration.Value.ToString(@"mm\:ss");
                }
            }
        }
    }
}

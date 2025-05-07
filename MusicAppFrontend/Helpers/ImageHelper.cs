using System;

namespace MusicApp.Helpers
{
    public static class ImageHelper
    {
        private static readonly Random Random = new Random();

        public static string GetRandomImageUrl(int width, int height)
        {
            int randomId = Random.Next(1, 1085);
            return $"https://picsum.photos/id/{randomId}/{width}/{height}";
        }

        public static string GetFallbackImageUrl(string type, int width = 300, int height = 300)
        {
            return type.ToLower() switch
            {
                "profile" => "/assets/default-profile.png",
                "artist" => $"https://placehold.co/{width}x{height}/212121/AAAAAA?text=Artist",
                "album" => $"https://placehold.co/{width}x{height}/212121/AAAAAA?text=Album",
                "song" => $"https://placehold.co/{width}x{height}/212121/AAAAAA?text=Song",
                "playlist" => $"https://placehold.co/{width}x{height}/212121/AAAAAA?text=Playlist",
                _ => GetRandomImageUrl(width, height)
            };
        }

        public static string GetImageUrl(string url, string type, int width = 300, int height = 300)
        {
            if (string.IsNullOrEmpty(url))
            {
                return type == "profile" ? GetFallbackImageUrl(type) : GetRandomImageUrl(width, height);
            }
            return url;
        }
    }
}
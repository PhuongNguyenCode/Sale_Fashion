namespace FashionStore.Helpers
{
    /// <summary>
    /// View-only fashion photography. Local /images paths in the seed data are unused files.
    /// </summary>
    public static class EditorialMedia
    {
        private static readonly string[] Looks =
        {
            "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1483985988355-763728e1935b?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1496747611176-843222e1e57c?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1488161628813-04466f872be2?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1539109136881-3be0616acf4b?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1509631179647-0177331693ae?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1469334031218-e382a71b716b?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1552374196-1ab2a1c593e8?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1490481651871-ab68de25d43d?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1445205170230-053b83016050?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1529139574466-a262c2b4d1d8?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1517841905240-472988babdf9?q=80&w=1200&auto=format&fit=crop",
            "https://images.unsplash.com/photo-1479064555552-3ef4979f8908?q=80&w=1200&auto=format&fit=crop"
        };

        public static string Product(int productId, string? storedUrl = null)
        {
            // The old seeded Polo image points to a missing remote photo. Keep any
            // replacement uploaded by an admin, but use the local catalogue image
            // until that happens.
            if (productId == 2 && (string.IsNullOrWhiteSpace(storedUrl) ||
                storedUrl.Contains("photo-1625910513413-5fc68c1c7f31", StringComparison.OrdinalIgnoreCase)))
            {
                return "/images/products/ao-polo-pique-classic.webp";
            }

            // Keep both remote editorial images and images uploaded to wwwroot.
            // Uploaded files use root-relative paths such as /images/products/..., which
            // previously fell through to a random editorial fallback.
            if (!string.IsNullOrWhiteSpace(storedUrl) &&
                (storedUrl.StartsWith("/", StringComparison.Ordinal) ||
                 storedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 storedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
            {
                return storedUrl;
            }

            return Looks[Math.Abs(productId) % Looks.Length];
        }

        public static string Hover(int productId)
        {
            return Looks[(Math.Abs(productId) + 5) % Looks.Length];
        }
    }
}

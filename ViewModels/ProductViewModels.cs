using FashionStore.Models;

namespace FashionStore.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> FeaturedCategories { get; set; } = new();
        public List<ProductCardViewModel> NewArrivals { get; set; } = new();
        public List<ProductCardViewModel> BestSellers { get; set; } = new();
        public List<ProductCardViewModel> SaleProducts { get; set; } = new();
        public int MaxSaleDiscountPercent { get; set; }
        public List<Brand> FeaturedBrands { get; set; } = new();
    }

    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? MainImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsInWishlist { get; set; }

        public decimal DisplayPrice => SalePrice ?? Price;
        public int? DiscountPercent => SalePrice.HasValue && Price > 0
            ? (int)Math.Round((1 - SalePrice.Value / Price) * 100) : null;
    }

    public class ProductListViewModel
    {
        public List<ProductCardViewModel> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Size> Sizes { get; set; } = new();
        public List<Color> Colors { get; set; } = new();

        // Filter state
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? SizeId { get; set; }
        public int? ColorId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool OnSale { get; set; }
        public string? SearchQuery { get; set; }
        public string SortBy { get; set; } = "newest";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 12;

        // Category/Brand name for breadcrumb
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
    }

    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public List<ProductImage> Images { get; set; } = new();
        public List<Size> AvailableSizes { get; set; } = new();
        public List<Color> AvailableColors { get; set; } = new();
        public List<ProductVariant> Variants { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
        public List<ProductCardViewModel> RelatedProducts { get; set; } = new();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public bool IsInWishlist { get; set; }
        public bool CanReview { get; set; }
    }
}

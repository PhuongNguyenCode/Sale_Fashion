using Microsoft.EntityFrameworkCore;
using FashionStore.Models;

namespace FashionStore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            modelBuilder.Entity<Brand>().HasIndex(b => b.Slug).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
            modelBuilder.Entity<Coupon>().HasIndex(c => c.CouponCode).IsUnique();

            // Composite unique: ProductVariant (ProductId + SizeId + ColorId)
            modelBuilder.Entity<ProductVariant>()
                .HasIndex(pv => new { pv.ProductId, pv.SizeId, pv.ColorId })
                .IsUnique();

            // 1:1 User -> Cart
            modelBuilder.Entity<User>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<Cart>(c => c.UserId)
                .IsRequired(false);

            modelBuilder.Entity<Cart>()
                .HasIndex(c => c.SessionId)
                .IsUnique()
                .HasFilter("[SessionId] IS NOT NULL");

            // 1:1 User -> Wishlist
            modelBuilder.Entity<User>()
                .HasOne(u => u.Wishlist)
                .WithOne(w => w.User)
                .HasForeignKey<Wishlist>(w => w.UserId);

            // 1:1 Order -> Payment
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId);

            // Payment -> PaymentTransactions (1:many)
            modelBuilder.Entity<Payment>()
                .HasMany(p => p.Transactions)
                .WithOne(pt => pt.Payment)
                .HasForeignKey(pt => pt.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Payment optimistic locking
            modelBuilder.Entity<Payment>()
                .Property(p => p.Version)
                .IsConcurrencyToken();

            // PaymentTransaction unique constraint on ExternalTransactionId for idempotency
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(pt => pt.ExternalTransactionId)
                .IsUnique();

            // Indexes for performance
            modelBuilder.Entity<Product>().HasIndex(p => p.CategoryId);
            modelBuilder.Entity<Product>().HasIndex(p => p.BrandId);
            modelBuilder.Entity<Product>().HasIndex(p => new { p.IsActive, p.IsFeatured });
            modelBuilder.Entity<Order>().HasIndex(o => o.UserId);
            modelBuilder.Entity<Order>().HasIndex(o => o.Status);
            modelBuilder.Entity<Review>().HasIndex(r => r.ProductId);
            modelBuilder.Entity<Review>().HasIndex(r => r.UserId);

            // Payment indexes
            modelBuilder.Entity<PaymentTransaction>().HasIndex(pt => pt.PaymentId);
            modelBuilder.Entity<Payment>().HasIndex(p => new { p.Status, p.CreatedAt });
            modelBuilder.Entity<Payment>().HasIndex(p => p.ExpiresAt)
                .HasFilter("[Status] IN ('Pending', 'Processing')");

            // Restrict delete behaviors
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Variant)
                .WithMany(v => v.OrderDetails)
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Variant)
                .WithMany(v => v.CartItems)
                .HasForeignKey(ci => ci.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(wi => wi.Product)
                .WithMany(p => p.WishlistItems)
                .HasForeignKey(wi => wi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Customer" }
            );

            // Seed Sizes
            modelBuilder.Entity<Size>().HasData(
                new Size { SizeId = 1, SizeName = "XS", DisplayOrder = 1 },
                new Size { SizeId = 2, SizeName = "S", DisplayOrder = 2 },
                new Size { SizeId = 3, SizeName = "M", DisplayOrder = 3 },
                new Size { SizeId = 4, SizeName = "L", DisplayOrder = 4 },
                new Size { SizeId = 5, SizeName = "XL", DisplayOrder = 5 },
                new Size { SizeId = 6, SizeName = "XXL", DisplayOrder = 6 }
            );

            // Seed Colors
            modelBuilder.Entity<Color>().HasData(
                new Color { ColorId = 1, ColorName = "Đen", ColorCode = "#000000" },
                new Color { ColorId = 2, ColorName = "Trắng", ColorCode = "#FFFFFF" },
                new Color { ColorId = 3, ColorName = "Xám", ColorCode = "#808080" },
                new Color { ColorId = 4, ColorName = "Đỏ", ColorCode = "#DC3545" },
                new Color { ColorId = 5, ColorName = "Xanh Navy", ColorCode = "#1B2A4A" },
                new Color { ColorId = 6, ColorName = "Xanh Rêu", ColorCode = "#2D5A27" },
                new Color { ColorId = 7, ColorName = "Be", ColorCode = "#D4C5A9" },
                new Color { ColorId = 8, ColorName = "Nâu", ColorCode = "#8B4513" }
            );

            // Seed Admin user (password: Admin@123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    RoleId = 1,
                    FullName = "Administrator",
                    Email = "admin@fashionstore.com",
                    PasswordHash = "$2a$11$mAzjDfc6sJT6VRZD47iT3uDtr0cTRClYI7tvdrwU1HAaOpV4x4yam", // Hash for Admin@123
                    Phone = "0123456789",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1)
                }
            );

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Áo", Slug = "ao", Description = "Các loại áo thời trang", IsActive = true, DisplayOrder = 1 },
                new Category { CategoryId = 2, CategoryName = "Quần", Slug = "quan", Description = "Các loại quần thời trang", IsActive = true, DisplayOrder = 2 },
                new Category { CategoryId = 3, CategoryName = "Váy & Đầm", Slug = "vay-dam", Description = "Váy và đầm thời trang", IsActive = true, DisplayOrder = 3 },
                new Category { CategoryId = 4, CategoryName = "Áo Khoác", Slug = "ao-khoac", Description = "Áo khoác, jacket", IsActive = true, DisplayOrder = 4 },
                new Category { CategoryId = 5, CategoryName = "Phụ Kiện", Slug = "phu-kien", Description = "Phụ kiện thời trang", IsActive = true, DisplayOrder = 5 }
            );

            // Seed Brands
            modelBuilder.Entity<Brand>().HasData(
                new Brand { BrandId = 1, BrandName = "YODY", Slug = "yody", Description = "Thời trang Việt Nam" },
                new Brand { BrandId = 2, BrandName = "CANIFA", Slug = "canifa", Description = "Thời trang gia đình" },
                new Brand { BrandId = 3, BrandName = "ROUTINE", Slug = "routine", Description = "Thời trang nam" },
                new Brand { BrandId = 4, BrandName = "IVY moda", Slug = "ivy-moda", Description = "Thời trang nữ cao cấp" },
                new Brand { BrandId = 5, BrandName = "OWEN", Slug = "owen", Description = "Thời trang công sở" }
            );

            SeedProducts(modelBuilder);
        }

        private void SeedProducts(ModelBuilder modelBuilder)
        {
            var products = new[]
            {
                new { ProductId = 1, CategoryId = 1, BrandId = 1, ProductName = "Áo Thun Cotton Basic", Slug = "ao-thun-cotton-basic", Description = "Áo thun cotton 100% thoáng mát, form regular fit phù hợp mọi dáng người.", Price = 199000m, SalePrice = (decimal?)149000m, MainImageUrl = "/images/products/ao-thun-basic.jpg", IsActive = true, IsFeatured = true, ViewCount = 250, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 2, CategoryId = 1, BrandId = 2, ProductName = "Áo Polo Pique Classic", Slug = "ao-polo-pique-classic", Description = "Áo polo chất liệu pique cao cấp, cổ bẻ lịch sự.", Price = 349000m, SalePrice = (decimal?)299000m, MainImageUrl = "/images/products/ao-polo-classic.jpg", IsActive = true, IsFeatured = true, ViewCount = 180, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 3, CategoryId = 1, BrandId = 3, ProductName = "Áo Sơ Mi Oxford", Slug = "ao-so-mi-oxford", Description = "Áo sơ mi Oxford dài tay, chất vải dày dặn.", Price = 450000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/ao-so-mi-oxford.jpg", IsActive = true, IsFeatured = false, ViewCount = 120, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 4, CategoryId = 1, BrandId = 5, ProductName = "Áo Sơ Mi Công Sở Slim", Slug = "ao-so-mi-cong-so-slim", Description = "Áo sơ mi công sở form slim fit, chống nhăn.", Price = 520000m, SalePrice = (decimal?)449000m, MainImageUrl = "/images/products/ao-so-mi-slim.jpg", IsActive = true, IsFeatured = true, ViewCount = 300, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 5, CategoryId = 2, BrandId = 3, ProductName = "Quần Jeans Slim Fit", Slug = "quan-jeans-slim-fit", Description = "Quần jeans co giãn, form slim fit năng động.", Price = 550000m, SalePrice = (decimal?)469000m, MainImageUrl = "/images/products/quan-jeans-slim.jpg", IsActive = true, IsFeatured = true, ViewCount = 400, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 6, CategoryId = 2, BrandId = 1, ProductName = "Quần Kaki Regular", Slug = "quan-kaki-regular", Description = "Quần kaki regular fit, chất liệu cotton pha.", Price = 399000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/quan-kaki-regular.jpg", IsActive = true, IsFeatured = false, ViewCount = 150, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 7, CategoryId = 2, BrandId = 2, ProductName = "Quần Jogger Thể Thao", Slug = "quan-jogger-the-thao", Description = "Quần jogger thoải mái, phù hợp thể thao và casual.", Price = 350000m, SalePrice = (decimal?)279000m, MainImageUrl = "/images/products/quan-jogger.jpg", IsActive = true, IsFeatured = true, ViewCount = 220, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 8, CategoryId = 2, BrandId = 5, ProductName = "Quần Âu Công Sở", Slug = "quan-au-cong-so", Description = "Quần âu công sở chống nhăn, form straight.", Price = 620000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/quan-au.jpg", IsActive = true, IsFeatured = false, ViewCount = 95, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 9, CategoryId = 3, BrandId = 4, ProductName = "Đầm Midi Hoa Nhí", Slug = "dam-midi-hoa-nhi", Description = "Đầm midi họa tiết hoa nhí, phong cách vintage.", Price = 750000m, SalePrice = (decimal?)599000m, MainImageUrl = "/images/products/dam-midi-hoa.jpg", IsActive = true, IsFeatured = true, ViewCount = 350, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 10, CategoryId = 3, BrandId = 4, ProductName = "Váy A-line Công Sở", Slug = "vay-a-line-cong-so", Description = "Váy A-line thanh lịch, phù hợp công sở.", Price = 480000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/vay-a-line.jpg", IsActive = true, IsFeatured = false, ViewCount = 110, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 11, CategoryId = 4, BrandId = 3, ProductName = "Áo Khoác Bomber", Slug = "ao-khoac-bomber", Description = "Áo khoác bomber phong cách streetwear.", Price = 680000m, SalePrice = (decimal?)549000m, MainImageUrl = "/images/products/ao-khoac-bomber.jpg", IsActive = true, IsFeatured = true, ViewCount = 280, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 12, CategoryId = 4, BrandId = 1, ProductName = "Áo Khoác Gió Nhẹ", Slug = "ao-khoac-gio-nhe", Description = "Áo khoác gió siêu nhẹ, chống nước nhẹ.", Price = 450000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/ao-khoac-gio.jpg", IsActive = true, IsFeatured = false, ViewCount = 175, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 13, CategoryId = 1, BrandId = 2, ProductName = "Áo Thun In Graphic", Slug = "ao-thun-in-graphic", Description = "Áo thun in graphic trendy, cotton 100%.", Price = 250000m, SalePrice = (decimal?)199000m, MainImageUrl = "/images/products/ao-thun-graphic.jpg", IsActive = true, IsFeatured = false, ViewCount = 190, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 14, CategoryId = 1, BrandId = 1, ProductName = "Áo Thun Oversize", Slug = "ao-thun-oversize", Description = "Áo thun form oversize thoải mái, unisex.", Price = 220000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/ao-thun-oversize.jpg", IsActive = true, IsFeatured = true, ViewCount = 320, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 15, CategoryId = 2, BrandId = 3, ProductName = "Quần Short Kaki", Slug = "quan-short-kaki", Description = "Quần short kaki thoáng mát mùa hè.", Price = 280000m, SalePrice = (decimal?)229000m, MainImageUrl = "/images/products/quan-short-kaki.jpg", IsActive = true, IsFeatured = false, ViewCount = 145, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 16, CategoryId = 3, BrandId = 4, ProductName = "Đầm Maxi Biển", Slug = "dam-maxi-bien", Description = "Đầm maxi bay bổng, hoàn hảo cho đi biển.", Price = 650000m, SalePrice = (decimal?)499000m, MainImageUrl = "/images/products/dam-maxi-bien.jpg", IsActive = true, IsFeatured = true, ViewCount = 260, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 17, CategoryId = 4, BrandId = 2, ProductName = "Áo Khoác Hoodie", Slug = "ao-khoac-hoodie", Description = "Áo hoodie nỉ bông ấm áp, phong cách trẻ trung.", Price = 420000m, SalePrice = (decimal?)349000m, MainImageUrl = "/images/products/ao-khoac-hoodie.jpg", IsActive = true, IsFeatured = true, ViewCount = 380, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 18, CategoryId = 1, BrandId = 5, ProductName = "Áo Vest Công Sở", Slug = "ao-vest-cong-so", Description = "Áo vest công sở lịch lãm, chất liệu cao cấp.", Price = 890000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/ao-vest.jpg", IsActive = true, IsFeatured = false, ViewCount = 85, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 19, CategoryId = 2, BrandId = 1, ProductName = "Quần Jeans Straight", Slug = "quan-jeans-straight", Description = "Quần jeans straight cổ điển, denim dày dặn.", Price = 490000m, SalePrice = (decimal?)null, MainImageUrl = "/images/products/quan-jeans-straight.jpg", IsActive = true, IsFeatured = false, ViewCount = 130, CreatedAt = new DateTime(2026,1,1) },
                new { ProductId = 20, CategoryId = 3, BrandId = 4, ProductName = "Váy Mini Denim", Slug = "vay-mini-denim", Description = "Váy mini denim cá tính, năng động.", Price = 380000m, SalePrice = (decimal?)319000m, MainImageUrl = "/images/products/vay-mini-denim.jpg", IsActive = true, IsFeatured = false, ViewCount = 200, CreatedAt = new DateTime(2026,1,1) },
            };

            foreach (var p in products)
            {
                modelBuilder.Entity<Product>().HasData(new Product
                {
                    ProductId = p.ProductId, CategoryId = p.CategoryId, BrandId = p.BrandId,
                    ProductName = p.ProductName, Slug = p.Slug, Description = p.Description,
                    Price = p.Price, SalePrice = p.SalePrice, MainImageUrl = p.MainImageUrl,
                    IsActive = p.IsActive, IsFeatured = p.IsFeatured, ViewCount = p.ViewCount,
                    CreatedAt = p.CreatedAt
                });
            }

            // Seed ProductVariants for all products
            int variantId = 1;
            var variantList = new List<ProductVariant>();
            var rng = new Random(42);

            for (int pid = 1; pid <= 20; pid++)
            {
                int[] sizes = pid <= 12 ? new[] { 2, 3, 4, 5 } : new[] { 3, 4, 5 }; // S,M,L,XL or M,L,XL
                int[] colors = (pid % 4) switch
                {
                    0 => new[] { 1, 2, 5 },     // Black, White, Navy
                    1 => new[] { 1, 3, 7 },     // Black, Gray, Beige
                    2 => new[] { 2, 4, 5 },     // White, Red, Navy
                    _ => new[] { 1, 2, 3, 6 }   // Black, White, Gray, Green
                };

                foreach (int sid in sizes)
                {
                    foreach (int cid in colors)
                    {
                        variantList.Add(new ProductVariant
                        {
                            VariantId = variantId,
                            ProductId = pid,
                            SizeId = sid,
                            ColorId = cid,
                            StockQuantity = rng.Next(5, 50),
                            AdditionalPrice = 0,
                            SKU = $"FS-{pid:D3}-{sid}-{cid}",
                            IsActive = true
                        });
                        variantId++;
                    }
                }
            }

            modelBuilder.Entity<ProductVariant>().HasData(variantList.ToArray());

            // Seed Coupons
            modelBuilder.Entity<Coupon>().HasData(
                new Coupon { CouponId = 1, CouponCode = "WELCOME10", Description = "Giảm 10% cho đơn hàng đầu tiên", DiscountType = "Percent", DiscountValue = 10, MinOrderAmount = 200000, MaxDiscountAmount = 100000, UsageLimit = 100, UsedCount = 0, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 12, 31), IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Coupon { CouponId = 2, CouponCode = "SUMMER50", Description = "Giảm 50K cho đơn từ 500K", DiscountType = "Amount", DiscountValue = 50000, MinOrderAmount = 500000, UsageLimit = 50, UsedCount = 0, StartDate = new DateTime(2026, 6, 1), EndDate = new DateTime(2026, 9, 30), IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
                new Coupon { CouponId = 3, CouponCode = "FREESHIP", Description = "Miễn phí vận chuyển đơn từ 300K", DiscountType = "Amount", DiscountValue = 30000, MinOrderAmount = 300000, UsageLimit = 200, UsedCount = 0, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2027, 12, 31), IsActive = true, CreatedAt = new DateTime(2026, 1, 1) }
            );
        }
    }
}

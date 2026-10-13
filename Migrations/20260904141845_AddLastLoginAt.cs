using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FashionStore.Migrations
{
    /// <inheritdoc />
    public partial class AddLastLoginAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    BrandId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.BrandId);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Colors",
                columns: table => new
                {
                    ColorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Colors", x => x.ColorId);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    CouponId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.CouponId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Sizes",
                columns: table => new
                {
                    SizeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sizes", x => x.SizeId);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    BrandId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MainImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    VariantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    AdditionalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.VariantId);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Colors_ColorId",
                        column: x => x.ColorId,
                        principalTable: "Colors",
                        principalColumn: "ColorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "Sizes",
                        principalColumn: "SizeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    CartId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.CartId);
                    table.ForeignKey(
                        name: "FK_Carts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CouponId = table.Column<int>(type: "int", nullable: true),
                    OrderCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "CouponId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Wishlists",
                columns: table => new
                {
                    WishlistId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wishlists", x => x.WishlistId);
                    table.ForeignKey(
                        name: "FK_Wishlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    CartItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CartId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.CartItemId);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "CartId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    OrderDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColorName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.OrderDetailId);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetails_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    WishlistItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WishlistId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.WishlistItemId);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Wishlists_WishlistId",
                        column: x => x.WishlistId,
                        principalTable: "Wishlists",
                        principalColumn: "WishlistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    ReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OrderDetailId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_Reviews_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "OrderDetailId");
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Brands",
                columns: new[] { "BrandId", "BrandName", "Description", "IsActive", "LogoUrl", "Slug" },
                values: new object[,]
                {
                    { 1, "YODY", "Thời trang Việt Nam", true, null, "yody" },
                    { 2, "CANIFA", "Thời trang gia đình", true, null, "canifa" },
                    { 3, "ROUTINE", "Thời trang nam", true, null, "routine" },
                    { 4, "IVY moda", "Thời trang nữ cao cấp", true, null, "ivy-moda" },
                    { 5, "OWEN", "Thời trang công sở", true, null, "owen" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "Description", "DisplayOrder", "ImageUrl", "IsActive", "Slug" },
                values: new object[,]
                {
                    { 1, "Áo", "Các loại áo thời trang", 1, null, true, "ao" },
                    { 2, "Quần", "Các loại quần thời trang", 2, null, true, "quan" },
                    { 3, "Váy & Đầm", "Váy và đầm thời trang", 3, null, true, "vay-dam" },
                    { 4, "Áo Khoác", "Áo khoác, jacket", 4, null, true, "ao-khoac" },
                    { 5, "Phụ Kiện", "Phụ kiện thời trang", 5, null, true, "phu-kien" }
                });

            migrationBuilder.InsertData(
                table: "Colors",
                columns: new[] { "ColorId", "ColorCode", "ColorName" },
                values: new object[,]
                {
                    { 1, "#000000", "Đen" },
                    { 2, "#FFFFFF", "Trắng" },
                    { 3, "#808080", "Xám" },
                    { 4, "#DC3545", "Đỏ" },
                    { 5, "#1B2A4A", "Xanh Navy" },
                    { 6, "#2D5A27", "Xanh Rêu" },
                    { 7, "#D4C5A9", "Be" },
                    { 8, "#8B4513", "Nâu" }
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "CouponId", "CouponCode", "CreatedAt", "Description", "DiscountType", "DiscountValue", "EndDate", "IsActive", "MaxDiscountAmount", "MinOrderAmount", "StartDate", "UsageLimit", "UsedCount" },
                values: new object[,]
                {
                    { 1, "WELCOME10", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Giảm 10% cho đơn hàng đầu tiên", 0, 10m, new DateTime(2027, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), true, 100000m, 200000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 100, 0 },
                    { 2, "SUMMER50", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Giảm 50K cho đơn từ 500K", 1, 50000m, new DateTime(2026, 9, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, 500000m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 50, 0 },
                    { 3, "FREESHIP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Miễn phí vận chuyển đơn từ 300K", 1, 30000m, new DateTime(2027, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, 300000m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 200, 0 }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Customer" }
                });

            migrationBuilder.InsertData(
                table: "Sizes",
                columns: new[] { "SizeId", "DisplayOrder", "SizeName" },
                values: new object[,]
                {
                    { 1, 1, "XS" },
                    { 2, 2, "S" },
                    { 3, 3, "M" },
                    { 4, 4, "L" },
                    { 5, 5, "XL" },
                    { 6, 6, "XXL" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "BrandId", "CategoryId", "CreatedAt", "Description", "IsActive", "IsFeatured", "MainImageUrl", "Price", "ProductName", "SalePrice", "Slug", "UpdatedAt", "ViewCount" },
                values: new object[,]
                {
                    { 1, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo thun cotton 100% thoáng mát, form regular fit phù hợp mọi dáng người.", true, true, "/images/products/ao-thun-basic.jpg", 199000m, "Áo Thun Cotton Basic", 149000m, "ao-thun-cotton-basic", null, 250 },
                    { 2, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo polo chất liệu pique cao cấp, cổ bẻ lịch sự.", true, true, "/images/products/ao-polo-classic.jpg", 349000m, "Áo Polo Pique Classic", 299000m, "ao-polo-pique-classic", null, 180 },
                    { 3, 3, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo sơ mi Oxford dài tay, chất vải dày dặn.", true, false, "/images/products/ao-so-mi-oxford.jpg", 450000m, "Áo Sơ Mi Oxford", null, "ao-so-mi-oxford", null, 120 },
                    { 4, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo sơ mi công sở form slim fit, chống nhăn.", true, true, "/images/products/ao-so-mi-slim.jpg", 520000m, "Áo Sơ Mi Công Sở Slim", 449000m, "ao-so-mi-cong-so-slim", null, 300 },
                    { 5, 3, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần jeans co giãn, form slim fit năng động.", true, true, "/images/products/quan-jeans-slim.jpg", 550000m, "Quần Jeans Slim Fit", 469000m, "quan-jeans-slim-fit", null, 400 },
                    { 6, 1, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần kaki regular fit, chất liệu cotton pha.", true, false, "/images/products/quan-kaki-regular.jpg", 399000m, "Quần Kaki Regular", null, "quan-kaki-regular", null, 150 },
                    { 7, 2, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần jogger thoải mái, phù hợp thể thao và casual.", true, true, "/images/products/quan-jogger.jpg", 350000m, "Quần Jogger Thể Thao", 279000m, "quan-jogger-the-thao", null, 220 },
                    { 8, 5, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần âu công sở chống nhăn, form straight.", true, false, "/images/products/quan-au.jpg", 620000m, "Quần Âu Công Sở", null, "quan-au-cong-so", null, 95 },
                    { 9, 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đầm midi họa tiết hoa nhí, phong cách vintage.", true, true, "/images/products/dam-midi-hoa.jpg", 750000m, "Đầm Midi Hoa Nhí", 599000m, "dam-midi-hoa-nhi", null, 350 },
                    { 10, 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Váy A-line thanh lịch, phù hợp công sở.", true, false, "/images/products/vay-a-line.jpg", 480000m, "Váy A-line Công Sở", null, "vay-a-line-cong-so", null, 110 },
                    { 11, 3, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo khoác bomber phong cách streetwear.", true, true, "/images/products/ao-khoac-bomber.jpg", 680000m, "Áo Khoác Bomber", 549000m, "ao-khoac-bomber", null, 280 },
                    { 12, 1, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo khoác gió siêu nhẹ, chống nước nhẹ.", true, false, "/images/products/ao-khoac-gio.jpg", 450000m, "Áo Khoác Gió Nhẹ", null, "ao-khoac-gio-nhe", null, 175 },
                    { 13, 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo thun in graphic trendy, cotton 100%.", true, false, "/images/products/ao-thun-graphic.jpg", 250000m, "Áo Thun In Graphic", 199000m, "ao-thun-in-graphic", null, 190 },
                    { 14, 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo thun form oversize thoải mái, unisex.", true, true, "/images/products/ao-thun-oversize.jpg", 220000m, "Áo Thun Oversize", null, "ao-thun-oversize", null, 320 },
                    { 15, 3, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần short kaki thoáng mát mùa hè.", true, false, "/images/products/quan-short-kaki.jpg", 280000m, "Quần Short Kaki", 229000m, "quan-short-kaki", null, 145 },
                    { 16, 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Đầm maxi bay bổng, hoàn hảo cho đi biển.", true, true, "/images/products/dam-maxi-bien.jpg", 650000m, "Đầm Maxi Biển", 499000m, "dam-maxi-bien", null, 260 },
                    { 17, 2, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo hoodie nỉ bông ấm áp, phong cách trẻ trung.", true, true, "/images/products/ao-khoac-hoodie.jpg", 420000m, "Áo Khoác Hoodie", 349000m, "ao-khoac-hoodie", null, 380 },
                    { 18, 5, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Áo vest công sở lịch lãm, chất liệu cao cấp.", true, false, "/images/products/ao-vest.jpg", 890000m, "Áo Vest Công Sở", null, "ao-vest-cong-so", null, 85 },
                    { 19, 1, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Quần jeans straight cổ điển, denim dày dặn.", true, false, "/images/products/quan-jeans-straight.jpg", 490000m, "Quần Jeans Straight", null, "quan-jeans-straight", null, 130 },
                    { 20, 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Váy mini denim cá tính, năng động.", true, false, "/images/products/vay-mini-denim.jpg", 380000m, "Váy Mini Denim", 319000m, "vay-mini-denim", null, 200 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "Address", "Avatar", "City", "CreatedAt", "Email", "FullName", "IsActive", "LastLoginAt", "PasswordHash", "Phone", "RoleId", "UpdatedAt" },
                values: new object[] { 1, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@fashionstore.com", "Administrator", true, null, "$2a$11$mAzjDfc6sJT6VRZD47iT3uDtr0cTRClYI7tvdrwU1HAaOpV4x4yam", "0123456789", 1, null });

            migrationBuilder.InsertData(
                table: "ProductVariants",
                columns: new[] { "VariantId", "AdditionalPrice", "ColorId", "IsActive", "ProductId", "SKU", "SizeId", "StockQuantity" },
                values: new object[,]
                {
                    { 1, 0m, 1, true, 1, "FS-001-2-1", 2, 35 },
                    { 2, 0m, 3, true, 1, "FS-001-2-3", 2, 11 },
                    { 3, 0m, 7, true, 1, "FS-001-2-7", 2, 10 },
                    { 4, 0m, 1, true, 1, "FS-001-3-1", 3, 28 },
                    { 5, 0m, 3, true, 1, "FS-001-3-3", 3, 12 },
                    { 6, 0m, 7, true, 1, "FS-001-3-7", 3, 16 },
                    { 7, 0m, 1, true, 1, "FS-001-4-1", 4, 37 },
                    { 8, 0m, 3, true, 1, "FS-001-4-3", 4, 28 },
                    { 9, 0m, 7, true, 1, "FS-001-4-7", 4, 12 },
                    { 10, 0m, 1, true, 1, "FS-001-5-1", 5, 39 },
                    { 11, 0m, 3, true, 1, "FS-001-5-3", 5, 15 },
                    { 12, 0m, 7, true, 1, "FS-001-5-7", 5, 16 },
                    { 13, 0m, 2, true, 2, "FS-002-2-2", 2, 27 },
                    { 14, 0m, 4, true, 2, "FS-002-2-4", 2, 19 },
                    { 15, 0m, 5, true, 2, "FS-002-2-5", 2, 22 },
                    { 16, 0m, 2, true, 2, "FS-002-3-2", 3, 16 },
                    { 17, 0m, 4, true, 2, "FS-002-3-4", 3, 28 },
                    { 18, 0m, 5, true, 2, "FS-002-3-5", 3, 6 },
                    { 19, 0m, 2, true, 2, "FS-002-4-2", 4, 41 },
                    { 20, 0m, 4, true, 2, "FS-002-4-4", 4, 30 },
                    { 21, 0m, 5, true, 2, "FS-002-4-5", 4, 22 },
                    { 22, 0m, 2, true, 2, "FS-002-5-2", 5, 11 },
                    { 23, 0m, 4, true, 2, "FS-002-5-4", 5, 9 },
                    { 24, 0m, 5, true, 2, "FS-002-5-5", 5, 36 },
                    { 25, 0m, 1, true, 3, "FS-003-2-1", 2, 41 },
                    { 26, 0m, 2, true, 3, "FS-003-2-2", 2, 29 },
                    { 27, 0m, 3, true, 3, "FS-003-2-3", 2, 6 },
                    { 28, 0m, 6, true, 3, "FS-003-2-6", 2, 37 },
                    { 29, 0m, 1, true, 3, "FS-003-3-1", 3, 11 },
                    { 30, 0m, 2, true, 3, "FS-003-3-2", 3, 45 },
                    { 31, 0m, 3, true, 3, "FS-003-3-3", 3, 36 },
                    { 32, 0m, 6, true, 3, "FS-003-3-6", 3, 28 },
                    { 33, 0m, 1, true, 3, "FS-003-4-1", 4, 13 },
                    { 34, 0m, 2, true, 3, "FS-003-4-2", 4, 6 },
                    { 35, 0m, 3, true, 3, "FS-003-4-3", 4, 18 },
                    { 36, 0m, 6, true, 3, "FS-003-4-6", 4, 31 },
                    { 37, 0m, 1, true, 3, "FS-003-5-1", 5, 41 },
                    { 38, 0m, 2, true, 3, "FS-003-5-2", 5, 5 },
                    { 39, 0m, 3, true, 3, "FS-003-5-3", 5, 29 },
                    { 40, 0m, 6, true, 3, "FS-003-5-6", 5, 41 },
                    { 41, 0m, 1, true, 4, "FS-004-2-1", 2, 38 },
                    { 42, 0m, 2, true, 4, "FS-004-2-2", 2, 22 },
                    { 43, 0m, 5, true, 4, "FS-004-2-5", 2, 11 },
                    { 44, 0m, 1, true, 4, "FS-004-3-1", 3, 6 },
                    { 45, 0m, 2, true, 4, "FS-004-3-2", 3, 7 },
                    { 46, 0m, 5, true, 4, "FS-004-3-5", 3, 40 },
                    { 47, 0m, 1, true, 4, "FS-004-4-1", 4, 33 },
                    { 48, 0m, 2, true, 4, "FS-004-4-2", 4, 10 },
                    { 49, 0m, 5, true, 4, "FS-004-4-5", 4, 16 },
                    { 50, 0m, 1, true, 4, "FS-004-5-1", 5, 39 },
                    { 51, 0m, 2, true, 4, "FS-004-5-2", 5, 18 },
                    { 52, 0m, 5, true, 4, "FS-004-5-5", 5, 34 },
                    { 53, 0m, 1, true, 5, "FS-005-2-1", 2, 11 },
                    { 54, 0m, 3, true, 5, "FS-005-2-3", 2, 7 },
                    { 55, 0m, 7, true, 5, "FS-005-2-7", 2, 32 },
                    { 56, 0m, 1, true, 5, "FS-005-3-1", 3, 28 },
                    { 57, 0m, 3, true, 5, "FS-005-3-3", 3, 7 },
                    { 58, 0m, 7, true, 5, "FS-005-3-7", 3, 23 },
                    { 59, 0m, 1, true, 5, "FS-005-4-1", 4, 36 },
                    { 60, 0m, 3, true, 5, "FS-005-4-3", 4, 33 },
                    { 61, 0m, 7, true, 5, "FS-005-4-7", 4, 14 },
                    { 62, 0m, 1, true, 5, "FS-005-5-1", 5, 5 },
                    { 63, 0m, 3, true, 5, "FS-005-5-3", 5, 21 },
                    { 64, 0m, 7, true, 5, "FS-005-5-7", 5, 17 },
                    { 65, 0m, 2, true, 6, "FS-006-2-2", 2, 8 },
                    { 66, 0m, 4, true, 6, "FS-006-2-4", 2, 37 },
                    { 67, 0m, 5, true, 6, "FS-006-2-5", 2, 8 },
                    { 68, 0m, 2, true, 6, "FS-006-3-2", 3, 25 },
                    { 69, 0m, 4, true, 6, "FS-006-3-4", 3, 5 },
                    { 70, 0m, 5, true, 6, "FS-006-3-5", 3, 40 },
                    { 71, 0m, 2, true, 6, "FS-006-4-2", 4, 24 },
                    { 72, 0m, 4, true, 6, "FS-006-4-4", 4, 28 },
                    { 73, 0m, 5, true, 6, "FS-006-4-5", 4, 27 },
                    { 74, 0m, 2, true, 6, "FS-006-5-2", 5, 5 },
                    { 75, 0m, 4, true, 6, "FS-006-5-4", 5, 42 },
                    { 76, 0m, 5, true, 6, "FS-006-5-5", 5, 5 },
                    { 77, 0m, 1, true, 7, "FS-007-2-1", 2, 5 },
                    { 78, 0m, 2, true, 7, "FS-007-2-2", 2, 7 },
                    { 79, 0m, 3, true, 7, "FS-007-2-3", 2, 34 },
                    { 80, 0m, 6, true, 7, "FS-007-2-6", 2, 6 },
                    { 81, 0m, 1, true, 7, "FS-007-3-1", 3, 46 },
                    { 82, 0m, 2, true, 7, "FS-007-3-2", 3, 46 },
                    { 83, 0m, 3, true, 7, "FS-007-3-3", 3, 25 },
                    { 84, 0m, 6, true, 7, "FS-007-3-6", 3, 21 },
                    { 85, 0m, 1, true, 7, "FS-007-4-1", 4, 32 },
                    { 86, 0m, 2, true, 7, "FS-007-4-2", 4, 6 },
                    { 87, 0m, 3, true, 7, "FS-007-4-3", 4, 21 },
                    { 88, 0m, 6, true, 7, "FS-007-4-6", 4, 11 },
                    { 89, 0m, 1, true, 7, "FS-007-5-1", 5, 24 },
                    { 90, 0m, 2, true, 7, "FS-007-5-2", 5, 40 },
                    { 91, 0m, 3, true, 7, "FS-007-5-3", 5, 28 },
                    { 92, 0m, 6, true, 7, "FS-007-5-6", 5, 23 },
                    { 93, 0m, 1, true, 8, "FS-008-2-1", 2, 18 },
                    { 94, 0m, 2, true, 8, "FS-008-2-2", 2, 45 },
                    { 95, 0m, 5, true, 8, "FS-008-2-5", 2, 31 },
                    { 96, 0m, 1, true, 8, "FS-008-3-1", 3, 38 },
                    { 97, 0m, 2, true, 8, "FS-008-3-2", 3, 5 },
                    { 98, 0m, 5, true, 8, "FS-008-3-5", 3, 44 },
                    { 99, 0m, 1, true, 8, "FS-008-4-1", 4, 48 },
                    { 100, 0m, 2, true, 8, "FS-008-4-2", 4, 19 },
                    { 101, 0m, 5, true, 8, "FS-008-4-5", 4, 36 },
                    { 102, 0m, 1, true, 8, "FS-008-5-1", 5, 12 },
                    { 103, 0m, 2, true, 8, "FS-008-5-2", 5, 9 },
                    { 104, 0m, 5, true, 8, "FS-008-5-5", 5, 25 },
                    { 105, 0m, 1, true, 9, "FS-009-2-1", 2, 19 },
                    { 106, 0m, 3, true, 9, "FS-009-2-3", 2, 39 },
                    { 107, 0m, 7, true, 9, "FS-009-2-7", 2, 11 },
                    { 108, 0m, 1, true, 9, "FS-009-3-1", 3, 11 },
                    { 109, 0m, 3, true, 9, "FS-009-3-3", 3, 14 },
                    { 110, 0m, 7, true, 9, "FS-009-3-7", 3, 31 },
                    { 111, 0m, 1, true, 9, "FS-009-4-1", 4, 28 },
                    { 112, 0m, 3, true, 9, "FS-009-4-3", 4, 49 },
                    { 113, 0m, 7, true, 9, "FS-009-4-7", 4, 39 },
                    { 114, 0m, 1, true, 9, "FS-009-5-1", 5, 35 },
                    { 115, 0m, 3, true, 9, "FS-009-5-3", 5, 37 },
                    { 116, 0m, 7, true, 9, "FS-009-5-7", 5, 18 },
                    { 117, 0m, 2, true, 10, "FS-010-2-2", 2, 29 },
                    { 118, 0m, 4, true, 10, "FS-010-2-4", 2, 49 },
                    { 119, 0m, 5, true, 10, "FS-010-2-5", 2, 34 },
                    { 120, 0m, 2, true, 10, "FS-010-3-2", 3, 6 },
                    { 121, 0m, 4, true, 10, "FS-010-3-4", 3, 20 },
                    { 122, 0m, 5, true, 10, "FS-010-3-5", 3, 46 },
                    { 123, 0m, 2, true, 10, "FS-010-4-2", 4, 6 },
                    { 124, 0m, 4, true, 10, "FS-010-4-4", 4, 14 },
                    { 125, 0m, 5, true, 10, "FS-010-4-5", 4, 16 },
                    { 126, 0m, 2, true, 10, "FS-010-5-2", 5, 6 },
                    { 127, 0m, 4, true, 10, "FS-010-5-4", 5, 14 },
                    { 128, 0m, 5, true, 10, "FS-010-5-5", 5, 31 },
                    { 129, 0m, 1, true, 11, "FS-011-2-1", 2, 23 },
                    { 130, 0m, 2, true, 11, "FS-011-2-2", 2, 9 },
                    { 131, 0m, 3, true, 11, "FS-011-2-3", 2, 49 },
                    { 132, 0m, 6, true, 11, "FS-011-2-6", 2, 10 },
                    { 133, 0m, 1, true, 11, "FS-011-3-1", 3, 9 },
                    { 134, 0m, 2, true, 11, "FS-011-3-2", 3, 19 },
                    { 135, 0m, 3, true, 11, "FS-011-3-3", 3, 19 },
                    { 136, 0m, 6, true, 11, "FS-011-3-6", 3, 38 },
                    { 137, 0m, 1, true, 11, "FS-011-4-1", 4, 42 },
                    { 138, 0m, 2, true, 11, "FS-011-4-2", 4, 5 },
                    { 139, 0m, 3, true, 11, "FS-011-4-3", 4, 6 },
                    { 140, 0m, 6, true, 11, "FS-011-4-6", 4, 42 },
                    { 141, 0m, 1, true, 11, "FS-011-5-1", 5, 45 },
                    { 142, 0m, 2, true, 11, "FS-011-5-2", 5, 15 },
                    { 143, 0m, 3, true, 11, "FS-011-5-3", 5, 46 },
                    { 144, 0m, 6, true, 11, "FS-011-5-6", 5, 43 },
                    { 145, 0m, 1, true, 12, "FS-012-2-1", 2, 17 },
                    { 146, 0m, 2, true, 12, "FS-012-2-2", 2, 29 },
                    { 147, 0m, 5, true, 12, "FS-012-2-5", 2, 33 },
                    { 148, 0m, 1, true, 12, "FS-012-3-1", 3, 33 },
                    { 149, 0m, 2, true, 12, "FS-012-3-2", 3, 13 },
                    { 150, 0m, 5, true, 12, "FS-012-3-5", 3, 18 },
                    { 151, 0m, 1, true, 12, "FS-012-4-1", 4, 13 },
                    { 152, 0m, 2, true, 12, "FS-012-4-2", 4, 6 },
                    { 153, 0m, 5, true, 12, "FS-012-4-5", 4, 15 },
                    { 154, 0m, 1, true, 12, "FS-012-5-1", 5, 47 },
                    { 155, 0m, 2, true, 12, "FS-012-5-2", 5, 49 },
                    { 156, 0m, 5, true, 12, "FS-012-5-5", 5, 40 },
                    { 157, 0m, 1, true, 13, "FS-013-3-1", 3, 11 },
                    { 158, 0m, 3, true, 13, "FS-013-3-3", 3, 44 },
                    { 159, 0m, 7, true, 13, "FS-013-3-7", 3, 13 },
                    { 160, 0m, 1, true, 13, "FS-013-4-1", 4, 18 },
                    { 161, 0m, 3, true, 13, "FS-013-4-3", 4, 30 },
                    { 162, 0m, 7, true, 13, "FS-013-4-7", 4, 30 },
                    { 163, 0m, 1, true, 13, "FS-013-5-1", 5, 37 },
                    { 164, 0m, 3, true, 13, "FS-013-5-3", 5, 10 },
                    { 165, 0m, 7, true, 13, "FS-013-5-7", 5, 31 },
                    { 166, 0m, 2, true, 14, "FS-014-3-2", 3, 22 },
                    { 167, 0m, 4, true, 14, "FS-014-3-4", 3, 44 },
                    { 168, 0m, 5, true, 14, "FS-014-3-5", 3, 25 },
                    { 169, 0m, 2, true, 14, "FS-014-4-2", 4, 20 },
                    { 170, 0m, 4, true, 14, "FS-014-4-4", 4, 48 },
                    { 171, 0m, 5, true, 14, "FS-014-4-5", 4, 25 },
                    { 172, 0m, 2, true, 14, "FS-014-5-2", 5, 29 },
                    { 173, 0m, 4, true, 14, "FS-014-5-4", 5, 47 },
                    { 174, 0m, 5, true, 14, "FS-014-5-5", 5, 41 },
                    { 175, 0m, 1, true, 15, "FS-015-3-1", 3, 10 },
                    { 176, 0m, 2, true, 15, "FS-015-3-2", 3, 9 },
                    { 177, 0m, 3, true, 15, "FS-015-3-3", 3, 5 },
                    { 178, 0m, 6, true, 15, "FS-015-3-6", 3, 13 },
                    { 179, 0m, 1, true, 15, "FS-015-4-1", 4, 47 },
                    { 180, 0m, 2, true, 15, "FS-015-4-2", 4, 37 },
                    { 181, 0m, 3, true, 15, "FS-015-4-3", 4, 23 },
                    { 182, 0m, 6, true, 15, "FS-015-4-6", 4, 31 },
                    { 183, 0m, 1, true, 15, "FS-015-5-1", 5, 23 },
                    { 184, 0m, 2, true, 15, "FS-015-5-2", 5, 10 },
                    { 185, 0m, 3, true, 15, "FS-015-5-3", 5, 45 },
                    { 186, 0m, 6, true, 15, "FS-015-5-6", 5, 48 },
                    { 187, 0m, 1, true, 16, "FS-016-3-1", 3, 45 },
                    { 188, 0m, 2, true, 16, "FS-016-3-2", 3, 12 },
                    { 189, 0m, 5, true, 16, "FS-016-3-5", 3, 20 },
                    { 190, 0m, 1, true, 16, "FS-016-4-1", 4, 29 },
                    { 191, 0m, 2, true, 16, "FS-016-4-2", 4, 32 },
                    { 192, 0m, 5, true, 16, "FS-016-4-5", 4, 47 },
                    { 193, 0m, 1, true, 16, "FS-016-5-1", 5, 42 },
                    { 194, 0m, 2, true, 16, "FS-016-5-2", 5, 38 },
                    { 195, 0m, 5, true, 16, "FS-016-5-5", 5, 17 },
                    { 196, 0m, 1, true, 17, "FS-017-3-1", 3, 20 },
                    { 197, 0m, 3, true, 17, "FS-017-3-3", 3, 28 },
                    { 198, 0m, 7, true, 17, "FS-017-3-7", 3, 41 },
                    { 199, 0m, 1, true, 17, "FS-017-4-1", 4, 16 },
                    { 200, 0m, 3, true, 17, "FS-017-4-3", 4, 44 },
                    { 201, 0m, 7, true, 17, "FS-017-4-7", 4, 34 },
                    { 202, 0m, 1, true, 17, "FS-017-5-1", 5, 13 },
                    { 203, 0m, 3, true, 17, "FS-017-5-3", 5, 17 },
                    { 204, 0m, 7, true, 17, "FS-017-5-7", 5, 15 },
                    { 205, 0m, 2, true, 18, "FS-018-3-2", 3, 42 },
                    { 206, 0m, 4, true, 18, "FS-018-3-4", 3, 33 },
                    { 207, 0m, 5, true, 18, "FS-018-3-5", 3, 8 },
                    { 208, 0m, 2, true, 18, "FS-018-4-2", 4, 23 },
                    { 209, 0m, 4, true, 18, "FS-018-4-4", 4, 41 },
                    { 210, 0m, 5, true, 18, "FS-018-4-5", 4, 44 },
                    { 211, 0m, 2, true, 18, "FS-018-5-2", 5, 40 },
                    { 212, 0m, 4, true, 18, "FS-018-5-4", 5, 47 },
                    { 213, 0m, 5, true, 18, "FS-018-5-5", 5, 47 },
                    { 214, 0m, 1, true, 19, "FS-019-3-1", 3, 25 },
                    { 215, 0m, 2, true, 19, "FS-019-3-2", 3, 45 },
                    { 216, 0m, 3, true, 19, "FS-019-3-3", 3, 48 },
                    { 217, 0m, 6, true, 19, "FS-019-3-6", 3, 12 },
                    { 218, 0m, 1, true, 19, "FS-019-4-1", 4, 31 },
                    { 219, 0m, 2, true, 19, "FS-019-4-2", 4, 14 },
                    { 220, 0m, 3, true, 19, "FS-019-4-3", 4, 33 },
                    { 221, 0m, 6, true, 19, "FS-019-4-6", 4, 27 },
                    { 222, 0m, 1, true, 19, "FS-019-5-1", 5, 36 },
                    { 223, 0m, 2, true, 19, "FS-019-5-2", 5, 9 },
                    { 224, 0m, 3, true, 19, "FS-019-5-3", 5, 41 },
                    { 225, 0m, 6, true, 19, "FS-019-5-6", 5, 20 },
                    { 226, 0m, 1, true, 20, "FS-020-3-1", 3, 28 },
                    { 227, 0m, 2, true, 20, "FS-020-3-2", 3, 37 },
                    { 228, 0m, 5, true, 20, "FS-020-3-5", 3, 14 },
                    { 229, 0m, 1, true, 20, "FS-020-4-1", 4, 28 },
                    { 230, 0m, 2, true, 20, "FS-020-4-2", 4, 40 },
                    { 231, 0m, 5, true, 20, "FS-020-4-5", 4, 31 },
                    { 232, 0m, 1, true, 20, "FS-020-5-1", 5, 13 },
                    { 233, 0m, 2, true, 20, "FS-020-5-2", 5, 47 },
                    { 234, 0m, 5, true, 20, "FS-020-5-5", 5, 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                table: "Brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_VariantId",
                table: "CartItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                table: "Carts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CouponCode",
                table: "Coupons",
                column: "CouponCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_VariantId",
                table: "OrderDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CouponId",
                table: "Orders",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderCode",
                table: "Orders",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_IsFeatured",
                table: "Products",
                columns: new[] { "IsActive", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ColorId",
                table: "ProductVariants",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_SizeId_ColorId",
                table: "ProductVariants",
                columns: new[] { "ProductId", "SizeId", "ColorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SizeId",
                table: "ProductVariants",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderDetailId",
                table: "Reviews",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ProductId",
                table: "WishlistItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId",
                table: "WishlistItems",
                column: "WishlistId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_UserId",
                table: "Wishlists",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "Carts");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Colors");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sizes");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}

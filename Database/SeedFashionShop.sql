/* Dữ liệu mẫu cho FashionShop. Có thể chạy lại an toàn trong SSMS. */
USE FashionShop;
GO

IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'Admin')
    INSERT INTO Roles (RoleName) VALUES (N'Admin');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = N'Customer')
    INSERT INTO Roles (RoleName) VALUES (N'Customer');
GO

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'admin@fashionstore.com')
    INSERT INTO Users (RoleId, FullName, Email, PasswordHash, Phone)
    SELECT RoleId, N'Administrator', N'admin@fashionstore.com',
           N'$2a$11$mAzjDfc6sJT6VRZD47iT3uDtr0cTRClYI7tvdrwU1HAaOpV4x4yam', N'0123456789'
    FROM Roles WHERE RoleName = N'Admin';
GO

IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = N'ao')
    INSERT INTO Categories (CategoryName, Slug, Description, DisplayOrder) VALUES (N'Áo', N'ao', N'Các mẫu áo thời trang', 1);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = N'quan')
    INSERT INTO Categories (CategoryName, Slug, Description, DisplayOrder) VALUES (N'Quần', N'quan', N'Các mẫu quần thời trang', 2);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = N'vay-dam')
    INSERT INTO Categories (CategoryName, Slug, Description, DisplayOrder) VALUES (N'Váy & Đầm', N'vay-dam', N'Váy và đầm thời trang', 3);
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = N'ao-khoac')
    INSERT INTO Categories (CategoryName, Slug, Description, DisplayOrder) VALUES (N'Áo Khoác', N'ao-khoac', N'Áo khoác và jacket', 4);
GO

IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = N'yody')
    INSERT INTO Brands (BrandName, Slug, Description) VALUES (N'YODY', N'yody', N'Thời trang Việt Nam');
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = N'canifa')
    INSERT INTO Brands (BrandName, Slug, Description) VALUES (N'CANIFA', N'canifa', N'Thời trang gia đình');
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = N'routine')
    INSERT INTO Brands (BrandName, Slug, Description) VALUES (N'ROUTINE', N'routine', N'Thời trang hiện đại');
IF NOT EXISTS (SELECT 1 FROM Brands WHERE Slug = N'ivy-moda')
    INSERT INTO Brands (BrandName, Slug, Description) VALUES (N'IVY moda', N'ivy-moda', N'Thời trang nữ');
GO

IF NOT EXISTS (SELECT 1 FROM Sizes WHERE SizeName = N'S') INSERT INTO Sizes (SizeName, DisplayOrder) VALUES (N'S', 1);
IF NOT EXISTS (SELECT 1 FROM Sizes WHERE SizeName = N'M') INSERT INTO Sizes (SizeName, DisplayOrder) VALUES (N'M', 2);
IF NOT EXISTS (SELECT 1 FROM Sizes WHERE SizeName = N'L') INSERT INTO Sizes (SizeName, DisplayOrder) VALUES (N'L', 3);
IF NOT EXISTS (SELECT 1 FROM Sizes WHERE SizeName = N'XL') INSERT INTO Sizes (SizeName, DisplayOrder) VALUES (N'XL', 4);
IF NOT EXISTS (SELECT 1 FROM Colors WHERE ColorName = N'Đen') INSERT INTO Colors (ColorName, ColorCode) VALUES (N'Đen', N'#000000');
IF NOT EXISTS (SELECT 1 FROM Colors WHERE ColorName = N'Trắng') INSERT INTO Colors (ColorName, ColorCode) VALUES (N'Trắng', N'#FFFFFF');
IF NOT EXISTS (SELECT 1 FROM Colors WHERE ColorName = N'Xanh Navy') INSERT INTO Colors (ColorName, ColorCode) VALUES (N'Xanh Navy', N'#1B2A4A');
IF NOT EXISTS (SELECT 1 FROM Colors WHERE ColorName = N'Be') INSERT INTO Colors (ColorName, ColorCode) VALUES (N'Be', N'#D4C5A9');
GO

IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = N'ao-thun-cotton-basic')
    INSERT INTO Products (CategoryId, BrandId, ProductName, Slug, Description, Price, SalePrice, MainImageUrl, IsFeatured)
    SELECT c.CategoryId, b.BrandId, N'Áo Thun Cotton Basic', N'ao-thun-cotton-basic', N'Áo thun cotton mềm mại, phù hợp mặc hằng ngày.', 199000, 149000,
           N'https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?q=80&w=1000&auto=format&fit=crop', 1
    FROM Categories c CROSS JOIN Brands b WHERE c.Slug = N'ao' AND b.Slug = N'yody';

IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = N'ao-polo-pique-classic')
    INSERT INTO Products (CategoryId, BrandId, ProductName, Slug, Description, Price, SalePrice, MainImageUrl, IsFeatured)
    SELECT c.CategoryId, b.BrandId, N'Áo Polo Pique Classic', N'ao-polo-pique-classic', N'Áo polo lịch sự, thoáng mát.', 349000, 299000,
           N'https://images.unsplash.com/photo-1625910513413-5fc68c1c7f31?q=80&w=1000&auto=format&fit=crop', 1
    FROM Categories c CROSS JOIN Brands b WHERE c.Slug = N'ao' AND b.Slug = N'canifa';

IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = N'quan-jeans-slim-fit')
    INSERT INTO Products (CategoryId, BrandId, ProductName, Slug, Description, Price, SalePrice, MainImageUrl, IsFeatured)
    SELECT c.CategoryId, b.BrandId, N'Quần Jeans Slim Fit', N'quan-jeans-slim-fit', N'Quần jeans co giãn, form slim fit năng động.', 550000, 469000,
           N'https://images.unsplash.com/photo-1542272604-787c3835535d?q=80&w=1000&auto=format&fit=crop', 1
    FROM Categories c CROSS JOIN Brands b WHERE c.Slug = N'quan' AND b.Slug = N'routine';

IF NOT EXISTS (SELECT 1 FROM Products WHERE Slug = N'dam-midi-hoa-nhi')
    INSERT INTO Products (CategoryId, BrandId, ProductName, Slug, Description, Price, SalePrice, MainImageUrl, IsFeatured)
    SELECT c.CategoryId, b.BrandId, N'Đầm Midi Hoa Nhí', N'dam-midi-hoa-nhi', N'Đầm midi họa tiết hoa nhí phong cách vintage.', 750000, 599000,
           N'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?q=80&w=1000&auto=format&fit=crop', 1
    FROM Categories c CROSS JOIN Brands b WHERE c.Slug = N'vay-dam' AND b.Slug = N'ivy-moda';
GO

DECLARE @Products TABLE (Slug NVARCHAR(250), SKU NVARCHAR(50), SizeName NVARCHAR(20), ColorName NVARCHAR(50), StockQuantity INT);
INSERT INTO @Products VALUES
    (N'ao-thun-cotton-basic', N'FS-TEE-S-BLK', N'S', N'Đen', 30), (N'ao-thun-cotton-basic', N'FS-TEE-M-BLK', N'M', N'Đen', 35),
    (N'ao-thun-cotton-basic', N'FS-TEE-L-WHT', N'L', N'Trắng', 24), (N'ao-polo-pique-classic', N'FS-POL-M-NAV', N'M', N'Xanh Navy', 20),
    (N'ao-polo-pique-classic', N'FS-POL-L-WHT', N'L', N'Trắng', 18), (N'quan-jeans-slim-fit', N'FS-JEA-M-BLK', N'M', N'Đen', 16),
    (N'quan-jeans-slim-fit', N'FS-JEA-L-BLK', N'L', N'Đen', 20), (N'dam-midi-hoa-nhi', N'FS-DRS-S-BEI', N'S', N'Be', 12),
    (N'dam-midi-hoa-nhi', N'FS-DRS-M-BEI', N'M', N'Be', 15);

INSERT INTO ProductVariants (ProductId, SizeId, ColorId, StockQuantity, SKU)
SELECT p.ProductId, s.SizeId, c.ColorId, v.StockQuantity, v.SKU
FROM @Products v JOIN Products p ON p.Slug = v.Slug JOIN Sizes s ON s.SizeName = v.SizeName JOIN Colors c ON c.ColorName = v.ColorName
WHERE NOT EXISTS (SELECT 1 FROM ProductVariants x WHERE x.ProductId = p.ProductId AND x.SizeId = s.SizeId AND x.ColorId = c.ColorId);
GO

IF NOT EXISTS (SELECT 1 FROM Coupons WHERE CouponCode = N'WELCOME10')
    INSERT INTO Coupons (CouponCode, Description, DiscountType, DiscountValue, MinOrderAmount, MaxDiscountAmount, UsageLimit, StartDate, EndDate)
    VALUES (N'WELCOME10', N'Giảm 10% cho đơn từ 200.000đ', N'Percent', 10, 200000, 100000, 100, '20260101', '20271231');
GO

SELECT N'Done' AS Result, (SELECT COUNT(*) FROM Products) AS ProductCount, (SELECT COUNT(*) FROM ProductVariants) AS VariantCount;

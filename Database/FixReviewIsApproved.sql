-- Script để sửa lỗi IsApproved trong bảng Reviews
-- Lỗi: InvalidCastException khi cast System.Byte to System.Int32

USE FashionShop;
GO

-- Kiểm tra kiểu dữ liệu hiện tại của cột IsApproved
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'IsApproved';
GO

-- Nếu cột IsApproved là TINYINT (byte), chuyển thành BIT (boolean)
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reviews' 
    AND COLUMN_NAME = 'IsApproved' 
    AND DATA_TYPE = 'tinyint'
)
BEGIN
    PRINT 'Converting IsApproved from TINYINT to BIT...';
    
    -- Đảm bảo dữ liệu chỉ có giá trị 0 hoặc 1
    UPDATE Reviews SET IsApproved = 0 WHERE IsApproved IS NULL OR IsApproved > 1;
    
    -- Chuyển đổi kiểu cột
    ALTER TABLE Reviews ALTER COLUMN IsApproved BIT NOT NULL;
    
    PRINT 'Conversion completed successfully.';
END
ELSE
BEGIN
    PRINT 'IsApproved column is already BIT type or does not exist.';
END
GO

-- Kiểm tra lại sau khi sửa
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'IsApproved';
GO

-- Hiển thị một vài bản ghi để kiểm tra
SELECT TOP 5 ReviewId, UserId, ProductId, Rating, IsApproved, CreatedAt
FROM Reviews
ORDER BY ReviewId DESC;
GO

PRINT 'Done! IsApproved column has been fixed.';

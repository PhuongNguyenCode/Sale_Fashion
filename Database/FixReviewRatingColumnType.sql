-- Script to fix Rating column type in Reviews table
-- Error: InvalidCastException when casting System.Byte to System.Int32
-- Root Cause: Rating column is TINYINT but model expects INT

USE FashionShop;
GO

-- Check current data type of Rating column
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'Rating';
GO

-- Convert Rating from TINYINT to INT
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reviews' 
    AND COLUMN_NAME = 'Rating' 
    AND DATA_TYPE = 'tinyint'
)
BEGIN
    PRINT 'Converting Rating from TINYINT to INT...';
    
    -- Convert column type
    ALTER TABLE Reviews ALTER COLUMN Rating INT NOT NULL;
    
    PRINT 'Conversion completed successfully.';
END
ELSE
BEGIN
    PRINT 'Rating column is already INT type or does not exist.';
END
GO

-- Verify after fix
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'Rating';
GO

-- Display a few records to verify
SELECT TOP 5 ReviewId, UserId, ProductId, Rating, IsApproved, CreatedAt
FROM Reviews
ORDER BY ReviewId DESC;
GO

PRINT 'Done! Rating column has been fixed to INT type.';

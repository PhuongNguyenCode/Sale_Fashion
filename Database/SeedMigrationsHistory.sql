-- Seed __EFMigrationsHistory table with existing migrations
-- This is needed when database was created manually without running migrations

USE FashionShop;
GO

-- Insert all existing migrations that have already been applied to the database
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES 
    ('20260904141845_AddLastLoginAt', '9.0.0'),
    ('20260905140000_AddGuestCartSession', '9.0.0'),
    ('20260907074553_AddPaymentIntegrationSystem', '9.0.0'),
    ('20260908061557_FixReviewIsApprovedColumnType', '9.0.0');

GO

-- Verify the inserted records
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
GO

PRINT 'Migrations history seeded successfully.';

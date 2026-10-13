-- Payment Integration System Manual Migration
-- This script adds payment-related tables and columns to existing database

SET QUOTED_IDENTIFIER ON;
GO

-- Check if PaymentTransactions table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    PRINT 'Creating PaymentTransactions table...';
    
    CREATE TABLE [dbo].[PaymentTransactions] (
        [TransactionId] INT NOT NULL IDENTITY(1,1),
        [PaymentId] INT NOT NULL,
        [Gateway] NVARCHAR(50) NOT NULL,
        [ExternalTransactionId] NVARCHAR(100) NOT NULL,
        [Amount] DECIMAL(18,2) NOT NULL,
        [Currency] NVARCHAR(10) NOT NULL DEFAULT 'VND',
        [Status] NVARCHAR(30) NOT NULL DEFAULT 'Pending',
        [ResponseCode] NVARCHAR(50) NULL,
        [ResponseMessage] NVARCHAR(500) NULL,
        [RawResponse] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY ([TransactionId]),
        CONSTRAINT [FK_PaymentTransactions_Payments] FOREIGN KEY ([PaymentId]) 
            REFERENCES [dbo].[Payments]([PaymentId]) ON DELETE CASCADE
    );
    
    -- Create unique index on ExternalTransactionId for idempotency
    CREATE UNIQUE INDEX [IX_PaymentTransactions_ExternalTransactionId] 
        ON [dbo].[PaymentTransactions]([ExternalTransactionId]);
    
    -- Create index on PaymentId
    CREATE INDEX [IX_PaymentTransactions_PaymentId] 
        ON [dbo].[PaymentTransactions]([PaymentId]);
    
    PRINT 'PaymentTransactions table created successfully.';
END
ELSE
BEGIN
    PRINT 'PaymentTransactions table already exists.';
END
GO

-- Add new columns to Payments table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'PaymentGateway')
BEGIN
    PRINT 'Adding PaymentGateway column to Payments table...';
    ALTER TABLE [dbo].[Payments] ADD [PaymentGateway] NVARCHAR(50) NULL;
    PRINT 'PaymentGateway column added.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'CreatedAt')
BEGIN
    PRINT 'Adding CreatedAt column to Payments table...';
    ALTER TABLE [dbo].[Payments] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT 'CreatedAt column added.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'ExpiresAt')
BEGIN
    PRINT 'Adding ExpiresAt column to Payments table...';
    ALTER TABLE [dbo].[Payments] ADD [ExpiresAt] DATETIME2 NULL;
    PRINT 'ExpiresAt column added.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'Version')
BEGIN
    PRINT 'Adding Version column to Payments table...';
    ALTER TABLE [dbo].[Payments] ADD [Version] INT NULL DEFAULT 1;
    PRINT 'Version column added.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'UpdatedAt')
BEGIN
    PRINT 'Adding UpdatedAt column to Payments table...';
    ALTER TABLE [dbo].[Payments] ADD [UpdatedAt] DATETIME2 NULL;
    PRINT 'UpdatedAt column added.';
END
GO

-- Modify PaymentDate to be nullable if it isn't already
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'PaymentDate' AND is_nullable = 0)
BEGIN
    PRINT 'Making PaymentDate nullable...';
    ALTER TABLE [dbo].[Payments] ALTER COLUMN [PaymentDate] DATETIME2 NULL;
    PRINT 'PaymentDate is now nullable.';
END
GO

-- Create indexes on Payments table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'IX_Payments_Status_CreatedAt')
BEGIN
    PRINT 'Creating index on Status and CreatedAt...';
    CREATE INDEX [IX_Payments_Status_CreatedAt] ON [dbo].[Payments]([Status], [CreatedAt]);
    PRINT 'Index created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'IX_Payments_ExpiresAt')
BEGIN
    PRINT 'Creating filtered index on ExpiresAt...';
    CREATE INDEX [IX_Payments_ExpiresAt] ON [dbo].[Payments]([ExpiresAt])
        WHERE [Status] IN ('Pending', 'Processing');
    PRINT 'Filtered index created.';
END
GO

PRINT '';
PRINT 'Payment Integration System migration completed successfully!';
PRINT 'New features added:';
PRINT '  - PaymentTransactions table for tracking all payment gateway transactions';
PRINT '  - Enhanced Payments table with gateway tracking and expiration';
PRINT '  - Indexes for performance optimization';
PRINT '';

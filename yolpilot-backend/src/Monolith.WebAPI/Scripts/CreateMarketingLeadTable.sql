-- MarketingLead table creation script
-- Run this in Azure SQL Database

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MarketingLeads' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[MarketingLeads] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Email] nvarchar(255) NOT NULL,
        [Company] nvarchar(200) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [VehicleCount] nvarchar(20) NULL,
        [Message] nvarchar(1000) NULL,
        [Source] nvarchar(50) NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_MarketingLeads] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Add indexes for performance
    CREATE INDEX [IX_MarketingLeads_Status] ON [dbo].[MarketingLeads] ([Status]);
    CREATE INDEX [IX_MarketingLeads_CreatedAt] ON [dbo].[MarketingLeads] ([CreatedAt]);
    CREATE INDEX [IX_MarketingLeads_Source] ON [dbo].[MarketingLeads] ([Source]);
    CREATE INDEX [IX_MarketingLeads_Email] ON [dbo].[MarketingLeads] ([Email]);

    PRINT 'MarketingLeads table created successfully';
END
ELSE
BEGIN
    PRINT 'MarketingLeads table already exists';
END

-- Verify table structure
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'MarketingLeads'
ORDER BY ORDINAL_POSITION;
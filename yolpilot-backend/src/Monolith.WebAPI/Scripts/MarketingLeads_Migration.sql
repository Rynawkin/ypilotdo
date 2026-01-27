-- Marketing Leads Table Creation Script for Azure SQL
-- Bu script'i Azure SQL Database'de çalıştırın

-- Create MarketingLeads table
CREATE TABLE [dbo].[MarketingLeads] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [Company] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [VehicleCount] nvarchar(50) NULL,
    [Message] nvarchar(1000) NULL,
    [Source] nvarchar(50) NOT NULL DEFAULT 'website',
    [SelectedPlan] nvarchar(50) NULL,
    [Status] int NOT NULL DEFAULT 0,
    [AdminNotes] nvarchar(1000) NULL,
    [ContactedAt] datetime2(7) NULL,
    [ClosedAt] datetime2(7) NULL,
    [AssignedTo] nvarchar(255) NULL,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] datetime2(7) NULL DEFAULT GETUTCDATE(),
    [IsDeleted] bit NOT NULL DEFAULT 0,

    CONSTRAINT [PK_MarketingLeads] PRIMARY KEY CLUSTERED ([Id] ASC)
);

-- Create indexes for better performance
CREATE NONCLUSTERED INDEX [IX_MarketingLeads_Email] ON [dbo].[MarketingLeads] ([Email]);
CREATE NONCLUSTERED INDEX [IX_MarketingLeads_Status] ON [dbo].[MarketingLeads] ([Status]);
CREATE NONCLUSTERED INDEX [IX_MarketingLeads_CreatedAt] ON [dbo].[MarketingLeads] ([CreatedAt] DESC);
CREATE NONCLUSTERED INDEX [IX_MarketingLeads_Source] ON [dbo].[MarketingLeads] ([Source]);

-- Lead Status Enum Values:
-- 0 = New
-- 1 = Contacted
-- 2 = Qualified
-- 3 = Demo
-- 4 = Proposal
-- 5 = Won
-- 6 = Lost
-- 7 = Archived

GO

PRINT 'MarketingLeads table created successfully!'
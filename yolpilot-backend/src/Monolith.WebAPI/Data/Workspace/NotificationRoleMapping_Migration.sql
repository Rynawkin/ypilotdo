-- NotificationRoleMapping table creation and default data migration
-- This script handles both new installations and existing data

-- Create table if not exists
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NotificationRoleMapping' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[NotificationRoleMapping] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [WorkspaceId] int NOT NULL,
        [NotificationType] nvarchar(100) NOT NULL,
        [ContactRole] nvarchar(50) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_NotificationRoleMapping] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_NotificationRoleMapping] UNIQUE ([WorkspaceId], [NotificationType], [ContactRole]),
        CONSTRAINT [FK_NotificationRoleMapping_Workspaces] FOREIGN KEY ([WorkspaceId]) REFERENCES [dbo].[Workspaces] ([Id]) ON DELETE CASCADE
    );
    
    PRINT 'NotificationRoleMapping table created successfully.';
END
ELSE
BEGIN
    PRINT 'NotificationRoleMapping table already exists.';
END

-- Insert default mappings for all existing workspaces using MERGE to avoid duplicates
DECLARE @workspaceId INT;
DECLARE workspace_cursor CURSOR FOR SELECT Id FROM [dbo].[Workspaces];

OPEN workspace_cursor;
FETCH NEXT FROM workspace_cursor INTO @workspaceId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Use MERGE to insert default mappings without creating duplicates
    WITH DefaultMappings AS (
        SELECT @workspaceId as WorkspaceId, NotificationType, ContactRole, IsEnabled FROM (VALUES
            ('JourneyStart', 'DepoSorumlusu', 1),
            ('JourneyStart', 'SatinalmasorumluSu', 1),
            ('JourneyStart', 'MuhasebeSorumlusu', 0),
            ('JourneyStart', 'Diger', 0),
            
            ('JourneyCheckIn', 'DepoSorumlusu', 1),
            ('JourneyCheckIn', 'SatinalmasorumluSu', 1),
            ('JourneyCheckIn', 'MuhasebeSorumlusu', 0),
            ('JourneyCheckIn', 'Diger', 1),
            
            ('DeliveryCompleted', 'DepoSorumlusu', 1),
            ('DeliveryCompleted', 'SatinalmasorumluSu', 1),
            ('DeliveryCompleted', 'MuhasebeSorumlusu', 1),
            ('DeliveryCompleted', 'Diger', 1),
            
            ('DeliveryFailed', 'DepoSorumlusu', 1),
            ('DeliveryFailed', 'SatinalmasorumluSu', 1),
            ('DeliveryFailed', 'MuhasebeSorumlusu', 1),
            ('DeliveryFailed', 'Diger', 0),
            
            ('JourneyAssigned', 'DepoSorumlusu', 1),
            ('JourneyAssigned', 'SatinalmasorumluSu', 1),
            ('JourneyAssigned', 'MuhasebeSorumlusu', 0),
            ('JourneyAssigned', 'Diger', 0),
            
            ('JourneyCancelled', 'DepoSorumlusu', 1),
            ('JourneyCancelled', 'SatinalmasorumluSu', 1),
            ('JourneyCancelled', 'MuhasebeSorumlusu', 1),
            ('JourneyCancelled', 'Diger', 0)
        ) AS MappingData(NotificationType, ContactRole, IsEnabled)
    )
    MERGE [dbo].[NotificationRoleMapping] AS target
    USING DefaultMappings AS source
    ON (target.WorkspaceId = source.WorkspaceId 
        AND target.NotificationType = source.NotificationType 
        AND target.ContactRole = source.ContactRole)
    WHEN NOT MATCHED THEN
        INSERT (WorkspaceId, NotificationType, ContactRole, IsEnabled, CreatedAt, UpdatedAt)
        VALUES (source.WorkspaceId, source.NotificationType, source.ContactRole, source.IsEnabled, GETUTCDATE(), GETUTCDATE());
    
    FETCH NEXT FROM workspace_cursor INTO @workspaceId;
END

CLOSE workspace_cursor;
DEALLOCATE workspace_cursor;

PRINT 'Default notification role mappings have been created for all workspaces.';
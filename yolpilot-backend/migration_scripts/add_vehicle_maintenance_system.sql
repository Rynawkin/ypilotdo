-- Migration Script: Add Vehicle Maintenance System
-- Date: 2025-10-02
-- Description: Creates VehicleMaintenance and MaintenanceReminder tables

-- Create VehicleMaintenance table
CREATE TABLE [dbo].[VehicleMaintenance] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [VehicleId] INT NOT NULL,
    [WorkspaceId] INT NOT NULL,
    [Type] NVARCHAR(50) NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Cost] DECIMAL(18,2) NULL,
    [MaintenanceDate] DATETIME2(7) NOT NULL,
    [NextMaintenanceDate] DATETIME2(7) NULL,
    [MaintenanceKm] INT NULL,
    [NextMaintenanceKm] INT NULL,
    [Workshop] NVARCHAR(200) NULL,
    [Parts] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    [ReminderDays] INT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [DeletedAt] DATETIME2(7) NULL,
    CONSTRAINT [PK_VehicleMaintenance] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_VehicleMaintenance_Vehicle] FOREIGN KEY ([VehicleId])
        REFERENCES [dbo].[Vehicles]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_VehicleMaintenance_Workspace] FOREIGN KEY ([WorkspaceId])
        REFERENCES [dbo].[Workspaces]([Id]) ON DELETE NO ACTION
);

-- Create index for VehicleId
CREATE NONCLUSTERED INDEX [IX_VehicleMaintenance_VehicleId]
    ON [dbo].[VehicleMaintenance]([VehicleId] ASC);

-- Create index for WorkspaceId
CREATE NONCLUSTERED INDEX [IX_VehicleMaintenance_WorkspaceId]
    ON [dbo].[VehicleMaintenance]([WorkspaceId] ASC);

-- Create index for soft delete
CREATE NONCLUSTERED INDEX [IX_VehicleMaintenance_DeletedAt]
    ON [dbo].[VehicleMaintenance]([DeletedAt] ASC);

-- Create MaintenanceReminder table
CREATE TABLE [dbo].[MaintenanceReminder] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [VehicleId] INT NOT NULL,
    [MaintenanceId] INT NOT NULL,
    [WorkspaceId] INT NOT NULL,
    [ReminderDays] INT NOT NULL,
    [NextMaintenanceDate] DATETIME2(7) NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [SentAt] DATETIME2(7) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_MaintenanceReminder] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MaintenanceReminder_Vehicle] FOREIGN KEY ([VehicleId])
        REFERENCES [dbo].[Vehicles]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MaintenanceReminder_Maintenance] FOREIGN KEY ([MaintenanceId])
        REFERENCES [dbo].[VehicleMaintenance]([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaintenanceReminder_Workspace] FOREIGN KEY ([WorkspaceId])
        REFERENCES [dbo].[Workspaces]([Id]) ON DELETE NO ACTION
);

-- Create index for VehicleId
CREATE NONCLUSTERED INDEX [IX_MaintenanceReminder_VehicleId]
    ON [dbo].[MaintenanceReminder]([VehicleId] ASC);

-- Create index for MaintenanceId
CREATE NONCLUSTERED INDEX [IX_MaintenanceReminder_MaintenanceId]
    ON [dbo].[MaintenanceReminder]([MaintenanceId] ASC);

-- Create index for WorkspaceId
CREATE NONCLUSTERED INDEX [IX_MaintenanceReminder_WorkspaceId]
    ON [dbo].[MaintenanceReminder]([WorkspaceId] ASC);

-- Create index for IsActive (for background job queries)
CREATE NONCLUSTERED INDEX [IX_MaintenanceReminder_IsActive]
    ON [dbo].[MaintenanceReminder]([IsActive] ASC);

GO

PRINT 'Vehicle Maintenance System tables created successfully!';

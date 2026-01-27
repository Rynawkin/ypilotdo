IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Logs] (
        [Id] int NOT NULL IDENTITY,
        [Message] nvarchar(max) NULL,
        [Route] nvarchar(max) NULL,
        [Method] nvarchar(max) NULL,
        [Query] nvarchar(max) NULL,
        [Body] nvarchar(max) NULL,
        [StackTrace] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Logs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NULL,
        [Token] nvarchar(max) NULL,
        [BearerToken] nvarchar(max) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [TempMembers] (
        [Token] uniqueidentifier NOT NULL,
        [FullName] nvarchar(128) NULL,
        [Email] nvarchar(64) NULL,
        [Roles] nvarchar(max) NULL,
        [IsSaved] bit NOT NULL,
        [InviterId] nvarchar(max) NULL,
        [WorkspaceId] int NOT NULL,
        [DepotId] int NOT NULL,
        [Id] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_TempMembers] PRIMARY KEY ([Token])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Workspaces] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NULL,
        [PhoneNumber] nvarchar(16) NULL,
        [Email] nvarchar(64) NULL,
        [DistanceUnit] nvarchar(10) NULL,
        [Currency] nvarchar(10) NULL,
        [TimeZone] nvarchar(30) NULL,
        [Language] nvarchar(30) NULL,
        [CostPerKm] float NULL,
        [CostPerHour] float NULL,
        [DefaultServiceTime] time NOT NULL,
        [MaximumDriverCount] int NOT NULL,
        [Active] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Workspaces] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [WorkspaceId] int NOT NULL,
        [Code] nvarchar(max) NULL,
        [Name] nvarchar(max) NULL,
        [Address] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [Priority] nvarchar(max) NULL,
        [EstimatedServiceTime] int NOT NULL,
        [TimeWindowStart] nvarchar(max) NULL,
        [TimeWindowEnd] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [Tags] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Depots] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NULL,
        [Address] nvarchar(1024) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [StartWorkingHours] time NOT NULL,
        [EndWorkingHours] time NULL,
        [WorkspaceId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Depots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Depots_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [SavedLocations] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NULL,
        [Address] nvarchar(1024) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [WorkspaceId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SavedLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SavedLocations_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] int NOT NULL IDENTITY,
        [WorkspaceId] int NOT NULL,
        [PlateNumber] nvarchar(20) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [Brand] nvarchar(50) NOT NULL,
        [Model] nvarchar(50) NOT NULL,
        [Year] int NOT NULL,
        [Capacity] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [FuelType] nvarchar(20) NULL,
        [DeletedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Vehicles_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(100) NULL,
        [LicenseNumber] nvarchar(100) NULL,
        [IsDriver] bit NOT NULL,
        [IsDispatcher] bit NOT NULL,
        [IsAdmin] bit NOT NULL,
        [IsOnboarded] bit NOT NULL,
        [DepotId] int NULL,
        [WorkspaceId] int NOT NULL,
        [DriverStatus] nvarchar(20) NULL,
        [CurrentLatitude] float NULL,
        [CurrentLongitude] float NULL,
        [Rating] decimal(18,2) NULL,
        [TotalDeliveries] int NOT NULL,
        [AssignedVehicleId] int NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUsers_Depots_DepotId] FOREIGN KEY ([DepotId]) REFERENCES [Depots] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Vehicles_AssignedVehicleId] FOREIGN KEY ([AssignedVehicleId]) REFERENCES [Vehicles] ([Id]),
        CONSTRAINT [FK_AspNetUsers_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Drivers] (
        [Id] int NOT NULL IDENTITY,
        [WorkspaceId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Email] nvarchar(100) NULL,
        [LicenseNumber] nvarchar(50) NOT NULL,
        [VehicleId] int NULL,
        [Status] nvarchar(20) NOT NULL,
        [CurrentLatitude] float NULL,
        [CurrentLongitude] float NULL,
        [Avatar] nvarchar(255) NULL,
        [Rating] float NULL,
        [TotalDeliveries] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Drivers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Drivers_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Drivers_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Routes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Date] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [DriverId] int NULL,
        [VehicleId] int NULL,
        [TotalDistance] float NULL,
        [TotalDuration] int NULL,
        [TotalDeliveries] int NOT NULL,
        [CompletedDeliveries] int NOT NULL,
        [Optimized] bit NOT NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [WorkspaceId] int NOT NULL,
        [DepotId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Routes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Routes_Depots_DepotId] FOREIGN KEY ([DepotId]) REFERENCES [Depots] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Routes_Drivers_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Drivers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Routes_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Routes_Workspaces_WorkspaceId] FOREIGN KEY ([WorkspaceId]) REFERENCES [Workspaces] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [Journeys] (
        [Id] int NOT NULL IDENTITY,
        [Polyline] nvarchar(max) NULL,
        [Date] datetime2 NOT NULL,
        [StartedAt] datetime2 NULL,
        [FinishedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [RouteId] int NOT NULL,
        [DriverId] int NOT NULL,
        [VehicleId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Journeys] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Journeys_Drivers_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Drivers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Journeys_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Journeys_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [RouteEndDetails] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [RouteId] int NOT NULL,
        CONSTRAINT [PK_RouteEndDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RouteEndDetails_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [RouteStartDetails] (
        [Id] int NOT NULL IDENTITY,
        [StartTime] time NOT NULL,
        [Name] nvarchar(200) NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [RouteId] int NOT NULL,
        CONSTRAINT [PK_RouteStartDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RouteStartDetails_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [RouteStops] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(128) NULL,
        [Address] nvarchar(1024) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [Notes] nvarchar(1024) NULL,
        [ContactFullName] nvarchar(128) NULL,
        [ContactPhone] nvarchar(16) NULL,
        [ContactEmail] nvarchar(64) NULL,
        [Type] int NOT NULL,
        [OrderType] int NOT NULL,
        [ProofOfDeliveryRequired] bit NOT NULL,
        [ArriveBetweenStart] time NULL,
        [ArriveBetweenEnd] time NULL,
        [ServiceTime] time NULL,
        [Order] int NOT NULL,
        [RouteId] int NOT NULL,
        [CustomerId] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_RouteStops] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RouteStops_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]),
        CONSTRAINT [FK_RouteStops_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [JourneyEndDetails] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [JourneyId] int NOT NULL,
        CONSTRAINT [PK_JourneyEndDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JourneyEndDetails_Journeys_JourneyId] FOREIGN KEY ([JourneyId]) REFERENCES [Journeys] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [JourneyStartDetails] (
        [Id] int NOT NULL IDENTITY,
        [StartTime] time NOT NULL,
        [Name] nvarchar(200) NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [JourneyId] int NOT NULL,
        CONSTRAINT [PK_JourneyStartDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JourneyStartDetails_Journeys_JourneyId] FOREIGN KEY ([JourneyId]) REFERENCES [Journeys] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [JourneyStops] (
        [Id] int NOT NULL IDENTITY,
        [JourneyId] int NOT NULL,
        [StopId] int NOT NULL,
        [RouteStopId] int NOT NULL,
        [Status] int NOT NULL,
        [Order] int NOT NULL,
        [Distance] float NOT NULL,
        [StartAddress] nvarchar(max) NULL,
        [StartLatitude] float NOT NULL,
        [StartLongitude] float NOT NULL,
        [EndAddress] nvarchar(max) NULL,
        [EndLatitude] float NOT NULL,
        [EndLongitude] float NOT NULL,
        [EstimatedArrivalTime] time NOT NULL,
        [EstimatedDepartureTime] time NULL,
        [ArriveBetweenStart] time NULL,
        [ArriveBetweenEnd] time NULL,
        CONSTRAINT [PK_JourneyStops] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JourneyStops_Journeys_JourneyId] FOREIGN KEY ([JourneyId]) REFERENCES [Journeys] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_JourneyStops_RouteStops_RouteStopId] FOREIGN KEY ([RouteStopId]) REFERENCES [RouteStops] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE TABLE [JourneyStatuses] (
        [Id] int NOT NULL IDENTITY,
        [JourneyId] int NOT NULL,
        [StopId] int NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(1024) NULL,
        [AdditionalValues] nvarchar(max) NULL,
        [Latitude] float NOT NULL,
        [Longitude] float NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_JourneyStatuses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JourneyStatuses_JourneyStops_StopId] FOREIGN KEY ([StopId]) REFERENCES [JourneyStops] ([Id]),
        CONSTRAINT [FK_JourneyStatuses_Journeys_JourneyId] FOREIGN KEY ([JourneyId]) REFERENCES [Journeys] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_AssignedVehicleId] ON [AspNetUsers] ([AssignedVehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_DepotId] ON [AspNetUsers] ([DepotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_WorkspaceId] ON [AspNetUsers] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_WorkspaceId] ON [Customers] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Depots_WorkspaceId] ON [Depots] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Drivers_VehicleId] ON [Drivers] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Drivers_WorkspaceId] ON [Drivers] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JourneyEndDetails_JourneyId] ON [JourneyEndDetails] ([JourneyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Journeys_DriverId] ON [Journeys] ([DriverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Journeys_RouteId] ON [Journeys] ([RouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Journeys_VehicleId] ON [Journeys] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JourneyStartDetails_JourneyId] ON [JourneyStartDetails] ([JourneyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JourneyStatuses_JourneyId] ON [JourneyStatuses] ([JourneyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JourneyStatuses_StopId] ON [JourneyStatuses] ([StopId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JourneyStops_JourneyId] ON [JourneyStops] ([JourneyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JourneyStops_RouteStopId] ON [JourneyStops] ([RouteStopId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RouteEndDetails_RouteId] ON [RouteEndDetails] ([RouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Routes_DepotId] ON [Routes] ([DepotId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Routes_DriverId] ON [Routes] ([DriverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Routes_VehicleId] ON [Routes] ([VehicleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Routes_WorkspaceId] ON [Routes] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RouteStartDetails_RouteId] ON [RouteStartDetails] ([RouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RouteStops_CustomerId] ON [RouteStops] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RouteStops_RouteId] ON [RouteStops] ([RouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SavedLocations_WorkspaceId] ON [SavedLocations] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vehicles_WorkspaceId] ON [Vehicles] ([WorkspaceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813164920_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250813164920_InitialCreate', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813192624_AddVehiclesDbSet'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250813192624_AddVehiclesDbSet', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813194130_AddVehiclesTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250813194130_AddVehiclesTable', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813215026_AddDepotIsDefaultAndWorkingHours'
)
BEGIN
    ALTER TABLE [Depots] ADD [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813215026_AddDepotIsDefaultAndWorkingHours'
)
BEGIN
    ALTER TABLE [Depots] ADD [WorkingHoursJson] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250813215026_AddDepotIsDefaultAndWorkingHours'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250813215026_AddDepotIsDefaultAndWorkingHours', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Workspaces] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [TempMembers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [SavedLocations] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [RouteStops] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Routes] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Logs] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [JourneyStatuses] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Journeys] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Drivers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Depots] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    ALTER TABLE [Customers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814162147_AddIsDeletedToBaseEntity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250814162147_AddIsDeletedToBaseEntity', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250814193940_AddCustomerIdToRouteStop'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250814193940_AddCustomerIdToRouteStop', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250815203241_AddCancelledAtToJourney'
)
BEGIN
    ALTER TABLE [Journeys] ADD [CancelledAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250815203241_AddCancelledAtToJourney'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250815203241_AddCancelledAtToJourney', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250816140318_AddJourneyStatusFields'
)
BEGIN
    ALTER TABLE [JourneyStatuses] ADD [FailureReason] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250816140318_AddJourneyStatusFields'
)
BEGIN
    ALTER TABLE [JourneyStatuses] ADD [PhotoBase64] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250816140318_AddJourneyStatusFields'
)
BEGIN
    ALTER TABLE [JourneyStatuses] ADD [SignatureBase64] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20250816140318_AddJourneyStatusFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20250816140318_AddJourneyStatusFields', N'8.0.8');
END;
GO

COMMIT;
GO


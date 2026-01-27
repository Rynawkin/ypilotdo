-- Migration script to add StartFuel, EndFuel, and VehicleCondition columns to Journeys table
-- Created: 2025-01-XX
-- Description: Adds fuel level and vehicle condition tracking fields to Journey entity

USE [RotaAppDB]
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Journeys]') AND name = 'StartFuel')
BEGIN
    ALTER TABLE [dbo].[Journeys]
    ADD [StartFuel] NVARCHAR(50) NULL;

    PRINT 'Column [StartFuel] added successfully to [dbo].[Journeys]';
END
ELSE
BEGIN
    PRINT 'Column [StartFuel] already exists in [dbo].[Journeys]';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Journeys]') AND name = 'EndFuel')
BEGIN
    ALTER TABLE [dbo].[Journeys]
    ADD [EndFuel] NVARCHAR(50) NULL;

    PRINT 'Column [EndFuel] added successfully to [dbo].[Journeys]';
END
ELSE
BEGIN
    PRINT 'Column [EndFuel] already exists in [dbo].[Journeys]';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Journeys]') AND name = 'VehicleCondition')
BEGIN
    ALTER TABLE [dbo].[Journeys]
    ADD [VehicleCondition] NVARCHAR(50) NULL;

    PRINT 'Column [VehicleCondition] added successfully to [dbo].[Journeys]';
END
ELSE
BEGIN
    PRINT 'Column [VehicleCondition] already exists in [dbo].[Journeys]';
END
GO

-- Verify the changes
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Journeys'
    AND COLUMN_NAME IN ('StartFuel', 'EndFuel', 'VehicleCondition')
ORDER BY COLUMN_NAME;
GO

PRINT 'Migration completed successfully!';
GO

-- NOTES:
-- StartFuel values: full, three_quarters, half, quarter, empty
-- EndFuel values: full, three_quarters, half, quarter, empty
-- VehicleCondition values: good, needs_cleaning, needs_maintenance, damaged

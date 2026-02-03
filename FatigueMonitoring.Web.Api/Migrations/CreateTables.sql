-- Fatigue Monitoring Dashboard - Database Schema
-- Run this script to create all aggregation tables

-- 1. AI_FatigueEvent_T - Raw fatigue event data
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_FatigueEvent_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_FatigueEvent_T] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [ExternalId] NVARCHAR(450) NOT NULL,
        [Identity] NVARCHAR(MAX) NOT NULL,
        [AlarmName] NVARCHAR(MAX) NOT NULL,
        [AlarmType] NVARCHAR(MAX) NOT NULL,
        [EventTime] DATETIME2 NOT NULL,
        [ServerTime] DATETIME2 NOT NULL,
        [Shift] NVARCHAR(MAX) NOT NULL,
        [ShiftDate] DATETIME2 NOT NULL,
        [Level] INT NOT NULL,
        [Speed] DECIMAL(10,2) NOT NULL,
        [IsFollowedUp] BIT NOT NULL,
        [Latitude] DECIMAL(12,8) NOT NULL,
        [Longitude] DECIMAL(12,8) NOT NULL,
        [GeofenceId] NVARCHAR(MAX) NULL,
        [DeviceId] NVARCHAR(MAX) NOT NULL,
        [DriverId] NVARCHAR(MAX) NULL,
        [ManualVerificationBy] NVARCHAR(MAX) NULL,
        [ManualVerificationTime] DATETIME2 NULL,
        [ManualVerificationMemo] NVARCHAR(MAX) NULL,
        [ManualVerificationWaitingDuration] INT NULL,
        [DeviceImei] NVARCHAR(MAX) NOT NULL,
        [UnitName] NVARCHAR(MAX) NOT NULL,
        [GroupName] NVARCHAR(MAX) NOT NULL,
        [Area] NVARCHAR(450) NOT NULL,
        [Location] NVARCHAR(MAX) NOT NULL,
        [ImageUrl] NVARCHAR(MAX) NULL,
        [VideoUrl] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
    
    CREATE UNIQUE INDEX [IX_AI_FatigueEvent_T_ExternalId] ON [dbo].[AI_FatigueEvent_T] ([ExternalId]);
    CREATE INDEX [IX_AI_FatigueEvent_T_EventTime] ON [dbo].[AI_FatigueEvent_T] ([EventTime]);
    CREATE INDEX [IX_AI_FatigueEvent_T_IsFollowedUp] ON [dbo].[AI_FatigueEvent_T] ([IsFollowedUp]);
    CREATE INDEX [IX_AI_FatigueEvent_T_Area] ON [dbo].[AI_FatigueEvent_T] ([Area]);
END
GO

-- 2. AI_DashboardSummary_T - Aggregated KPI data
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_DashboardSummary_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_DashboardSummary_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FilterType] NVARCHAR(450) NOT NULL,
        [TotalAlarms] INT NOT NULL,
        [FollowedUp] INT NOT NULL,
        [WaitingFollowUp] INT NOT NULL,
        [MiningTotal] INT NOT NULL,
        [MiningOpen] INT NOT NULL,
        [MiningResolved] INT NOT NULL,
        [HaulingTotal] INT NOT NULL,
        [HaulingOpen] INT NOT NULL,
        [HaulingResolved] INT NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_DashboardSummary_T_FilterType] ON [dbo].[AI_DashboardSummary_T] ([FilterType]);
END
GO

-- 3. AI_AreaDistribution_T - Area distribution data
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_AreaDistribution_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_AreaDistribution_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Area] NVARCHAR(450) NOT NULL,
        [GroupName] NVARCHAR(450) NOT NULL,
        [Location] NVARCHAR(450) NOT NULL,
        [OpenAlertCount] INT NOT NULL,
        [TotalAlertCount] INT NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_AreaDistribution_T_Area_GroupName] ON [dbo].[AI_AreaDistribution_T] ([Area], [GroupName]);
END
GO

-- Add GroupName column if table exists but column doesn't
IF EXISTS (SELECT * FROM sysobjects WHERE name='AI_AreaDistribution_T' AND xtype='U')
   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AI_AreaDistribution_T') AND name = 'GroupName')
BEGIN
    ALTER TABLE [dbo].[AI_AreaDistribution_T] ADD [GroupName] NVARCHAR(450) NOT NULL DEFAULT '';
END
GO

-- 4. AI_ActiveAlert_T - Active alerts
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_ActiveAlert_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_ActiveAlert_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FatigueEventId] UNIQUEIDENTIFIER NOT NULL,
        [ExternalId] NVARCHAR(MAX) NOT NULL,
        [UnitName] NVARCHAR(MAX) NOT NULL,
        [OperatorName] NVARCHAR(MAX) NOT NULL,
        [AlertType] NVARCHAR(MAX) NOT NULL,
        [Area] NVARCHAR(450) NOT NULL,
        [Location] NVARCHAR(MAX) NOT NULL,
        [EventTime] DATETIME2 NOT NULL,
        [OpenDurationMinutes] INT NOT NULL,
        [Status] NVARCHAR(450) NOT NULL,
        [Speed] DECIMAL(10,2) NOT NULL,
        [AlertCountToday] INT NOT NULL,
        [ImageUrl] NVARCHAR(MAX) NULL,
        [VideoUrl] NVARCHAR(MAX) NULL,
        [Latitude] DECIMAL(12,8) NOT NULL,
        [Longitude] DECIMAL(12,8) NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_ActiveAlert_T_FatigueEventId] ON [dbo].[AI_ActiveAlert_T] ([FatigueEventId]);
    CREATE INDEX [IX_AI_ActiveAlert_T_Status] ON [dbo].[AI_ActiveAlert_T] ([Status]);
    CREATE INDEX [IX_AI_ActiveAlert_T_Area] ON [dbo].[AI_ActiveAlert_T] ([Area]);
END
GO

-- 5. AI_DelayedFollowUp_T - Delayed follow-ups (>30 min)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_DelayedFollowUp_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_DelayedFollowUp_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FatigueEventId] UNIQUEIDENTIFIER NOT NULL,
        [ExternalId] NVARCHAR(MAX) NOT NULL,
        [UnitName] NVARCHAR(MAX) NOT NULL,
        [OperatorName] NVARCHAR(MAX) NOT NULL,
        [AlertType] NVARCHAR(MAX) NOT NULL,
        [Area] NVARCHAR(MAX) NOT NULL,
        [Location] NVARCHAR(MAX) NOT NULL,
        [EventTime] DATETIME2 NOT NULL,
        [DelayMinutes] INT NOT NULL,
        [Speed] DECIMAL(10,2) NOT NULL,
        [ImageUrl] NVARCHAR(MAX) NULL,
        [VideoUrl] NVARCHAR(MAX) NULL,
        [Latitude] DECIMAL(12,8) NOT NULL,
        [Longitude] DECIMAL(12,8) NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_DelayedFollowUp_T_FatigueEventId] ON [dbo].[AI_DelayedFollowUp_T] ([FatigueEventId]);
    CREATE INDEX [IX_AI_DelayedFollowUp_T_DelayMinutes] ON [dbo].[AI_DelayedFollowUp_T] ([DelayMinutes]);
END
GO

-- 6. AI_RecurrentUnit_T - Recurrent units
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_RecurrentUnit_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_RecurrentUnit_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UnitName] NVARCHAR(450) NOT NULL,
        [OperatorName] NVARCHAR(MAX) NOT NULL,
        [DeviceId] NVARCHAR(MAX) NOT NULL,
        [EventCount] INT NOT NULL,
        [PrimaryArea] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(MAX) NOT NULL,
        [FromDate] DATETIME2 NOT NULL,
        [ToDate] DATETIME2 NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_RecurrentUnit_T_UnitName] ON [dbo].[AI_RecurrentUnit_T] ([UnitName]);
    CREATE INDEX [IX_AI_RecurrentUnit_T_EventCount] ON [dbo].[AI_RecurrentUnit_T] ([EventCount]);
END
GO

-- 7. AI_HighRiskArea_T - High risk areas
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_HighRiskArea_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_HighRiskArea_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Location] NVARCHAR(450) NOT NULL,
        [Area] NVARCHAR(MAX) NOT NULL,
        [EventCount] INT NOT NULL,
        [RiskLevel] NVARCHAR(MAX) NOT NULL,
        [FromDate] DATETIME2 NOT NULL,
        [ToDate] DATETIME2 NOT NULL,
        [LastCalculatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_HighRiskArea_T_Location] ON [dbo].[AI_HighRiskArea_T] ([Location]);
    CREATE INDEX [IX_AI_HighRiskArea_T_EventCount] ON [dbo].[AI_HighRiskArea_T] ([EventCount]);
END
GO

-- 8. AI_TokenCache_T - Token cache
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_TokenCache_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_TokenCache_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [TokenType] NVARCHAR(450) NOT NULL,
        [AccessToken] NVARCHAR(MAX) NOT NULL,
        [Token] NVARCHAR(MAX) NOT NULL,
        [Pid] NVARCHAR(MAX) NOT NULL,
        [CompanyId] NVARCHAR(MAX) NOT NULL,
        [CompanyName] NVARCHAR(MAX) NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
    
    CREATE INDEX [IX_AI_TokenCache_T_TokenType] ON [dbo].[AI_TokenCache_T] ([TokenType]);
END
GO

-- 9. AI_SyncState_T - Sync state tracking
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AI_SyncState_T' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AI_SyncState_T] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SyncType] NVARCHAR(450) NOT NULL,
        [LastSyncTime] DATETIME2 NOT NULL,
        [LastSyncRecordCount] INT NOT NULL,
        [LastSyncStatus] NVARCHAR(MAX) NOT NULL,
        [LastSyncError] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [UpdatedAt] DATETIME2 NOT NULL
    );
    
    CREATE UNIQUE INDEX [IX_AI_SyncState_T_SyncType] ON [dbo].[AI_SyncState_T] ([SyncType]);
END
GO

-- EF Core Migrations History Table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL PRIMARY KEY,
        [ProductVersion] NVARCHAR(32) NOT NULL
    );
    
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260201000000_InitialCreate', '10.0.0');
END
GO

PRINT 'All tables created successfully!'
GO

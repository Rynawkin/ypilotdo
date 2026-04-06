using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;

namespace Monolith.WebAPI.Services.Marketing;

public static class MarketingAnalyticsDatabaseInitializer
{
    public static async Task EnsureTablesAsync(AppDbContext context, ILogger logger)
    {
        const string createAnalyticsTableSql = @"
CREATE TABLE IF NOT EXISTS dbo.marketinganalyticsevents (
    id serial PRIMARY KEY,
    visitorid varchar(64) NOT NULL,
    sessionid varchar(64) NOT NULL,
    eventtype varchar(50) NOT NULL,
    eventname varchar(100) NULL,
    pagepath varchar(255) NULL,
    pagetitle varchar(200) NULL,
    referrer varchar(500) NULL,
    utmsource varchar(100) NULL,
    utmmedium varchar(100) NULL,
    utmcampaign varchar(100) NULL,
    utmcontent varchar(100) NULL,
    utmterm varchar(100) NULL,
    devicetype varchar(50) NULL,
    browser varchar(120) NULL,
    os varchar(120) NULL,
    iphash varchar(128) NULL,
    ipaddress varchar(64) NULL,
    countrycode varchar(8) NULL,
    countryname varchar(120) NULL,
    region varchar(120) NULL,
    city varchar(120) NULL,
    useragent varchar(500) NULL,
    leadid integer NULL,
    metadatajson varchar(4000) NULL,
    occurredat timestamp without time zone NOT NULL DEFAULT now(),
    createdat timestamp without time zone NOT NULL DEFAULT now(),
    updatedat timestamp without time zone NULL,
    isdeleted boolean NOT NULL DEFAULT false,
    CONSTRAINT fk_marketinganalyticsevents_marketingleads_leadid
        FOREIGN KEY (leadid) REFERENCES dbo.marketingleads (id) ON DELETE SET NULL
);";

        var alterLeadColumnsSql = new[]
        {
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS landingpage varchar(255) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS referrer varchar(500) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS utmsource varchar(100) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS utmmedium varchar(100) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS utmcampaign varchar(100) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS utmcontent varchar(100) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS utmterm varchar(100) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS visitorid varchar(64) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketingleads ADD COLUMN IF NOT EXISTS sessionid varchar(64) NULL;"
        };

        var alterAnalyticsColumnsSql = new[]
        {
            "ALTER TABLE IF EXISTS dbo.marketinganalyticsevents ADD COLUMN IF NOT EXISTS ipaddress varchar(64) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketinganalyticsevents ADD COLUMN IF NOT EXISTS countrycode varchar(8) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketinganalyticsevents ADD COLUMN IF NOT EXISTS countryname varchar(120) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketinganalyticsevents ADD COLUMN IF NOT EXISTS region varchar(120) NULL;",
            "ALTER TABLE IF EXISTS dbo.marketinganalyticsevents ADD COLUMN IF NOT EXISTS city varchar(120) NULL;"
        };

        var indexSql = new[]
        {
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_visitorid_occurredat ON dbo.marketinganalyticsevents (visitorid, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_sessionid_occurredat ON dbo.marketinganalyticsevents (sessionid, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_eventtype_occurredat ON dbo.marketinganalyticsevents (eventtype, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_pagepath_occurredat ON dbo.marketinganalyticsevents (pagepath, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_utmsource_utmcampaign ON dbo.marketinganalyticsevents (utmsource, utmcampaign);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_city_occurredat ON dbo.marketinganalyticsevents (city, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketinganalyticsevents_ipaddress_occurredat ON dbo.marketinganalyticsevents (ipaddress, occurredat DESC);",
            "CREATE INDEX IF NOT EXISTS ix_marketingleads_utmsource_utmcampaign ON dbo.marketingleads (utmsource, utmcampaign);",
            "CREATE INDEX IF NOT EXISTS ix_marketingleads_visitorid ON dbo.marketingleads (visitorid);"
        };

        try
        {
            await context.Database.ExecuteSqlRawAsync(createAnalyticsTableSql);

            foreach (var sql in alterLeadColumnsSql)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }

            foreach (var sql in alterAnalyticsColumnsSql)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }

            foreach (var sql in indexSql)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }

            logger.LogInformation("Marketing analytics tables and columns are ready.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure marketing analytics tables.");
            throw;
        }
    }
}

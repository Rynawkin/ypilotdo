using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Monolith.WebAPI.Applications.Events;
using Monolith.WebAPI.Data.Journeys;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Marketing;
using Monolith.WebAPI.Data.Notifications;
using Monolith.WebAPI.Data.Workspace;
using Route = Monolith.WebAPI.Data.Journeys.Route;

namespace Monolith.WebAPI.Data;

// ApplicationRole kullanıyoruz - IdentityRole<Guid> yerine
public class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Workspace.Workspace> Workspaces { get; set; }
    public DbSet<Depot> Depots { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Driver> Drivers { get; set; } // ✅ DRIVER DBSET EKLENDI
    
    // Route entities
    public DbSet<Route> Routes { get; set; }
    public DbSet<RouteStop> RouteStops { get; set; }
    public DbSet<RouteStartDetails> RouteStartDetails { get; set; }
    public DbSet<RouteEndDetails> RouteEndDetails { get; set; }
    public DbSet<RouteOptimizationJob> RouteOptimizationJobs { get; set; }
    public DbSet<SavedLocation> SavedLocations { get; set; }

    // Journey entities
    public DbSet<Journey> Journeys { get; set; }
    public DbSet<JourneyStatus> JourneyStatuses { get; set; }
    public DbSet<JourneyStop> JourneyStops { get; set; }
    public DbSet<JourneyStartDetails> JourneyStartDetails { get; set; }
    public DbSet<JourneyEndDetails> JourneyEndDetails { get; set; }
    public DbSet<LocationUpdateRequest> LocationUpdateRequests { get; set; }

    // 🆕 ÇOKLU FOTOĞRAF DESTEĞİ İÇİN YENİ TABLO
    public DbSet<JourneyStopPhoto> JourneyStopPhotos { get; set; }

    // Auth & misc entities
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TempMember> TempMembers { get; set; }
    public DbSet<Log> Logs { get; set; }
    
    // Template & Feedback entities - YENİ EKLENEN
    public DbSet<MessageTemplate> MessageTemplates { get; set; }
    public DbSet<CustomerFeedback> CustomerFeedback { get; set; }
    
    // Issue tracking entity - YENİ EKLENEN
    public DbSet<Issue> Issues { get; set; }

    // Notifications - YENİ EKLENEN
    public DbSet<Notification> Notifications { get; set; }
    
    // Payment entities - YENİ EKLENEN
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<PaymentMethodEntity> PaymentMethods { get; set; }
    
    // Customer Contact entities - YENİ EKLENEN
    public DbSet<CustomerContact> CustomerContacts { get; set; }
    public DbSet<NotificationRoleMapping> NotificationRoleMappings { get; set; }

    // Marketing entities - YENİ EKLENEN
    public DbSet<MarketingLead> MarketingLeads { get; set; }

    // Vehicle Maintenance entities - YENİ EKLENEN
    public DbSet<VehicleMaintenance> VehicleMaintenances { get; set; }
    public DbSet<MaintenanceReminder> MaintenanceReminders { get; set; }

    private (List<NotificationEvent> beforeSaveDomainEvents, List<NotificationEvent> afterSaveDomainEvents) GetDomainEvents()
    {
        var domainEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0)
            .SelectMany(x => x.Entity.DomainEvents)
            .Union(ChangeTracker.Entries<ApplicationUser>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0)
                .SelectMany(x => x.Entity.DomainEvents))
            .ToList();

        foreach (var entity in ChangeTracker.Entries<BaseEntity>()
                     .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0))
            entity.Entity.ClearDomainEvents();

        foreach (var entity in ChangeTracker.Entries<ApplicationUser>()
                     .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Count != 0))
            entity.Entity.ClearDomainEvents();

        var beforeSaveDomainEvents = domainEvents.Where(e => !e.AfterSavingChanges).ToList();
        var afterSaveDomainEvents = domainEvents.Where(e => e.AfterSavingChanges).ToList();

        return (beforeSaveDomainEvents, afterSaveDomainEvents);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var (beforeSaveDomainEvents, afterSaveDomainEvents) = GetDomainEvents();

        foreach (var domainEvent in beforeSaveDomainEvents)
            await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var domainEvent in afterSaveDomainEvents)
            await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

        return result;
    }

    public override int SaveChanges()
    {
        var (beforeSaveDomainEvents, afterSaveDomainEvents) = GetDomainEvents();

        foreach (var domainEvent in beforeSaveDomainEvents)
            publisher.Publish(domainEvent).ConfigureAwait(false).GetAwaiter().GetResult();

        var result = base.SaveChanges();

        foreach (var domainEvent in afterSaveDomainEvents)
            publisher.Publish(domainEvent).ConfigureAwait(false).GetAwaiter().GetResult();

        return result;
    }

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore NotificationEvent to prevent Entity Framework from trying to map it
        modelBuilder.Ignore<NotificationEvent>();
        modelBuilder.Ignore<INotification>();

        // ✅ WORKSPACE SETTINGS JSON CONFIGURATION
        modelBuilder.Entity<Workspace.Workspace>()
            .Property(w => w.Settings)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => v == null ? null : JsonSerializer.Deserialize<WorkspaceSettings>(v, (JsonSerializerOptions)null))
            .HasColumnType("nvarchar(max)");

        // ✅ IGNORE WORKSPACE SETTINGS NESTED CLASSES
        modelBuilder.Ignore<WorkspaceSettings>();
        modelBuilder.Ignore<WhatsAppSettings>();
        modelBuilder.Ignore<EmailTemplates>();
        modelBuilder.Ignore<DeliveryApproachingTemplate>();
        modelBuilder.Ignore<DeliveryCompletedTemplate>();
        modelBuilder.Ignore<DeliveryFailedTemplate>();
        modelBuilder.Ignore<JourneyStartedTemplate>();
        modelBuilder.Ignore<WorkingHours>();
        modelBuilder.Ignore<PrioritySettings>();
        modelBuilder.Ignore<NotificationEvents>();

        // ✅ FIX: ApplicationUser Rating property precision warning
        modelBuilder.Entity<ApplicationUser>()
            .Property(u => u.Rating)
            .HasPrecision(18, 2);

        // ✅ NOTIFICATION ENTITY CONFIGURATION
        modelBuilder.Entity<Notification>()
            .Property(e => e.Type)
            .HasConversion<string>();

        
        modelBuilder.Entity<Monolith.WebAPI.Data.Workspace.LocationUpdateRequest>(entity =>
        {
            entity.Property(e => e.CurrentLatitude).HasColumnType("decimal(10,8)");
            entity.Property(e => e.CurrentLongitude).HasColumnType("decimal(11,8)");
            entity.Property(e => e.RequestedLatitude).HasColumnType("decimal(10,8)");
            entity.Property(e => e.RequestedLongitude).HasColumnType("decimal(11,8)");
        });
        
        
        // Workspace relationships
        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Depots)
            .WithOne(d => d.Workspace)
            .HasForeignKey(d => d.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Vehicles)
            .WithOne(v => v.Workspace)
            .HasForeignKey(v => v.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Customer-Workspace relationship
        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Customers)
            .WithOne(c => c.Workspace)
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ DRIVER-WORKSPACE RELATIONSHIP EKLENDI
        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Drivers)
            .WithOne(d => d.Workspace)
            .HasForeignKey(d => d.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Routes)
            .WithOne(r => r.Workspace)
            .HasForeignKey(r => r.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.Users)
            .WithOne(u => u.Workspace)
            .HasForeignKey(u => u.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Workspace.Workspace>()
            .HasMany(w => w.SavedLocations)
            .WithOne(s => s.Workspace)
            .HasForeignKey(s => s.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ DÜZELTİLDİ: Journey-Vehicle ilişkisi açıkça tanımlandı
        modelBuilder.Entity<Journey>()
            .HasOne(j => j.Vehicle)
            .WithMany(v => v.Journeys)
            .HasForeignKey(j => j.VehicleId)
            .OnDelete(DeleteBehavior.SetNull); // Vehicle silinirse journey'deki VehicleId null olsun

        // ✅ DÜZELTİLDİ: Journey-Workspace ilişkisi - NULLABLE YAPILDI
        modelBuilder.Entity<Journey>()
            .HasOne(j => j.Workspace)
            .WithMany()
            .HasForeignKey(j => j.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // ✅ ÖNEMLİ: WorkspaceId nullable olarak ayarlandı

        // ✅ DRIVER-VEHICLE RELATIONSHIP EKLENDI
        modelBuilder.Entity<Driver>()
            .HasOne(d => d.Vehicle)
            .WithMany()
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Route relationships
        modelBuilder.Entity<Route>()
            .HasMany(r => r.Stops)
            .WithOne(l => l.Route)
            .HasForeignKey(l => l.RouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Route>()
            .HasOne(r => r.Driver)
            .WithMany()
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Route Details relationships
        modelBuilder.Entity<Route>()
            .HasOne(r => r.StartDetails)
            .WithOne(d => d.Route)
            .HasForeignKey<RouteStartDetails>(d => d.RouteId);
        
        modelBuilder.Entity<Route>()
            .HasOne(r => r.EndDetails)
            .WithOne(d => d.Route)
            .HasForeignKey<RouteEndDetails>(d => d.RouteId);

        modelBuilder.Entity<RouteOptimizationJob>(entity =>
        {
            entity.HasIndex(e => e.PublicId).IsUnique();
            entity.HasIndex(e => new { e.WorkspaceId, e.Status, e.CreatedAt });
            entity.HasIndex(e => e.RouteId);

            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OptimizationMode).HasMaxLength(20);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.ResultJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.Error).HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Route)
                .WithMany()
                .HasForeignKey(e => e.RouteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Journey relationships
        modelBuilder.Entity<Journey>()
            .HasOne(j => j.Route)
            .WithMany(r => r.Journeys)
            .HasForeignKey(j => j.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journey>()
            .HasOne(j => j.Driver)
            .WithMany()
            .HasForeignKey(j => j.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Journey>()
            .HasOne(j => j.StartDetails)
            .WithOne(d => d.Journey)
            .HasForeignKey<JourneyStartDetails>(d => d.JourneyId);

        modelBuilder.Entity<Journey>()
            .HasOne(j => j.EndDetails)
            .WithOne(d => d.Journey)
            .HasForeignKey<JourneyEndDetails>(d => d.JourneyId);

        // ✅ V38 - LiveLocation JSON olarak saklanacak
        modelBuilder.Entity<Journey>()
            .OwnsOne(j => j.LiveLocation, l =>
            {
                l.Property(p => p.Latitude);
                l.Property(p => p.Longitude);
                l.Property(p => p.Timestamp);
                l.Property(p => p.Speed);
                l.Property(p => p.Heading);
                l.Property(p => p.Accuracy);
            });

        // JourneyStatus relationships
        modelBuilder.Entity<JourneyStatus>()
            .HasOne(js => js.Journey)
            .WithMany(j => j.Statuses)
            .HasForeignKey(js => js.JourneyId);

        modelBuilder.Entity<JourneyStatus>()
            .HasOne(js => js.Stop)
            .WithMany()
            .HasForeignKey(js => js.StopId)
            .OnDelete(DeleteBehavior.NoAction);

        // JourneyStatus AdditionalValues configuration
        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (d1, d2) => d1.SequenceEqual(d2),
            d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            d => d.ToDictionary(kv => kv.Key, kv => kv.Value)
        );
        modelBuilder.Entity<JourneyStatus>()
            .Property(js => js.AdditionalValues)
            .HasConversion(new DictionaryToJsonConverter())
            .Metadata.SetValueComparer(dictionaryComparer);

        // JourneyStop relationships
        modelBuilder.Entity<JourneyStop>()
            .HasOne(js => js.Journey)
            .WithMany(j => j.Stops)
            .HasForeignKey(js => js.JourneyId);

        modelBuilder.Entity<JourneyStop>()
            .HasOne(js => js.RouteStop)
            .WithMany()
            .HasForeignKey(js => js.StopId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // JourneyStop RouteStopId relationship
        modelBuilder.Entity<JourneyStop>()
            .HasOne(js => js.RouteStop)
            .WithMany()
            .HasForeignKey(js => js.RouteStopId)
            .OnDelete(DeleteBehavior.Restrict);
        // ✅ FIX: JourneyStop has database triggers - tell EF Core to avoid OUTPUT clause
        modelBuilder.Entity<JourneyStop>()
            .ToTable(tb => tb.HasTrigger("tr_JourneyStop_UpdatedAt"));

        // TempMember configuration
        modelBuilder.Entity<TempMember>().HasKey(tm => tm.Token);

        // ============= MESSAGE TEMPLATES & CUSTOMER FEEDBACK CONFIGURATION - YENİ EKLENEN =============
        
        // MessageTemplate configuration
        modelBuilder.Entity<MessageTemplate>(entity =>
        {
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => new { e.TemplateType, e.Channel });
            entity.HasIndex(e => new { e.WorkspaceId, e.TemplateType, e.Channel, e.Name }).IsUnique();
            
            entity.HasOne(t => t.Workspace)
                .WithMany()
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CustomerFeedback configuration
        modelBuilder.Entity<CustomerFeedback>(entity =>
        {
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.JourneyId);
            entity.HasIndex(e => e.FeedbackToken).IsUnique();
            entity.HasIndex(e => e.SubmittedAt);
            
            entity.HasOne(f => f.Workspace)
                .WithMany()
                .HasForeignKey(f => f.WorkspaceId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(f => f.Journey)
                .WithMany()
                .HasForeignKey(f => f.JourneyId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(f => f.JourneyStop)
                .WithMany()
                .HasForeignKey(f => f.JourneyStopId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(f => f.Customer)
                .WithMany()
                .HasForeignKey(f => f.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============= ISSUE TRACKING CONFIGURATION - YENİ EKLENEN =============
        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.IssueType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(50);
            entity.Property(e => e.ReportedBy).HasMaxLength(100);
            entity.Property(e => e.ReportedByName).HasMaxLength(100);
            
            entity.HasOne(e => e.Workspace)
                .WithMany()
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ============= 🆕 JOURNEY STOP PHOTOS CONFIGURATION - ÇOKLU FOTOĞRAF DESTEĞİ =============
        modelBuilder.Entity<JourneyStopPhoto>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.PhotoUrl)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(500);
            
            entity.Property(e => e.Caption)
                .HasMaxLength(200);
            
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0);
            
            // Journey ilişkisi - Cascade delete ile journey silinince fotoğraflar da silinir
            entity.HasOne(e => e.Journey)
                .WithMany()
                .HasForeignKey(e => e.JourneyId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // JourneyStop ilişkisi - Stop silinince fotoğraflar silinmez (NoAction)
            entity.HasOne(e => e.Stop)
                .WithMany()
                .HasForeignKey(e => e.StopId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // Performans için index
            entity.HasIndex(e => new { e.JourneyId, e.StopId })
                .HasDatabaseName("IX_JourneyStopPhotos_JourneyId_StopId");
            
            // DisplayOrder için index
            entity.HasIndex(e => e.DisplayOrder);
        });

        // ============= PAYMENT SYSTEM CONFIGURATION - YENİ EKLENEN =============
        
        // PaymentTransaction configuration
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Ignore IsDeleted property from BaseEntity since it's not in the database
            entity.Ignore(e => e.IsDeleted);
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();
            
            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.ProviderTransactionId)
                .HasMaxLength(100);
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.ProviderResponse)
                .HasColumnType("nvarchar(max)");
            
            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(500);
            
            // Workspace relationship - ignore navigation property and use explicit foreign key
            entity.Ignore(e => e.Workspace);
            entity.Property(e => e.WorkspaceId).IsRequired();
            
            // Ignore the Invoices navigation property to avoid conflicts
            entity.Ignore(e => e.Invoices);
            
            // Indexes for performance
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.ProviderTransactionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Ignore IsDeleted property from BaseEntity since it's not in the database
            entity.Ignore(e => e.IsDeleted);
            
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Tax)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Total)
                .HasPrecision(18, 2)
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.PlanType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            // Fix value comparer for Invoice.Items collection
            var invoiceItemsComparer = new ValueComparer<List<InvoiceItem>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? null : c.ToList()
            );
            
            entity.Property(e => e.Items)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => v == null ? null : JsonSerializer.Deserialize<List<InvoiceItem>>(v, (JsonSerializerOptions)null))
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(invoiceItemsComparer);
            
            // Workspace relationship - ignore navigation property and use explicit foreign key
            entity.Ignore(e => e.Workspace);
            entity.Property(e => e.WorkspaceId).IsRequired();
            
            // PaymentTransaction relationship (optional) - explicit foreign key configuration
            // Use WithMany() instead of WithMany(pt => pt.Invoices) to avoid conflicts
            entity.HasOne(e => e.PaymentTransaction)
                .WithMany()
                .HasForeignKey(e => e.PaymentTransactionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Invoices_PaymentTransactions");
            
            // Indexes for performance
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.CreatedAt);
        });

        // PaymentMethodEntity configuration
        modelBuilder.Entity<PaymentMethodEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Ignore IsDeleted property from BaseEntity since it's not in the database
            entity.Ignore(e => e.IsDeleted);
            
            entity.Property(e => e.Provider)
                .HasMaxLength(50);
            
            entity.Property(e => e.ProviderMethodId)
                .HasMaxLength(256);
            
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.LastFourDigits)
                .HasMaxLength(4);
            
            entity.Property(e => e.ExpiryMonth)
                .HasMaxLength(2);
            
            entity.Property(e => e.ExpiryYear)
                .HasMaxLength(4);
            
            entity.Property(e => e.CardHolderName)
                .HasMaxLength(100);
            
            entity.Property(e => e.BrandName)
                .HasMaxLength(50);
            
            // Workspace relationship - ignore navigation property and use explicit foreign key
            entity.Ignore(e => e.Workspace);
            entity.Property(e => e.WorkspaceId).IsRequired();
            
            // Indexes for performance
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.ProviderMethodId);
            entity.HasIndex(e => e.IsActive);
        });

        // Workspace additional configuration for new payment fields
        modelBuilder.Entity<Workspace.Workspace>(entity =>
        {
            entity.Property(e => e.CurrentMonthAdditionalCharges)
                .HasPrecision(18, 2);
        });
        
        // Match existing Postgres schema/table/column names (dbo + lowercase)
        modelBuilder.HasDefaultSchema("dbo");
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetSchema("dbo");

            var tableName = entity.GetTableName();
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                entity.SetTableName(tableName.ToLowerInvariant());

                var storeObject = StoreObjectIdentifier.Table(entity.GetTableName() ?? tableName, entity.GetSchema());
                foreach (var property in entity.GetProperties())
                {
                    var columnName = property.GetColumnName(storeObject);
                    if (!string.IsNullOrWhiteSpace(columnName))
                    {
                        property.SetColumnName(columnName.ToLowerInvariant());
                    }
                }
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (!string.IsNullOrWhiteSpace(keyName))
                {
                    key.SetName(keyName.ToLowerInvariant());
                }
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                var fkName = fk.GetConstraintName();
                if (!string.IsNullOrWhiteSpace(fkName))
                {
                    fk.SetConstraintName(fkName.ToLowerInvariant());
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (!string.IsNullOrWhiteSpace(indexName))
                {
                    index.SetDatabaseName(indexName.ToLowerInvariant());
                }
            }
        }
    }
}

public class DictionaryToJsonConverter() : ValueConverter<Dictionary<string, string>, string>(
    v => JsonSerializer.Serialize(v, (JsonSerializerOptions) null),
    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions) null));

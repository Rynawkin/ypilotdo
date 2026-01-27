using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Monolith.WebAPI;
using Monolith.WebAPI.Applications.Behaviors;
using Monolith.WebAPI.Applications.Commands.Customers;
using Monolith.WebAPI.Applications.Commands.Journeys;
using Monolith.WebAPI.Applications.Commands.Members;
using Monolith.WebAPI.Applications.Commands.Vehicles;
using Monolith.WebAPI.Applications.Commands.Workspace;
using Monolith.WebAPI.Data;
using Monolith.WebAPI.Data.Members;
using Monolith.WebAPI.Data.Workspace;
using Monolith.WebAPI.External.Google;
using Monolith.WebAPI.External.RouteXL;
using Monolith.WebAPI.Hubs;
using Monolith.WebAPI.Services.Subscription;
using Monolith.WebAPI.Infrastructure;
using Monolith.WebAPI.Services.Email;
using Monolith.WebAPI.Services.Members;
using Monolith.WebAPI.Services.Notifications;
using Monolith.WebAPI.Services.Storage;
using Monolith.WebAPI.Services.WhatsApp;
using Monolith.WebAPI.Services.Workspace;
using Monolith.WebAPI.Services.Optimization;
using Monolith.WebAPI.Services.Templates;
using Monolith.WebAPI.Services.Feedback;
using Monolith.WebAPI.Services.BackgroundJobs;
using Monolith.WebAPI.Services.Cache;

var builder = WebApplication.CreateBuilder(args);

// AZURE APP SERVICE Ã„Â°Ãƒâ€¡Ã„Â°N SADECE BU KADAR YETERLÃ„Â°
if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    
    // Environment variables
    builder.Configuration.AddEnvironmentVariables();
    
    // Logging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
});
// BUGFIX: GoogleApiCacheService must be singleton because GoogleApiService is singleton
builder.Services.AddSingleton<IGoogleApiCacheService, GoogleApiCacheService>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseLazyLoadingProxies(false);
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // ⭐ PERFORMANCE MONITORING: Enable query performance logging in Development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
        options.LogTo(Console.WriteLine, LogLevel.Information);
    }
    
    // ⭐ PERFORMANCE OPTIMIZATION: Enable caching and detailed errors only when needed
    options.EnableServiceProviderCaching();
});

// JSON SERIALIZATION AYARLARI - CAMELCASE + CASE INSENSITIVE
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1024 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
    options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

AddRateLimiter();
AddCors();
AddSwagger();
AddDependencies();
AddIdentity();
AddBearerAuthentication();
AddMediatR();

var app = builder.Build();

// Startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation($"Application starting. Environment: {app.Environment.EnvironmentName}");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<CustomExceptionHandlerMiddleware>();

// SECURITY: HTTPS enforcement in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// SECURITY: Rate limiting middleware
app.UseRateLimiter();

app.UseCors("AllowAll");

// SECURITY: Swagger only enabled in development environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "YolPilot API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SubscriptionControllerMiddleware>();

app.UseStaticFiles();

// TEST ENDPOINTS
app.MapGet("/", () => Results.Ok(new 
{ 
    status = "running",
    api = "YolPilot API",
    version = "1.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health", () => "Healthy");
app.MapGet("/api/health", () => "Healthy");
app.MapGet("/ping", () => "pong");

app.MapControllers();

app.MapHub<JourneyHub>("/hubs/journey");
app.MapHub<TrackingHub>("/hubs/tracking");
app.MapHub<NotificationHub>("/hubs/notifications");

// DATABASE MIGRATION
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var migrationLogger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        migrationLogger.LogInformation("Starting database migration...");
        var context = services.GetRequiredService<AppDbContext>();
        // Azure'da migration otomatik çalışsın
        //await context.Database.MigrateAsync();
        migrationLogger.LogInformation("Database migration skipped - manual SQL required.");
        
        await DatabaseSeeder.SeedRolesAsync(services);
        migrationLogger.LogInformation("Roles seeded successfully.");
        
        await DatabaseSeeder.SeedDemoUsersAsync(services);
        migrationLogger.LogInformation("Demo users and workspace seeded successfully.");
        
        await MigrateExistingDriversToUsers(services, migrationLogger);
        
        if (args.Contains("--reset-demo"))
        {
            await DatabaseSeeder.ResetDemoDataAsync(services);
            migrationLogger.LogInformation("Demo data reset successfully.");
        }
        
        if (args.Contains("--migrate-drivers"))
        {
            migrationLogger.LogInformation("Force migrating existing drivers to users...");
            await MigrateExistingDriversToUsers(services, migrationLogger, forceUpdate: true);
        }
    }
    catch (Exception ex)
    {
        migrationLogger.LogError(ex, "An error occurred while seeding the database.");
        // Azure'da hata olsa bile uygulamayÃ„Â± baÃ…Å¸lat
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

logger.LogInformation("Application started successfully. Ready to accept requests.");

app.Run();
return;

async Task MigrateExistingDriversToUsers(IServiceProvider serviceProvider, ILogger logger, bool forceUpdate = false)
{
    try
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var driversWithoutUser = await context.Set<Driver>()
            .Where(d => d.UserId == null)
            .Include(d => d.Workspace)
            .ToListAsync();
        
        if (!driversWithoutUser.Any())
        {
            logger.LogInformation("No drivers found to migrate.");
            return;
        }
        
        logger.LogInformation($"Found {driversWithoutUser.Count} drivers to migrate.");
        
        var migratedDrivers = new List<(string Name, string Email, string TempPassword)>();
        var failedDrivers = new List<(string Name, string Error)>();
        
        foreach (var driver in driversWithoutUser)
        {
            try
            {
                var email = !string.IsNullOrEmpty(driver.Email) 
                    ? driver.Email 
                    : $"{driver.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "")}@rotaapp.temp";
                
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null && !forceUpdate)
                {
                    driver.UserId = existingUser.Id;
                    logger.LogInformation($"Driver '{driver.Name}' linked to existing user with email '{email}'");
                    continue;
                }
                
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    PhoneNumber = driver.Phone,
                    PhoneNumberConfirmed = false
                };
                
                typeof(ApplicationUser).GetProperty("FullName")
                    ?.SetValue(user, driver.Name);
                typeof(ApplicationUser).GetProperty("WorkspaceId")
                    ?.SetValue(user, driver.WorkspaceId);
                typeof(ApplicationUser).GetProperty("IsDriver")
                    ?.SetValue(user, true);
                typeof(ApplicationUser).GetProperty("IsAdmin")
                    ?.SetValue(user, false);
                typeof(ApplicationUser).GetProperty("IsDispatcher")
                    ?.SetValue(user, false);
                typeof(ApplicationUser).GetProperty("IsSuperAdmin")
                    ?.SetValue(user, false);
                typeof(ApplicationUser).GetProperty("IsOnboarded")
                    ?.SetValue(user, true);
                typeof(ApplicationUser).GetProperty("LicenseNumber")
                    ?.SetValue(user, driver.LicenseNumber);
                typeof(ApplicationUser).GetProperty("DriverStatus")
                    ?.SetValue(user, driver.Status);
                
                var defaultDepot = await context.Depots
                    .FirstOrDefaultAsync(d => d.WorkspaceId == driver.WorkspaceId && d.IsDefault);
                if (defaultDepot != null)
                {
                    typeof(ApplicationUser).GetProperty("DepotId")
                        ?.SetValue(user, defaultDepot.Id);
                }
                
                var tempPassword = $"Rota{driver.Id}2025!";
                
                var result = await userManager.CreateAsync(user, tempPassword);
                
                if (result.Succeeded)
                {
                    driver.UserId = user.Id;
                    migratedDrivers.Add((driver.Name, email, tempPassword));
                    logger.LogInformation($"Successfully migrated driver: {driver.Name} | Email: {email}");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    failedDrivers.Add((driver.Name, errors));
                    logger.LogError($"Failed to create user for driver {driver.Name}: {errors}");
                }
            }
            catch (Exception ex)
            {
                failedDrivers.Add((driver.Name, ex.Message));
                logger.LogError(ex, $"Error migrating driver {driver.Name}");
            }
        }
        
        await context.SaveChangesAsync();
        
        logger.LogInformation("=".PadRight(80, '='));
        logger.LogInformation("DRIVER MIGRATION SUMMARY");
        logger.LogInformation("=".PadRight(80, '='));
        logger.LogInformation($"Total drivers processed: {driversWithoutUser.Count}");
        logger.LogInformation($"Successfully migrated: {migratedDrivers.Count}");
        logger.LogInformation($"Failed: {failedDrivers.Count}");
        
        if (migratedDrivers.Any())
        {
            logger.LogInformation("-".PadRight(80, '-'));
            logger.LogInformation("SUCCESSFULLY MIGRATED DRIVERS:");
            logger.LogInformation("-".PadRight(80, '-'));
            foreach (var (name, email, password) in migratedDrivers)
            {
                logger.LogInformation($"Driver: {name,-30} | Email: {email,-40}");
                // SECURITY: Passwords are NOT logged. Use email reset to provide credentials to users.
            }
            logger.LogWarning("IMPORTANT: Passwords have been created but not logged for security. Use password reset emails to provide credentials.");
        }
        
        if (failedDrivers.Any())
        {
            logger.LogWarning("-".PadRight(80, '-'));
            logger.LogWarning("FAILED MIGRATIONS:");
            logger.LogWarning("-".PadRight(80, '-'));
            foreach (var (name, error) in failedDrivers)
            {
                logger.LogWarning($"Driver: {name} | Error: {error}");
            }
        }
        
        logger.LogInformation("=".PadRight(80, '='));
        
        if (migratedDrivers.Any())
        {
            var credentialsFile = Path.Combine(Directory.GetCurrentDirectory(), $"driver_credentials_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var lines = new List<string>
            {
                "DRIVER LOGIN CREDENTIALS",
                "Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                new string('=', 80),
                ""
            };
            
            foreach (var (name, email, password) in migratedDrivers)
            {
                lines.Add($"Driver: {name}");
                lines.Add($"Email: {email}");
                lines.Add($"Password: {password}");
                lines.Add("");
            }
            
            await File.WriteAllLinesAsync(credentialsFile, lines);
            logger.LogInformation($"Credentials saved to: {credentialsFile}");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in MigrateExistingDriversToUsers");
    }
}

void AddSwagger()
{
    var swaggerControllerOrder = new SwaggerControllerOrder<ControllerBase>(Assembly.GetEntryAssembly());
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "YolPilot API",
            Version = "v1",
            Description = "YolPilot - Multitenant SaaS Rota Optimizasyonu Platformu API"
        });
        
        c.EnableAnnotations();
        c.OrderActionsBy(apiDesc => $"{swaggerControllerOrder.SortKey(apiDesc.ActionDescriptor.RouteValues["controller"])}");

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please insert JWT with Bearer into field (Example: 'Bearer YOUR_TOKEN')",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
        
        c.OperationFilter<Monolith.WebAPI.Infrastructure.SwaggerFileOperationFilter>();
    });
}

void AddIdentity()
{
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
    {
        opts.User.RequireUniqueEmail = true;
        // SECURITY: Strong password policy (min 8 chars, uppercase, lowercase, digit, special char)
        opts.Password.RequiredLength = 8;
        opts.Password.RequireNonAlphanumeric = true;
        opts.Password.RequireUppercase = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireDigit = true;
    }).AddEntityFrameworkStores<AppDbContext>()
      .AddDefaultTokenProviders();
    
    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
        options.ClaimsIdentity.UserNameClaimType = ClaimTypes.Name;
        options.ClaimsIdentity.RoleClaimType = ClaimTypes.Role;
    });
}

void AddBearerAuthentication()
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        // SECURITY: Require HTTPS in production
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,  // DÜZELTILDI - true olmalı
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            
            ValidateAudience = true,  // DÜZELTILDI - true olmalı
            ValidAudience = builder.Configuration["Jwt:Audience"],
            
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
                    "YolPilot2024SecureJWTKeyWithMinimum32CharactersForProduction!@#$%")),
            ClockSkew = TimeSpan.Zero
        };
        
        // Events kısmı aynı kalacak...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/hubs")))
                {
                    context.Token = accessToken;
                    
                    if (builder.Environment.IsDevelopment())
                    {
                        Console.WriteLine($"SignalR Token received for path: {path}");
                    }
                }
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"JWT Authentication failed: {context.Exception}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"JWT Token validated for: {context.Principal?.Identity?.Name}");
                }
                return Task.CompletedTask;
            }
        };
    });
    
    builder.Services.AddAuthorization();
}

void AddMediatR()
{
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblyContaining(typeof(Program));

        cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
    });

    // WORKSPACE VALIDATORS
    builder.Services.AddSingleton<IValidator<CreateWorkspaceCommand>, CreateWorkspaceCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateWorkspaceCommand>, UpdateWorkspaceCommandValidator>();

    // EMAIL SERVICE
    builder.Services.AddScoped<IEmailService, EmailService>();

    // DiÃ„Å¸er validator'lar
    builder.Services.AddSingleton<IValidator<CreateDepotCommand>, CreateDepotCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateDepotCommand>, UpdateDepotCommandValidator>();
    builder.Services.AddSingleton<IValidator<DeleteDepotCommand>, DeleteDepotCommandValidator>();
    builder.Services.AddSingleton<IValidator<AddMemberToDepotCommand>, AddMemberToDepotCommandValidator>();
    builder.Services.AddSingleton<IValidator<OptimizeJourneyForDeviationCommand>, OptimizeJourneyForDeviationCommandValidator>();
    builder.Services.AddSingleton<IValidator<CreateTempMemberCommand>, AddMemberCommandValidator>();
    builder.Services.AddSingleton<IValidator<SaveMemberCommand>, SaveMemberCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateMemberDepotCommand>, UpdateMemberDepotCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateMemberRoleCommand>, UpdateMemberRoleCommandValidator>();
    builder.Services.AddSingleton<IValidator<AddJourneyStatusCommand>, AddStatusCommandValidator>();
    builder.Services.AddSingleton<IValidator<AssignRouteCommand>, AssignRouteCommandValidator>();
    builder.Services.AddSingleton<IValidator<OptimizeJourneyCommand>, OptimizeJourneyCommandValidator>();
    builder.Services.AddSingleton<IValidator<OptimizeRouteCommand>, OptimizeRouteCommandValidator>();
    builder.Services.AddSingleton<IValidator<CreateSavedLocationCommand>, CreateSavedLocationCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateSavedLocationCommand>, UpdateSavedLocationCommandValidator>();
    builder.Services.AddSingleton<IValidator<CreateRouteCommand>, CreateRouteCommandValidator>();
    builder.Services.AddSingleton<IValidator<AddRouteStopsCommand>, AddStopsCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateStopCommand>, UpdateStopCommandValidator>();
    builder.Services.AddSingleton<IValidator<CreateCustomerCommand>, CreateCustomerCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateCustomerCommand>, UpdateCustomerCommandValidator>();
    builder.Services.AddSingleton<IValidator<CreateVehicleCommand>, CreateVehicleCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateVehicleCommand>, UpdateVehicleCommandValidator>();
    builder.Services.AddSingleton<IValidator<UpdateVehicleStatusCommand>, UpdateVehicleStatusCommandValidator>();
    builder.Services.AddSingleton<IValidator<DeleteVehicleCommand>, DeleteVehicleCommandValidator>();
}

void AddRateLimiter()
{
    // SECURITY: API Rate Limiting to prevent brute force and DDoS attacks
    builder.Services.AddRateLimiter(options =>
    {
        // Global rate limit: 100 requests per minute per IP
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        // Strict rate limit for login endpoint: 5 attempts per 15 minutes per IP
        options.AddPolicy("login", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });
}

void AddCors()
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "https://app.yolpilot.com",
                "https://yolpilot.com",
                "https://www.yolpilot.com",
                "https://yolpilot-api.azurewebsites.net",
                "https://yolpilot.vercel.app"
            };

            var allowVercelWildcard = builder.Environment.IsEnvironment("DigitalOcean");

            policy.SetIsOriginAllowed(origin =>
                    allowedOrigins.Contains(origin) ||
                    (allowVercelWildcard && origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

        // SECURITY: Restrict marketing CORS to specific domains only
        options.AddPolicy("MarketingCors", policy =>
        {
            policy.WithOrigins(
                    "https://yolpilot.com",
                    "https://www.yolpilot.com"
                )
                .WithMethods("GET", "POST")
                .AllowAnyHeader();
        });
    });
}

void AddDependencies()
{
    builder.Services.AddSingleton<ITokenService, TokenService>();
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
    
    // Storage Services - CLOUDINARY EKLENDI
    var storageMode = builder.Configuration.GetValue<string>("Storage:Mode") ?? "Local";
    
    // Cloudinary Service
    if (storageMode == "Cloudinary")
    {
        builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
        builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>(); // Migration iÃƒÂ§in sakla
    }
    else
    {
        builder.Services.AddSingleton<ICloudinaryService, NullCloudinaryService>(); // Fallback
        builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
    }
    
    builder.Services.AddSingleton<GoogleApiService>();
    builder.Services.AddScoped<RouteXlService>();
    builder.Services.AddScoped<OptimizeJourneyForDeviationCommandHandler>();
    builder.Services.AddScoped<OptimizeRouteCommandHandler>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    builder.Services.AddScoped<IWhatsAppService, TwilioWhatsAppService>();

    // WhatsApp Rate Limiting & Queue Services
    builder.Services.AddScoped<IWhatsAppRateLimiter, WhatsAppRateLimiter>();
    
    // OR-Tools Optimization Service
    builder.Services.AddScoped<IOptimizationService, OrToolsOptimizationService>();
    
    // ============= TEMPLATE & FEEDBACK SERVICES - YENÃ„Â° EKLENEN =============
    builder.Services.AddScoped<ITemplateService, TemplateService>();
    builder.Services.AddScoped<IFeedbackService, FeedbackService>();
    
    // ============= NOTIFICATION SERVICE - YENI EKLENEN =============
    builder.Services.AddScoped<INotificationService, NotificationService>();
    
    // ============= PAYMENT SERVICES - YENI EKLENEN =============
    // HttpClient services for payment providers
    builder.Services.AddHttpClient();
    builder.Services.AddHttpClient<Monolith.WebAPI.Services.Payment.PayTRProvider>();
    builder.Services.AddHttpClient<Monolith.WebAPI.Services.Payment.ParamPOSProvider>();
    
    // Payment services
    builder.Services.AddScoped<Monolith.WebAPI.Services.Payment.IPaymentService, Monolith.WebAPI.Services.Payment.PaymentService>();
    builder.Services.AddScoped<Monolith.WebAPI.Services.Payment.ITrialService, Monolith.WebAPI.Services.Payment.TrialService>();
    builder.Services.AddScoped<Monolith.WebAPI.Services.Payment.IPaymentProvider, Monolith.WebAPI.Services.Payment.PayTRProvider>();
    builder.Services.AddScoped<Monolith.WebAPI.Services.Payment.PayTRProvider>();
    builder.Services.AddScoped<Monolith.WebAPI.Services.Payment.ParamPOSProvider>();

    // ============= BACKGROUND JOBS - YENI EKLENEN =============
    builder.Services.AddHostedService<MaintenanceReminderJob>();
    builder.Services.AddHostedService<RouteOptimizationJobWorker>();
}

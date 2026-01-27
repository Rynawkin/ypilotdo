using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monolith.WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTimeWindowColumnsToTimeSpan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Workspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowOverageCharges",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Workspaces",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateFormat",
                table: "Workspaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstDayOfWeek",
                table: "Workspaces",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrialUsed",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Workspaces",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrioritySettings",
                table: "Workspaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Workspaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialStartDate",
                table: "Workspaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Workspaces",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhatsAppMode",
                table: "Workspaces",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppNotifyCheckIn",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppNotifyCompletion",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppNotifyFailure",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppNotifyJourneyStart",
                table: "Workspaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "Workspaces",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhotoRequired",
                table: "RouteStops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SignatureRequired",
                table: "RouteStops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AvoidTolls",
                table: "Routes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Polyline",
                table: "Routes",
                type: "nvarchar(max)",
                maxLength: 2147483647,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EstimatedArrivalTime",
                table: "RouteEndDetails",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "JourneyStatuses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Journeys",
                type: "nvarchar(max)",
                nullable: true);

            // Önce temporary kolonlar ekle
            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeWindowStart_New",
                table: "Customers",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeWindowEnd_New",
                table: "Customers",
                type: "time",
                nullable: true);

            // Mevcut string değerleri TimeSpan'e dönüştür
            migrationBuilder.Sql(@"
                UPDATE Customers
                SET TimeWindowStart_New = CAST(TimeWindowStart AS time)
                WHERE TimeWindowStart IS NOT NULL
                AND TimeWindowStart != ''
                AND ISDATE('1900-01-01 ' + TimeWindowStart) = 1;

                UPDATE Customers
                SET TimeWindowEnd_New = CAST(TimeWindowEnd AS time)
                WHERE TimeWindowEnd IS NOT NULL
                AND TimeWindowEnd != ''
                AND ISDATE('1900-01-01 ' + TimeWindowEnd) = 1;
            ");

            // Eski kolonları sil
            migrationBuilder.DropColumn(
                name: "TimeWindowStart",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TimeWindowEnd",
                table: "Customers");

            // Yeni kolonları yeniden adlandır
            migrationBuilder.RenameColumn(
                name: "TimeWindowStart_New",
                table: "Customers",
                newName: "TimeWindowStart");

            migrationBuilder.RenameColumn(
                name: "TimeWindowEnd_New",
                table: "Customers",
                newName: "TimeWindowEnd");

            migrationBuilder.CreateTable(
                name: "CustomerContact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveJourneyStart = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveJourneyCheckIn = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveDeliveryCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveDeliveryFailed = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveJourneyAssigned = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveJourneyCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContact_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerContact_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFeedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    JourneyId = table.Column<int>(type: "int", nullable: false),
                    JourneyStopId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OverallRating = table.Column<int>(type: "int", nullable: false),
                    DeliverySpeedRating = table.Column<int>(type: "int", nullable: true),
                    DriverBehaviorRating = table.Column<int>(type: "int", nullable: true),
                    PackageConditionRating = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FeedbackToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerFeedback_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerFeedback_JourneyStops_JourneyStopId",
                        column: x => x.JourneyStopId,
                        principalTable: "JourneyStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerFeedback_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerFeedback_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JourneyStopPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JourneyId = table.Column<int>(type: "int", nullable: false),
                    StopId = table.Column<int>(type: "int", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JourneyStopPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JourneyStopPhotos_JourneyStops_StopId",
                        column: x => x.StopId,
                        principalTable: "JourneyStops",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JourneyStopPhotos_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationUpdateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    JourneyId = table.Column<int>(type: "int", nullable: false),
                    JourneyStopId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CurrentLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    CurrentLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    CurrentAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    RequestedLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    RequestedAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationUpdateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationUpdateRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationUpdateRequests_JourneyStops_JourneyStopId",
                        column: x => x.JourneyStopId,
                        principalTable: "JourneyStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationUpdateRequests_Journeys_JourneyId",
                        column: x => x.JourneyId,
                        principalTable: "Journeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LocationUpdateRequests_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    TemplateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Variables = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplates_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRoleMapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRoleMapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRoleMapping_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastFourDigits = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    CardHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryMonth = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ExpiryYear = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProviderMethodId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProviderResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentTransactionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_PaymentTransactions",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Invoices_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContact_CustomerId",
                table: "CustomerContact",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContact_WorkspaceId",
                table: "CustomerContact",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_CustomerId",
                table: "CustomerFeedback",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_FeedbackToken",
                table: "CustomerFeedback",
                column: "FeedbackToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_JourneyId",
                table: "CustomerFeedback",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_JourneyStopId",
                table: "CustomerFeedback",
                column: "JourneyStopId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_SubmittedAt",
                table: "CustomerFeedback",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFeedback_WorkspaceId",
                table: "CustomerFeedback",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedAt",
                table: "Invoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DueDate",
                table: "Invoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentTransactionId",
                table: "Invoices",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Status",
                table: "Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_WorkspaceId",
                table: "Invoices",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_CreatedAt",
                table: "Issues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Status",
                table: "Issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_WorkspaceId",
                table: "Issues",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyStopPhotos_DisplayOrder",
                table: "JourneyStopPhotos",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_JourneyStopPhotos_JourneyId_StopId",
                table: "JourneyStopPhotos",
                columns: new[] { "JourneyId", "StopId" });

            migrationBuilder.CreateIndex(
                name: "IX_JourneyStopPhotos_StopId",
                table: "JourneyStopPhotos",
                column: "StopId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationUpdateRequests_CustomerId",
                table: "LocationUpdateRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationUpdateRequests_JourneyId",
                table: "LocationUpdateRequests",
                column: "JourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationUpdateRequests_JourneyStopId",
                table: "LocationUpdateRequests",
                column: "JourneyStopId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationUpdateRequests_WorkspaceId",
                table: "LocationUpdateRequests",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_TemplateType_Channel",
                table: "MessageTemplates",
                columns: new[] { "TemplateType", "Channel" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_WorkspaceId",
                table: "MessageTemplates",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_WorkspaceId_TemplateType_Channel_Name",
                table: "MessageTemplates",
                columns: new[] { "WorkspaceId", "TemplateType", "Channel", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRoleMapping_WorkspaceId",
                table: "NotificationRoleMapping",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                table: "PaymentMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_ProviderMethodId",
                table: "PaymentMethods",
                column: "ProviderMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_WorkspaceId",
                table: "PaymentMethods",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAt",
                table: "PaymentTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProviderTransactionId",
                table: "PaymentTransactions",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status",
                table: "PaymentTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_WorkspaceId",
                table: "PaymentTransactions",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerContact");

            migrationBuilder.DropTable(
                name: "CustomerFeedback");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "JourneyStopPhotos");

            migrationBuilder.DropTable(
                name: "LocationUpdateRequests");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropTable(
                name: "NotificationRoleMapping");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "AllowOverageCharges",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "DateFormat",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "FirstDayOfWeek",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "IsTrialUsed",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PrioritySettings",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TrialEndDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "TrialStartDate",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsAppMode",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsAppNotifyCheckIn",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsAppNotifyCompletion",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsAppNotifyFailure",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WhatsAppNotifyJourneyStart",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PhotoRequired",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "SignatureRequired",
                table: "RouteStops");

            migrationBuilder.DropColumn(
                name: "AvoidTolls",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "Polyline",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "EstimatedArrivalTime",
                table: "RouteEndDetails");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "JourneyStatuses");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Journeys");

            migrationBuilder.AlterColumn<string>(
                name: "TimeWindowStart",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TimeWindowEnd",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);
        }
    }
}

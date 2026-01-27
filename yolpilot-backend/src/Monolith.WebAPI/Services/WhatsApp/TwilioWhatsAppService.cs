using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Monolith.WebAPI.Services.Templates;
using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Services.WhatsApp;

public class TwilioWhatsAppService : IWhatsAppService
{
    private readonly ILogger<TwilioWhatsAppService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITemplateService _templateService;
    private readonly IWhatsAppRateLimiter _rateLimiter;

    public TwilioWhatsAppService(
        ILogger<TwilioWhatsAppService> logger,
        IConfiguration configuration,
        ITemplateService templateService,
        IWhatsAppRateLimiter rateLimiter)
    {
        _logger = logger;
        _configuration = configuration;
        _templateService = templateService;
        _rateLimiter = rateLimiter;
    }

    private bool InitializeTwilioClient(Monolith.WebAPI.Data.Workspace.Workspace workspace, out string fromNumber, out string messagePrefix)
    {
        fromNumber = string.Empty;
        messagePrefix = string.Empty;

        if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Disabled)
        {
            _logger.LogInformation("WhatsApp is disabled for workspace {WorkspaceId}", workspace.Id);
            return false;
        }

        if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
        {
            var sharedAccountSid = _configuration["Twilio:SharedAccountSid"];
            var sharedAuthToken = _configuration["Twilio:SharedAuthToken"];
            fromNumber = FormatWhatsAppFromNumber(_configuration["Twilio:SharedWhatsAppNumber"]);

            if (string.IsNullOrEmpty(sharedAccountSid) || string.IsNullOrEmpty(sharedAuthToken))
            {
                _logger.LogError("Shared Twilio credentials not configured");
                return false;
            }
            if (string.IsNullOrEmpty(fromNumber))
            {
                _logger.LogError("Shared WhatsApp number not configured");
                return false;
            }

            TwilioClient.Init(sharedAccountSid, sharedAuthToken);
            messagePrefix = $"*{workspace.Name}*\n";
            return true;
        }
        else if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom)
        {
            if (!workspace.HasTwilioIntegration())
            {
                _logger.LogWarning("Workspace {WorkspaceId} has custom mode but no Twilio integration configured", workspace.Id);
                return false;
            }

            TwilioClient.Init(workspace.TwilioAccountSid, DecryptString(workspace.TwilioAuthToken));
            fromNumber = FormatWhatsAppFromNumber(workspace.TwilioWhatsAppNumber);
            if (string.IsNullOrEmpty(fromNumber))
            {
                _logger.LogError("Custom WhatsApp number not configured for workspace {WorkspaceId}", workspace.Id);
                return false;
            }
            messagePrefix = "";
            return true;
        }

        return false;
    }

    private string? GetContentTemplateSid(TemplateType templateType)
    {
        return templateType switch
        {
            TemplateType.JourneyStart => _configuration["Twilio:ContentTemplates:JourneyStart"],
            TemplateType.CheckIn => _configuration["Twilio:ContentTemplates:CheckIn"],
            TemplateType.DeliveryCompleted => _configuration["Twilio:ContentTemplates:DeliveryCompleted"],
            TemplateType.DeliveryFailed => _configuration["Twilio:ContentTemplates:DeliveryFailed"],
            _ => null
        };
    }

    private bool ShouldUseContentTemplate(Monolith.WebAPI.Data.Workspace.Workspace workspace, string? contentSid)
    {
        if (string.IsNullOrWhiteSpace(contentSid))
            return false;

        if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            return false;

        return true;
    }

    private bool AllowFreeform(Monolith.WebAPI.Data.Workspace.Workspace workspace)
    {
        return workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox;
    }

    private string GetWorkspaceDisplayName(Monolith.WebAPI.Data.Workspace.Workspace workspace)
    {
        var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return workspace.Name;
    }

    private async Task<MessageResource> SendWhatsAppMessageAsync(
        string fromNumber,
        string toNumber,
        string? contentSid,
        Dictionary<string, string>? contentVariables,
        string body)
    {
        var options = new CreateMessageOptions(new PhoneNumber(toNumber))
        {
            From = new PhoneNumber(fromNumber)
        };

        if (!string.IsNullOrWhiteSpace(contentSid))
        {
            options.ContentSid = contentSid;
            if (contentVariables != null && contentVariables.Count > 0)
            {
                options.ContentVariables = JsonSerializer.Serialize(contentVariables);
            }
        }
        else
        {
            options.Body = body;
        }

        return await MessageResource.CreateAsync(options);
    }

    public async Task<bool> SendJourneyStartedMessage(
        Monolith.WebAPI.Data.Workspace.Workspace workspace, 
        string toNumber, 
        string customerName, 
        string driverName, 
        string estimatedTime)
    {
        // Rate limit kontrolü
        var rateLimitResult = await _rateLimiter.CheckRateLimit(workspace.Id);
        
        if (!rateLimitResult.CanSend)
        {
            _logger.LogWarning("WhatsApp rate limit hit for workspace {WorkspaceId}: {Reason}", 
                workspace.Id, rateLimitResult.Reason);
            return false;
        }

        if (!InitializeTwilioClient(workspace, out string fromNumber, out string messagePrefix))
        {
            return false;
        }

        try
        {
            var formattedNumber = FormatPhoneNumber(toNumber);
            var trackingUrl = $"{_configuration["Tracking:ApiBaseUrl"]}/tracking";
            var contentSid = GetContentTemplateSid(TemplateType.JourneyStart);
            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = GetWorkspaceDisplayName(workspace),
                ["2"] = customerName ?? "",
                ["3"] = driverName ?? "",
                ["4"] = estimatedTime ?? "",
                ["5"] = trackingUrl
            };
            var useTemplate = ShouldUseContentTemplate(workspace, contentSid);
            var message = string.Empty;

            if (!useTemplate && !AllowFreeform(workspace))
            {
                _logger.LogError("WhatsApp content template missing for JourneyStart. WorkspaceId: {WorkspaceId}", workspace.Id);
                return false;
            }

            if (!useTemplate)
            {
                var templateData = new Dictionary<string, object>
            {
                ["customer"] = new { name = customerName },
                ["journey"] = new { date = DateTime.Now.ToString("dd MMMM yyyy") },
                ["driver"] = new { name = driverName, phone = "0850 756 62 67" },
                    ["estimatedCompletionTime"] = estimatedTime,
                    ["trackingUrl"] = trackingUrl,
                ["workspace"] = new 
                { 
                    name = workspace.Name,
                    email = workspace.Email ?? "",
                    phoneNumber = workspace.PhoneNumber ?? ""
                },
                ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["currentTime"] = DateTime.Now.ToString("HH:mm")
            };

            message = await _templateService.GetProcessedWhatsAppContentAsync(
                workspace.Id, 
                TemplateType.JourneyStart, 
                templateData);

            if (string.IsNullOrEmpty(message))
            {
                message = $"📦 Sayın {customerName}, siparişiniz yola çıktı!\n\n" +
                         $"🚛 Teslimat görevlisi: {driverName}\n" +
                         $"⏰ Tahmini varış: {estimatedTime}\n\n" +
                         $"Teslimatınız yaklaştığında tekrar bilgilendirileceksiniz.\n\n";
                
                if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
                {
                    message += "YolPilot";
                }
                else
                {
                    var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName ?? workspace.Name;
                    message += displayName;
                }
            }

            if (!string.IsNullOrEmpty(messagePrefix))
            {
                message = messagePrefix + message;
            }

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            {
                message = $"Your appointment is coming up on {estimatedTime} at {customerName}";
            }

            // YolPilot markasını ekle
            message = AddYolPilotBranding(message);
        }

            var messageResource = await SendWhatsAppMessageAsync(
                fromNumber,
                $"whatsapp:{formattedNumber}",
                useTemplate ? contentSid : null,
                useTemplate ? contentVariables : null,
                message
            );

            // Başarılı gönderim sonrası rate limit kaydı
            await _rateLimiter.RecordMessage(workspace.Id);

            _logger.LogInformation("WhatsApp message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}", 
                workspace.Id, formattedNumber, messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp journey started message from workspace {WorkspaceId} to {Number}", 
                workspace.Id, toNumber);
            return false;
        }
    }

    public async Task<bool> SendApproachingMessage(
        Monolith.WebAPI.Data.Workspace.Workspace workspace,
        string toNumber,
        string customerName,
        string minTime,
        string maxTime,
        string driverName = null,
        string driverPhone = null)
    {
        // Rate limit kontrolü
        var rateLimitResult = await _rateLimiter.CheckRateLimit(workspace.Id);

        if (!rateLimitResult.CanSend)
        {
            _logger.LogWarning("WhatsApp rate limit hit for workspace {WorkspaceId}: {Reason}",
                workspace.Id, rateLimitResult.Reason);
            return false;
        }

        if (!InitializeTwilioClient(workspace, out string fromNumber, out string messagePrefix))
        {
            return false;
        }

        try
        {
            var formattedNumber = FormatPhoneNumber(toNumber);
            var trackingUrl = $"{_configuration["Tracking:ApiBaseUrl"]}/tracking";
            var contentSid = GetContentTemplateSid(TemplateType.CheckIn);
            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = GetWorkspaceDisplayName(workspace),
                ["2"] = customerName ?? "",
                ["3"] = minTime ?? "",
                ["4"] = maxTime ?? "",
                ["5"] = driverName ?? "Teslimat gorevlisi",
                ["6"] = driverPhone ?? workspace.PhoneNumber ?? "0850 756 62 67",
                ["7"] = trackingUrl
            };
            var useTemplate = ShouldUseContentTemplate(workspace, contentSid);
            var message = string.Empty;

            if (!useTemplate && !AllowFreeform(workspace))
            {
                _logger.LogError("WhatsApp content template missing for CheckIn. WorkspaceId: {WorkspaceId}", workspace.Id);
                return false;
            }

            if (!useTemplate)
            {
                var templateData = new Dictionary<string, object>
            {
                ["customer"] = new { name = customerName, address = "Teslimat Adresi" },
                ["stop"] = new { estimatedArrivalTime = $"{minTime} - {maxTime}" },
                ["driver"] = new
                {
                    name = driverName ?? "Teslimat Görevlisi",
                    phoneNumber = driverPhone ?? workspace.PhoneNumber ?? "0850 756 62 67"
                },
                ["trackingUrl"] = trackingUrl,
                ["workspace"] = new
                {
                    name = workspace.Name,
                    email = workspace.Email ?? "",
                    phoneNumber = workspace.PhoneNumber ?? ""
                },
                ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["currentTime"] = DateTime.Now.ToString("HH:mm")
            };

            message = await _templateService.GetProcessedWhatsAppContentAsync(
                workspace.Id, 
                TemplateType.CheckIn, 
                templateData);

            if (string.IsNullOrEmpty(message))
            {
                message = $"⏰ Sayın {customerName}, teslimatınız yaklaşıyor!\n\n" +
                         $"🚛 Sürücümüz {minTime} - {maxTime} arasında sizinle olacak.\n\n" +
                         $"Lütfen teslimat adresinde bulunmaya özen gösterin.\n\n";
                
                if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
                {
                    message += "YolPilot";
                }
                else
                {
                    var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName ?? workspace.Name;
                    message += displayName;
                }
            }

            if (!string.IsNullOrEmpty(messagePrefix))
            {
                message = messagePrefix + message;
            }

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            {
                message = $"Your appointment is coming up on {minTime} at {customerName}";
            }

            // YolPilot markasını ekle
            message = AddYolPilotBranding(message);
        }

            var messageResource = await SendWhatsAppMessageAsync(
                fromNumber,
                $"whatsapp:{formattedNumber}",
                useTemplate ? contentSid : null,
                useTemplate ? contentVariables : null,
                message
            );

            // Başarılı gönderim sonrası rate limit kaydı
            await _rateLimiter.RecordMessage(workspace.Id);

            _logger.LogInformation("WhatsApp approaching message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}", 
                workspace.Id, formattedNumber, messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp approaching message from workspace {WorkspaceId} to {Number}", 
                workspace.Id, toNumber);
            return false;
        }
    }

    // ✅ YENİ: receiverName parametresi eklendi
    public async Task<bool> SendDeliveryCompletedMessage(
        Monolith.WebAPI.Data.Workspace.Workspace workspace, 
        string toNumber, 
        string customerName,
        string receiverName,  // ✅ YENİ PARAMETRE
        string trackingUrl,
        string feedbackUrl)
    {
        // Rate limit kontrolü
        var rateLimitResult = await _rateLimiter.CheckRateLimit(workspace.Id);
        
        if (!rateLimitResult.CanSend)
        {
            _logger.LogWarning("WhatsApp rate limit hit for workspace {WorkspaceId}: {Reason}", 
                workspace.Id, rateLimitResult.Reason);
            return false;
        }

        if (!InitializeTwilioClient(workspace, out string fromNumber, out string messagePrefix))
        {
            return false;
        }

        try
        {
            var formattedNumber = FormatPhoneNumber(toNumber);
            var completedTime = DateTime.Now.ToString("HH:mm");
            var contentSid = GetContentTemplateSid(TemplateType.DeliveryCompleted);
            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = GetWorkspaceDisplayName(workspace),
                ["2"] = customerName ?? "",
                ["3"] = receiverName ?? "",
                ["4"] = completedTime,
                ["5"] = trackingUrl ?? "",
                ["6"] = feedbackUrl ?? ""
            };
            var useTemplate = ShouldUseContentTemplate(workspace, contentSid);
            var message = string.Empty;

            if (!useTemplate && !AllowFreeform(workspace))
            {
                _logger.LogError("WhatsApp content template missing for DeliveryCompleted. WorkspaceId: {WorkspaceId}", workspace.Id);
                return false;
            }

            if (!useTemplate)
            {
                var templateData = new Dictionary<string, object>
            {
                ["customer"] = new { name = customerName, address = "Teslimat Adresi" },
                ["completedTime"] = completedTime,
                ["receiverName"] = receiverName ?? "",  // ✅ YENİ
                ["driver"] = new { name = "Teslimat Görevlisi" },
                ["signatureUrl"] = $"{trackingUrl}/signature",
                ["photoUrl"] = $"{trackingUrl}/photo",
                ["stop"] = new { notes = "" },
                ["feedbackUrl"] = feedbackUrl,
                ["trackingUrl"] = trackingUrl,
                ["workspace"] = new 
                { 
                    name = workspace.Name,
                    email = workspace.Email ?? "",
                    phoneNumber = workspace.PhoneNumber ?? ""
                },
                ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["currentTime"] = DateTime.Now.ToString("HH:mm")
            };

            message = await _templateService.GetProcessedWhatsAppContentAsync(
                workspace.Id, 
                TemplateType.DeliveryCompleted, 
                templateData);

            if (string.IsNullOrEmpty(message))
            {
                // ✅ Fallback mesajda da receiverName göster
                message = $"✅ Sayın {customerName}, teslimatınız tamamlandı!\n\n";
                
                if (!string.IsNullOrEmpty(receiverName))
                {
                    message += $"👤 Teslim Alan: {receiverName}\n";
                }
                
                message += $"⏰ Teslim Zamanı: {DateTime.Now:HH:mm}\n\n" +
                          $"📋 Teslimat detayları için:\n{trackingUrl}\n\n" +
                          $"⭐ Deneyiminizi değerlendirin:\n{feedbackUrl}\n\n" +
                          $"Bizi tercih ettiğiniz için teşekkür ederiz.\n\n";
                
                if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
                {
                    message += "YolPilot";
                }
                else
                {
                    var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName ?? workspace.Name;
                    message += displayName;
                }
            }

            if (!string.IsNullOrEmpty(messagePrefix))
            {
                message = messagePrefix + message;
            }

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            {
                message = $"Your appointment is coming up on today at {customerName}";
            }

            // YolPilot markasını ekle
            message = AddYolPilotBranding(message);
        }

            var messageResource = await SendWhatsAppMessageAsync(
                fromNumber,
                $"whatsapp:{formattedNumber}",
                useTemplate ? contentSid : null,
                useTemplate ? contentVariables : null,
                message
            );

            // Başarılı gönderim sonrası rate limit kaydı
            await _rateLimiter.RecordMessage(workspace.Id);

            _logger.LogInformation("WhatsApp completed message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}", 
                workspace.Id, formattedNumber, messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp completed message from workspace {WorkspaceId} to {Number}", 
                workspace.Id, toNumber);
            return false;
        }
    }

    public async Task<bool> SendDeliveryFailedMessage(
        Monolith.WebAPI.Data.Workspace.Workspace workspace, 
        string toNumber, 
        string customerName, 
        string failureReason, 
        string trackingUrl,
        string feedbackUrl)
    {
        // Rate limit kontrolü
        var rateLimitResult = await _rateLimiter.CheckRateLimit(workspace.Id);
        
        if (!rateLimitResult.CanSend)
        {
            _logger.LogWarning("WhatsApp rate limit hit for workspace {WorkspaceId}: {Reason}", 
                workspace.Id, rateLimitResult.Reason);
            return false;
        }

        if (!InitializeTwilioClient(workspace, out string fromNumber, out string messagePrefix))
        {
            return false;
        }

        try
        {
            var formattedNumber = FormatPhoneNumber(toNumber);
            var turkishReason = TranslateFailureReason(failureReason);
            var rescheduleUrl = $"{trackingUrl}&reschedule=true";
            var contentSid = GetContentTemplateSid(TemplateType.DeliveryFailed);
            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = GetWorkspaceDisplayName(workspace),
                ["2"] = customerName ?? "",
                ["3"] = turkishReason ?? "",
                ["4"] = rescheduleUrl,
                ["5"] = trackingUrl ?? ""
            };
            var useTemplate = ShouldUseContentTemplate(workspace, contentSid);
            var message = string.Empty;

            if (!useTemplate && !AllowFreeform(workspace))
            {
                _logger.LogError("WhatsApp content template missing for DeliveryFailed. WorkspaceId: {WorkspaceId}", workspace.Id);
                return false;
            }

            if (!useTemplate)
            {
                var templateData = new Dictionary<string, object>
            {
                ["customer"] = new { name = customerName, address = "Teslimat Adresi" },
                ["failureReason"] = turkishReason,
                ["failureTime"] = DateTime.Now.ToString("HH:mm"),
                ["driver"] = new { name = "Teslimat Görevlisi", phone = workspace.PhoneNumber ?? "0850 756 62 67" },
                ["rescheduleUrl"] = rescheduleUrl,
                ["workspace"] = new 
                { 
                    name = workspace.Name,
                    email = workspace.Email ?? "",
                    phoneNumber = workspace.PhoneNumber ?? ""
                },
                ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                ["currentTime"] = DateTime.Now.ToString("HH:mm")
            };

            message = await _templateService.GetProcessedWhatsAppContentAsync(
                workspace.Id, 
                TemplateType.DeliveryFailed, 
                templateData);

            if (string.IsNullOrEmpty(message))
            {
                message = $"❌ Sayın {customerName}, teslimatınız gerçekleştirilemedi.\n\n" +
                         $"📋 Sebep: {turkishReason}\n\n" +
                         $"En geç 1 iş günü içinde sizinle iletişime geçeceğiz.\n" +
                         $"Detaylar: {trackingUrl}\n\n";
                
                if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
                {
                    message += "YolPilot";
                }
                else
                {
                    var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName ?? workspace.Name;
                    message += displayName;
                }
            }

            if (!string.IsNullOrEmpty(messagePrefix))
            {
                message = messagePrefix + message;
            }

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            {
                message = $"Your appointment is coming up on tomorrow at {customerName}";
            }

            // YolPilot markasını ekle
            message = AddYolPilotBranding(message);
        }

            var messageResource = await SendWhatsAppMessageAsync(
                fromNumber,
                $"whatsapp:{formattedNumber}",
                useTemplate ? contentSid : null,
                useTemplate ? contentVariables : null,
                message
            );

            // Başarılı gönderim sonrası rate limit kaydı
            await _rateLimiter.RecordMessage(workspace.Id);

            _logger.LogInformation("WhatsApp failed message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}", 
                workspace.Id, formattedNumber, messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp failed message from workspace {WorkspaceId} to {Number}", 
                workspace.Id, toNumber);
            return false;
        }
    }

    public async Task<bool> SendTestMessage(Monolith.WebAPI.Data.Workspace.Workspace workspace, string toNumber)
    {
        // Test mesajı rate limit kontrolüne tabi değil
        if (!InitializeTwilioClient(workspace, out string fromNumber, out string messagePrefix))
        {
            return false;
        }

        try
        {
            var formattedNumber = FormatPhoneNumber(toNumber);
            var trackingUrl = $"{_configuration["Tracking:ApiBaseUrl"]}/tracking";
            var contentSid = GetContentTemplateSid(TemplateType.JourneyStart);
            var contentVariables = new Dictionary<string, string>
            {
                ["1"] = GetWorkspaceDisplayName(workspace),
                ["2"] = "Test Musteri",
                ["3"] = "Test Surucu",
                ["4"] = "Test",
                ["5"] = trackingUrl
            };
            var useTemplate = ShouldUseContentTemplate(workspace, contentSid);

            if (useTemplate)
            {
                var templateMessageResource = await SendWhatsAppMessageAsync(
                    fromNumber,
                    $"whatsapp:{formattedNumber}",
                    contentSid,
                    contentVariables,
                    string.Empty
                );

                _logger.LogInformation("WhatsApp test message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}",
                    workspace.Id, formattedNumber, templateMessageResource.Sid);
                return true;
            }

            if (!AllowFreeform(workspace))
            {
                _logger.LogError("WhatsApp content template missing for test message. WorkspaceId: {WorkspaceId}", workspace.Id);
                return false;
            }
            var message = messagePrefix +
                         $"🎉 Test mesajı!\n\n" +
                         $"WhatsApp entegrasyonunuz başarıyla çalışıyor.\n";

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Shared)
            {
                message += $"Mode: YolPilot Paylaşımlı Numara\n" +
                          $"Workspace: {workspace.Name}\n\n" +
                          $"YolPilot";
            }
            else
            {
                message += $"Mode: Özel WhatsApp Numarası\n" +
                          $"Workspace: {workspace.Name}\n\n";
                var displayName = workspace.Settings?.WhatsAppSettings?.BusinessDisplayName ?? workspace.Name;
                message += displayName;
            }

            if (workspace.WhatsAppMode == Monolith.WebAPI.Data.Workspace.Enums.WhatsAppMode.Custom && workspace.TwilioUseSandbox)
            {
                message = "Your appointment is coming up on today at Test";
            }

            // YolPilot markasını ekle
            message = AddYolPilotBranding(message);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber($"whatsapp:{formattedNumber}")
            );

            _logger.LogInformation("WhatsApp test message sent from workspace {WorkspaceId} to {Number}. MessageSid: {Sid}", 
                workspace.Id, formattedNumber, messageResource.Sid);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp test message from workspace {WorkspaceId} to {Number}", 
                workspace.Id, toNumber);
            return false;
        }
    }

    public async Task<bool> VerifyPhoneNumber(string phoneNumber)
    {
        var formatted = FormatPhoneNumber(phoneNumber);
        return await Task.FromResult(!string.IsNullOrEmpty(formatted) && formatted.StartsWith("+"));
    }

    public string FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return "";

        var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");
        
        if (digitsOnly.StartsWith("90"))
        {
            return $"+{digitsOnly}";
        }
        else if (digitsOnly.StartsWith("0"))
        {
            return $"+9{digitsOnly}";
        }
        else if (digitsOnly.Length == 10)
        {
            return $"+90{digitsOnly}";
        }
        
        if (phoneNumber.StartsWith("+"))
        {
            return phoneNumber;
        }
        
        return $"+90{digitsOnly}";
    }

    private string FormatWhatsAppFromNumber(string fromNumber)
    {
        if (string.IsNullOrWhiteSpace(fromNumber))
            return "";

        var trimmed = fromNumber.Trim();

        if (trimmed.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (trimmed.StartsWith("+"))
            return $"whatsapp:{trimmed}";

        var formatted = FormatPhoneNumber(trimmed);
        if (string.IsNullOrEmpty(formatted))
            return "";

        return $"whatsapp:{formatted}";
    }

    private string TranslateFailureReason(string failureReason)
    {
        return failureReason switch
        {
            "Customer not available" => "Müşteri adreste bulunamadı",
            "Wrong address" => "Adres bilgisi hatalı",
            "Customer refused" => "Müşteri teslimatı kabul etmedi",
            "Package damaged" => "Paket hasarlı",
            "Vehicle breakdown" => "Araç arızası",
            "Weather conditions" => "Hava koşulları",
            "Traffic accident" => "Trafik kazası",
            "Access restricted" => "Adrese erişim kısıtlı",
            _ => failureReason
        };
    }

    /// <summary>
    /// WhatsApp mesajına YolPilot markasını otomatik ekler
    /// </summary>
    private string AddYolPilotBranding(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // YolPilot markasını mesajın sonuna ekle
        return message + "\n\n---\nYolPilot ile güvenle teslimat 🚀";
    }

    private string DecryptString(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
            return encrypted;

        try
        {
            var key = _configuration["Encryption:Key"] ?? "YP2024Enc!xKm9$nQ8#pL5@wR7&bT3*v";
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];
            
            var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(
                Convert.FromBase64String(encrypted), 
                0, 
                Convert.FromBase64String(encrypted).Length);
            
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt auth token");
            return encrypted;
        }
    }
}

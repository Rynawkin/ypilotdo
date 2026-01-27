using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Monolith.WebAPI.Services.Templates;
using Monolith.WebAPI.Data.Workspace;
using Microsoft.EntityFrameworkCore;
using Monolith.WebAPI.Data;

namespace Monolith.WebAPI.Services.Email;

public interface IEmailService
{
    System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string htmlContent, string plainTextContent = null);
    System.Threading.Tasks.Task SendEmailToMultipleRecipientsAsync(List<string> recipients, string subject, string htmlContent, string plainTextContent = null);
    System.Threading.Tasks.Task SendWelcomeEmailToWorkspaceAdmin(string adminEmail, string adminName, string workspaceName);
    System.Threading.Tasks.Task SendWelcomeEmailToDriver(string driverEmail, string driverName, string tempPassword);
    System.Threading.Tasks.Task SendJourneyAssignmentEmail(string driverEmail, string driverName, int journeyId, DateTime date, int stopCount);
    
    // Eski tek kişi metodları (backward compatibility)
    System.Threading.Tasks.Task SendJourneyStartedToCustomers(string customerEmail, string customerName, string driverName, string estimatedTime);
    System.Threading.Tasks.Task SendNearbyNotificationAsync(string customerEmail, string customerName, string minTime, string maxTime);
    System.Threading.Tasks.Task SendDeliveryCompletedEmail(string customerEmail, string customerName, string trackingUrl, string notes, string receiverName = null);
    System.Threading.Tasks.Task SendDeliveryFailedEmail(string customerEmail, string customerName, string failureReason, string notes, string trackingUrl);
    
    // Yeni çoklu kişi metodları
    System.Threading.Tasks.Task SendJourneyStartedToCustomerContactsAsync(int customerId, string customerName, string driverName, string estimatedTime);
    System.Threading.Tasks.Task SendNearbyNotificationToCustomerContactsAsync(int customerId, string customerName, string minTime, string maxTime);
    System.Threading.Tasks.Task SendDeliveryCompletedToCustomerContactsAsync(int customerId, string customerName, string trackingUrl, string notes, string receiverName = null, string feedbackUrl = null);
    System.Threading.Tasks.Task SendDeliveryFailedToCustomerContactsAsync(int customerId, string customerName, string failureReason, string notes, string trackingUrl);
    System.Threading.Tasks.Task SendJourneyAssignedToCustomerContactsAsync(int customerId, string customerName, int journeyId, DateTime date);
    System.Threading.Tasks.Task SendJourneyCancelledToCustomerContactsAsync(int customerId, string customerName, int journeyId, string reason);
    
    System.Threading.Tasks.Task SendPasswordResetEmail(string email, string resetToken);
    System.Threading.Tasks.Task SendDailyReportToAdmins(string adminEmail, string reportHtml);
    System.Threading.Tasks.Task SendWelcomeEmailToDispatcher(string dispatcherEmail, string dispatcherName, string workspaceName);
    System.Threading.Tasks.Task SendMaintenanceReminderEmail(string to, string recipientName, string vehiclePlate, string vehicleBrand, string maintenanceType, DateTime maintenanceDate, int daysRemaining);
    System.Threading.Tasks.Task SendJourneyDelayReportAsync(List<string> recipients, int journeyId, string journeyName, string driverName, DateTime plannedCompletionTime, DateTime actualCompletionTime, TimeSpan delayDuration, string workspaceName, decimal? plannedDistanceKm = null, decimal? actualDistanceKm = null);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly TransactionalEmailsApi _emailApi;
    private readonly ITemplateService _templateService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _replyToEmail;

    public EmailService(
        IConfiguration configuration, 
        ILogger<EmailService> logger,
        ITemplateService templateService,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _templateService = templateService;
        _serviceProvider = serviceProvider;

        var apiKey = _configuration["Email:Brevo:ApiKey"] ?? _configuration["Email__Brevo__ApiKey"];
        
        _logger.LogInformation($"EmailService initialization started");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _logger.LogInformation($"API Key found, starts with: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
            
            try
            {
                Configuration.Default.ApiKey["api-key"] = apiKey;
                _emailApi = new TransactionalEmailsApi();
                _logger.LogInformation("Brevo TransactionalEmailsApi initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Brevo API");
            }
        }
        else
        {
            _logger.LogError("CRITICAL: Brevo API key not configured. Email service will not work.");
        }
        
        _senderEmail = _configuration["Email:Brevo:SenderEmail"] ?? _configuration["Email__Brevo__SenderEmail"] ?? "noreply@yolpilot.com";
        _senderName = _configuration["Email:Brevo:SenderName"] ?? _configuration["Email__Brevo__SenderName"] ?? "YolPilot";
        _replyToEmail = _configuration["Email:Brevo:ReplyToEmail"] ?? _configuration["Email__Brevo__ReplyToEmail"] ?? "info@yolpilot.com";
        
        _logger.LogInformation($"EmailService configured with:");
        _logger.LogInformation($"  Sender Email: {_senderEmail}");
        _logger.LogInformation($"  Sender Name: {_senderName}");
        _logger.LogInformation($"  Reply To: {_replyToEmail}");
    }

    public async System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string htmlContent, string plainTextContent = null)
    {
        _logger.LogInformation($"[EMAIL] SendEmailAsync called for {to} with subject: {subject}");

        try
        {
            if (_emailApi == null)
            {
                _logger.LogError($"[EMAIL] CRITICAL: Email API is null. Cannot send email to {to}");
                _logger.LogError($"[EMAIL] This usually means API key was not configured properly during initialization");
                return;
            }

            _logger.LogInformation($"[EMAIL] Email API is initialized, proceeding with send to {to}");

            if (!htmlContent.Contains("charset") && htmlContent.Contains("<head>"))
            {
                htmlContent = htmlContent.Replace("<head>", "<head>\n    <meta charset=\"UTF-8\">");
            }

            // YolPilot markasını otomatik ekle
            htmlContent = AddYolPilotBranding(htmlContent);

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_senderName, _senderEmail),
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to) },
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = plainTextContent ?? StripHtml(htmlContent),
                ReplyTo = new SendSmtpEmailReplyTo(_replyToEmail),
                Headers = new Dictionary<string, string>
                {
                    { "charset", "UTF-8" },
                    { "Content-Type", "text/html; charset=UTF-8" }
                }
            };

            _logger.LogInformation($"[EMAIL] Calling Brevo API to send email to {to}");
            _logger.LogInformation($"[EMAIL] Request details - From: {_senderEmail}, To: {to}, Subject: {subject}");

            var result = await _emailApi.SendTransacEmailAsync(sendSmtpEmail);

            _logger.LogInformation($"[EMAIL] ✅ Email sent successfully to {to}. MessageId: {result?.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[EMAIL] ❌ Failed to send email to {to}");
            _logger.LogError($"[EMAIL] Exception type: {ex.GetType().Name}");
            _logger.LogError($"[EMAIL] Exception message: {ex.Message}");
            _logger.LogError($"[EMAIL] Stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                _logger.LogError($"[EMAIL] Inner exception: {ex.InnerException.Message}");
                _logger.LogError($"[EMAIL] Inner exception stack: {ex.InnerException.StackTrace}");
            }

            // API response hatasını yakalayalım
            if (ex.Message.Contains("ApiException") || ex.Message.Contains("400") || ex.Message.Contains("401") || ex.Message.Contains("403"))
            {
                _logger.LogError($"[EMAIL] This looks like a Brevo API error. Check API key and sender email configuration.");
            }

            throw;
        }
    }

    public async System.Threading.Tasks.Task SendWelcomeEmailToWorkspaceAdmin(string adminEmail, string adminName, string workspaceName)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var workspace = await context.Workspaces
                .FirstOrDefaultAsync(w => w.Name == workspaceName);
            
            if (workspace != null)
            {
                var data = new Dictionary<string, object>
                {
                    ["user"] = new { fullName = adminName, email = adminEmail },
                    ["workspace"] = new { 
                        name = workspaceName, 
                        email = workspace.Email ?? "info@yolpilot.com", 
                        phoneNumber = workspace.PhoneNumber ?? "0850 123 45 67" 
                    },
                    ["loginUrl"] = "https://app.yolpilot.com/dashboard",
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                    workspace.Id, 
                    TemplateType.WelcomeEmail, 
                    data);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailAsync(adminEmail, subject, body);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template-based welcome email, falling back to default");
        }

        // Fallback to default template
        var defaultSubject = "YolPilot'a Hoş Geldiniz - Hesabınız Hazır!";
        var defaultHtml = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; border-radius: 10px 10px 0 0; text-align: center; }
        .header h1 { margin: 0; font-size: 28px; }
        .content { background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px; }
        .welcome-box { background: linear-gradient(135deg, #f5f3ff 0%, #e8e6ff 100%); padding: 25px; border-radius: 10px; margin: 25px 0; border-left: 4px solid #667eea; }
        .features { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; }
        .feature-item { padding: 10px 0; border-bottom: 1px solid #e0e0e0; }
        .feature-item:last-child { border-bottom: none; }
        .button { display: inline-block; padding: 14px 32px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin-top: 20px; font-weight: 500; }
        .button:hover { background: #5a67d8; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; font-size: 14px; color: #666; text-align: center; }
        .success-badge { display: inline-block; background: #28a745; color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; margin-left: 10px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 YolPilot'a Hoş Geldiniz!</h1>
        </div>
        <div class=""content"">
            <p>Merhaba <strong>" + adminName + @"</strong>,</p>
            <p>YolPilot ailesine hoş geldiniz! <strong>" + workspaceName + @"</strong> çalışma alanınız başarıyla oluşturuldu ve kullanıma hazır.</p>
            
            <div class=""welcome-box"">
                <h3 style=""margin-top: 0; color: #667eea;"">🚀 Hesap Bilgileriniz</h3>
                <p><strong>Çalışma Alanı:</strong> " + workspaceName + @" <span class=""success-badge"">Aktif</span></p>
                <p><strong>E-posta Adresiniz:</strong> " + adminEmail + @"</p>
                <p><strong>Rol:</strong> Yönetici (Admin)</p>
                <p style=""margin-top: 15px; color: #666; font-size: 14px;"">🔒 Güvenliğiniz için şifreniz e-posta ile paylaşılmamaktadır.</p>
            </div>

            <div class=""features"">
                <h3 style=""margin-top: 0; color: #333;"">✨ YolPilot ile Neler Yapabilirsiniz?</h3>
                <div class=""feature-item"">
                    <strong>🗺 Rota Optimizasyonu:</strong> Teslimat rotalarınızı otomatik olarak optimize edin
                </div>
                <div class=""feature-item"">
                    <strong>👥 Ekip Yönetimi:</strong> Sürücülerinizi ve araçlarınızı kolayca yönetin
                </div>
                <div class=""feature-item"">
                    <strong>📊 Gerçek Zamanlı Takip:</strong> Teslimatları canlı olarak izleyin
                </div>
                <div class=""feature-item"">
                    <strong>📈 Detaylı Raporlar:</strong> Performans ve verimlilik analizleri alın
                </div>
                <div class=""feature-item"">
                    <strong>📬 Otomatik Bildirimler:</strong> Müşterilerinize SMS ve e-posta bildirimleri gönderin
                </div>
            </div>
            
            <center>
                <a href=""https://app.yolpilot.com/dashboard"" class=""button"">📱 Kontrol Paneline Git</a>
            </center>
            
            <div class=""footer"">
                <p><strong>Sorularınız mı var?</strong></p>
                <p>Destek ekibimiz size yardımcı olmak için hazır!</p>
                <p>📧 destek@yolpilot.com | 📞 0850 123 45 67</p>
                <p style=""margin-top: 20px; font-size: 12px;"">
                    © " + DateTime.Now.Year + @" YolPilot. Tüm hakları saklıdır.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(adminEmail, defaultSubject, defaultHtml);
    }

    
    public async System.Threading.Tasks.Task SendWelcomeEmailToDispatcher(string dispatcherEmail, string dispatcherName, string workspaceName)
    {
        var subject = "YolPilot Dispatcher Hesabınız Oluşturuldu";
        var htmlContent = @"
    <!DOCTYPE html>
    <html lang=""tr"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%); color: white; padding: 40px 30px; border-radius: 10px 10px 0 0; text-align: center; }
            .header h1 { margin: 0; font-size: 28px; }
            .content { background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px; }
            .info-box { background: #eff6ff; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #3b82f6; }
            .info-box h3 { margin-top: 0; color: #1e40af; }
            .features-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin: 20px 0; }
            .feature { padding: 10px; background: #f9fafb; border-radius: 6px; }
            .button { display: inline-block; padding: 14px 32px; background: #3b82f6; color: white; text-decoration: none; border-radius: 6px; margin-top: 20px; font-weight: 500; }
            .button:hover { background: #2563eb; }
            .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; font-size: 14px; color: #666; text-align: center; }
            .role-badge { display: inline-block; background: #3b82f6; color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; }
        </style>
    </head>
    <body>
        <div class=""container"">
            <div class=""header"">
                <h1>🎯 YolPilot Dispatcher Hesabınız Hazır!</h1>
            </div>
            <div class=""content"">
                <p>Merhaba <strong>" + dispatcherName + @"</strong>,</p>
                <p><strong>" + workspaceName + @"</strong> için Dispatcher hesabınız başarıyla oluşturuldu. Artık rota yönetimi ve operasyon kontrolü yetkileriniz aktif.</p>
                
                <div class=""info-box"">
                    <h3>👤 Hesap Bilgileriniz</h3>
                    <p><strong>Çalışma Alanı:</strong> " + workspaceName + @"</p>
                    <p><strong>E-posta:</strong> " + dispatcherEmail + @"</p>
                    <p><strong>Rol:</strong> <span class=""role-badge"">DISPATCHER</span></p>
                    <p style=""margin-top: 15px; color: #666; font-size: 14px;"">
                        🔐 Şifreniz yöneticiniz tarafından size iletilecektir.
                    </p>
                </div>

                <h3 style=""color: #1e40af; margin-top: 30px;"">🛠️ Dispatcher Yetkileri</h3>
                <div class=""features-grid"">
                    <div class=""feature"">
                        <strong>📍 Rota Yönetimi</strong><br>
                        <small>Rota oluşturma ve düzenleme</small>
                    </div>
                    <div class=""feature"">
                        <strong>👥 Müşteri Yönetimi</strong><br>
                        <small>Müşteri kayıtları ve bilgileri</small>
                    </div>
                    <div class=""feature"">
                        <strong>🚗 Araç Takibi</strong><br>
                        <small>Araç durumu ve atamaları</small>
                    </div>
                    <div class=""feature"">
                        <strong>📊 Raporlar</strong><br>
                        <small>Operasyonel raporlara erişim</small>
                    </div>
                    <div class=""feature"">
                        <strong>📱 Canlı Takip</strong><br>
                        <small>Sürücü ve teslimat takibi</small>
                    </div>
                    <div class=""feature"">
                        <strong>📋 Sefer Yönetimi</strong><br>
                        <small>Aktif seferleri izleme</small>
                    </div>
                </div>
                
                <center>
                    <a href=""https://app.yolpilot.com/login"" class=""button"">🚀 Kontrol Paneline Git</a>
                </center>
                
                <div class=""footer"">
                    <p><strong>İlk Giriş İpuçları:</strong></p>
                    <ul style=""text-align: left; display: inline-block;"">
                        <li>Giriş yapmak için e-posta adresinizi kullanın</li>
                        <li>Şifrenizi yöneticinizden alın</li>
                        <li>İlk girişte şifrenizi değiştirmeniz önerilir</li>
                    </ul>
                    <p style=""margin-top: 20px;"">
                        <strong>Destek:</strong> destek@yolpilot.com | 0850 123 45 67
                    </p>
                    <p style=""margin-top: 15px; font-size: 12px; color: #999;"">
                        © " + DateTime.Now.Year + @" YolPilot. Tüm hakları saklıdır.
                    </p>
                </div>
            </div>
        </div>
    </body>
    </html>";

        await SendEmailAsync(dispatcherEmail, subject, htmlContent);
    }
    
    public async System.Threading.Tasks.Task SendWelcomeEmailToDriver(string driverEmail, string driverName, string tempPassword)
    {
        var subject = "YolPilot'a Hoş Geldiniz - Giriş Bilgileriniz";
        var htmlContent = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; border-radius: 10px 10px 0 0; text-align: center; }
        .header h1 { margin: 0; font-size: 28px; }
        .content { background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px; }
        .credentials { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #667eea; }
        .credentials h3 { margin-top: 0; color: #667eea; }
        .credentials p { margin: 10px 0; }
        .button { display: inline-block; padding: 14px 32px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin-top: 20px; font-weight: 500; }
        .button:hover { background: #5a67d8; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; font-size: 14px; color: #666; text-align: center; }
        .warning { color: #dc3545; font-weight: 500; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 YolPilot Ailesine Hoş Geldiniz!</h1>
        </div>
        <div class=""content"">
            <p>Merhaba <strong>" + driverName + @"</strong>,</p>
            <p>YolPilot rota optimizasyon sistemine başarıyla kayıt oldunuz.</p>
            
            <div class=""credentials"">
                <h3>📱 Giriş Bilgileriniz</h3>
                <p><strong>E-posta Adresiniz:</strong> " + driverEmail + @"</p>
                <p><strong>Geçici Şifreniz:</strong> " + tempPassword + @"</p>
                <p class=""warning"">⚠️ Güvenliğiniz için ilk girişte şifrenizi değiştirmeniz gerekmektedir.</p>
            </div>
            
            <center>
                <a href=""https://play.google.com/store/apps/details?id=com.yolpilot.driver"" class=""button"">📲 Android Uygulamasını İndir</a>
            </center>
            
            <div class=""footer"">
                <p>Sorularınız için destek ekibimize 7/24 ulaşabilirsiniz.</p>
                <p><strong>Destek:</strong> destek@yolpilot.com | <strong>Tel:</strong> 0850 756 62 67</p>
                <p>İyi çalışmalar dileriz! 🚚</p>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(driverEmail, subject, htmlContent);
    }

    public async System.Threading.Tasks.Task SendJourneyAssignmentEmail(string driverEmail, string driverName, int journeyId, DateTime date, int stopCount)
    {
        var subject = "Yeni Sefer Ataması - YolPilot";
        var htmlContent = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; border-radius: 10px 10px 0 0; text-align: center; }
        .content { background: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-radius: 0 0 10px 10px; }
        .info-card { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #667eea; }
        .info-card h3 { margin-top: 0; color: #667eea; }
        .button { display: inline-block; padding: 14px 32px; background: #667eea; color: white; text-decoration: none; border-radius: 6px; margin-top: 20px; font-weight: 500; }
        .alert { background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 8px; margin: 20px 0; color: #856404; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📋 Yeni Sefer Atandı!</h1>
        </div>
        <div class=""content"">
            <p>Merhaba <strong>" + driverName + @"</strong>,</p>
            <p>Size yeni bir teslimat seferi atandı.</p>
            
            <div class=""info-card"">
                <h3>🚚 Sefer Detayları</h3>
                <p><strong>Sefer Numarası:</strong> #" + journeyId + @"</p>
                <p><strong>Tarih:</strong> " + date.ToString("dd MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR")) + @"</p>
                <p><strong>Toplam Durak:</strong> " + stopCount + @" teslimat noktası</p>
            </div>
            
            <center>
                <a href=""#"" class=""button"">📱 Mobil Uygulamayı Aç</a>
            </center>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(driverEmail, subject, htmlContent);
    }

    public async System.Threading.Tasks.Task SendJourneyStartedToCustomers(string customerEmail, string customerName, string driverName, string estimatedTime)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Email == customerEmail);
            
            if (customer?.Workspace != null)
            {
                var data = new Dictionary<string, object>
                {
                    ["customer"] = new { name = customerName },
                    ["journey"] = new { date = DateTime.Now.ToString("dd MMMM yyyy") },
                    ["driver"] = new { name = driverName, phone = "0850 756 62 67" },
                    ["vehicle"] = new { brand = "Mercedes", model = "Sprinter", plateNumber = "34 XX 123" },
                    ["estimatedCompletionTime"] = estimatedTime,
                    ["trackingUrl"] = "https://app.yolpilot.com/tracking",
                    ["workspace"] = new { 
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email ?? "info@yolpilot.com",
                        phoneNumber = customer.Workspace.PhoneNumber ?? "0850 123 45 67"
                    },
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId, 
                    TemplateType.JourneyStart, 
                    data);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailAsync(customerEmail, subject, body);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template-based journey start email");
        }

        // Fallback
        var defaultSubject = "Siparişiniz Yola Çıktı - YolPilot";
        var defaultHtml = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #28a745; color: white; padding: 40px; text-align: center; }
        .content { background: #fff; padding: 30px; }
        .tracking-box { background: #f8f9fa; padding: 25px; text-align: center; border: 2px solid #28a745; }
        .time-display { font-size: 32px; color: #28a745; font-weight: bold; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🚚 Siparişiniz Yolda!</h1>
        </div>
        <div class=""content"">
            <p>Sayın <strong>" + customerName + @"</strong>,</p>
            <p>Güzel haber! Siparişiniz teslimat için yola çıktı.</p>
            
            <div class=""tracking-box"">
                <h3>⏰ Tahmini Varış Zamanı</h3>
                <p class=""time-display"">" + estimatedTime + @"</p>
            </div>

            <p><strong>Teslimat Görevlisi:</strong> " + driverName + @"</p>
            <p>Teşekkür ederiz, iyi günler dileriz!</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, defaultSubject, defaultHtml);
    }
    
    public async System.Threading.Tasks.Task SendNearbyNotificationAsync(string customerEmail, string customerName, string minTime, string maxTime)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Email == customerEmail);
            
            if (customer?.Workspace != null)
            {
                var data = new Dictionary<string, object>
                {
                    ["customer"] = new { name = customerName, address = customer.Address },
                    ["stop"] = new { estimatedArrivalTime = $"{minTime} - {maxTime}" },
                    ["driver"] = new { name = "Teslimat Görevlisi", phone = "0850 756 62 67" },
                    ["trackingUrl"] = "https://app.yolpilot.com/tracking",
                    ["workspace"] = new { 
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email ?? "info@yolpilot.com",
                        phoneNumber = customer.Workspace.PhoneNumber ?? "0850 123 45 67"
                    },
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId, 
                    TemplateType.CheckIn, 
                    data);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailAsync(customerEmail, subject, body);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template-based check-in notification");
        }

        // Fallback
        var defaultSubject = $"Teslimatınız {maxTime} içinde ulaşacak - YolPilot";
        var defaultHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #ffc107; color: #333; padding: 40px; text-align: center; }}
        .content {{ background: #fff; padding: 30px; }}
        .alert-box {{ background: #fff3cd; border: 2px solid #ffc107; padding: 20px; text-align: center; }}
        .time-range {{ font-size: 24px; font-weight: bold; color: #856404; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⏰ Teslimatınız Yaklaşıyor!</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='alert-box'>
                <h3>🚚 Sürücümüz yolda!</h3>
                <p class='time-range'>{minTime} - {maxTime}</p>
            </div>

            <p>Anlayışınız için teşekkür ederiz.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, defaultSubject, defaultHtml);
    }

    // YENİ PARAMETRE EKLENDİ: receiverName
    public async System.Threading.Tasks.Task SendDeliveryCompletedEmail(
        string customerEmail, 
        string customerName, 
        string trackingUrl, 
        string notes,
        string receiverName = null)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Email == customerEmail);
            
            if (customer?.Workspace != null)
            {
                var feedbackUrl = $"{trackingUrl}&feedback=true";

                var data = new Dictionary<string, object>
                {
                    ["customer"] = new { name = customerName, address = customer.Address },
                    ["completedTime"] = DateTime.Now.ToString("HH:mm"),
                    ["receiverName"] = receiverName ?? "", // YENİ
                    ["driver"] = new { name = "Teslimat Görevlisi" },
                    ["signatureUrl"] = $"{trackingUrl}/signature",
                    ["photoUrl"] = $"{trackingUrl}/photo",
                    ["stop"] = new { notes = notes },
                    ["feedbackUrl"] = feedbackUrl,
                    ["trackingUrl"] = trackingUrl,
                    ["workspace"] = new { 
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email ?? "info@yolpilot.com",
                        phoneNumber = customer.Workspace.PhoneNumber ?? "0850 123 45 67"
                    },
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId, 
                    TemplateType.DeliveryCompleted, 
                    data);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailAsync(customerEmail, subject, body);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template-based delivery completed email");
        }

        // Fallback
        var defaultSubject = "✅ Teslimatınız Tamamlandı - YolPilot";
        var defaultHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .success-message {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; text-align: center; }}
        .info-box {{ background: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 8px; }}
        .button {{ display: inline-block; padding: 14px 32px; background: #28a745; color: white; text-decoration: none; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Teslimat Başarıyla Tamamlandı!</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='success-message'>
                <h2>🎉 Siparişiniz Teslim Edildi!</h2>
            </div>

            {(!string.IsNullOrEmpty(receiverName) ? $@"
            <div class='info-box'>
                <p><strong>👤 Teslim Alan:</strong> {receiverName}</p>
                <p><strong>⏰ Teslim Zamanı:</strong> {DateTime.Now:HH:mm}</p>
            </div>" : "")}

            {(!string.IsNullOrEmpty(notes) ? $"<p><strong>Teslimat Notları:</strong> {notes}</p>" : "")}

            <center>
                <a href='{trackingUrl}' class='button'>✨ Teslimat Kanıtlarını Görüntüle</a>
            </center>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, defaultSubject, defaultHtml);
    }

    public async System.Threading.Tasks.Task SendDeliveryFailedEmail(string customerEmail, string customerName, string failureReason, string notes, string trackingUrl)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Email == customerEmail);
            
            if (customer?.Workspace != null)
            {
                var turkishReason = TranslateFailureReason(failureReason);
                var rescheduleUrl = $"{trackingUrl}&reschedule=true";

                var data = new Dictionary<string, object>
                {
                    ["customer"] = new { name = customerName, address = customer.Address },
                    ["failureReason"] = turkishReason,
                    ["failureTime"] = DateTime.Now.ToString("HH:mm"),
                    ["driver"] = new { name = "Teslimat Görevlisi", phone = "0850 756 62 67" },
                    ["rescheduleUrl"] = rescheduleUrl,
                    ["workspace"] = new { 
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email ?? "info@yolpilot.com",
                        phoneNumber = customer.Workspace.PhoneNumber ?? "0850 123 45 67"
                    },
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await _templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId, 
                    TemplateType.DeliveryFailed, 
                    data);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailAsync(customerEmail, subject, body);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template-based delivery failed email");
        }

        // Fallback
        var turkishReasonFallback = TranslateFailureReason(failureReason);
        var defaultSubject = "❌ Teslimatınız Gerçekleştirilemedi - YolPilot";
        var defaultHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .failure-message {{ background: #f8d7da; border: 1px solid #f5c6cb; padding: 20px; }}
        .reason-box {{ background: #fff; border: 2px solid #dc3545; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Teslimatınız Gerçekleştirilemedi</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='reason-box'>
                <h3>📋 Teslim Edilememe Nedeni:</h3>
                <p>{turkishReasonFallback}</p>
            </div>

            {(!string.IsNullOrEmpty(notes) ? $"<p><strong>Ek Notlar:</strong> {notes}</p>" : "")}

            <p>En geç 1 iş günü içinde sizinle iletişime geçeceğiz.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, defaultSubject, defaultHtml);
    }

    public async System.Threading.Tasks.Task SendPasswordResetEmail(string email, string resetToken)
    {
        _logger.LogInformation($"SendPasswordResetEmail called for {email}");
        
        var subject = "Şifre Sıfırlama Talebi - YolPilot";
        var resetLink = $"https://app.yolpilot.com/reset-password?token={resetToken}";
        var htmlContent = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; }
        .content { background: #ffffff; padding: 30px; }
        .button { display: inline-block; padding: 14px 32px; background: #667eea; color: white; text-decoration: none; }
        .warning { background: #fff3cd; border: 1px solid #ffc107; padding: 15px; color: #856404; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔒 Şifre Sıfırlama Talebi</h1>
        </div>
        <div class=""content"">
            <p>Merhaba,</p>
            <p>YolPilot hesabınız için şifre sıfırlama talebinde bulunuldu.</p>
            
            <center>
                <a href=""" + resetLink + @""" class=""button"">🔐 Şifremi Sıfırla</a>
            </center>
            
            <div class=""warning"">
                <p><strong>⚠️ Önemli:</strong></p>
                <ul>
                    <li>Bu bağlantı 24 saat geçerlidir</li>
                    <li>Bu talebi siz yapmadıysanız, bu e-postayı görmezden gelin</li>
                </ul>
            </div>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, subject, htmlContent);
    }

    public async System.Threading.Tasks.Task SendDailyReportToAdmins(string adminEmail, string reportHtml)
    {
        var subject = $"Günlük Özet Raporu - {DateTime.Now:dd MMMM yyyy} - YolPilot";
        await SendEmailAsync(adminEmail, subject, reportHtml);
    }

    private string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
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
            "Weather conditions" => "Hava koşulları nedeniyle",
            "Traffic accident" => "Trafik kazası",
            "Access restricted" => "Adrese erişim kısıtlı",
            "Other" => "Diğer nedenler",
            _ => failureReason
        };
    }

    // ÇOKLU KİŞİ DESTEĞİ - YENİ METODLAR

    public async System.Threading.Tasks.Task SendEmailToMultipleRecipientsAsync(List<string> recipients, string subject, string htmlContent, string plainTextContent = null)
    {
        _logger.LogInformation($"[EMAIL-MULTI] SendEmailToMultipleRecipientsAsync called with {recipients?.Count ?? 0} recipients");

        if (recipients == null || !recipients.Any())
        {
            _logger.LogWarning("[EMAIL-MULTI] No recipients provided, skipping email send");
            return;
        }

        _logger.LogInformation($"[EMAIL-MULTI] Recipients: {string.Join(", ", recipients)}");

        try
        {
            if (_emailApi == null)
            {
                _logger.LogError($"[EMAIL-MULTI] CRITICAL: Email API is null. Cannot send email to {recipients.Count} recipients");
                return;
            }

            // YolPilot markasını otomatik ekle
            htmlContent = AddYolPilotBranding(htmlContent);

            var sendSmtpEmail = new SendSmtpEmail
            {
                Sender = new SendSmtpEmailSender(_senderName, _senderEmail),
                To = recipients
                    .Where(email => !string.IsNullOrWhiteSpace(email))
                    .Select(email => new SendSmtpEmailTo(email.Trim()))
                    .ToList(),
                Subject = subject,
                HtmlContent = htmlContent,
                TextContent = plainTextContent ?? ExtractTextFromHtml(htmlContent),
                ReplyTo = new SendSmtpEmailReplyTo(_replyToEmail)
            };

            _logger.LogInformation($"[EMAIL-MULTI] Calling Brevo API to send email to {sendSmtpEmail.To.Count} recipients");
            _logger.LogInformation($"[EMAIL-MULTI] From: {_senderEmail}, Subject: {subject}");

            var result = await _emailApi.SendTransacEmailAsync(sendSmtpEmail);

            _logger.LogInformation($"[EMAIL-MULTI] ✅ Email sent successfully to {recipients.Count} recipients. MessageId: {result?.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[EMAIL-MULTI] ❌ Failed to send email to multiple recipients: {string.Join(", ", recipients)}");
            _logger.LogError($"[EMAIL-MULTI] Exception type: {ex.GetType().Name}");
            _logger.LogError($"[EMAIL-MULTI] Exception message: {ex.Message}");
            throw;
        }
    }

    public async System.Threading.Tasks.Task SendJourneyStartedToCustomerContactsAsync(int customerId, string customerName, string driverName, string estimatedTime)
    {
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.JourneyStart);
            
            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for journey start notification");
                return;
            }

            var subject = $"🚚 Teslimatınız Yola Çıktı - YolPilot";
            var htmlContent = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚚 Teslimatınız Yola Çıktı!</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='info-box'>
                <p><strong>📦 Teslimatınız başladı!</strong></p>
                <p><strong>🚗 Sürücü:</strong> {driverName}</p>
                <p><strong>⏰ Tahmini Varış:</strong> {estimatedTime}</p>
            </div>

            <p>Sürücümüz size ulaştığında bildirim alacaksınız.</p>
            
            <p>İyi günler dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, subject, htmlContent);
            _logger.LogInformation($"Journey started notification sent to {recipients.Count} contacts for customer {customerId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending journey started notification to customer contacts {customerId}");
        }
    }

    public async System.Threading.Tasks.Task SendNearbyNotificationToCustomerContactsAsync(int customerId, string customerName, string minTime, string maxTime)
    {
        _logger.LogInformation($"SendNearbyNotificationToCustomerContactsAsync called for customer {customerId} ({customerName})");
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.JourneyCheckIn);

            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for nearby notification");
                return;
            }

            // Template servisini kullan
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();

            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer?.Workspace != null)
            {
                // En son duraktaki driver bilgilerini bul
                var latestJourneyStop = await context.JourneyStops
                    .Include(js => js.Journey)
                        .ThenInclude(j => j.Driver)
                    .Where(js => js.RouteStop.CustomerId == customerId)
                    .OrderByDescending(js => js.CreatedAt)
                    .FirstOrDefaultAsync();

                var driver = latestJourneyStop?.Journey?.Driver;

                var templateData = new Dictionary<string, object>
                {
                    ["customer"] = new
                    {
                        name = customerName,
                        address = customer.Address
                    },
                    ["stop"] = new
                    {
                        estimatedArrivalTime = $"{minTime} - {maxTime}"
                    },
                    ["driver"] = new
                    {
                        name = driver?.Name ?? "Teslimat Görevlisi",
                        phoneNumber = driver?.Phone ?? customer.Workspace.PhoneNumber
                    },
                    ["trackingUrl"] = "https://app.yolpilot.com/tracking",
                    ["workspace"] = new
                    {
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email,
                        phoneNumber = customer.Workspace.PhoneNumber
                    }
                };

                var (subject, body) = await templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId,
                    TemplateType.CheckIn,
                    templateData);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailToMultipleRecipientsAsync(recipients, subject, body);
                    _logger.LogInformation($"Nearby notification sent to {recipients.Count} contacts for customer {customerId} using template");
                    return;
                }
            }

            // Fallback
            var fallbackSubject = $"📍 Sürücümüz Yaklaşıyor - YolPilot";
            var fallbackHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #fd7e14 0%, #e83e8c 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .alert-box {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📍 Sürücümüz Yaklaşıyor!</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>

            <div class='alert-box'>
                <p><strong>⏰ Sürücümüz yaklaştı!</strong></p>
                <p>Tahmini varış süresi: <strong>{minTime} - {maxTime}</strong></p>
                <p>Lütfen hazır bulunun.</p>
            </div>

            <p>İyi günler dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, fallbackSubject, fallbackHtml);
            _logger.LogInformation($"Nearby notification sent to {recipients.Count} contacts for customer {customerId} using fallback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending nearby notification to customer contacts {customerId}");
        }
    }

    public async System.Threading.Tasks.Task SendDeliveryCompletedToCustomerContactsAsync(int customerId, string customerName, string trackingUrl, string notes, string receiverName = null, string feedbackUrl = null)
    {
        _logger.LogInformation($"SendDeliveryCompletedToCustomerContactsAsync called for customer {customerId} ({customerName})");
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.DeliveryCompleted);

            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for delivery completed notification");
                return;
            }

            // Template servisini kullan
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();

            var customer = await context.Customers
                .Include(c => c.Workspace)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer?.Workspace != null)
            {
                // Feedback URL - parametre olarak gelmezse oluştur
                if (string.IsNullOrEmpty(feedbackUrl))
                {
                    feedbackUrl = $"{trackingUrl}?feedback=true";
                }

                var templateData = new Dictionary<string, object>
                {
                    ["customer"] = new
                    {
                        name = customerName,
                        address = customer.Address
                    },
                    ["completedTime"] = DateTime.Now.ToString("HH:mm"),
                    ["receiverName"] = receiverName ?? "",
                    ["driver"] = new { name = "Teslimat Görevlisi" },
                    ["signatureUrl"] = $"{trackingUrl}/signature",
                    ["photoUrl"] = $"{trackingUrl}/photo",
                    ["stop"] = new { notes = notes ?? "" },
                    ["feedbackUrl"] = feedbackUrl,
                    ["trackingUrl"] = trackingUrl,
                    ["workspace"] = new
                    {
                        name = customer.Workspace.Name,
                        email = customer.Workspace.Email ?? "info@yolpilot.com",
                        phoneNumber = customer.Workspace.PhoneNumber ?? "0850 123 45 67"
                    },
                    ["currentDate"] = DateTime.Now.ToString("dd MMMM yyyy"),
                    ["currentTime"] = DateTime.Now.ToString("HH:mm")
                };

                var (subject, body) = await templateService.GetProcessedEmailContentAsync(
                    customer.WorkspaceId,
                    TemplateType.DeliveryCompleted,
                    templateData);

                if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(body))
                {
                    await SendEmailToMultipleRecipientsAsync(recipients, subject, body);
                    _logger.LogInformation($"Delivery completed notification sent to {recipients.Count} contacts for customer {customerId} using template");
                    return;
                }
            }

            // Fallback
            var fallbackSubject = $"✅ Teslimatınız Tamamlandı - YolPilot";
            var receiverInfo = !string.IsNullOrEmpty(receiverName) ? $"<p><strong>📝 Teslim Alan:</strong> {receiverName}</p>" : "";
            var notesInfo = !string.IsNullOrEmpty(notes) ? $"<p><strong>📋 Notlar:</strong> {notes}</p>" : "";

            var fallbackHtml = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .success-box {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Teslimatınız Tamamlandı!</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>

            <div class='success-box'>
                <p><strong>🎉 Teslimatınız başarıyla tamamlandı!</strong></p>
                {receiverInfo}
                {notesInfo}
                <p><strong>📅 Teslim Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>

            <p>Hizmetimizi tercih ettiğiniz için teşekkür ederiz.</p>

            <p>İyi günler dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, fallbackSubject, fallbackHtml);
            _logger.LogInformation($"Delivery completed notification sent to {recipients.Count} contacts for customer {customerId} using fallback");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending delivery completed notification to customer contacts {customerId}");
        }
    }

    public async System.Threading.Tasks.Task SendDeliveryFailedToCustomerContactsAsync(int customerId, string customerName, string failureReason, string notes, string trackingUrl)
    {
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.DeliveryFailed);
            
            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for delivery failed notification");
                return;
            }

            var turkishReason = TranslateFailureReason(failureReason);
            var subject = $"❌ Teslimat Gerçekleştirilemedi - YolPilot";
            var notesInfo = !string.IsNullOrEmpty(notes) ? $"<p><strong>📋 Ek Notlar:</strong> {notes}</p>" : "";
            
            var htmlContent = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .failure-box {{ background: #f8d7da; border: 1px solid #f5c6cb; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>❌ Teslimat Gerçekleştirilemedi</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='failure-box'>
                <p><strong>📋 Teslim Edilememe Nedeni:</strong></p>
                <p>{turkishReason}</p>
                {notesInfo}
                <p><strong>📅 Deneme Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>

            <p>En kısa sürede sizinle iletişime geçeceğiz.</p>
            
            <p>Özür dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, subject, htmlContent);
            _logger.LogInformation($"Delivery failed notification sent to {recipients.Count} contacts for customer {customerId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending delivery failed notification to customer contacts {customerId}");
        }
    }

    public async System.Threading.Tasks.Task SendJourneyAssignedToCustomerContactsAsync(int customerId, string customerName, int journeyId, DateTime date)
    {
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.JourneyStart);
            
            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for journey assigned notification");
                return;
            }

            var subject = $"📋 Teslimat Planlandı - YolPilot";
            var htmlContent = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #6f42c1 0%, #e83e8c 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .info-box {{ background: #e2e3f1; border: 1px solid #c7c8d6; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 Teslimat Planlandı</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='info-box'>
                <p><strong>📦 Teslimatınız planlandı!</strong></p>
                <p><strong>📅 Tarih:</strong> {date:dd.MM.yyyy}</p>
                <p><strong>🔢 Seyahat No:</strong> #{journeyId}</p>
            </div>

            <p>Teslimat başladığında bilgilendirileceksiniz.</p>
            
            <p>İyi günler dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, subject, htmlContent);
            _logger.LogInformation($"Journey assigned notification sent to {recipients.Count} contacts for customer {customerId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending journey assigned notification to customer contacts {customerId}");
        }
    }

    public async System.Threading.Tasks.Task SendJourneyCancelledToCustomerContactsAsync(int customerId, string customerName, int journeyId, string reason)
    {
        try
        {
            var recipients = await GetCustomerContactEmails(customerId, NotificationRoleMapping.NotificationTypes.DeliveryFailed);
            
            if (!recipients.Any())
            {
                _logger.LogWarning($"No contacts found for customer {customerId} for journey cancelled notification");
                return;
            }

            var subject = $"🚫 Teslimat İptal Edildi - YolPilot";
            var reasonInfo = !string.IsNullOrEmpty(reason) ? $"<p><strong>📋 İptal Nedeni:</strong> {reason}</p>" : "";
            
            var htmlContent = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 40px; text-align: center; }}
        .content {{ padding: 30px; }}
        .cancel-box {{ background: #f8d7da; border: 1px solid #f5c6cb; padding: 20px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚫 Teslimat İptal Edildi</h1>
        </div>
        <div class='content'>
            <p>Sayın <strong>{customerName}</strong>,</p>
            
            <div class='cancel-box'>
                <p><strong>📦 Teslimatınız iptal edildi</strong></p>
                <p><strong>🔢 Seyahat No:</strong> #{journeyId}</p>
                {reasonInfo}
                <p><strong>📅 İptal Tarihi:</strong> {DateTime.Now:dd.MM.yyyy HH:mm}</p>
            </div>

            <p>Yeni bir teslimat tarih için sizinle iletişime geçeceğiz.</p>
            
            <p>Özür dileriz,<br/>YolPilot Ekibi</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailToMultipleRecipientsAsync(recipients, subject, htmlContent);
            _logger.LogInformation($"Journey cancelled notification sent to {recipients.Count} contacts for customer {customerId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending journey cancelled notification to customer contacts {customerId}");
        }
    }

    private async Task<List<string>> GetCustomerContactEmails(int customerId, string notificationType)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Müşteri workspace'ini al
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
            {
                _logger.LogWarning($"Customer not found: {customerId}");
                return new List<string>();
            }

            _logger.LogInformation($"Processing email notifications for customer {customerId} ({customer.Name}) in workspace {customer.WorkspaceId} for notification type: {notificationType}");

            // Debug: Bu workspace'deki tüm NotificationRoleMapping'leri kontrol et
            var allMappings = await context.NotificationRoleMappings
                .Where(nrm => nrm.WorkspaceId == customer.WorkspaceId)
                .ToListAsync();

            _logger.LogInformation($"DEBUG: Total NotificationRoleMappings in workspace {customer.WorkspaceId}: {allMappings.Count}");
            foreach (var mapping in allMappings)
            {
                _logger.LogInformation($"DEBUG: Mapping - NotificationType: {mapping.NotificationType}, ContactRole: {mapping.ContactRole}, IsEnabled: {mapping.IsEnabled}");
            }

            // Bu workspace için bildirim rol eşleştirmelerini al
            var enabledRoles = await context.NotificationRoleMappings
                .Where(nrm => nrm.WorkspaceId == customer.WorkspaceId &&
                            nrm.NotificationType == notificationType &&
                            nrm.IsEnabled)
                .Select(nrm => nrm.ContactRole)
                .ToListAsync();

            if (!enabledRoles.Any())
            {
                _logger.LogWarning($"No enabled roles found for notification type {notificationType} in workspace {customer.WorkspaceId}");
                return new List<string>();
            }

            _logger.LogInformation($"Found {enabledRoles.Count} enabled roles for notification type {notificationType}: {string.Join(", ", enabledRoles)}");

            // Debug: Bu müşteriye ait tüm contact'ları kontrol et
            var allContacts = await context.CustomerContacts
                .Where(cc => cc.CustomerId == customerId)
                .ToListAsync();

            _logger.LogInformation($"DEBUG: Total CustomerContacts for customer {customerId}: {allContacts.Count}");
            foreach (var contact in allContacts)
            {
                _logger.LogInformation($"DEBUG: Contact - Name: {contact.FirstName} {contact.LastName}, Email: {contact.Email}, Role: {contact.Role}, IsActive: {contact.IsActive}");
            }

            // Bu rollerdeki aktif kişilerin email adreslerini al
            var contactEmails = await context.CustomerContacts
                .Where(cc => cc.CustomerId == customerId &&
                           cc.IsActive &&
                           enabledRoles.Contains(cc.Role))
                .Where(cc => !string.IsNullOrWhiteSpace(cc.Email))
                .Select(cc => cc.Email.Trim())
                .Distinct()
                .ToListAsync();

            _logger.LogInformation($"Found {contactEmails.Count} contact emails for customer {customerId}: {string.Join(", ", contactEmails)}");

            _logger.LogInformation($"Final email list for customer {customerId}: {contactEmails.Count} recipients - {string.Join(", ", contactEmails)}");
            return contactEmails.Where(email => !string.IsNullOrWhiteSpace(email)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting customer contact emails for customer {customerId}, notification type {notificationType}");
            return new List<string>();
        }
    }

    private string ExtractTextFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Basit HTML'den text çıkarma
        // HTML etiketlerini kaldır
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);

        // HTML entity'lerini çöz
        text = System.Net.WebUtility.HtmlDecode(text);

        // Fazla boşlukları temizle
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    /// <summary>
    /// Email içeriğine YolPilot markasını otomatik ekler
    /// </summary>
    private string AddYolPilotBranding(string htmlContent)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return htmlContent;

        // YolPilot markası footer'ı
        var yolpilotBranding = @"
<div style='margin-top: 20px; padding-top: 15px; border-top: 2px solid #e0e0e0; text-align: center; font-size: 12px; color: #999;'>
    <p><strong>YolPilot</strong> ile güvenle teslimat 🚀</p>
</div>";

        // </body> tag'inden önce ekle
        if (htmlContent.Contains("</body>"))
        {
            htmlContent = htmlContent.Replace("</body>", yolpilotBranding + "\n</body>");
        }
        // Eğer </body> yoksa HTML'in sonuna ekle
        else if (htmlContent.Contains("</html>"))
        {
            htmlContent = htmlContent.Replace("</html>", yolpilotBranding + "\n</html>");
        }
        // Hiçbiri yoksa sonuna ekle
        else
        {
            htmlContent += yolpilotBranding;
        }

        return htmlContent;
    }

    /// <summary>
    /// Araç bakım hatırlatma maili gönder
    /// </summary>
    public async System.Threading.Tasks.Task SendMaintenanceReminderEmail(
        string to,
        string recipientName,
        string vehiclePlate,
        string vehicleBrand,
        string maintenanceType,
        DateTime maintenanceDate,
        int daysRemaining)
    {
        try
        {
            var frontendUrl = _configuration["Urls:Frontend"] ?? "https://app.yolpilot.com";

            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .content {{
            padding: 30px 25px;
            background: #ffffff;
        }}
        .info-box {{
            background: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
        }}
        .info-box h3 {{
            margin: 0 0 15px 0;
            color: #92400e;
            font-size: 18px;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #fde68a;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: 600;
            color: #78350f;
        }}
        .info-value {{
            color: #92400e;
        }}
        .btn {{
            display: inline-block;
            padding: 14px 28px;
            background: #2563eb;
            color: white;
            text-decoration: none;
            border-radius: 8px;
            margin-top: 20px;
            font-weight: 600;
            transition: background 0.3s;
        }}
        .btn:hover {{
            background: #1d4ed8;
        }}
        .footer {{
            text-align: center;
            padding: 25px;
            background: #f9fafb;
            color: #6b7280;
            font-size: 13px;
            border-top: 1px solid #e5e7eb;
        }}
        .divider {{
            height: 1px;
            background: #e5e7eb;
            margin: 20px 0;
        }}
        .warning {{
            color: #dc2626;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>🔧</div>
            <h1>Araç Bakım Hatırlatması</h1>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{recipientName}</strong>,</p>
            <p>Aşağıdaki aracınız için bakım tarihi yaklaşmaktadır:</p>

            <div class='info-box'>
                <h3>📋 Bakım Detayları</h3>
                <div class='info-row'>
                    <span class='info-label'>Araç:</span>
                    <span class='info-value'>{vehicleBrand}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Plaka:</span>
                    <span class='info-value'>{vehiclePlate}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Bakım Türü:</span>
                    <span class='info-value'>{maintenanceType}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Bakım Tarihi:</span>
                    <span class='info-value'>{maintenanceDate:dd.MM.yyyy}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Kalan Süre:</span>
                    <span class='info-value warning'>{daysRemaining} gün</span>
                </div>
            </div>

            <div class='divider'></div>

            <p><strong>Önemli:</strong> Lütfen bakım işlemlerini zamanında yaptırmayı unutmayın. Düzenli bakım, aracınızın performansını ve güvenliğini korumak için kritik öneme sahiptir.</p>

            <center>
                <a href='{frontendUrl}/vehicles' class='btn'>Araçları Görüntüle</a>
            </center>
        </div>
        <div class='footer'>
            <p><strong>YolPilot</strong> - Rota Optimizasyon ve Filo Yönetimi</p>
            <p>© 2024 YolPilot - Tüm hakları saklıdır</p>
            <p style='margin-top: 10px; font-size: 11px;'>
                Bu otomatik bir hatırlatma mesajıdır. Lütfen yanıtlamayın.
            </p>
        </div>
    </div>
</body>
</html>";

            var sendSmtpEmail = new SendSmtpEmail
            {
                To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to, recipientName) },
                Sender = new SendSmtpEmailSender("YolPilot", "noreply@yolpilot.com"),
                Subject = $"🔧 Araç Bakım Hatırlatması: {vehiclePlate}",
                HtmlContent = htmlContent,
                TextContent = $@"
Araç Bakım Hatırlatması

Merhaba {recipientName},

Aşağıdaki aracınız için bakım tarihi yaklaşmaktadır:

📋 Bakım Detayları:
- Araç: {vehicleBrand}
- Plaka: {vehiclePlate}
- Bakım Türü: {maintenanceType}
- Bakım Tarihi: {maintenanceDate:dd.MM.yyyy}
- Kalan Süre: {daysRemaining} gün

Lütfen bakım işlemlerini zamanında yaptırmayı unutmayın.

Araçları görüntülemek için: {frontendUrl}/vehicles

© 2024 YolPilot - Bu otomatik bir hatırlatma mesajıdır.
"
            };

            var response = await _emailApi.SendTransacEmailAsync(sendSmtpEmail);
            _logger.LogInformation($"Maintenance reminder email sent successfully to {to} for vehicle {vehiclePlate}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send maintenance reminder email to {to} for vehicle {vehiclePlate}");
            throw;
        }
    }

    /// <summary>
    /// Gecikmiş sefer raporu email'i gönderir
    /// </summary>
    public async System.Threading.Tasks.Task SendJourneyDelayReportAsync(
        List<string> recipients,
        int journeyId,
        string journeyName,
        string driverName,
        DateTime plannedCompletionTime,
        DateTime actualCompletionTime,
        TimeSpan delayDuration,
        string workspaceName,
        decimal? plannedDistanceKm = null,
        decimal? actualDistanceKm = null)
    {
        if (recipients == null || !recipients.Any())
        {
            _logger.LogWarning("No recipients provided for journey delay report");
            return;
        }

        try
        {
            var delayHours = Math.Floor(delayDuration.TotalHours);
            var delayMinutes = delayDuration.Minutes;
            var delayText = delayHours > 0
                ? $"{delayHours} saat {delayMinutes} dakika"
                : $"{delayMinutes} dakika";

            // Kilometre farkını hesapla
            string distanceSection = "";
            if (plannedDistanceKm.HasValue && actualDistanceKm.HasValue)
            {
                var distanceDiff = actualDistanceKm.Value - plannedDistanceKm.Value;
                var distancePercentage = plannedDistanceKm.Value > 0
                    ? (distanceDiff / plannedDistanceKm.Value) * 100
                    : 0;

                var diffColor = distanceDiff > 0 ? "#dc2626" : (distanceDiff < 0 ? "#16a34a" : "#6b7280");
                var diffIcon = distanceDiff > 0 ? "⬆️" : (distanceDiff < 0 ? "⬇️" : "➡️");
                var diffText = distanceDiff > 0
                    ? $"+{distanceDiff:F1} km fazla"
                    : (distanceDiff < 0 ? $"{Math.Abs(distanceDiff):F1} km az" : "Plana uygun");

                distanceSection = $@"
            <div class='info-box'>
                <h3 style='margin-top: 0;'>🛣️ Mesafe Bilgileri</h3>
                <div class='info-row'>
                    <span class='info-label'>Planlanan Mesafe:</span>
                    <span class='info-value'>{plannedDistanceKm.Value:F1} km</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Gerçekleşen Mesafe:</span>
                    <span class='info-value'>{actualDistanceKm.Value:F1} km</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Fark:</span>
                    <span class='info-value' style='color: {diffColor}; font-weight: bold;'>{diffIcon} {diffText} ({distancePercentage:+0.0;-0.0;0}%)</span>
                </div>
            </div>";
            }

            var frontendUrl = _configuration["Frontend:Url"] ?? "https://app.yolpilot.com";

            var htmlContent = $@"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #dc2626 0%, #991b1b 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }}
        .header .icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 600;
        }}
        .content {{
            padding: 30px;
        }}
        .alert-box {{
            background: #fef2f2;
            border-left: 4px solid #dc2626;
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .delay-info {{
            font-size: 32px;
            font-weight: bold;
            color: #dc2626;
            text-align: center;
            margin: 15px 0;
        }}
        .info-box {{
            background: #f9fafb;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            padding: 20px;
            margin: 20px 0;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .info-label {{
            font-weight: 600;
            color: #6b7280;
        }}
        .info-value {{
            color: #111827;
            text-align: right;
        }}
        .btn {{
            display: inline-block;
            padding: 14px 32px;
            background: #dc2626;
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 500;
            margin-top: 20px;
        }}
        .btn:hover {{
            background: #b91c1c;
        }}
        .footer {{
            background: #f9fafb;
            padding: 20px 30px;
            text-align: center;
            color: #6b7280;
            font-size: 14px;
            border-top: 1px solid #e5e7eb;
        }}
        .divider {{
            height: 1px;
            background: #e5e7eb;
            margin: 20px 0;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='icon'>⚠️</div>
            <h1>Sefer Gecikme Raporu</h1>
        </div>
        <div class='content'>
            <div class='alert-box'>
                <h3 style='margin-top: 0; color: #dc2626;'>🚨 Sefer Gecikme Bildirimi</h3>
                <p style='margin: 0;'>Aşağıdaki sefer planlanan tamamlanma saatinden geç tamamlanmıştır.</p>
            </div>

            <div class='delay-info'>
                {delayText} GECİKME
            </div>

            <div class='info-box'>
                <h3 style='margin-top: 0;'>📋 Sefer Detayları</h3>
                <div class='info-row'>
                    <span class='info-label'>Sefer ID:</span>
                    <span class='info-value'>#{journeyId}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Sefer Adı:</span>
                    <span class='info-value'>{journeyName ?? "İsimsiz Sefer"}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Sürücü:</span>
                    <span class='info-value'>{driverName}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Workspace:</span>
                    <span class='info-value'>{workspaceName}</span>
                </div>
            </div>

            <div class='info-box'>
                <h3 style='margin-top: 0;'>⏱️ Zaman Bilgileri</h3>
                <div class='info-row'>
                    <span class='info-label'>Planlanan Tamamlanma:</span>
                    <span class='info-value'>{plannedCompletionTime:dd.MM.yyyy HH:mm}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Gerçekleşen Tamamlanma:</span>
                    <span class='info-value'>{actualCompletionTime:dd.MM.yyyy HH:mm}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Gecikme Süresi:</span>
                    <span class='info-value' style='color: #dc2626; font-weight: bold;'>{delayText}</span>
                </div>
            </div>

            {distanceSection}

            <div class='divider'></div>

            <p><strong>Öneriler:</strong></p>
            <ul style='color: #6b7280;'>
                <li>Sürücü ile iletişime geçerek gecikme nedenini araştırın</li>
                <li>Gelecekte benzer gecikmeleri önlemek için rota planlamasını gözden geçirin</li>
                <li>Gerekirse müşterilerle iletişime geçin ve durumu açıklayın</li>
            </ul>

            <center>
                <a href='{frontendUrl}/journeys/{journeyId}' class='btn'>Sefer Detaylarını Görüntüle</a>
            </center>
        </div>
        <div class='footer'>
            <p><strong>YolPilot</strong> - Rota Optimizasyon ve Filo Yönetimi</p>
            <p>© 2024 YolPilot - Tüm hakları saklıdır</p>
            <p style='margin-top: 10px; font-size: 11px;'>
                Bu otomatik bir gecikme bildirimidir. Ayarlarınızı kontrol panelinden değiştirebilirsiniz.
            </p>
        </div>
    </div>
</body>
</html>";

            var subject = $"⚠️ Sefer Gecikme Raporu: #{journeyId} - {delayText} Gecikme";

            await SendEmailToMultipleRecipientsAsync(recipients, subject, htmlContent);

            _logger.LogInformation($"Journey delay report sent successfully for Journey #{journeyId} to {recipients.Count} recipient(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send journey delay report for Journey #{journeyId}");
            // Don't throw - gecikme raporu gönderilemese bile journey tamamlanmalı
        }
    }
}

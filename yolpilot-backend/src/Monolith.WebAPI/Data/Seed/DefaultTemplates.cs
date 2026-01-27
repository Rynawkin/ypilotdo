using Monolith.WebAPI.Data.Workspace;

namespace Monolith.WebAPI.Data.Seed;

public static class DefaultTemplates
{
    public static List<MessageTemplate> GetDefaultTemplates(int workspaceId)
    {
        return new List<MessageTemplate>
        {
            // WELCOME EMAIL TEMPLATES - Mevcut EmailService'den
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "WelcomeEmail",
                Channel = "Email",
                Name = "Varsayılan Hoş Geldin E-postası",
                Subject = "YolPilot'a Hoş Geldiniz - Hesabınız Hazır!",
                Body = @"<!DOCTYPE html>
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
            <p>Merhaba <strong>{{user.fullName}}</strong>,</p>
            <p>YolPilot ailesine hoş geldiniz! <strong>{{workspace.name}}</strong> çalışma alanınız başarıyla oluşturuldu ve kullanıma hazır.</p>
            
            <div class=""welcome-box"">
                <h3 style=""margin-top: 0; color: #667eea;"">🚀 Hesap Bilgileriniz</h3>
                <p><strong>Çalışma Alanı:</strong> {{workspace.name}} <span class=""success-badge"">Aktif</span></p>
                <p><strong>E-posta Adresiniz:</strong> {{user.email}}</p>
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
                    <strong>📬 Otomatik Bildirimler:</strong> Müşterilerinize WhatsApp ve e-posta bildirimleri gönderin
                </div>
            </div>
            
            <center>
                <a href=""{{loginUrl}}"" class=""button"">📱 Kontrol Paneline Git</a>
            </center>
            
            <div class=""footer"">
                <p><strong>Sorularınız mı var?</strong></p>
                <p>Destek ekibimiz size yardımcı olmak için hazır!</p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
                <p style=""margin-top: 20px; font-size: 12px;"">
                    © {{currentDate}} YolPilot. Tüm hakları saklıdır.
                </p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // JOURNEY START - Email
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "JourneyStart",
                Channel = "Email",
                Name = "Varsayılan Sefer Başladı E-postası",
                Subject = "{{workspace.name}} - Sipariş Takip Bildirimi",
                Body = @"<!DOCTYPE html>
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
        .info-row { padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{{workspace.name}}</h1>
            <p style=""margin: 10px 0 0 0; font-size: 18px;"">Sipariş Takip Bildirimi</p>
        </div>
        <div class=""content"">
            <h2 style=""color: #28a745; margin-top: 0;"">🚚 Siparişiniz Yola Çıktı!</h2>

            <p>{{workspace.name}}'den vermiş olduğunuz sipariş teslimat için yola çıktı.</p>

            <div class=""tracking-box"">
                <h3>⏰ Tahmini Varış Zamanı</h3>
                <p class=""time-display"">{{estimatedCompletionTime}}</p>
            </div>

            <div style=""margin-top: 20px;"">
                <div class=""info-row"">
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                <div class=""info-row"">
                    <strong>🚛 Teslimat Görevlisi:</strong> {{driver.name}}
                </div>
            </div>

            <p style=""margin-top: 20px;"">Teslimatınız yaklaştığında tekrar bilgilendirileceksiniz.</p>

            <div class=""footer"">
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // JOURNEY START - WhatsApp
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "JourneyStart",
                Channel = "WhatsApp",
                Name = "Varsayılan Sefer Başladı WhatsApp",
                Body = @"{{workspace.name}} - Sipariş Takip Bildirimi

🚚 Siparişiniz Yola Çıktı!

{{workspace.name}}'den vermiş olduğunuz sipariş teslimat için yola çıktı.

📍 Teslimat: {{customer.name}}
🚛 Görevli: {{driver.name}}
⏰ Tahmini varış: {{estimatedCompletionTime}}

Teslimatınız yaklaştığında tekrar bilgilendirileceksiniz.",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // CHECK-IN - Email
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "CheckIn",
                Channel = "Email",
                Name = "Varsayılan Teslimat Yaklaşıyor E-postası",
                Subject = "{{workspace.name}} - Teslimat Yaklaşıyor",
                Body = @"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ffc107; color: #333; padding: 40px; text-align: center; }
        .content { background: #fff; padding: 30px; }
        .alert-box { background: #fff3cd; border: 2px solid #ffc107; padding: 20px; text-align: center; border-radius: 8px; }
        .time-range { font-size: 24px; font-weight: bold; color: #856404; }
        .info-row { padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{workspace.name}}</h1>
            <p style='margin: 10px 0 0 0; font-size: 18px;'>Teslimat Yaklaşıyor</p>
        </div>
        <div class='content'>
            <h2 style='color: #ffc107; margin-top: 0;'>⏰ Sürücümüz Yolda!</h2>

            <div class='alert-box'>
                <h3 style='margin-top: 0;'>Tahmini Varış Süresi</h3>
                <p class='time-range'>{{stop.estimatedArrivalTime}}</p>
            </div>

            <div style='margin-top: 20px;'>
                <div class='info-row'>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                <div class='info-row'>
                    <strong>🚛 Teslimat Görevlisi:</strong> {{driver.name}}
                </div>
                <div class='info-row'>
                    <strong>📞 Telefon:</strong> {{driver.phoneNumber}}
                </div>
            </div>

            <p style='margin-top: 20px;'>Lütfen teslimat adresinde bulunmaya özen gösterin.</p>

            <div class='footer'>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // CHECK-IN - WhatsApp
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "CheckIn",
                Channel = "WhatsApp",
                Name = "Varsayılan Teslimat Yaklaşıyor WhatsApp",
                Body = @"{{workspace.name}} - Teslimat Yaklaşıyor

⏰ Sürücümüz Yolda!

Sürücümüz {{stop.estimatedArrivalTime}} arasında teslimat adresinde olacak.

📍 Teslimat: {{customer.name}}
👤 Görevli: {{driver.name}}
📞 Telefon: {{driver.phoneNumber}}

Lütfen teslimat adresinde bulunmaya özen gösterin.",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // DELIVERY COMPLETED - Email - RECEİVERNAME EKLENDİ
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "DeliveryCompleted",
                Channel = "Email",
                Name = "Varsayılan Teslimat Tamamlandı E-postası",
                Subject = "{{workspace.name}} - Teslimat Tamamlandı",
                Body = @"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; }
        .header { background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 40px; text-align: center; }
        .content { padding: 30px; }
        .success-message { background: #d4edda; border: 1px solid #c3e6cb; padding: 20px; text-align: center; border-radius: 8px; }
        .info-box { background: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 8px; }
        .info-row { padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
        .button { display: inline-block; padding: 14px 32px; background: #28a745; color: white; text-decoration: none; border-radius: 6px; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{workspace.name}}</h1>
            <p style='margin: 10px 0 0 0; font-size: 18px;'>Teslimat Tamamlandı</p>
        </div>
        <div class='content'>
            <div class='success-message'>
                <h2 style='margin: 0; color: #28a745;'>✅ Teslimat Başarıyla Tamamlandı!</h2>
            </div>

            <p style='margin-top: 20px;'>{{workspace.name}}'den vermiş olduğunuz sipariş başarıyla teslim edilmiştir.</p>

            <div style='margin-top: 20px;'>
                <div class='info-row'>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                {{#if receiverName}}
                <div class='info-row'>
                    <strong>👤 Teslim Alan:</strong> {{receiverName}}
                </div>
                {{/if}}
                <div class='info-row'>
                    <strong>⏰ Teslim Zamanı:</strong> {{completedTime}}
                </div>
            </div>

            {{#if stop.notes}}
            <div class='info-box'>
                <p style='margin: 0;'><strong>📝 Teslimat Notları:</strong></p>
                <p style='margin: 10px 0 0 0;'>{{stop.notes}}</p>
            </div>
            {{/if}}

            <center style='margin-top: 30px;'>
                <a href='{{trackingUrl}}' class='button'>✨ Teslimat Kanıtlarını Görüntüle</a>
            </center>

            <p style='margin-top: 20px; text-align: center;'>
                <a href='{{feedbackUrl}}' style='color: #28a745; text-decoration: none;'>⭐ Deneyiminizi Değerlendirin</a>
            </p>

            <p style='text-align: center; margin-top: 20px;'>Bizi tercih ettiğiniz için teşekkür ederiz.</p>

            <div class='footer'>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // DELIVERY COMPLETED - WhatsApp - RECEİVERNAME EKLENDİ
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "DeliveryCompleted",
                Channel = "WhatsApp",
                Name = "Varsayılan Teslimat Tamamlandı WhatsApp",
                Body = @"{{workspace.name}} - Teslimat Tamamlandı

✅ Teslimat Başarıyla Tamamlandı!

{{workspace.name}}'den vermiş olduğunuz sipariş başarıyla teslim edilmiştir.

📍 Teslimat: {{customer.name}}
{{#if receiverName}}
👤 Teslim Alan: {{receiverName}}
{{/if}}
⏰ Teslim Zamanı: {{completedTime}}

📋 Teslimat detayları için:
{{trackingUrl}}

⭐ Deneyiminizi değerlendirin:
{{feedbackUrl}}

Bizi tercih ettiğiniz için teşekkür ederiz.",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // DELIVERY FAILED - Email
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "DeliveryFailed",
                Channel = "Email",
                Name = "Varsayılan Teslimat Başarısız E-postası",
                Subject = "{{workspace.name}} - Teslimat Gerçekleştirilemedi",
                Body = @"<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; }
        .header { background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); color: white; padding: 40px; text-align: center; }
        .content { padding: 30px; }
        .failure-message { background: #f8d7da; border: 1px solid #f5c6cb; padding: 20px; border-radius: 8px; }
        .reason-box { background: #fff; border: 2px solid #dc3545; padding: 15px; margin: 20px 0; border-radius: 8px; }
        .info-row { padding: 10px 0; border-bottom: 1px solid #f0f0f0; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #e0e0e0; text-align: center; color: #666; font-size: 14px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{{workspace.name}}</h1>
            <p style='margin: 10px 0 0 0; font-size: 18px;'>Teslimat Bildirimi</p>
        </div>
        <div class='content'>
            <div class='failure-message'>
                <h2 style='margin: 0; color: #dc3545;'>❌ Teslimat Gerçekleştirilemedi</h2>
            </div>

            <p style='margin-top: 20px;'>{{workspace.name}}'den gönderilen siparişiniz teslim edilememiştir.</p>

            <div style='margin-top: 20px;'>
                <div class='info-row'>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
            </div>

            <div class='reason-box'>
                <h3 style='margin-top: 0; color: #dc3545;'>📋 Teslim Edilememe Nedeni</h3>
                <p style='margin: 0;'>{{failureReason}}</p>
            </div>

            {{#if failureNotes}}
            <div class='reason-box'>
                <p style='margin: 0;'><strong>📝 Ek Notlar:</strong></p>
                <p style='margin: 10px 0 0 0;'>{{failureNotes}}</p>
            </div>
            {{/if}}

            <p>En geç 1 iş günü içinde sizinle iletişime geçeceğiz.</p>

            <p style='margin-top: 30px;'>
                <a href='{{rescheduleUrl}}' style='color: #dc3545; text-decoration: none;'>📅 Yeniden Planlama Talebi</a>
            </p>

            <div class='footer'>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // DELIVERY FAILED - WhatsApp
            new MessageTemplate
            {
                WorkspaceId = workspaceId,
                TemplateType = "DeliveryFailed",
                Channel = "WhatsApp",
                Name = "Varsayılan Teslimat Başarısız WhatsApp",
                Body = @"{{workspace.name}} - Teslimat Bildirimi

❌ Teslimat Gerçekleştirilemedi

{{workspace.name}}'den gönderilen siparişiniz teslim edilememiştir.

📍 Teslimat: {{customer.name}}
📋 Sebep: {{failureReason}}
{{#if failureNotes}}
📝 Detay: {{failureNotes}}
{{/if}}

En geç 1 iş günü içinde sizinle iletişime geçeceğiz.

Detaylar: {{trackingUrl}}",
                IsActive = true,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }
}
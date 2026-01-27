-- Mevcut workspace'lerdeki default template'leri yeni formatla güncelleme
-- (Greeting'ler kaldırıldı, workspace adı üstte, customer.name teslimat adresi olarak)

-- JOURNEY START - Email
UPDATE MessageTemplates
SET
    Subject = N'{{workspace.name}} - Sipariş Takip Bildirimi',
    Body = N'<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
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
    <div class="container">
        <div class="header">
            <h1>{{workspace.name}}</h1>
            <p style="margin: 10px 0 0 0; font-size: 18px;">Sipariş Takip Bildirimi</p>
        </div>
        <div class="content">
            <h2 style="color: #28a745; margin-top: 0;">🚚 Siparişiniz Yola Çıktı!</h2>

            <p>{{workspace.name}}''den vermiş olduğunuz sipariş teslimat için yola çıktı.</p>

            <div class="tracking-box">
                <h3>⏰ Tahmini Varış Zamanı</h3>
                <p class="time-display">{{estimatedCompletionTime}}</p>
            </div>

            <div style="margin-top: 20px;">
                <div class="info-row">
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                <div class="info-row">
                    <strong>🚛 Teslimat Görevlisi:</strong> {{driver.name}}
                </div>
            </div>

            <p style="margin-top: 20px;">Teslimatınız yaklaştığında tekrar bilgilendirileceksiniz.</p>

            <div class="footer">
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'JourneyStart' AND Channel = 'Email' AND IsDefault = 1;

-- JOURNEY START - WhatsApp
UPDATE MessageTemplates
SET
    Body = N'{{workspace.name}} - Sipariş Takip Bildirimi

🚚 Siparişiniz Yola Çıktı!

{{workspace.name}}''den vermiş olduğunuz sipariş teslimat için yola çıktı.

📍 Teslimat: {{customer.name}}
🚛 Görevli: {{driver.name}}
⏰ Tahmini varış: {{estimatedCompletionTime}}

Teslimatınız yaklaştığında tekrar bilgilendirileceksiniz.',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'JourneyStart' AND Channel = 'WhatsApp' AND IsDefault = 1;

-- CHECK-IN - Email
UPDATE MessageTemplates
SET
    Subject = N'{{workspace.name}} - Teslimat Yaklaşıyor',
    Body = N'<!DOCTYPE html>
<html lang=''tr''>
<head>
    <meta charset=''UTF-8''>
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
    <div class=''container''>
        <div class=''header''>
            <h1>{{workspace.name}}</h1>
            <p style=''margin: 10px 0 0 0; font-size: 18px;''>Teslimat Yaklaşıyor</p>
        </div>
        <div class=''content''>
            <h2 style=''color: #ffc107; margin-top: 0;''>⏰ Sürücümüz Yolda!</h2>

            <div class=''alert-box''>
                <h3 style=''margin-top: 0;''>Tahmini Varış Süresi</h3>
                <p class=''time-range''>{{stop.estimatedArrivalTime}}</p>
            </div>

            <div style=''margin-top: 20px;''>
                <div class=''info-row''>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                <div class=''info-row''>
                    <strong>🚛 Teslimat Görevlisi:</strong> {{driver.name}}
                </div>
                <div class=''info-row''>
                    <strong>📞 Telefon:</strong> {{driver.phoneNumber}}
                </div>
            </div>

            <p style=''margin-top: 20px;''>Lütfen teslimat adresinde bulunmaya özen gösterin.</p>

            <div class=''footer''>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'CheckIn' AND Channel = 'Email' AND IsDefault = 1;

-- CHECK-IN - WhatsApp
UPDATE MessageTemplates
SET
    Body = N'{{workspace.name}} - Teslimat Yaklaşıyor

⏰ Sürücümüz Yolda!

Sürücümüz {{stop.estimatedArrivalTime}} arasında teslimat adresinde olacak.

📍 Teslimat: {{customer.name}}
👤 Görevli: {{driver.name}}
📞 Telefon: {{driver.phoneNumber}}

Lütfen teslimat adresinde bulunmaya özen gösterin.',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'CheckIn' AND Channel = 'WhatsApp' AND IsDefault = 1;

-- DELIVERY COMPLETED - Email
UPDATE MessageTemplates
SET
    Subject = N'{{workspace.name}} - Teslimat Tamamlandı',
    Body = N'<!DOCTYPE html>
<html lang=''tr''>
<head>
    <meta charset=''UTF-8''>
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
    <div class=''container''>
        <div class=''header''>
            <h1>{{workspace.name}}</h1>
            <p style=''margin: 10px 0 0 0; font-size: 18px;''>Teslimat Tamamlandı</p>
        </div>
        <div class=''content''>
            <div class=''success-message''>
                <h2 style=''margin: 0; color: #28a745;''>✅ Teslimat Başarıyla Tamamlandı!</h2>
            </div>

            <p style=''margin-top: 20px;''>{{workspace.name}}''den vermiş olduğunuz sipariş başarıyla teslim edilmiştir.</p>

            <div style=''margin-top: 20px;''>
                <div class=''info-row''>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
                {{#if receiverName}}
                <div class=''info-row''>
                    <strong>👤 Teslim Alan:</strong> {{receiverName}}
                </div>
                {{/if}}
                <div class=''info-row''>
                    <strong>⏰ Teslim Zamanı:</strong> {{completedTime}}
                </div>
            </div>

            {{#if stop.notes}}
            <div class=''info-box''>
                <p style=''margin: 0;''><strong>📝 Teslimat Notları:</strong></p>
                <p style=''margin: 10px 0 0 0;''>{{stop.notes}}</p>
            </div>
            {{/if}}

            <center style=''margin-top: 30px;''>
                <a href=''{{trackingUrl}}'' class=''button''>✨ Teslimat Kanıtlarını Görüntüle</a>
            </center>

            <p style=''margin-top: 20px; text-align: center;''>
                <a href=''{{feedbackUrl}}'' style=''color: #28a745; text-decoration: none;''>⭐ Deneyiminizi Değerlendirin</a>
            </p>

            <p style=''text-align: center; margin-top: 20px;''>Bizi tercih ettiğiniz için teşekkür ederiz.</p>

            <div class=''footer''>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'DeliveryCompleted' AND Channel = 'Email' AND IsDefault = 1;

-- DELIVERY COMPLETED - WhatsApp
UPDATE MessageTemplates
SET
    Body = N'{{workspace.name}} - Teslimat Tamamlandı

✅ Teslimat Başarıyla Tamamlandı!

{{workspace.name}}''den vermiş olduğunuz sipariş başarıyla teslim edilmiştir.

📍 Teslimat: {{customer.name}}
{{#if receiverName}}
👤 Teslim Alan: {{receiverName}}
{{/if}}
⏰ Teslim Zamanı: {{completedTime}}

📋 Teslimat detayları için:
{{trackingUrl}}

⭐ Deneyiminizi değerlendirin:
{{feedbackUrl}}

Bizi tercih ettiğiniz için teşekkür ederiz.',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'DeliveryCompleted' AND Channel = 'WhatsApp' AND IsDefault = 1;

-- DELIVERY FAILED - Email
UPDATE MessageTemplates
SET
    Subject = N'{{workspace.name}} - Teslimat Gerçekleştirilemedi',
    Body = N'<!DOCTYPE html>
<html lang=''tr''>
<head>
    <meta charset=''UTF-8''>
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
    <div class=''container''>
        <div class=''header''>
            <h1>{{workspace.name}}</h1>
            <p style=''margin: 10px 0 0 0; font-size: 18px;''>Teslimat Bildirimi</p>
        </div>
        <div class=''content''>
            <div class=''failure-message''>
                <h2 style=''margin: 0; color: #dc3545;''>❌ Teslimat Gerçekleştirilemedi</h2>
            </div>

            <p style=''margin-top: 20px;''>{{workspace.name}}''den gönderilen siparişiniz teslim edilememiştir.</p>

            <div style=''margin-top: 20px;''>
                <div class=''info-row''>
                    <strong>📍 Teslimat Adresi:</strong> {{customer.name}}
                </div>
            </div>

            <div class=''reason-box''>
                <h3 style=''margin-top: 0; color: #dc3545;''>📋 Teslim Edilememe Nedeni</h3>
                <p style=''margin: 0;''>{{failureReason}}</p>
            </div>

            {{#if failureNotes}}
            <div class=''reason-box''>
                <p style=''margin: 0;''><strong>📝 Ek Notlar:</strong></p>
                <p style=''margin: 10px 0 0 0;''>{{failureNotes}}</p>
            </div>
            {{/if}}

            <p>En geç 1 iş günü içinde sizinle iletişime geçeceğiz.</p>

            <p style=''margin-top: 30px;''>
                <a href=''{{rescheduleUrl}}'' style=''color: #dc3545; text-decoration: none;''>📅 Yeniden Planlama Talebi</a>
            </p>

            <div class=''footer''>
                <p><strong>{{workspace.name}}</strong></p>
                <p>📧 {{workspace.email}} | 📞 {{workspace.phoneNumber}}</p>
            </div>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'DeliveryFailed' AND Channel = 'Email' AND IsDefault = 1;

-- DELIVERY FAILED - WhatsApp
UPDATE MessageTemplates
SET
    Body = N'{{workspace.name}} - Teslimat Bildirimi

❌ Teslimat Gerçekleştirilemedi

{{workspace.name}}''den gönderilen siparişiniz teslim edilememiştir.

📍 Teslimat: {{customer.name}}
📋 Sebep: {{failureReason}}
{{#if failureNotes}}
📝 Detay: {{failureNotes}}
{{/if}}

En geç 1 iş günü içinde sizinle iletişime geçeceğiz.

Detaylar: {{trackingUrl}}',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'DeliveryFailed' AND Channel = 'WhatsApp' AND IsDefault = 1;

-- Güncellenen kayıt sayısını göster
SELECT
    TemplateType,
    Channel,
    COUNT(*) as UpdatedCount
FROM MessageTemplates
WHERE IsDefault = 1
GROUP BY TemplateType, Channel
ORDER BY TemplateType, Channel;

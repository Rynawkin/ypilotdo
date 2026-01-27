-- =====================================================
-- YOLPİLOT MESSAGE TEMPLATES UPDATE SCRIPT
-- Tarih: 2025-10-02
-- Açıklama: Mail ve WhatsApp şablonlarının güncellenmesi
-- =====================================================

-- =====================================================
-- 1. JOURNEY START - EMAIL GÜNCELLEME
-- =====================================================
UPDATE MessageTemplates
SET Body = '<!DOCTYPE html>
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
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🚚 Siparişiniz Yolda!</h1>
        </div>
        <div class="content">
            <p>Sayın <strong>{{customer.name}}</strong>,</p>
            <p>Güzel haber! Siparişiniz teslimat için yola çıktı.</p>

            <div class="tracking-box">
                <h3>⏰ Tahmini Varış Zamanı</h3>
                <p class="time-display">{{estimatedCompletionTime}}</p>
            </div>

            <p><strong>Teslimat Görevlisi:</strong> {{driver.name}}</p>
            <p>Teslimatınız için teşekkür ederiz, iyi günler dileriz!</p>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'JourneyStart'
  AND Channel = 'Email'
  AND IsDefault = 1;

-- =====================================================
-- 2. WELCOME EMAIL - SMS → WhatsApp DEĞİŞİKLİĞİ
-- =====================================================
UPDATE MessageTemplates
SET Body = REPLACE(Body, 'Müşterilerinize SMS ve e-posta bildirimleri gönderin', 'Müşterilerinize WhatsApp ve e-posta bildirimleri gönderin'),
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'WelcomeEmail'
  AND Channel = 'Email'
  AND IsDefault = 1
  AND Body LIKE '%SMS ve e-posta bildirimleri%';

-- =====================================================
-- 3. CHECK-IN EMAIL - ŞOFÖR BİLGİSİ EKLEME
-- =====================================================
UPDATE MessageTemplates
SET Body = '<!DOCTYPE html>
<html lang=''tr''>
<head>
    <meta charset=''UTF-8''>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ffc107; color: #333; padding: 40px; text-align: center; }
        .content { background: #fff; padding: 30px; }
        .alert-box { background: #fff3cd; border: 2px solid #ffc107; padding: 20px; text-align: center; }
        .time-range { font-size: 24px; font-weight: bold; color: #856404; }
    </style>
</head>
<body>
    <div class=''container''>
        <div class=''header''>
            <h1>⏰ Teslimatınız Yaklaşıyor!</h1>
        </div>
        <div class=''content''>
            <p>Sayın <strong>{{customer.name}}</strong>,</p>

            <div class=''alert-box''>
                <h3>🚚 Sürücümüz yolda!</h3>
                <p class=''time-range''>{{stop.estimatedArrivalTime}}</p>
            </div>

            <p><strong>Teslimat Görevlisi:</strong> {{driver.name}}</p>
            <p><strong>Telefon:</strong> {{driver.phoneNumber}}</p>

            <p>Lütfen teslimat adresinde bulunmaya özen gösterin.</p>
            <p>Anlayışınız için teşekkür ederiz.</p>
        </div>
    </div>
</body>
</html>',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'CheckIn'
  AND Channel = 'Email'
  AND IsDefault = 1;

-- =====================================================
-- 4. CHECK-IN WHATSAPP - ŞOFÖR BİLGİSİ EKLEME
-- =====================================================
UPDATE MessageTemplates
SET Body = '⏰ Sayın {{customer.name}}, teslimatınız yaklaşıyor!

🚛 Sürücümüz {{stop.estimatedArrivalTime}} arasında sizinle olacak.

👤 Teslimat Görevlisi: {{driver.name}}
📞 Telefon: {{driver.phoneNumber}}

Lütfen teslimat adresinde bulunmaya özen gösterin.

{{workspace.name}}',
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'CheckIn'
  AND Channel = 'WhatsApp'
  AND IsDefault = 1;

-- =====================================================
-- 5. DELIVERY COMPLETED EMAIL - TEŞEKKÜR METNİ EKLEME
-- =====================================================
UPDATE MessageTemplates
SET Body = REPLACE(
    Body,
    '<p style=''margin-top: 30px; text-align: center;''>
                <a href=''{{feedbackUrl}}'' style=''color: #28a745;''>⭐ Deneyiminizi Değerlendirin</a>
            </p>',
    '<p style=''margin-top: 30px; text-align: center;''>
                <a href=''{{feedbackUrl}}'' style=''color: #28a745;''>⭐ Deneyiminizi Değerlendirin</a>
            </p>

            <p style=''text-align: center; margin-top: 20px;''>Bizi tercih ettiğiniz için teşekkür ederiz.</p>'
),
    UpdatedAt = GETUTCDATE()
WHERE TemplateType = 'DeliveryCompleted'
  AND Channel = 'Email'
  AND IsDefault = 1
  AND Body NOT LIKE '%Bizi tercih ettiğiniz için teşekkür ederiz%';

-- =====================================================
-- 6. KONTROL SORGUSU - Değişiklikleri Doğrulama
-- =====================================================
SELECT
    TemplateType,
    Channel,
    Name,
    CASE
        WHEN Body LIKE '%Teslimatınız için teşekkür ederiz%' AND TemplateType = 'JourneyStart' THEN '✓ Güncellendi'
        WHEN Body LIKE '%WhatsApp ve e-posta bildirimleri%' AND TemplateType = 'WelcomeEmail' THEN '✓ Güncellendi'
        WHEN Body LIKE '%driver.phoneNumber%' AND TemplateType = 'CheckIn' THEN '✓ Güncellendi'
        WHEN Body LIKE '%Bizi tercih ettiğiniz için teşekkür ederiz%' AND TemplateType = 'DeliveryCompleted' AND Channel = 'Email' THEN '✓ Güncellendi'
        ELSE '⚠ Kontrol Edilmeli'
    END AS GuncellemeStatus,
    UpdatedAt
FROM MessageTemplates
WHERE IsDefault = 1
ORDER BY TemplateType, Channel;

-- =====================================================
-- 7. WORKSPACE BAZINDA İSTATİSTİK
-- =====================================================
SELECT
    w.Name AS WorkspaceName,
    COUNT(mt.Id) AS ToplamSablon,
    SUM(CASE WHEN mt.Channel = 'Email' THEN 1 ELSE 0 END) AS EmailSablon,
    SUM(CASE WHEN mt.Channel = 'WhatsApp' THEN 1 ELSE 0 END) AS WhatsAppSablon
FROM Workspaces w
LEFT JOIN MessageTemplates mt ON w.Id = mt.WorkspaceId
GROUP BY w.Id, w.Name
ORDER BY w.Name;

// C:\Projects\RotaAppMobile\src\screens\settings\TermsOfServiceScreen.tsx

import React from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { Card, Text, Divider } from 'react-native-paper';

const TermsOfServiceScreen: React.FC = () => {
  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Text variant="headlineSmall" style={styles.title}>
            Kullanım Koşulları
          </Text>
          <Text variant="bodySmall" style={styles.date}>
            Son Güncelleme: 1 Eylül 2025
          </Text>
          
          <Divider style={styles.divider} />
          
          <Text variant="titleMedium" style={styles.sectionTitle}>
            1. Kabul ve Onay
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot uygulamasını kullanarak bu kullanım koşullarını kabul etmiş olursunuz. 
            Bu koşulları kabul etmiyorsanız, uygulamayı kullanmamanız gerekmektedir.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            2. Hizmet Tanımı
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot, teslimat ve rota optimizasyonu hizmeti sunan bir SaaS platformudur. 
            Hizmetlerimiz:
            {'\n'}• Rota planlama ve optimizasyon
            {'\n'}• Teslimat takibi
            {'\n'}• Sürücü ve araç yönetimi
            {'\n'}• Müşteri bildirimleri
            {'\n'}• Performans raporları
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            3. Hesap Sorumluluğu
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            • Hesap bilgilerinizin gizliliğinden siz sorumlusunuz
            {'\n'}• Hesabınızda gerçekleşen tüm aktivitelerden sorumlusunuz
            {'\n'}• Yetkisiz kullanımı derhal bildirmelisiniz
            {'\n'}• 18 yaşından büyük olmalı veya yasal temsilci onayı almalısınız
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            4. Kullanım Kuralları
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Aşağıdaki davranışlar yasaktır:
            {'\n'}• Yasalara aykırı kullanım
            {'\n'}• Başkalarının haklarını ihlal eden içerik
            {'\n'}• Spam veya kötü amaçlı yazılım dağıtımı
            {'\n'}• Sistemi manipüle etme girişimleri
            {'\n'}• Sahte veya yanıltıcı bilgi girişi
            {'\n'}• Fikri mülkiyet haklarının ihlali
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            5. Ödeme ve Abonelik
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            • Abonelik ücretleri aylık olarak tahsil edilir
            {'\n'}• Plan değişiklikleri bir sonraki fatura döneminde geçerli olur
            {'\n'}• İptal durumunda dönem sonuna kadar hizmet devam eder
            {'\n'}• Limit aşımları ek ücrete tabidir
            {'\n'}• Fiyat değişiklikleri 30 gün önceden bildirilir
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            6. Fikri Mülkiyet
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot'un tüm içeriği, logosu, tasarımı ve yazılımı fikri mülkiyet 
            haklarına tabidir. İzinsiz kopyalama, dağıtma veya değiştirme yasaktır.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            7. Veri ve İçerik
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            • Yüklediğiniz veriler size aittir
            {'\n'}• Verilerinizin doğruluğundan siz sorumlusunuz
            {'\n'}• YolPilot, hizmet sunmak için verilerinizi işleme hakkına sahiptir
            {'\n'}• Yedekleme sorumluluğu size aittir
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            8. Hizmet Kesintileri
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot, bakım veya teknik sorunlar nedeniyle geçici kesintiler yaşayabilir. 
            Planlı bakımlar önceden bildirilir. %99 uptime hedefliyoruz ancak garanti edilmez.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            9. Sorumluluk Sınırı
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot, dolaylı, tesadüfi veya sonuç olarak ortaya çıkan zararlardan 
            sorumlu değildir. Maksimum sorumluluk, son ay ödenen abonelik ücreti kadardır.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            10. Hesap Sonlandırma
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            • İstediğiniz zaman hesabınızı kapatabilirsiniz
            {'\n'}• Kural ihlallerinde hesap askıya alınabilir
            {'\n'}• 90 gün pasif hesaplar silinebilir
            {'\n'}• Hesap kapatma sonrası veriler 30 gün saklanır
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            11. Değişiklikler
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Bu koşullar değiştirilebilir. Önemli değişiklikler 30 gün önceden bildirilir. 
            Kullanıma devam etmeniz, yeni koşulları kabul ettiğiniz anlamına gelir.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            12. Uygulanacak Hukuk
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Bu sözleşme Türkiye Cumhuriyeti kanunlarına tabidir. Uyuşmazlıklarda 
            İstanbul Mahkemeleri ve İcra Daireleri yetkilidir.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            13. İletişim
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Sorularınız için bizimle iletişime geçebilirsiniz:
            {'\n'}E-posta: info@yolpilot.com
            {'\n'}WhatsApp: 0530 178 35 70
          </Text>
        </Card.Content>
      </Card>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  card: {
    margin: 10,
  },
  title: {
    fontWeight: 'bold',
    marginBottom: 5,
  },
  date: {
    color: '#666',
    marginBottom: 10,
  },
  divider: {
    marginVertical: 15,
  },
  sectionTitle: {
    fontWeight: 'bold',
    marginTop: 20,
    marginBottom: 10,
    color: '#3B82F6',
  },
  paragraph: {
    lineHeight: 22,
    color: '#333',
  },
});

export default TermsOfServiceScreen;
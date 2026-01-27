// C:\Projects\RotaAppMobile\src\screens\settings\PrivacyPolicyScreen.tsx

import React from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { Card, Text, Divider } from 'react-native-paper';

const PrivacyPolicyScreen: React.FC = () => {
  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Text variant="headlineSmall" style={styles.title}>
            Gizlilik Politikası
          </Text>
          <Text variant="bodySmall" style={styles.date}>
            Son Güncelleme: 1 Eylül 2025
          </Text>
          
          <Divider style={styles.divider} />
          
          <Text variant="titleMedium" style={styles.sectionTitle}>
            1. Toplanan Bilgiler
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            YolPilot uygulaması, hizmetlerimizi sunabilmek için aşağıdaki bilgileri toplar:
            {'\n'}• Ad, soyad ve e-posta adresi
            {'\n'}• Telefon numarası (isteğe bağlı)
            {'\n'}• Konum bilgileri (teslimat takibi için)
            {'\n'}• Teslimat adresleri ve müşteri bilgileri
            {'\n'}• Araç ve sürücü bilgileri
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            2. Bilgilerin Kullanımı
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Topladığımız bilgiler şu amaçlarla kullanılır:
            {'\n'}• Rota optimizasyonu ve teslimat yönetimi
            {'\n'}• Müşteri bildirimleri ve iletişim
            {'\n'}• Performans raporları ve analizler
            {'\n'}• Hizmet kalitesinin iyileştirilmesi
            {'\n'}• Yasal yükümlülüklerin yerine getirilmesi
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            3. Bilgi Güvenliği
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Verileriniz, endüstri standardı güvenlik önlemleri ile korunmaktadır:
            {'\n'}• SSL/TLS şifreleme
            {'\n'}• Güvenli bulut altyapısı (Microsoft Azure)
            {'\n'}• Düzenli güvenlik güncellemeleri
            {'\n'}• Erişim kontrolü ve yetkilendirme
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            4. Konum Verileri
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Uygulama, teslimat takibi için konum verilerinizi kullanır. Konum izni:
            {'\n'}• Sadece çalışma saatleri içinde aktiftir
            {'\n'}• İstediğiniz zaman devre dışı bırakılabilir
            {'\n'}• Sadece teslimat optimizasyonu için kullanılır
            {'\n'}• Üçüncü taraflarla paylaşılmaz
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            5. Veri Paylaşımı
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Verileriniz aşağıdaki durumlar dışında üçüncü taraflarla paylaşılmaz:
            {'\n'}• Yasal zorunluluklar
            {'\n'}• Açık izniniz olması durumunda
            {'\n'}• Anonim ve toplu istatistikler
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            6. Çerezler
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Uygulama, oturum yönetimi ve tercihlerinizi hatırlamak için çerezler kullanır. 
            Bu çerezler cihazınızda yerel olarak saklanır ve kişisel bilgi içermez.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            7. Veri Saklama
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Verileriniz, hesabınız aktif olduğu sürece saklanır. Hesap kapatma talebinde 
            bulunduğunuzda, yasal saklama süreleri hariç tüm verileriniz 30 gün içinde silinir.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            8. KVKK Hakları
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            6698 sayılı KVKK kapsamında aşağıdaki haklara sahipsiniz:
            {'\n'}• Kişisel verilerinizin işlenip işlenmediğini öğrenme
            {'\n'}• İşlenen veriler hakkında bilgi talep etme
            {'\n'}• Verilerin işlenme amacını öğrenme
            {'\n'}• Verilerin düzeltilmesini veya silinmesini isteme
            {'\n'}• Verilerin aktarıldığı üçüncü kişileri bilme
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            9. Çocukların Gizliliği
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Hizmetlerimiz 18 yaş altı bireylere yönelik değildir. 18 yaş altı bireylerden 
            bilerek kişisel veri toplamayız.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            10. Değişiklikler
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Bu gizlilik politikası zaman zaman güncellenebilir. Önemli değişiklikler 
            e-posta ile bildirilecektir.
          </Text>

          <Text variant="titleMedium" style={styles.sectionTitle}>
            11. İletişim
          </Text>
          <Text variant="bodyMedium" style={styles.paragraph}>
            Gizlilik politikamız hakkında sorularınız için:
            {'\n'}E-posta: info@yolpilot.com
            {'\n'}WhatsApp: 0530 178 35 70
            {'\n'}Web: https://app.yolpilot.com/privacy
          </Text>
          
          <Text variant="bodySmall" style={[styles.paragraph, { fontStyle: 'italic', marginTop: 10 }]}>
            Bu gizlilik politikasının güncel versiyonu https://app.yolpilot.com/privacy 
            adresinde de mevcuttur.
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

export default PrivacyPolicyScreen;
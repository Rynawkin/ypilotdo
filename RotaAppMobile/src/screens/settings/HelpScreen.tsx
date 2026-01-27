// C:\Projects\RotaAppMobile\src\screens\settings\HelpScreen.tsx

import React, { useState } from 'react';
import { View, ScrollView, StyleSheet, Linking, Alert } from 'react-native';
import { 
  List, 
  Text, 
  Card,
  Searchbar,
  FAB,
  Divider,
  Chip
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';

const HelpScreen: React.FC = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [expandedSection, setExpandedSection] = useState<string | null>(null);

  const faqData = [
    {
      category: 'Başlarken',
      icon: 'rocket-launch',
      questions: [
        {
          q: 'Uygulamaya nasıl giriş yaparım?',
          a: 'Size verilen email ve şifre ile giriş yapabilirsiniz. Şifrenizi unuttuysanız, giriş ekranında "Şifremi Unuttum" seçeneğini kullanabilirsiniz.'
        },
        {
          q: 'İlk giriş sonrası ne yapmalıyım?',
          a: 'İlk girişte size atanan seferleri göreceksiniz. Ana sayfadan günlük özet bilgilerinizi, Seferler sekmesinden detaylı rota bilgilerinizi görüntüleyebilirsiniz.'
        },
        {
          q: 'Profil bilgilerimi nasıl güncellerim?',
          a: 'Sağ üst köşedeki profil ikonuna tıklayıp "Profil" seçeneğinden bilgilerinizi görüntüleyebilirsiniz. Değişiklikler için yöneticinizle iletişime geçin.'
        }
      ]
    },
    {
      category: 'Sefer Yönetimi',
      icon: 'truck-delivery',
      questions: [
        {
          q: 'Sefere nasıl başlarım?',
          a: 'Seferler listesinden günün seferini seçin, "Sefere Başla" butonuna tıklayın. Sefer başladıktan sonra durakları sırayla ziyaret edebilirsiniz.'
        },
        {
          q: 'Durağa vardığımda ne yapmalıyım?',
          a: 'Durağa vardığınızda "Check-in" butonuna basın. Teslimatı tamamladıktan sonra müşteri imzası/fotoğraf alarak "Tamamla" butonuna basın.'
        },
        {
          q: 'Teslimat yapılamazsa ne olur?',
          a: '"Başarısız" butonuna basıp sebep seçin. Sistem otomatik olarak kayıt oluşturacak ve müşteriye bildirim gönderecektir.'
        },
        {
          q: 'Rota sırasını değiştirebilir miyim?',
          a: 'Hayır, rota sırası optimize edilmiştir. Ancak zorunlu durumlarda yöneticiniz web panelden değişiklik yapabilir.'
        },
        {
          q: 'Offline çalışabilir miyim?',
          a: 'Evet! İnternet bağlantınız olmasa bile check-in ve teslimat işlemlerini yapabilirsiniz. Bağlantı geldiğinde otomatik senkronize olacaktır.'
        }
      ]
    },
    {
      category: 'Navigasyon',
      icon: 'navigation',
      questions: [
        {
          q: 'Hangi navigasyon uygulamalarını kullanabilirim?',
          a: 'Google Maps, Yandex Navi, Yandex Maps, Waze ve Apple Maps (iOS) desteklenmektedir. Ayarlar > Navigasyon Ayarları\'ndan varsayılan uygulamanızı seçebilirsiniz.'
        },
        {
          q: 'Navigasyon açılmıyor ne yapmalıyım?',
          a: 'Seçtiğiniz navigasyon uygulamasının yüklü olduğundan emin olun. Ayarlardan farklı bir uygulama seçmeyi deneyin.'
        },
        {
          q: 'Her seferinde uygulama seçmek istemiyorum',
          a: 'Ayarlar > Navigasyon Ayarları\'ndan varsayılan bir uygulama belirleyebilirsiniz. Böylece her seferinde sorulmaz.'
        }
      ]
    },
    {
      category: 'İmza ve Fotoğraf',
      icon: 'camera',
      questions: [
        {
          q: 'İmza almak zorunlu mu?',
          a: 'Yöneticinizin belirlediği kurallara göre değişir. Zorunluysa sistem imza olmadan teslimatı tamamlamanıza izin vermez.'
        },
        {
          q: 'Fotoğraf çekerken nelere dikkat etmeliyim?',
          a: 'Paketin teslim edildiği yeri net şekilde gösterin. Kapı numarası veya tanımlayıcı detaylar görünsün. Işık yeterli olsun.'
        },
        {
          q: 'İmza/fotoğraf yüklenmiyor',
          a: 'İnternet bağlantınızı kontrol edin. Offline iseniz, bağlantı geldiğinde otomatik yüklenecektir. Sorun devam ederse uygulamayı yeniden başlatın.'
        }
      ]
    },
    {
      category: 'Performans',
      icon: 'chart-line',
      questions: [
        {
          q: 'Performans metriklerim nasıl hesaplanıyor?',
          a: 'Tamamlama oranı, zamanında teslimat, müşteri memnuniyeti gibi faktörler değerlendirilir. Detayları Performans sekmesinden görebilirsiniz.'
        },
        {
          q: 'Günlük hedeflerim nerede?',
          a: 'Ana sayfada günlük özet kartında görüntülenir. Ayrıca Performans sekmesinde detaylı istatistikler bulunur.'
        }
      ]
    },
    {
      category: 'Sorun Giderme',
      icon: 'wrench',
      questions: [
        {
          q: 'Uygulama donuyor veya yavaş çalışıyor',
          a: 'Uygulamayı kapatıp yeniden açın. Sorun devam ederse cihazınızı yeniden başlatın. Hafıza doluysa gereksiz uygulamaları kapatın.'
        },
        {
          q: 'Giriş yapamıyorum',
          a: 'Email ve şifrenizi kontrol edin. Caps Lock açık olabilir. Şifrenizi unuttuysanız "Şifremi Unuttum" seçeneğini kullanın.'
        },
        {
          q: 'Bildirimler gelmiyor',
          a: 'Cihaz ayarlarından YolPilot için bildirimlere izin verdiğinizden emin olun. Ayarlar > Bildirimler > YolPilot\'u kontrol edin.'
        },
        {
          q: 'Konum izni sorunu',
          a: 'Cihaz ayarlarından YolPilot\'a konum izni verdiğinizden emin olun. "Her Zaman" seçeneğini tercih edin.'
        },
        {
          q: 'Senkronizasyon hatası',
          a: 'İnternet bağlantınızı kontrol edin. Wi-Fi veya mobil veri açık olmalı. Profil > Tüm Verileri Temizle seçeneği son çare olarak kullanılabilir.'
        }
      ]
    }
  ];

  const filteredFAQ = faqData.map(category => ({
    ...category,
    questions: category.questions.filter(
      item => 
        item.q.toLowerCase().includes(searchQuery.toLowerCase()) ||
        item.a.toLowerCase().includes(searchQuery.toLowerCase())
    )
  })).filter(category => category.questions.length > 0);

  const toggleSection = (category: string) => {
    setExpandedSection(expandedSection === category ? null : category);
  };

  const openSupport = () => {
    Alert.alert(
      'Destek',
      'Nasıl iletişime geçmek istersiniz?',
      [
        { 
          text: 'E-posta', 
          onPress: () => Linking.openURL('mailto:info@yolpilot.com?subject=Yardım Talebi')
        },
        { 
          text: 'WhatsApp', 
          onPress: () => {
            const phoneNumber = '905301783570';
            const message = encodeURIComponent('Merhaba, YolPilot uygulaması hakkında yardıma ihtiyacım var.');
            Linking.openURL(`whatsapp://send?phone=${phoneNumber}&text=${message}`);
          }
        },
        { text: 'İptal', style: 'cancel' }
      ]
    );
  };

  const quickActions = [
    { 
      title: 'Video Eğitimler', 
      icon: 'play-circle', 
      onPress: () => Alert.alert('Yakında', 'Video eğitimler yakında eklenecek')
    },
    { 
      title: 'Kullanım Kılavuzu', 
      icon: 'book-open', 
      onPress: () => Linking.openURL('https://www.yolpilot.com/docs')
    },
    { 
      title: 'Sürüm Notları', 
      icon: 'update', 
      onPress: () => Alert.alert('Sürüm 1.0.0', '• İlk sürüm\n• Temel özellikler eklendi')
    }
  ];

  return (
    <View style={styles.container}>
      <ScrollView>
        {/* Arama */}
        <Card style={styles.searchCard}>
          <Card.Content>
            <Searchbar
              placeholder="Yardım konusu arayın..."
              onChangeText={setSearchQuery}
              value={searchQuery}
              style={styles.searchBar}
            />
          </Card.Content>
        </Card>

        {/* Hızlı Erişim */}
        <Card style={styles.card}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Hızlı Erişim
            </Text>
            <View style={styles.chipContainer}>
              {quickActions.map((action, index) => (
                <Chip
                  key={index}
                  icon={action.icon}
                  onPress={action.onPress}
                  style={styles.chip}
                >
                  {action.title}
                </Chip>
              ))}
            </View>
          </Card.Content>
        </Card>

        {/* SSS */}
        <Card style={styles.card}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Sık Sorulan Sorular
            </Text>
          </Card.Content>
          
          <List.Section>
            {filteredFAQ.map((category, categoryIndex) => (
              <View key={categoryIndex}>
                <List.Accordion
                  title={category.category}
                  left={props => <List.Icon {...props} icon={category.icon} />}
                  expanded={expandedSection === category.category}
                  onPress={() => toggleSection(category.category)}
                >
                  {category.questions.map((item, index) => (
                    <View key={index} style={styles.faqItem}>
                      <Text variant="titleSmall" style={styles.question}>
                        {item.q}
                      </Text>
                      <Text variant="bodyMedium" style={styles.answer}>
                        {item.a}
                      </Text>
                      {index < category.questions.length - 1 && <Divider style={styles.divider} />}
                    </View>
                  ))}
                </List.Accordion>
                <Divider />
              </View>
            ))}
          </List.Section>
        </Card>

        {/* İletişim */}
        <Card style={[styles.card, styles.contactCard]}>
          <Card.Content>
            <Text variant="titleMedium" style={styles.sectionTitle}>
              Hala yardıma mı ihtiyacınız var?
            </Text>
            <Text variant="bodyMedium" style={styles.contactText}>
              Destek ekibimiz size yardımcı olmak için hazır.
            </Text>
            <View style={styles.contactInfo}>
              <View style={styles.contactRow}>
                <Icon name="email" size={20} color="#666" />
                <Text style={styles.contactDetail}>info@yolpilot.com</Text>
              </View>
              <View style={styles.contactRow}>
                <Icon name="whatsapp" size={20} color="#666" />
                <Text style={styles.contactDetail}>0530 178 35 70</Text>
              </View>
              <View style={styles.contactRow}>
                <Icon name="clock-outline" size={20} color="#666" />
                <Text style={styles.contactDetail}>Pazartesi - Cuma, 09:00 - 18:00</Text>
              </View>
            </View>
          </Card.Content>
        </Card>

        <View style={styles.bottomPadding} />
      </ScrollView>

      {/* FAB */}
      <FAB
        icon="headset"
        style={styles.fab}
        onPress={openSupport}
        label="Destek"
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  searchCard: {
    margin: 10,
    marginBottom: 5,
  },
  searchBar: {
    backgroundColor: '#f0f0f0',
  },
  card: {
    margin: 10,
    marginBottom: 5,
  },
  sectionTitle: {
    fontWeight: 'bold',
    marginBottom: 10,
    color: '#3B82F6',
  },
  chipContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  chip: {
    marginRight: 8,
    marginBottom: 8,
  },
  faqItem: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#fafafa',
  },
  question: {
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  answer: {
    color: '#666',
    lineHeight: 20,
  },
  divider: {
    marginTop: 12,
  },
  contactCard: {
    marginBottom: 80,
  },
  contactText: {
    color: '#666',
    marginBottom: 16,
  },
  contactInfo: {
    backgroundColor: '#f0f0f0',
    padding: 12,
    borderRadius: 8,
  },
  contactRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  contactDetail: {
    marginLeft: 12,
    color: '#333',
  },
  bottomPadding: {
    height: 20,
  },
  fab: {
    position: 'absolute',
    margin: 16,
    right: 0,
    bottom: 0,
    backgroundColor: '#3B82F6',
  },
});

export default HelpScreen;
// C:\Projects\RotaAppMobile\src\screens\settings\SettingsScreen.tsx

import React, { useState, useEffect } from 'react';
import { View, StyleSheet, ScrollView, Alert, Linking } from 'react-native';
import { 
  List, 
  Text, 
  Card,
  Switch,
  Divider,
  Button,
  Surface,
  Chip
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useAuth } from '../../contexts/AuthContext';
import { useNavigation } from '@react-navigation/native';
import navigationService from '../../services/navigationService';
import DeviceInfo from 'react-native-device-info';

const SettingsScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { user, logout } = useAuth();
  const [preferredNavApp, setPreferredNavApp] = useState<string | null>(null);
  const [installedApps, setInstalledApps] = useState<any[]>([]);
  const appVersion = DeviceInfo.getVersion();
  const buildNumber = DeviceInfo.getBuildNumber();

  useEffect(() => {
    loadNavigationSettings();
  }, []);

  const loadNavigationSettings = async () => {
    const apps = await navigationService.detectInstalledApps();
    setInstalledApps(apps);
    const savedPref = await navigationService.loadPreference();
    setPreferredNavApp(savedPref);
  };

  const handleNavigationAppSelect = async (appId: string) => {
    try {
      await navigationService.savePreference(appId);
      setPreferredNavApp(appId);
      Alert.alert('Başarılı', 'Varsayılan navigasyon uygulaması değiştirildi');
    } catch (error) {
      Alert.alert('Hata', 'Ayar kaydedilemedi');
    }
  };

  const clearNavigationPreference = async () => {
    try {
      await navigationService.clearPreference();
      setPreferredNavApp(null);
      Alert.alert('Başarılı', 'Varsayılan navigasyon uygulaması tercihi kaldırıldı');
    } catch (error) {
      Alert.alert('Hata', 'Tercih temizlenemedi');
    }
  };

  const handleLogout = () => {
    Alert.alert(
      'Çıkış Yap',
      'Çıkış yapmak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        { 
          text: 'Çıkış Yap', 
          style: 'destructive',
          onPress: async () => {
            try {
              await logout();
            } catch (error) {
              Alert.alert('Hata', 'Çıkış yapılırken bir hata oluştu');
            }
          }
        }
      ]
    );
  };

  const openPrivacyPolicy = () => {
    navigation.navigate('PrivacyPolicy');
  };

  const openTermsOfService = () => {
    navigation.navigate('TermsOfService');
  };

  const openSupport = () => {
    Alert.alert(
      'Destek',
      'Nasıl yardımcı olabiliriz?',
      [
        { 
          text: 'E-posta Gönder', 
          onPress: () => Linking.openURL('mailto:info@yolpilot.com')
        },
        { 
          text: 'WhatsApp', 
          onPress: () => {
            const phoneNumber = '905301783570'; // +90 olmadan
            const message = encodeURIComponent('Merhaba, YolPilot uygulaması hakkında destek almak istiyorum.');
            Linking.openURL(`whatsapp://send?phone=${phoneNumber}&text=${message}`);
          }
        },
        { text: 'İptal', style: 'cancel' }
      ]
    );
  };

  const openReportIssue = () => {
    navigation.navigate('ReportIssue');
  };

  const getSelectedNavAppName = () => {
    if (!preferredNavApp) return 'Otomatik';
    const app = installedApps.find(a => a.id === preferredNavApp);
    return app ? app.name : 'Otomatik';
  };

  const getUserRole = () => {
    if (user?.isSuperAdmin) return 'Süper Admin';
    if (user?.isAdmin) return 'Admin';
    if (user?.isDispatcher) return 'Dispatcher';
    if (user?.isDriver) return 'Sürücü';
    return 'Kullanıcı';
  };

  const getRoleColor = () => {
    if (user?.isSuperAdmin) return '#d32f2f';
    if (user?.isAdmin) return '#f57c00';
    if (user?.isDispatcher) return '#388e3c';
    if (user?.isDriver) return '#1976d2';
    return '#757575';
  };

  return (
    <ScrollView style={styles.container}>
      {/* Kullanıcı Bilgileri */}
      <Surface style={styles.userSection} elevation={1}>
        <View style={styles.userInfo}>
          <Icon name="account-circle" size={50} color="#3B82F6" />
          <View style={styles.userDetails}>
            <Text variant="titleMedium" style={styles.userName}>
              {user?.fullName || 'İsimsiz Kullanıcı'}
            </Text>
            <Text variant="bodySmall" style={styles.userEmail}>
              {user?.email}
            </Text>
            <Chip 
              size={20}
              style={[styles.roleChip, { backgroundColor: getRoleColor() }]}
              textStyle={{ fontSize: 11, color: 'white' }}
            >
              {getUserRole()}
            </Chip>
          </View>
        </View>
      </Surface>

      {/* Navigasyon Ayarları */}
      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Navigasyon Ayarları</List.Subheader>
            
            <List.Item
              title="Varsayılan Uygulama"
              description={getSelectedNavAppName()}
              left={props => <List.Icon {...props} icon="navigation" />}
              onPress={() => {
                const buttons = installedApps.map(app => ({
                  text: app.name,
                  onPress: () => handleNavigationAppSelect(app.id)
                }));
                
                if (preferredNavApp) {
                  buttons.push({
                    text: 'Otomatik Seçim',
                    onPress: () => clearNavigationPreference()
                  });
                }
                
                buttons.push({
                  text: 'İptal',
                  style: 'cancel'
                });

                Alert.alert('Navigasyon Uygulaması Seç', 
                  'Varsayılan navigasyon uygulamasını seçin',
                  buttons
                );
              }}
              right={props => <List.Icon {...props} icon="chevron-right" />}
            />

            <Divider />
            
            <Text variant="bodySmall" style={styles.helperText}>
              Yüklü uygulamalar: {installedApps.map(a => a.name).join(', ')}
            </Text>
          </List.Section>
        </Card.Content>
      </Card>

      {/* Genel Ayarlar */}
      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Genel</List.Subheader>
            
            <List.Item
              title="Bildirimler"
              description="Yakında eklenecek"
              left={props => <List.Icon {...props} icon="bell" />}
              right={() => <Switch value={false} disabled />}
              disabled
            />
            
            <Divider />
            
            <List.Item
              title="Konum Servisleri"
              description="Yakında eklenecek"
              left={props => <List.Icon {...props} icon="map-marker" />}
              right={() => <Switch value={false} disabled />}
              disabled
            />
          </List.Section>
        </Card.Content>
      </Card>

      {(user?.isAdmin || user?.isDispatcher || user?.isSuperAdmin) && (
        <Card style={styles.card}>
          <Card.Content>
            <List.Section>
              <List.Subheader>Operasyon</List.Subheader>

              <List.Item
                title="Depo Yönetimi"
                description="Depoları görüntüle ve düzenle"
                left={props => <List.Icon {...props} icon="warehouse" />}
                onPress={() => navigation.navigate('DepotsList')}
                right={props => <List.Icon {...props} icon="chevron-right" />}
              />

              <Divider />

              <List.Item
                title="Konum Güncelleme Talepleri"
                description="Bekleyen talepleri yönet"
                left={props => <List.Icon {...props} icon="map-marker-alert" />}
                onPress={() => navigation.navigate('LocationUpdateRequests')}
                right={props => <List.Icon {...props} icon="chevron-right" />}
              />
            </List.Section>
          </Card.Content>
        </Card>
      )}

      {/* Yardım ve Destek */}
      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Yardım ve Destek</List.Subheader>
            
            <List.Item
              title="Destek"
              description="E-posta veya WhatsApp ile ulaşın"
              left={props => <List.Icon {...props} icon="help-circle" />}
              onPress={openSupport}
              right={props => <List.Icon {...props} icon="chevron-right" />}
            />
            
            <Divider />
            
            <List.Item
              title="Gizlilik Politikası"
              left={props => <List.Icon {...props} icon="shield-lock" />}
              onPress={openPrivacyPolicy}
              right={props => <List.Icon {...props} icon="chevron-right" />}
            />
            
            <Divider />
            
            <List.Item
              title="Kullanım Koşulları"
              left={props => <List.Icon {...props} icon="file-document" />}
              onPress={openTermsOfService}
              right={props => <List.Icon {...props} icon="chevron-right" />}
            />
            
            <Divider />
            
            <List.Item
              title="Sorun Bildir"
              description="Hata veya öneri bildirin"
              left={props => <List.Icon {...props} icon="bug" />}
              onPress={openReportIssue}
              right={props => <List.Icon {...props} icon="chevron-right" />}
            />
          </List.Section>
        </Card.Content>
      </Card>

      {/* Uygulama Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Hakkında</List.Subheader>
            
            <List.Item
              title="Versiyon"
              description={`${appVersion} (${buildNumber})`}
              left={props => <List.Icon {...props} icon="information" />}
            />
            
            <Divider />
            
            <List.Item
              title="YolPilot"
              description="© 2025 YolPilot. Tüm hakları saklıdır."
              left={props => <List.Icon {...props} icon="truck" />}
            />
            
            <Divider />
            
            <List.Item
              title="İletişim"
              description="info@yolpilot.com"
              left={props => <List.Icon {...props} icon="email" />}
              onPress={() => Linking.openURL('mailto:info@yolpilot.com')}
            />
          </List.Section>
        </Card.Content>
      </Card>

      {/* Çıkış Butonu */}
      <View style={styles.logoutContainer}>
        <Button
          mode="contained"
          onPress={handleLogout}
          style={styles.logoutButton}
          icon="logout"
        >
          Çıkış Yap
        </Button>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  userSection: {
    backgroundColor: 'white',
    padding: 16,
    marginBottom: 10,
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  userDetails: {
    marginLeft: 16,
    flex: 1,
  },
  userName: {
    fontWeight: 'bold',
  },
  userEmail: {
    color: '#666',
    marginTop: 2,
  },
  roleChip: {
    marginTop: 8,
    alignSelf: 'flex-start',
  },
  card: {
    margin: 10,
    marginBottom: 5,
  },
  helperText: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    color: '#666',
    fontStyle: 'italic',
  },
  logoutContainer: {
    padding: 20,
    paddingBottom: 30,
  },
  logoutButton: {
    backgroundColor: '#EF4444',
  },
});

export default SettingsScreen;

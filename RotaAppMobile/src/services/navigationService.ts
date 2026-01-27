// C:\Projects\RotaAppMobile\src\services\navigationService.ts

import { Linking, Alert, Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';

export interface NavigationApp {
  id: string;
  name: string;
  icon: string;
  urlScheme: (lat: number, lng: number, label?: string) => string;
  packageName?: string;
  appStoreUrl: string;
}

const NAVIGATION_APPS: NavigationApp[] = [
  {
    id: 'google_maps',
    name: 'Google Maps',
    icon: 'google-maps',
    urlScheme: (lat, lng, label) => {
      const encodedLabel = label ? encodeURIComponent(label) : '';
      return Platform.select({
        ios: `comgooglemaps://?daddr=${lat},${lng}&destination_label=${encodedLabel}`,
        android: `google.navigation:q=${lat},${lng}`,
        default: `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}&destination_label=${encodedLabel}`
      }) as string;
    },
    packageName: 'com.google.android.apps.maps',
    appStoreUrl: Platform.select({
      ios: 'https://apps.apple.com/app/google-maps/id585027354',
      android: 'market://details?id=com.google.android.apps.maps',
      default: ''
    }) as string
  },
  {
    id: 'yandex_navi',
    name: 'Yandex Navi',
    icon: 'map-marker-radius',
    urlScheme: (lat, lng, label) => {
      return `yandexnavi://build_route_on_map?lat_to=${lat}&lon_to=${lng}`;
    },
    packageName: 'ru.yandex.yandexnavi',
    appStoreUrl: Platform.select({
      ios: 'https://apps.apple.com/app/yandex-navi/id474500851',
      android: 'market://details?id=ru.yandex.yandexnavi',
      default: ''
    }) as string
  },
  {
    id: 'yandex_maps',
    name: 'Yandex Maps',
    icon: 'map',
    urlScheme: (lat, lng, label) => {
      return `yandexmaps://maps.yandex.ru/?pt=${lng},${lat}&z=16&l=map`;
    },
    packageName: 'ru.yandex.yandexmaps',
    appStoreUrl: Platform.select({
      ios: 'https://apps.apple.com/app/yandex-maps/id313877526',
      android: 'market://details?id=ru.yandex.yandexmaps',
      default: ''
    }) as string
  },
  {
    id: 'waze',
    name: 'Waze',
    icon: 'navigation-variant',
    urlScheme: (lat, lng, label) => {
      return `waze://?ll=${lat},${lng}&navigate=yes`;
    },
    packageName: 'com.waze',
    appStoreUrl: Platform.select({
      ios: 'https://apps.apple.com/app/waze-navigation/id323229106',
      android: 'market://details?id=com.waze',
      default: ''
    }) as string
  },
  {
    id: 'apple_maps',
    name: 'Apple Maps',
    icon: 'apple',
    urlScheme: (lat, lng, label) => {
      const encodedLabel = label ? encodeURIComponent(label) : '';
      return `maps://maps.apple.com/?daddr=${lat},${lng}&q=${encodedLabel}`;
    },
    appStoreUrl: ''
  }
];

class NavigationService {
  private readonly PREFERENCE_KEY = 'preferred_navigation_app';
  private installedApps: NavigationApp[] = [];
  private preferredAppId: string | null = null;

  // Yüklü navigasyon uygulamalarını tespit et
  async detectInstalledApps(): Promise<NavigationApp[]> {
    const installed: NavigationApp[] = [];
    
    for (const app of NAVIGATION_APPS) {
      // iOS'ta Apple Maps her zaman vardır
      if (Platform.OS === 'ios' && app.id === 'apple_maps') {
        installed.push(app);
        continue;
      }
      
      // Android'de Google Maps genellikle vardır ama kontrol edelim
      if (Platform.OS === 'android' && app.id === 'google_maps') {
        // Google Maps her zaman web fallback olarak çalışır
        installed.push(app);
        continue;
      }
      
      try {
        // URL scheme'i desteklenip desteklenmediğini kontrol et
        const testUrl = app.urlScheme(0, 0);
        const canOpen = await Linking.canOpenURL(testUrl);
        
        if (canOpen) {
          installed.push(app);
          console.log(`✅ ${app.name} yüklü`);
        } else {
          console.log(`❌ ${app.name} yüklü değil`);
        }
      } catch (error) {
        console.log(`${app.name} kontrol edilemedi:`, error);
      }
    }
    
    // Hiç uygulama bulunamazsa, web fallback olarak Google Maps'i ekle
    if (installed.length === 0) {
      const googleMaps = NAVIGATION_APPS.find(app => app.id === 'google_maps');
      if (googleMaps) {
        installed.push(googleMaps);
      }
    }
    
    this.installedApps = installed;
    console.log(`Toplam ${installed.length} navigasyon uygulaması bulundu`);
    return installed;
  }

  // Tercih edilen uygulamayı yükle
  async loadPreference(): Promise<string | null> {
    try {
      const savedPref = await AsyncStorage.getItem(this.PREFERENCE_KEY);
      this.preferredAppId = savedPref;
      console.log('Tercih yüklendi:', savedPref);
      return savedPref;
    } catch (error) {
      console.error('Tercih yüklenemedi:', error);
      return null;
    }
  }

  // Tercih edilen uygulamayı kaydet
  async savePreference(appId: string): Promise<void> {
    try {
      await AsyncStorage.setItem(this.PREFERENCE_KEY, appId);
      this.preferredAppId = appId;
      console.log('Tercih kaydedildi:', appId);
    } catch (error) {
      console.error('Tercih kaydedilemedi:', error);
    }
  }

  // Tercihi temizle
  async clearPreference(): Promise<void> {
    try {
      await AsyncStorage.removeItem(this.PREFERENCE_KEY);
      this.preferredAppId = null;
      console.log('Tercih temizlendi');
    } catch (error) {
      console.error('Tercih temizlenemedi:', error);
    }
  }

  // Navigasyon uygulamasını aç
  async openNavigation(
    latitude: number,
    longitude: number,
    label?: string,
    preferredAppId?: string
  ): Promise<void> {
    try {
      // Yüklü uygulamaları tespit et
      if (this.installedApps.length === 0) {
        await this.detectInstalledApps();
      }

      // Tercih edilen uygulama varsa ve yüklüyse onu kullan
      let appToUse: NavigationApp | undefined;
      
      if (preferredAppId) {
        appToUse = this.installedApps.find(app => app.id === preferredAppId);
      } else if (this.preferredAppId) {
        appToUse = this.installedApps.find(app => app.id === this.preferredAppId);
      }

      // Tercih edilen uygulama yoksa veya yüklü değilse, ilk yüklü uygulamayı kullan
      if (!appToUse && this.installedApps.length > 0) {
        appToUse = this.installedApps[0];
      }

      if (appToUse) {
        const url = appToUse.urlScheme(latitude, longitude, label);
        console.log(`${appToUse.name} açılıyor:`, url);
        
        const canOpen = await Linking.canOpenURL(url);
        
        if (canOpen) {
          await Linking.openURL(url);
        } else {
          // Uygulama yüklü değilse web fallback'i dene
          console.log('Uygulama açılamadı, web fallback deneniyor');
          const webUrl = `https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`;
          await Linking.openURL(webUrl);
        }
      } else {
        // Hiçbir uygulama yoksa web'de aç
        console.log('Hiçbir uygulama bulunamadı, web\'de açılıyor');
        const webUrl = `https://www.google.com/maps/dir/?api=1&destination=${latitude},${longitude}`;
        await Linking.openURL(webUrl);
      }
    } catch (error) {
      console.error('Navigasyon açılamadı:', error);
      Alert.alert('Hata', 'Navigasyon uygulaması açılamadı.');
    }
  }

  // Uygulama seçimi için modal göster - İPTAL BUTONU DÜZELTİLDİ
  async showAppSelectionModal(
    latitude: number,
    longitude: number,
    label?: string,
    onSelect?: (app: NavigationApp) => void
  ): Promise<NavigationApp | null> {
    // Yüklü uygulamaları tespit et
    if (this.installedApps.length === 0) {
      await this.detectInstalledApps();
    }

    // Sadece bir uygulama varsa ve uzun basılmadıysa direkt onu aç
    // Uzun basıldıysa her zaman seçim göster
    
    return new Promise((resolve) => {
      const buttons = this.installedApps.map(app => ({
        text: app.name,
        onPress: async () => {
          await this.openNavigation(latitude, longitude, label, app.id);
          if (onSelect) onSelect(app);
          resolve(app);
        }
      }));

      // Her zaman kullan seçeneği ekle
      if (this.installedApps.length > 1) {
        buttons.push({
          text: 'Varsayılan Uygulama Seçimi',
          onPress: () => {
            this.showDefaultAppSelection(latitude, longitude, label);
            resolve(null);
          }
        });
      }

      // Tercih varsa temizleme seçeneği
      if (this.preferredAppId) {
        const prefApp = this.installedApps.find(app => app.id === this.preferredAppId);
        if (prefApp) {
          buttons.push({
            text: `Varsayılanı Kaldır (${prefApp.name})`,
            onPress: async () => {
              await this.clearPreference();
              Alert.alert('Başarılı', 'Varsayılan uygulama tercihi kaldırıldı');
              resolve(null);
            }
          });
        }
      }

      // İPTAL BUTONU - HİÇBİR ŞEY YAPMADAN MODAL'I KAPATIR
      buttons.push({
        text: 'İptal',
        style: 'cancel',
        onPress: () => {
          // Sadece modal'ı kapat, başka bir şey yapma
          resolve(null);
        }
      });

      Alert.alert(
        'Navigasyon Uygulaması Seçin',
        this.installedApps.length > 1 
          ? 'Hangi uygulama ile yol tarifi almak istersiniz?' 
          : 'Navigasyon seçenekleri',
        buttons,
        { cancelable: true, onDismiss: () => resolve(null) } // Dışarı tıklanınca da kapatılabilir
      );
    });
  }

  // Varsayılan uygulama seçimi
  private async showDefaultAppSelection(
    latitude: number,
    longitude: number,
    label?: string
  ): Promise<void> {
    const buttons = this.installedApps.map(app => ({
      text: `${app.name}`,
      onPress: async () => {
        await this.savePreference(app.id);
        await this.openNavigation(latitude, longitude, label, app.id);
        Alert.alert('Başarılı', `${app.name} varsayılan navigasyon uygulaması olarak ayarlandı.`);
      }
    }));

    buttons.push({
      text: 'İptal',
      style: 'cancel' as const
    });

    Alert.alert(
      'Varsayılan Uygulama Seç',
      'Bundan sonra her zaman kullanılacak navigasyon uygulamasını seçin.',
      buttons,
      { cancelable: true }
    );
  }

  // Yüklü uygulamaları getir
  getInstalledApps(): NavigationApp[] {
    return this.installedApps;
  }

  // Tercih edilen uygulamayı getir
  getPreferredApp(): NavigationApp | null {
    if (!this.preferredAppId) return null;
    return this.installedApps.find(app => app.id === this.preferredAppId) || null;
  }
}

export default new NavigationService();
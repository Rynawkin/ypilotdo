// C:\Projects\RotaAppMobile\src\services\permissionService.ts

import { Platform, Alert, Linking } from 'react-native';
import {
  check,
  request,
  PERMISSIONS,
  RESULTS,
  openSettings,
  Permission,
} from 'react-native-permissions';

class PermissionService {
  // Kamera izni kontrol ve istek
  async checkCameraPermission(): Promise<boolean> {
    let permission: Permission;
    
    if (Platform.OS === 'ios') {
      permission = PERMISSIONS.IOS.CAMERA;
    } else {
      permission = PERMISSIONS.ANDROID.CAMERA;
    }

    const result = await check(permission);
    
    switch (result) {
      case RESULTS.GRANTED:
        return true;
      case RESULTS.DENIED:
        const requestResult = await request(permission);
        return requestResult === RESULTS.GRANTED;
      case RESULTS.BLOCKED:
        this.showPermissionAlert('Kamera', 'kamera');
        return false;
      default:
        return false;
    }
  }

  // Galeri/Storage izni kontrol ve istek
  async checkStoragePermission(): Promise<boolean> {
    let permission: Permission;
    
    if (Platform.OS === 'ios') {
      permission = PERMISSIONS.IOS.PHOTO_LIBRARY;
    } else {
      // Android 13+ (API 33+) için yeni izin sistemi
      if (Platform.Version >= 33) {
        // Android 13+ için storage izni gerekmez, otomatik verilir
        return true;
      } else {
        permission = PERMISSIONS.ANDROID.READ_EXTERNAL_STORAGE;
      }
    }

    const result = await check(permission);
    
    switch (result) {
      case RESULTS.GRANTED:
        return true;
      case RESULTS.DENIED:
        const requestResult = await request(permission);
        return requestResult === RESULTS.GRANTED;
      case RESULTS.BLOCKED:
        this.showPermissionAlert('Galeri', 'fotoğraf galerisi');
        return false;
      default:
        return false;
    }
  }

  // Konum izni kontrol ve istek
  async checkLocationPermission(): Promise<boolean> {
    let permission: Permission;
    
    if (Platform.OS === 'ios') {
      permission = PERMISSIONS.IOS.LOCATION_WHEN_IN_USE;
    } else {
      permission = PERMISSIONS.ANDROID.ACCESS_FINE_LOCATION;
    }

    const result = await check(permission);
    
    switch (result) {
      case RESULTS.GRANTED:
        return true;
      case RESULTS.DENIED:
        const requestResult = await request(permission);
        return requestResult === RESULTS.GRANTED;
      case RESULTS.BLOCKED:
        this.showPermissionAlert('Konum', 'konum servisleri');
        return false;
      default:
        return false;
    }
  }

  // Arka plan konum izni (Android)
  async checkBackgroundLocationPermission(): Promise<boolean> {
    if (Platform.OS === 'ios') {
      // iOS için LOCATION_ALWAYS kullanılır
      const permission = PERMISSIONS.IOS.LOCATION_ALWAYS;
      const result = await check(permission);
      
      switch (result) {
        case RESULTS.GRANTED:
          return true;
        case RESULTS.DENIED:
          const requestResult = await request(permission);
          return requestResult === RESULTS.GRANTED;
        case RESULTS.BLOCKED:
          this.showPermissionAlert('Arka Plan Konum', 'sürekli konum takibi');
          return false;
        default:
          return false;
      }
    } else {
      // Android için ACCESS_BACKGROUND_LOCATION
      if (Platform.Version >= 29) {
        const permission = PERMISSIONS.ANDROID.ACCESS_BACKGROUND_LOCATION;
        const result = await check(permission);
        
        switch (result) {
          case RESULTS.GRANTED:
            return true;
          case RESULTS.DENIED:
            const requestResult = await request(permission);
            return requestResult === RESULTS.GRANTED;
          case RESULTS.BLOCKED:
            this.showPermissionAlert('Arka Plan Konum', 'arka plan konum takibi');
            return false;
          default:
            return false;
        }
      }
      return true; // Android 29 altı için gerek yok
    }
  }

  // Telefon arama izni
  async checkPhonePermission(): Promise<boolean> {
    if (Platform.OS === 'ios') {
      // iOS'ta telefon arama için özel izin gerekmez
      return true;
    } else {
      const permission = PERMISSIONS.ANDROID.CALL_PHONE;
      const result = await check(permission);
      
      switch (result) {
        case RESULTS.GRANTED:
          return true;
        case RESULTS.DENIED:
          const requestResult = await request(permission);
          return requestResult === RESULTS.GRANTED;
        case RESULTS.BLOCKED:
          this.showPermissionAlert('Telefon', 'telefon arama');
          return false;
        default:
          return false;
      }
    }
  }

  // Tüm gerekli izinleri kontrol et
  async checkAllPermissions(): Promise<{
    camera: boolean;
    storage: boolean;
    location: boolean;
    phone: boolean;
  }> {
    const [camera, storage, location, phone] = await Promise.all([
      this.checkCameraPermission(),
      this.checkStoragePermission(),
      this.checkLocationPermission(),
      this.checkPhonePermission(),
    ]);

    return {
      camera,
      storage,
      location,
      phone,
    };
  }

  // İzin reddi alert'i
  private showPermissionAlert(title: string, feature: string) {
    Alert.alert(
      `${title} İzni Gerekli`,
      `Bu özelliği kullanabilmek için ${feature} iznini vermeniz gerekiyor. Ayarlar'dan izni verebilirsiniz.`,
      [
        {
          text: 'İptal',
          style: 'cancel',
        },
        {
          text: 'Ayarlara Git',
          onPress: () => openSettings(),
        },
      ],
    );
  }

  // Direkt ayarlara yönlendir
  async openAppSettings() {
    try {
      await openSettings();
    } catch (error) {
      console.error('Ayarlar açılamadı:', error);
      Alert.alert('Hata', 'Uygulama ayarları açılamadı.');
    }
  }

  // Telefon numarasını ara
  async makePhoneCall(phoneNumber: string) {
    const hasPermission = await this.checkPhonePermission();
    
    if (!hasPermission) {
      return false;
    }

    const url = `tel:${phoneNumber}`;
    
    try {
      const canOpen = await Linking.canOpenURL(url);
      if (canOpen) {
        await Linking.openURL(url);
        return true;
      } else {
        Alert.alert('Hata', 'Telefon araması yapılamıyor.');
        return false;
      }
    } catch (error) {
      console.error('Telefon arama hatası:', error);
      Alert.alert('Hata', 'Telefon araması başlatılamadı.');
      return false;
    }
  }

  // Haritada göster (Google Maps veya Apple Maps)
  async openMaps(latitude: number, longitude: number, label?: string) {
    const scheme = Platform.select({
      ios: 'maps:0,0?q=',
      android: 'geo:0,0?q=',
    });
    
    const latLng = `${latitude},${longitude}`;
    const url = Platform.select({
      ios: `${scheme}${label || 'Teslimat Noktası'}@${latLng}`,
      android: `${scheme}${latLng}(${label || 'Teslimat Noktası'})`,
    });

    try {
      const canOpen = await Linking.canOpenURL(url!);
      if (canOpen) {
        await Linking.openURL(url!);
        return true;
      } else {
        // Alternatif olarak Google Maps web'i aç
        const webUrl = `https://www.google.com/maps/search/?api=1&query=${latLng}`;
        await Linking.openURL(webUrl);
        return true;
      }
    } catch (error) {
      console.error('Harita açma hatası:', error);
      Alert.alert('Hata', 'Harita uygulaması açılamadı.');
      return false;
    }
  }
}

export default new PermissionService();
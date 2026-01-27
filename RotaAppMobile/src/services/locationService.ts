// src/services/locationService.ts
import { Platform } from 'react-native';
import Geolocation, { GeoPosition, GeoError } from 'react-native-geolocation-service';
import DeviceInfo from 'react-native-device-info';
import permissionService from './permissionService';

export type SafePosition = {
  latitude: number;
  longitude: number;
  accuracy?: number | null;
  isMock?: boolean;
};

class LocationService {
  /**
   * Gerçek zamanlı, yüksek doğruluklu konum döndürür.
   * Mock (emülatör veya sahte konum) algılanırsa hata fırlatır.
   */
  async getCurrentPreciseLocation(timeoutMs = 15000): Promise<SafePosition> {
    const hasPermission = await permissionService.checkLocationPermission();
    if (!hasPermission) {
      throw new Error('Konum izni verilmedi. Lütfen ayarlardan konum izni verin.');
    }

    const isEmu = await DeviceInfo.isEmulator();

    return new Promise<SafePosition>((resolve, reject) => {
      Geolocation.getCurrentPosition(
        (pos: GeoPosition & { mocked?: boolean }) => {
          const { latitude, longitude, accuracy } = pos.coords;
          const mockedFromAPI = (Platform.OS === 'android' ? (pos as any).mocked === true : false);
          const isMock = mockedFromAPI || isEmu;

          if (isMock) {
            reject(
              new Error(
                'Mock konum algılandı. Lütfen gerçek cihazda test edin ve “Select mock location app” kapalı olsun.'
              )
            );
            return;
          }

          resolve({
            latitude,
            longitude,
            accuracy: accuracy ?? null,
            isMock,
          });
        },
        (error: GeoError) => {
          reject(error);
        },
        {
          enableHighAccuracy: true,
          timeout: timeoutMs,
          maximumAge: 0,
          forceRequestLocation: true,
          showLocationDialog: true,
        }
      );
    });
  }

  /**
   * Basit konum bilgisi döndürür (mock kontrolü yapmaz).
   */
  async getCurrentLocation(timeoutMs = 15000): Promise<SafePosition> {
    const hasPermission = await permissionService.checkLocationPermission();
    if (!hasPermission) {
      throw new Error('Konum izni verilmedi. Lütfen ayarlardan konum izni verin.');
    }

    return new Promise<SafePosition>((resolve, reject) => {
      Geolocation.getCurrentPosition(
        (pos: GeoPosition) => {
          const { latitude, longitude, accuracy } = pos.coords;
          resolve({
            latitude,
            longitude,
            accuracy: accuracy ?? null,
            isMock: false,
          });
        },
        (error: GeoError) => {
          reject(error);
        },
        {
          enableHighAccuracy: true,
          timeout: timeoutMs,
          maximumAge: 5000,
          forceRequestLocation: true,
          showLocationDialog: true,
        }
      );
    });
  }
}

export default new LocationService();

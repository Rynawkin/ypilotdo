// src/utils/responsive.ts
import { Dimensions, Platform, PixelRatio } from 'react-native';
import { normalize } from './dimensions';

const { width, height } = Dimensions.get('window');

// Cihaz boyut kontrolleri
export const isSmallDevice = width < 375;
export const isMediumDevice = width >= 375 && width < 414;
export const isLargeDevice = width >= 414;
export const isExtraLargeDevice = width >= 428; // iPhone 14 Plus, Pro Max
export const isTablet = width >= 768;
export const isIOS = Platform.OS === 'ios';
export const isAndroid = Platform.OS === 'android';

// Büyük ekran kontrolü - SORUN BU OLABILIR
export const isLargeScreen = false; // ŞİMDİLİK KAPAT

// Debug için
console.log('Device Info:', {
  width,
  height,
  isLargeScreen,
  isExtraLargeDevice,
  pixelRatio: PixelRatio.get()
});

// Font scale helper - Büyük ekranlar için düzeltildi
export const fontScale = () => {
    if (isSmallDevice) return 0.9;
    if (isLargeDevice) return 1.1; // Büyük ekranlar için azaltıldı
    if (isExtraLargeDevice) return 1.15; // Çok büyük ekranlar için
    if (isTablet) return 1.2;
    return 1;
};

// Responsive font size
export const fontSize = {
    xs: normalize(10 * fontScale()),
    sm: normalize(12 * fontScale()),
    md: normalize(14 * fontScale()),
    lg: normalize(16 * fontScale()),
    xl: normalize(18 * fontScale()),
    xxl: normalize(24 * fontScale()),
    xxxl: normalize(32 * fontScale()),
};

// Responsive spacing
export const spacing = {
    xs: normalize(4),
    sm: normalize(8),
    md: normalize(12),
    lg: normalize(16),
    xl: normalize(24),
    xxl: normalize(32),
};

// Responsive border radius
export const borderRadius = {
    sm: normalize(4),
    md: normalize(8),
    lg: normalize(12),
    xl: normalize(16),
    full: 9999,
};

// Responsive icon sizes
export const iconSize = {
    xs: normalize(12),
    sm: normalize(16),
    md: normalize(20),
    lg: normalize(24),
    xl: normalize(32),
    xxl: normalize(48),
};

// A70 ve eski cihazlar için özel kontrol
export const isLowEndDevice = () => {
    // RAM miktarı 3GB'den az olan cihazları tespit et
    const deviceModel = Platform.select({
        android: 'android',
        ios: 'ios',
    });

    // A70 gibi eski Samsung modelleri için
    if (isAndroid && width <= 360 && height <= 760) {
        return true;
    }

    return false;
};

// FlatList optimizasyonu için
export const getFlatListOptimizations = () => {
    if (isLowEndDevice()) {
        return {
            initialNumToRender: 5,
            maxToRenderPerBatch: 3,
            windowSize: 7,
            removeClippedSubviews: true,
            updateCellsBatchingPeriod: 100,
            legacyImplementation: false,
        };
    }
    
    if (isLargeScreen) {
        return {
            initialNumToRender: 10,
            maxToRenderPerBatch: 5,
            windowSize: 15,
            removeClippedSubviews: true,
            updateCellsBatchingPeriod: 50,
            legacyImplementation: false,
        };
    }
    
    return {
        initialNumToRender: 10,
        maxToRenderPerBatch: 10,
        windowSize: 21,
        removeClippedSubviews: false,
    };
};

// Performans optimizasyonu için
export const shouldReduceMotion = isLowEndDevice();
export const maxListItems = isLowEndDevice() ? 10 : 50;
export const enableShadows = !isLowEndDevice();
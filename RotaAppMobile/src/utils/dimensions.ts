// src/utils/dimensions.ts
import { Dimensions, PixelRatio, Platform } from 'react-native';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

// Tasarım temel alınan cihaz boyutları (örn: iPhone 11)
const baseWidth = 375;
const baseHeight = 812;

// Büyük ekranlar için max scale limiti ekle
const scaleWidth = screenWidth / baseWidth;
const scaleHeight = screenHeight / baseHeight;
const scale = Math.min(scaleWidth, scaleHeight);

// Maximum scale limiti - büyük ekranlarda aşırı büyümeyi önle
const maxScale = 1.3;
const finalScale = Math.min(scale, maxScale);

export const normalize = (size: number) => {
  const newSize = size * finalScale; // scale yerine finalScale kullan
  if (Platform.OS === 'ios') {
    return Math.round(PixelRatio.roundToNearestPixel(newSize));
  } else {
    // Android için daha hassas hesaplama
    const androidSize = Math.round(PixelRatio.roundToNearestPixel(newSize));
    return androidSize > 2 ? androidSize - 2 : androidSize;
  }
};

export const wp = (percentage: number) => {
  return (percentage * screenWidth) / 100;
};

export const hp = (percentage: number) => {
  return (percentage * screenHeight) / 100;
};

// Büyük ekranlar için güvenli alan
export const getSafeAreaPadding = () => {
  if (screenHeight > 850) {
    return {
      top: 50,
      bottom: 30,
    };
  }
  return {
    top: 20,
    bottom: 10,
  };
};
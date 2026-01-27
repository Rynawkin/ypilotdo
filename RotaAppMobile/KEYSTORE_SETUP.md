# Keystore ve Signing Kurulum Rehberi

## 1. Android Keystore Oluşturma

### Yeni keystore oluşturmak için:

```bash
cd android/app
keytool -genkey -v -keystore yolpilot-release.keystore -alias yolpilot -keyalg RSA -keysize 2048 -validity 10000
```

### Sorulacak bilgiler:
- **Keystore password**: Güçlü bir şifre (örn: YolPilot2025SecureKey!)
- **Key password**: Aynı şifre veya farklı güçlü şifre
- **First and last name**: YolPilot
- **Organizational unit**: Development Team
- **Organization**: YolPilot
- **City**: Istanbul
- **State**: Istanbul
- **Country code**: TR

## 2. Local Properties Ayarı

1. `android/local.properties.example` dosyasını `android/local.properties` olarak kopyala
2. İçindeki bilgileri gerçek keystore bilgilerinle değiştir:

```properties
YOLPILOT_UPLOAD_STORE_FILE=yolpilot-release.keystore
YOLPILOT_UPLOAD_KEY_ALIAS=yolpilot
YOLPILOT_UPLOAD_STORE_PASSWORD=YolPilot2025SecureKey!
YOLPILOT_UPLOAD_KEY_PASSWORD=YolPilot2025SecureKey!
```

## 3. Release Build Oluşturma

```bash
cd android
./gradlew assembleRelease
```

APK dosyası şurada oluşur: `android/app/build/outputs/apk/release/app-release.apk`

## 4. Önemli Güvenlik Notları

- ✅ `local.properties` dosyası git'e commit edilmez (zaten .gitignore'da)
- ✅ Keystore dosyasını güvenli bir yere backup'la
- ✅ Şifrelerini unutma - kaybet
- ⚠️ Keystore dosyasını paylaşma
- ⚠️ Şifreleri kod içine yazma

## 5. Google Play Console'a Upload

1. Google Play Console'da uygulamayı oluştur
2. Play App Signing'i etkinleştir
3. İlk APK/AAB'ı upload et
4. Sonraki güncellemeler için aynı keystore kullan

## Sorun Yaşarsan

Eğer keystore oluşturma veya build alma konusunda sorun yaşarsan, bu adımları takip et ve hata mesajını paylaş.
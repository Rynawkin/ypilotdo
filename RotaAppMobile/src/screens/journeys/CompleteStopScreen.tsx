// C:\Projects\RotaAppMobile\src\screens\journeys\CompleteStopScreen.tsx

import React, { useState, useRef, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Alert,
  Image,
  TouchableOpacity,
  Modal,
  ActivityIndicator,
  Dimensions,
  FlatList,
} from 'react-native';
import { Button, TextInput, Card, HelperText, IconButton } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation } from '@react-navigation/native';
import SignatureCanvas from 'react-native-signature-canvas';
import {
  launchCamera,
  launchImageLibrary,
  ImagePickerResponse,
  CameraOptions,
  ImageLibraryOptions,
  Asset,
} from 'react-native-image-picker';
import journeyService from '../../services/journeyService';
import permissionService from '../../services/permissionService';
import networkService from '../../services/networkService';
import offlineQueueService from '../../services/offlineQueueService';
import locationService from '../../services/locationService'; // YENİ: Konum servisi

interface RouteParams {
  journeyId: number;
  stopId: number;
  stopName: string;
  requiresProof: boolean;
  signatureRequired?: boolean;
  photoRequired?: boolean;
  customerId?: number; // YENİ: Müşteri ID
  currentLatitude?: number; // YENİ: Mevcut enlem
  currentLongitude?: number; // YENİ: Mevcut boylam
  currentAddress?: string; // YENİ: Mevcut adres
}

interface PhotoItem {
  uri: string;
  type?: string;
  name?: string;
  base64?: string;
}

// YENİ: Konum güncelleme modal state interface
interface LocationUpdateState {
  visible: boolean;
  currentLocation: { latitude: number; longitude: number } | null;
  reason: string;
  loading: boolean;
}

const { width: screenWidth } = Dimensions.get('window');
const MAX_PHOTOS = 10; // Maksimum fotoğraf sayısı

const CompleteStopScreen: React.FC = () => {
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { 
    journeyId, 
    stopId, 
    stopName, 
    requiresProof, 
    signatureRequired, 
    photoRequired,
    customerId,
    currentLatitude,
    currentLongitude,
    currentAddress
  } = route.params as RouteParams;

  // Ensure stopName is always a string
  const safeStopName = String(stopName || 'Depo Dönüşü');
  console.log('CompleteStopScreen - stopName:', stopName, 'safeStopName:', safeStopName);

  const [notes, setNotes] = useState('');
  const [receiverName, setReceiverName] = useState('');
  const [signature, setSignature] = useState<string | null>(null);
  const [photos, setPhotos] = useState<PhotoItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [showSignatureModal, setShowSignatureModal] = useState(false);
  const [photoLoading, setPhotoLoading] = useState(false);
  const [isOnline, setIsOnline] = useState(true);
  const [selectedPhotoIndex, setSelectedPhotoIndex] = useState<number | null>(null);
  
  // YENİ: Konum güncelleme state
  const [locationUpdate, setLocationUpdate] = useState<LocationUpdateState>({
    visible: false,
    currentLocation: null,
    reason: '',
    loading: false
  });

  // Signature ref
  const signatureRef = useRef<any>(null);

  // Network durumunu dinle
  useEffect(() => {
    setIsOnline(networkService.getIsConnected());

    const handleConnectionChange = (connected: boolean) => {
      setIsOnline(connected);
    };

    networkService.on('connectionChange', handleConnectionChange);

    return () => {
      networkService.removeListener('connectionChange', handleConnectionChange);
    };
  }, []);

  // YENİ: Mevcut konumu al
  const getCurrentLocation = async () => {
    try {
      const hasPermission = await permissionService.checkLocationPermission();
      if (!hasPermission) {
        Alert.alert('İzin Gerekli', 'Konum erişimi için izin vermeniz gerekiyor.');
        return null;
      }

      const location = await locationService.getCurrentLocation();
      return location;
    } catch (error) {
      console.error('Konum alınamadı:', error);
      return null;
    }
  };

  // YENİ: Konum güncelleme talebi modal aç
  const handleOpenLocationUpdateModal = async () => {
    try {
      // Önce loading göster
      setLocationUpdate(prev => ({
        ...prev,
        // modalı henüz açma
        visible: false,
        loading: true,
      }));

      // Gerçek konumu yüksek doğrulukla al (mock algılamalı)
      const pos = await locationService.getCurrentPreciseLocation();

      // Başarılı: modalı gerçek konumla aç
      setLocationUpdate({
        visible: true,
        currentLocation: {
          latitude: pos.latitude,
          longitude: pos.longitude,
        },
        reason: '',
        loading: false,
      });
    } catch (err: any) {
      console.error('Konum hatası:', err);
      // Hata: fallback asla kullanma, uyar ve modalı açma
      Alert.alert(
        'Konum alınamadı',
        err?.message || 'Konum servisine erişilemedi. Lütfen konum izinlerini ve cihaz ayarlarını kontrol edin.'
      );
      setLocationUpdate(prev => ({ ...prev, loading: false, visible: false }));
    }
  };

  // YENİ: Konum güncelleme talebi gönder
  const handleSubmitLocationUpdate = async () => {
    if (!locationUpdate.reason.trim()) {
      Alert.alert('Eksik Bilgi', 'Lütfen güncelleme nedenini girin.');
      return;
    }

    if (!locationUpdate.currentLocation) {
      Alert.alert('Hata', 'Konum bilgisi eksik.');
      return;
    }

    setLocationUpdate({ ...locationUpdate, loading: true });

    try {
      if (!isOnline) {
        // Offline durumda queue'ya ekle
        await offlineQueueService.addToQueue({
          type: 'LOCATION_UPDATE_REQUEST',
          journeyId,
          stopId,
          data: {
            customerId,
            currentLatitude: currentLatitude || 0,
            currentLongitude: currentLongitude || 0,
            currentAddress: currentAddress || '',
            requestedLatitude: locationUpdate.currentLocation.latitude,
            requestedLongitude: locationUpdate.currentLocation.longitude,
            reason: locationUpdate.reason
          }
        });

        Alert.alert(
          'Talep Kaydedildi',
          'Konum güncelleme talebiniz internet bağlantısı kurulduğunda gönderilecek.',
          [{ text: 'Tamam', onPress: () => setLocationUpdate({ ...locationUpdate, visible: false, reason: '' }) }]
        );
      } else {
        // Online durumda API çağrısı yap
        const response = await journeyService.createLocationUpdateRequest({
          journeyId,
          stopId,
          customerId: customerId || 0,
          currentLatitude: currentLatitude || 0,
          currentLongitude: currentLongitude || 0,
          currentAddress: currentAddress || '',
          requestedLatitude: locationUpdate.currentLocation.latitude,
          requestedLongitude: locationUpdate.currentLocation.longitude,
          reason: locationUpdate.reason
        });

        Alert.alert(
          'Talep Gönderildi',
          'Konum güncelleme talebiniz yöneticiye iletildi. Onaylandığında müşteri konumu güncellenecek.',
          [{ text: 'Tamam', onPress: () => setLocationUpdate({ ...locationUpdate, visible: false, reason: '' }) }]
        );
      }
    } catch (error) {
      Alert.alert('Hata', 'Talep gönderilemedi. Lütfen tekrar deneyin.');
    } finally {
      setLocationUpdate({ ...locationUpdate, loading: false });
    }
  };

  // İmza modalını aç
  const handleOpenSignatureModal = () => {
    setShowSignatureModal(true);
    setValidationError(null);
  };

  // İmza onayla
  const handleSignatureOK = (signature: string) => {
    if (!signature.startsWith('data:')) {
      signature = `data:image/png;base64,${signature}`;
    }
    setSignature(signature);
    setShowSignatureModal(false);
  };

  // İmza temizle
  const handleClearSignature = () => {
    setSignature(null);
    if (signatureRef.current) {
      signatureRef.current.clearSignature();
    }
  };

  // Fotoğraf seçim modalı
  const showPhotoOptions = () => {
    if (photos.length >= MAX_PHOTOS) {
      Alert.alert('Limit', `En fazla ${MAX_PHOTOS} fotoğraf ekleyebilirsiniz.`);
      return;
    }

    Alert.alert(
      'Fotoğraf Ekle',
      `Nasıl fotoğraf eklemek istersiniz? (${photos.length}/${MAX_PHOTOS})`,
      [
        { text: 'İptal', style: 'cancel' },
        { text: 'Kamera', onPress: handleTakePhoto },
        { text: 'Galeri', onPress: handleChooseFromGallery },
      ],
      { cancelable: true }
    );
  };

  // Kameradan fotoğraf çek
  const handleTakePhoto = async () => {
    const hasPermission = await permissionService.checkCameraPermission();
    if (!hasPermission) return;

    const options: CameraOptions = {
      mediaType: 'photo',
      includeBase64: true,
      maxHeight: 1024,
      maxWidth: 1024,
      quality: 0.7,
      saveToPhotos: false,
    };

    setPhotoLoading(true);

    launchCamera(options, (response: ImagePickerResponse) => {
      setPhotoLoading(false);

      if (response.didCancel) {
        console.log('Kullanıcı kamerayı iptal etti');
      } else if (response.errorMessage) {
        console.error('Kamera hatası:', response.errorMessage);
        Alert.alert('Hata', 'Fotoğraf çekilemedi: ' + response.errorMessage);
      } else if (response.assets && response.assets[0]) {
        const asset = response.assets[0];
        if (asset.base64) {
          const base64Photo = `data:${asset.type};base64,${asset.base64}`;
          addPhoto({
            uri: base64Photo,
            type: asset.type,
            name: `photo_${Date.now()}.jpg`
          });
        }
      }
    });
  };

  // Galeriden fotoğraf seç - Çoklu seçim desteği
  const handleChooseFromGallery = async () => {
    const hasPermission = await permissionService.checkStoragePermission();
    if (!hasPermission) return;

    const remainingSlots = MAX_PHOTOS - photos.length;

    const options: ImageLibraryOptions = {
      mediaType: 'photo',
      includeBase64: true,
      maxHeight: 1024,
      maxWidth: 1024,
      quality: 0.7,
      selectionLimit: remainingSlots,
    };

    setPhotoLoading(true);

    launchImageLibrary(options, (response: ImagePickerResponse) => {
      setPhotoLoading(false);

      if (response.didCancel) {
        console.log('Kullanıcı galeriyi iptal etti');
      } else if (response.errorMessage) {
        console.error('Galeri hatası:', response.errorMessage);
        Alert.alert('Hata', 'Fotoğraf seçilemedi: ' + response.errorMessage);
      } else if (response.assets && response.assets.length > 0) {
        response.assets.forEach((asset: Asset, index: number) => {
          if (asset.base64 && photos.length + index < MAX_PHOTOS) {
            const base64Photo = `data:${asset.type};base64,${asset.base64}`;
            addPhoto({
              uri: base64Photo,
              type: asset.type,
              name: `photo_${Date.now()}_${index}.jpg`
            });
          }
        });
      }
    });
  };

  // Fotoğraf ekle
  const addPhoto = (photo: PhotoItem) => {
    setPhotos(prev => {
      if (prev.length >= MAX_PHOTOS) {
        Alert.alert('Limit', `En fazla ${MAX_PHOTOS} fotoğraf ekleyebilirsiniz.`);
        return prev;
      }
      return [...prev, photo];
    });
    setValidationError(null);
  };

  // Fotoğraf sil
  const handleDeletePhoto = (index: number) => {
    Alert.alert(
      'Fotoğrafı Sil',
      'Bu fotoğrafı silmek istediğinize emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Sil',
          style: 'destructive',
          onPress: () => {
            setPhotos(prev => prev.filter((_, i) => i !== index));
          },
        },
      ]
    );
  };

  // Validasyon kontrolü
  const validateForm = (): boolean => {
    // Teslim alan kişi kontrolü
    if (!receiverName || receiverName.trim().length < 3) {
      setValidationError('Teslim alan kişinin adını girmelisiniz (en az 3 karakter).');
      return false;
    }

    // ProofOfDeliveryRequired kontrolü (ikisi birden zorunlu)
    if (requiresProof) {
      if (!signature) {
        setValidationError('Bu teslimat için imza zorunludur.');
        return false;
      }
      if (photos.length === 0) {
        setValidationError('Bu teslimat için en az bir fotoğraf zorunludur.');
        return false;
      }
    }

    // Ayrı ayrı kontroller
    if (signatureRequired && !signature) {
      setValidationError('Bu teslimat için imza zorunludur.');
      return false;
    }

    if (photoRequired && photos.length === 0) {
      setValidationError('Bu teslimat için en az bir fotoğraf zorunludur.');
      return false;
    }

    // Not alanı minimum uzunluk kontrolü
    if (!notes || notes.trim().length < 3) {
      setValidationError('Lütfen en az 3 karakterlik bir not girin.');
      return false;
    }

    setValidationError(null);
    return true;
  };

  // Tamamlama işlemi
  const handleComplete = async () => {
    if (!validateForm()) {
      return;
    }

    // Offline uyarısı
    if (!isOnline) {
      Alert.alert(
        'Çevrimdışı Mod',
        'Teslimat bilgileri kaydedilecek ve internet bağlantısı kurulduğunda gönderilecek. Devam etmek istiyor musunuz?',
        [
          { text: 'İptal', style: 'cancel' },
          {
            text: 'Devam Et',
            onPress: async () => {
              await processComplete();
            }
          }
        ]
      );
    } else {
      Alert.alert(
        'Teslimatı Tamamla',
        'Teslimatı tamamlamak istediğinize emin misiniz?',
        [
          { text: 'İptal', style: 'cancel' },
          {
            text: 'Tamamla',
            onPress: async () => {
              await processComplete();
            },
          },
        ]
      );
    }
  };

  const processComplete = async () => {
    try {
      setLoading(true);

      if (!validateForm()) {
        Alert.alert('Eksik Bilgi', validationError || 'Zorunlu alanları doldurun');
        setLoading(false);
        return;
      }

      // Offline durumda queue'ya ekle
      if (!isOnline) {
        try {
          await offlineQueueService.addToQueue({
            type: 'COMPLETE',
            journeyId,
            stopId,
            data: {
              notes,
              receiverName,
              photoUris: photos.map(p => p.uri),
              signatureUri: signature,
              requirements: {
                signatureRequired: signatureRequired || requiresProof,
                photoRequired: photoRequired || false
              }
            }
          });

          Alert.alert(
            'Çevrimdışı Kaydedildi',
            'Teslimat bilgileri kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
            [{ text: 'Tamam', onPress: () => navigation.goBack() }]
          );
        } catch (error: any) {
          Alert.alert(
            'Eksik Bilgi',
            error.message || 'Zorunlu alanları doldurun',
            [{ text: 'Tamam' }]
          );
          setLoading(false);
          return;
        }
        setLoading(false);
        return;
      }

      // Online durumda FormData ile gönder
      const formData = new FormData();

      if (notes) {
        formData.append('notes', notes);
      }

      // Teslim alan kişiyi FormData'ya ekle
      if (receiverName) {
        formData.append('receiverName', receiverName);
      }

      // İmzayı FormData'ya ekle
      if (signature) {
        formData.append('signature', {
          uri: signature,
          type: 'image/png',
          name: 'signature.png',
        } as any);
      }

      // Çoklu fotoğrafları FormData'ya ekle
      photos.forEach((photo, index) => {
        // İlk fotoğraf için 'photo' field'ını kullan (backward compatibility)
        const fieldName = index === 0 ? 'photo' : `photos`;

        formData.append(fieldName, {
          uri: photo.uri,
          type: photo.type || 'image/jpeg',
          name: photo.name || `photo_${index}.jpg`,
        } as any);
      });

      const result = await journeyService.completeStopWithFiles(journeyId, stopId, formData);

      if (result) {
        Alert.alert(
          'Başarılı',
          'Teslimat başarıyla tamamlandı.',
          [
            {
              text: 'Tamam',
              onPress: () => {
                navigation.goBack();
              },
            },
          ]
        );
      }
    } catch (error: any) {
      console.error('Complete stop error:', error);

      const errorMessage = error.response?.data?.message ||
        error.response?.data?.errors?.Notes?.[0] ||
        'Teslimat tamamlanırken bir hata oluştu.';

      Alert.alert('Hata', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Fotoğraf galerisi render
  const renderPhotoItem = ({ item, index }: { item: PhotoItem; index: number }) => (
    <TouchableOpacity
      style={styles.photoThumbnail}
      onPress={() => setSelectedPhotoIndex(index)}
    >
      <Image source={{ uri: item.uri }} style={styles.photoThumbnailImage} />
      <TouchableOpacity
        style={styles.photoDeleteButton}
        onPress={() => handleDeletePhoto(index)}
      >
        <Icon name="close-circle" size={20} color="#EF4444" />
      </TouchableOpacity>
      <View style={styles.photoNumber}>
        <Text style={styles.photoNumberText}>{index + 1}</Text>
      </View>
    </TouchableOpacity>
  );

  return (
    <>
      <ScrollView style={styles.container}>
        {/* Offline göstergesi */}
        {!isOnline && (
          <Card style={styles.offlineCard}>
            <Card.Content>
              <View style={styles.offlineContent}>
                <Icon name="cloud-off-outline" size={20} color="#EF4444" />
                <Text style={styles.offlineText}>
                  Çevrimdışı moddasınız. İşlemler kaydedilecek ve bağlantı kurulduğunda gönderilecek.
                </Text>
              </View>
            </Card.Content>
          </Card>
        )}

        {/* Stop bilgileri */}
        <Card style={styles.infoCard}>
          <Card.Content>
            <Text style={styles.stopName}>{safeStopName}</Text>
            <Text style={styles.label}>Teslimat Durağı</Text>

            {/* YENİ: Konum güncelleme talebi butonu */}
            {customerId != null && (
              <Button
                mode="outlined"
                onPress={handleOpenLocationUpdateModal}
                style={styles.locationUpdateButton}
                icon="map-marker-question"
                textColor="#F59E0B"
              >
                Konum Yanlış mı?
              </Button>
            )}
          </Card.Content>
        </Card>

        {/* Teslim Alan Kişi */}
        <Card style={styles.card}>
          <Card.Content>
            <Text style={styles.sectionTitle}>
              Teslim Alan Kişi <Text style={styles.required}>*</Text>
            </Text>
            <TextInput
              mode="outlined"
              label="Ad Soyad"
              value={receiverName}
              onChangeText={(text) => {
                setReceiverName(text);
                setValidationError(null);
              }}
              style={styles.input}
              placeholder="Örn: Ahmet Yılmaz"
              error={!!validationError && !receiverName.trim()}
            />
            {validationError && !receiverName.trim() && (
              <HelperText type="error" visible={true}>
                Teslim alan kişinin adını girmelisiniz (en az 3 karakter)
              </HelperText>
            )}
          </Card.Content>
        </Card>

        {/* Notlar */}
        <Card style={styles.card}>
          <Card.Content>
            <Text style={styles.sectionTitle}>
              Teslimat Notları <Text style={styles.required}>*</Text>
            </Text>
            <TextInput
              mode="outlined"
              label="Not ekleyin"
              value={notes}
              onChangeText={(text) => {
                setNotes(text);
                setValidationError(null);
              }}
              multiline
              numberOfLines={3}
              style={styles.input}
              placeholder="Örn: Paket müşteriye teslim edildi, kapıcıya bırakıldı vb."
              error={!!validationError && (!notes || notes.trim().length < 3)}
            />
            {validationError && (!notes || notes.trim().length < 3) && (
              <HelperText type="error" visible={true}>
                En az 3 karakterlik bir not girmelisiniz
              </HelperText>
            )}
          </Card.Content>
        </Card>

        {/* İmza */}
        <Card style={styles.card}>
          <Card.Content>
            <View style={styles.sectionHeader}>
              <Text style={styles.sectionTitle}>
                Alıcı İmzası {(requiresProof || signatureRequired) && <Text style={styles.required}>*</Text>}
              </Text>
              {signature && (
                <TouchableOpacity onPress={handleClearSignature}>
                  <Text style={styles.clearButton}>Temizle</Text>
                </TouchableOpacity>
              )}
            </View>

            {signature ? (
              <View style={styles.signatureContainer}>
                <Image
                  source={{ uri: signature }}
                  style={styles.signatureImage}
                  resizeMode="contain"
                />
                <IconButton
                  icon="pencil"
                  size={20}
                  style={styles.editButton}
                  onPress={handleOpenSignatureModal}
                />
              </View>
            ) : (
              <TouchableOpacity
                style={[
                  styles.signaturePlaceholder,
                  validationError && (requiresProof || signatureRequired) && !signature && styles.errorBorder
                ]}
                onPress={handleOpenSignatureModal}
              >
                <Icon name="draw" size={40} color="#999" />
                <Text style={styles.placeholderText}>İmza almak için dokunun</Text>
              </TouchableOpacity>
            )}
            {validationError && (requiresProof || signatureRequired) && !signature && (
              <HelperText type="error" visible={true}>
                Bu teslimat için imza zorunludur
              </HelperText>
            )}
          </Card.Content>
        </Card>

        {/* Çoklu Fotoğraf */}
        <Card style={styles.card}>
          <Card.Content>
            <View style={styles.sectionHeader}>
              <Text style={styles.sectionTitle}>
                Teslimat Fotoğrafları ({photos.length}/{MAX_PHOTOS})
                {(requiresProof || photoRequired) ? <Text style={styles.required}> *</Text> : ' (Opsiyonel)'}
              </Text>
              {photos.length > 0 && (
                <TouchableOpacity onPress={() => setPhotos([])}>
                  <Text style={styles.clearButton}>Tümünü Sil</Text>
                </TouchableOpacity>
              )}
            </View>

            {photoLoading ? (
              <View style={styles.photoPlaceholder}>
                <ActivityIndicator size="large" color="#3B82F6" />
                <Text style={styles.placeholderText}>Fotoğraf yükleniyor...</Text>
              </View>
            ) : (
              <>
                {photos.length > 0 && (
                  <FlatList
                    data={photos}
                    renderItem={renderPhotoItem}
                    keyExtractor={(_, index) => index.toString()}
                    horizontal
                    showsHorizontalScrollIndicator={false}
                    style={styles.photoGallery}
                  />
                )}

                {photos.length < MAX_PHOTOS && (
                  <TouchableOpacity
                    style={[
                      styles.addPhotoButton,
                      validationError && (requiresProof || photoRequired) && photos.length === 0 && styles.errorBorder
                    ]}
                    onPress={showPhotoOptions}
                  >
                    <Icon name="camera-plus" size={32} color="#3B82F6" />
                    <Text style={styles.addPhotoText}>
                      {photos.length === 0 ? 'Fotoğraf Ekle' : 'Daha Fazla Ekle'}
                    </Text>
                  </TouchableOpacity>
                )}
              </>
            )}

            {validationError && (requiresProof || photoRequired) && photos.length === 0 && (
              <HelperText type="error" visible={true}>
                Bu teslimat için en az bir fotoğraf zorunludur
              </HelperText>
            )}
          </Card.Content>
        </Card>

        {/* Validasyon hatası genel mesajı */}
        {validationError && (
          <Card style={[styles.card, styles.errorCard]}>
            <Card.Content>
              <View style={styles.errorContent}>
                <Icon name="alert-circle" size={20} color="#EF4444" />
                <Text style={styles.errorText}>{validationError}</Text>
              </View>
            </Card.Content>
          </Card>
        )}

        {/* Aksiyon butonları */}
        <View style={styles.actions}>
          <Button
            mode="outlined"
            onPress={() => navigation.goBack()}
            style={styles.cancelButton}
            disabled={loading}
          >
            İptal
          </Button>
          <Button
            mode="contained"
            onPress={handleComplete}
            style={styles.completeButton}
            buttonColor={!isOnline ? '#F59E0B' : '#10B981'}
            loading={loading}
            disabled={loading}
            icon={!isOnline ? "cloud-off-outline" : "check"}
          >
            {!isOnline ? 'Çevrimdışı Kaydet' : 'Teslimatı Tamamla'}
          </Button>
        </View>
      </ScrollView>

      {/* İmza Modal */}
      <Modal
        visible={showSignatureModal}
        animationType="slide"
        onRequestClose={() => setShowSignatureModal(false)}
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>İmza Ekranı</Text>
            <IconButton
              icon="close"
              size={24}
              onPress={() => setShowSignatureModal(false)}
            />
          </View>

          <View style={styles.signatureWrapper}>
            <Text style={styles.signatureInstruction}>İmzanızı aşağıdaki alana çizin</Text>

            <View style={styles.signatureCanvasContainer}>
              <SignatureCanvas
                ref={signatureRef}
                onOK={handleSignatureOK}
                onEmpty={() => Alert.alert('Hata', 'Lütfen imza atın')}
                onClear={() => console.log('İmza temizlendi')}
                descriptionText=""
                clearText="Temizle"
                confirmText="Onayla"
                webStyle={`
                  .m-signature-pad {
                    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
                    border: 2px solid #e5e5e5;
                    border-radius: 8px;
                    margin: 0;
                  }
                  .m-signature-pad--body {
                    border: none;
                    background-color: #fff;
                  }
                  .m-signature-pad--body canvas {
                    border-radius: 6px;
                  }
                  .m-signature-pad--footer {
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                    padding: 12px 16px;
                    background-color: #f9f9f9;
                    border-top: 1px solid #e5e5e5;
                    border-radius: 0 0 6px 6px;
                  }
                  .button {
                    background-color: #3B82F6;
                    color: white;
                    border: none;
                    padding: 10px 24px;
                    border-radius: 6px;
                    font-size: 15px;
                    font-weight: 600;
                    min-width: 100px;
                    cursor: pointer;
                    transition: background-color 0.2s;
                  }
                  .button:hover {
                    background-color: #2563EB;
                  }
                  .button.clear {
                    background-color: #EF4444;
                  }
                  .button.clear:hover {
                    background-color: #DC2626;
                  }
                  body {
                    background-color: transparent;
                    margin: 0;
                    padding: 0;
                  }
                `}
                backgroundColor="transparent"
                penColor="black"
                minWidth={1.5}
                maxWidth={3.5}
                imageType="image/png"
                trimWhitespace={true}
              />
            </View>

            <Text style={styles.signatureHint}>
              Parmağınız veya kalem ile imzanızı atabilirsiniz
            </Text>
          </View>
        </View>
      </Modal>

      {/* YENİ: Konum Güncelleme Talebi Modal */}
      <Modal
        visible={locationUpdate.visible}
        animationType="slide"
        onRequestClose={() => setLocationUpdate({ ...locationUpdate, visible: false })}
      >
        <View style={styles.modalContainer}>
          <View style={styles.modalHeader}>
            <Text style={styles.modalTitle}>Konum Güncelleme Talebi</Text>
            <IconButton
              icon="close"
              size={24}
              onPress={() => setLocationUpdate({ ...locationUpdate, visible: false })}
            />
          </View>

          <ScrollView style={styles.locationUpdateContent}>
            <Card style={styles.locationCard}>
              <Card.Content>
                <Text style={styles.locationTitle}>Mevcut Müşteri Adresi:</Text>
                <Text style={styles.locationAddress}>{String(currentAddress || safeStopName)}</Text>
                
                {locationUpdate.currentLocation && (
                  <>
                    <Text style={styles.locationTitle}>Sizin Konumunuz:</Text>
                    <Text style={styles.locationCoords}>
                      Enlem: {locationUpdate.currentLocation.latitude.toFixed(6)}
                    </Text>
                    <Text style={styles.locationCoords}>
                      Boylam: {locationUpdate.currentLocation.longitude.toFixed(6)}
                    </Text>
                  </>
                )}
              </Card.Content>
            </Card>

            <Card style={styles.locationCard}>
              <Card.Content>
                <Text style={styles.locationTitle}>
                  Güncelleme Nedeni <Text style={styles.required}>*</Text>
                </Text>
                <TextInput
                  mode="outlined"
                  value={locationUpdate.reason}
                  onChangeText={(text) => setLocationUpdate({ ...locationUpdate, reason: text })}
                  multiline
                  numberOfLines={4}
                  placeholder="Örn: Müşteri adresi yanlış, doğru konum karşı sokakta 50 metre ileride."
                  style={styles.input}
                />
                <HelperText type="info" visible={true}>
                  Mümkün olduğunca detaylı açıklama yazın. Bu talep yöneticinize gönderilecek.
                </HelperText>
              </Card.Content>
            </Card>

            <View style={styles.locationUpdateActions}>
              <Button
                mode="outlined"
                onPress={() => setLocationUpdate({ ...locationUpdate, visible: false })}
                style={styles.cancelButton}
                disabled={locationUpdate.loading}
              >
                İptal
              </Button>
              <Button
                mode="contained"
                onPress={handleSubmitLocationUpdate}
                style={styles.submitButton}
                buttonColor="#F59E0B"
                loading={locationUpdate.loading}
                disabled={locationUpdate.loading || !locationUpdate.reason.trim()}
                icon="send"
              >
                Talep Gönder
              </Button>
            </View>
          </ScrollView>
        </View>
      </Modal>

      {/* Fotoğraf Görüntüleme Modal */}
      {selectedPhotoIndex !== null && (
        <Modal
          visible={true}
          transparent={true}
          animationType="fade"
          onRequestClose={() => setSelectedPhotoIndex(null)}
        >
          <TouchableOpacity
            style={styles.photoViewModal}
            activeOpacity={1}
            onPress={() => setSelectedPhotoIndex(null)}
          >
            <Image
              source={{ uri: photos[selectedPhotoIndex].uri }}
              style={styles.photoViewImage}
              resizeMode="contain"
            />
            <TouchableOpacity
              style={styles.photoViewClose}
              onPress={() => setSelectedPhotoIndex(null)}
            >
              <Icon name="close" size={30} color="white" />
            </TouchableOpacity>
            <View style={styles.photoViewCounter}>
              <Text style={styles.photoViewCounterText}>
                {selectedPhotoIndex + 1} / {photos.length}
              </Text>
            </View>
          </TouchableOpacity>
        </Modal>
      )}
    </>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  offlineCard: {
    margin: 16,
    marginBottom: 8,
    backgroundColor: '#FEF3C7',
    elevation: 2,
  },
  offlineContent: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  offlineText: {
    fontSize: 13,
    color: '#92400E',
    marginLeft: 8,
    flex: 1,
  },
  infoCard: {
    margin: 16,
    marginBottom: 8,
    elevation: 2,
  },
  card: {
    marginHorizontal: 16,
    marginVertical: 8,
    elevation: 2,
  },
  errorCard: {
    backgroundColor: '#FEE2E2',
  },
  stopName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  label: {
    fontSize: 14,
    color: '#666',
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
  },
  required: {
    color: '#EF4444',
  },
  clearButton: {
    color: '#3B82F6',
    fontSize: 14,
    fontWeight: '600',
  },
  input: {
    backgroundColor: 'white',
  },
  signatureContainer: {
    height: 150,
    borderWidth: 1,
    borderColor: '#e5e5e5',
    borderRadius: 8,
    backgroundColor: 'white',
    padding: 8,
    position: 'relative',
  },
  signaturePlaceholder: {
    height: 150,
    borderWidth: 2,
    borderColor: '#e5e5e5',
    borderStyle: 'dashed',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#fafafa',
  },
  signatureImage: {
    width: '100%',
    height: '100%',
  },
  photoGallery: {
    marginBottom: 12,
  },
  photoThumbnail: {
    width: 100,
    height: 100,
    marginRight: 8,
    borderRadius: 8,
    overflow: 'hidden',
    position: 'relative',
  },
  photoThumbnailImage: {
    width: '100%',
    height: '100%',
  },
  photoDeleteButton: {
    position: 'absolute',
    top: 4,
    right: 4,
    backgroundColor: 'white',
    borderRadius: 10,
  },
  photoNumber: {
    position: 'absolute',
    bottom: 4,
    left: 4,
    backgroundColor: 'rgba(0,0,0,0.7)',
    borderRadius: 10,
    paddingHorizontal: 6,
    paddingVertical: 2,
  },
  photoNumberText: {
    color: 'white',
    fontSize: 10,
    fontWeight: '600',
  },
  addPhotoButton: {
    height: 100,
    borderWidth: 2,
    borderColor: '#3B82F6',
    borderStyle: 'dashed',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#EBF5FF',
  },
  addPhotoText: {
    fontSize: 14,
    color: '#3B82F6',
    marginTop: 4,
    fontWeight: '500',
  },
  placeholderText: {
    fontSize: 14,
    color: '#999',
    marginTop: 8,
  },
  errorBorder: {
    borderColor: '#EF4444',
  },
  errorContent: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  errorText: {
    fontSize: 14,
    color: '#EF4444',
    marginLeft: 8,
    flex: 1,
  },
  actions: {
    flexDirection: 'row',
    padding: 16,
    gap: 12,
  },
  cancelButton: {
    flex: 1,
  },
  completeButton: {
    flex: 1,
  },
  editButton: {
    position: 'absolute',
    top: 4,
    right: 4,
    backgroundColor: 'rgba(255,255,255,0.9)',
    borderRadius: 20,
  },
  modalContainer: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: 'white',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: '#e5e5e5',
    elevation: 2,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  signatureWrapper: {
    flex: 1,
    padding: 20,
    justifyContent: 'center',
    maxHeight: 500,
  },
  signatureInstruction: {
    fontSize: 15,
    color: '#333',
    textAlign: 'center',
    marginBottom: 20,
    fontWeight: '500',
  },
  signatureCanvasContainer: {
    height: 350,
    width: '100%',
    backgroundColor: 'white',
    borderRadius: 12,
    elevation: 3,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    overflow: 'hidden',
  },
  signatureHint: {
    fontSize: 13,
    color: '#666',
    textAlign: 'center',
    marginTop: 20,
    fontStyle: 'italic',
  },
  photoPlaceholder: {
    height: 100,
    justifyContent: 'center',
    alignItems: 'center',
  },
  photoViewModal: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.9)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  photoViewImage: {
    width: screenWidth,
    height: screenWidth,
  },
  photoViewClose: {
    position: 'absolute',
    top: 50,
    right: 20,
    padding: 10,
  },
  photoViewCounter: {
    position: 'absolute',
    bottom: 50,
    backgroundColor: 'rgba(0,0,0,0.7)',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
  photoViewCounterText: {
    color: 'white',
    fontSize: 14,
    fontWeight: '600',
  },
  // YENİ: Konum güncelleme stilleri
  locationUpdateButton: {
    marginTop: 12,
    borderColor: '#F59E0B',
  },
  locationUpdateContent: {
    flex: 1,
    padding: 16,
  },
  locationCard: {
    marginBottom: 16,
    elevation: 2,
  },
  locationTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginBottom: 8,
  },
  locationAddress: {
    fontSize: 14,
    color: '#666',
    marginBottom: 12,
  },
  locationCoords: {
    fontSize: 12,
    color: '#999',
    marginTop: 4,
  },
  locationUpdateActions: {
    flexDirection: 'row',
    padding: 16,
    gap: 12,
  },
  submitButton: {
    flex: 1,
  },
});

export default CompleteStopScreen;
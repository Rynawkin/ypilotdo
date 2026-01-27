import React, { useState, useEffect } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Alert,
  TouchableOpacity,
  Dimensions,
} from 'react-native';
import {
  TextInput,
  Button,
  Card,
  Text,
  HelperText,
  Chip,
  SegmentedButtons,
  ActivityIndicator,
  Switch,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation, useRoute } from '@react-navigation/native';
import { Customer, CreateCustomerRequest } from '../../types/customer.types';
import customerService from '../../services/customerService';
import offlineQueueService from '../../services/offlineQueueService';
import networkService from '../../services/networkService';
import locationService from '../../services/locationService';
import permissionService from '../../services/permissionService';
import { isValidPhone10, isValidEmail, isValidLatLng } from '../../utils/validators';
import { isLargeScreen } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';
import GooglePlacesComponent from '../../components/SimpleGooglePlaces';

// Telefon numarasını WhatsApp formatına çevir
const formatPhoneForWhatsApp = (phone: string): string => {
  if (!phone) return '';
  // Tüm boşluk, parantez, tire gibi karakterleri temizle
  let cleaned = phone.replace(/[\s\(\)\-\+]/g, '');
  // Başındaki sıfırı kaldır
  if (cleaned.startsWith('0')) {
    cleaned = cleaned.substring(1);
  }
  // +90 ile başlıyorsa kaldır
  if (cleaned.startsWith('90') && cleaned.length > 10) {
    cleaned = cleaned.substring(2);
  }
  return cleaned;
};

interface RouteParams {
  customerId: number;
}

const EditCustomerScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute();
  const { customerId } = route.params as RouteParams;
  
  const [formData, setFormData] = useState<CreateCustomerRequest>({
    code: '',
    name: '',
    address: '',
    phone: '',
    email: '',
    whatsApp: '',
    whatsAppOptIn: false,
    whatsAppVerified: false,
    whatsAppOptInDate: undefined,
    latitude: undefined,
    longitude: undefined,
    priority: 'normal',
    estimatedServiceTime: undefined,
    notes: '',
    tags: [],
    timeWindow: undefined,
  });

  const [originalCustomer, setOriginalCustomer] = useState<Customer | null>(null);
  const [loading, setLoading] = useState(false);
  const [initialLoading, setInitialLoading] = useState(true);
  const [locationLoading, setLocationLoading] = useState(false);
  const [isOnline, setIsOnline] = useState(true);
  const [tagInput, setTagInput] = useState('');
  const [showTimeWindow, setShowTimeWindow] = useState(false);
  const [timeWindowStart, setTimeWindowStart] = useState('09:00');
  const [timeWindowEnd, setTimeWindowEnd] = useState('17:00');
  const [useGooglePlaces, setUseGooglePlaces] = useState(false);

  const [errors, setErrors] = useState<{[key: string]: string}>({});

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

  useEffect(() => {
    loadCustomer();
  }, [customerId]);

  const loadCustomer = async () => {
    try {
      setInitialLoading(true);
      const customer = await customerService.getById(customerId);
      setOriginalCustomer(customer);
      
      setFormData({
        code: customer.code,
        name: customer.name,
        address: customer.address || '',
        phone: customer.phone || '',
        email: customer.email || '',
        whatsApp: customer.whatsApp || formatPhoneForWhatsApp(customer.phone || ''),
        whatsAppOptIn: customer.whatsAppOptIn || false,
        whatsAppVerified: customer.whatsAppVerified || false,
        whatsAppOptInDate: customer.whatsAppOptInDate,
        latitude: customer.latitude,
        longitude: customer.longitude,
        priority: customer.priority || 'normal',
        estimatedServiceTime: customer.estimatedServiceTime,
        notes: customer.notes || '',
        tags: customer.tags || [],
        timeWindow: customer.timeWindow,
      });

      if (customer.timeWindow) {
        setShowTimeWindow(true);
        setTimeWindowStart(customer.timeWindow.start);
        setTimeWindowEnd(customer.timeWindow.end);
      }
    } catch (error: any) {
      console.error('Load customer error:', error);
      const errorMessage = error.userFriendlyMessage || error.message || 'Müşteri bilgileri yüklenemedi.';
      Alert.alert(
        'Hata',
        errorMessage,
        [
          { text: 'Tekrar Dene', onPress: loadCustomer },
          { text: 'Geri', onPress: () => navigation.goBack() }
        ]
      );
    } finally {
      setInitialLoading(false);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: {[key: string]: string} = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Müşteri adı zorunludur';
    }

    if (!formData.code.trim()) {
      newErrors.code = 'Müşteri kodu zorunludur';
    }

    if (formData.phone && !isValidPhone10(formData.phone)) {
      newErrors.phone = 'Telefon numarası 10 haneli olmalıdır (örn: 5321234567)';
    }

    if (formData.email && !isValidEmail(formData.email)) {
      newErrors.email = 'Geçerli bir e-posta adresi girin';
    }

    if (formData.latitude !== undefined && formData.longitude !== undefined) {
      if (!isValidLatLng(formData.latitude, formData.longitude)) {
        newErrors.location = 'Geçersiz koordinat değerleri';
      }
    }

    if (formData.estimatedServiceTime !== undefined && (formData.estimatedServiceTime <= 0 || formData.estimatedServiceTime > 1440)) {
      newErrors.estimatedServiceTime = 'Hizmet süresi 1-1440 dakika arasında olmalıdır';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleUseCurrentLocation = async () => {
    try {
      setLocationLoading(true);
      
      const hasPermission = await permissionService.checkLocationPermission();
      if (!hasPermission) {
        Alert.alert('İzin Gerekli', 'Konum erişimi için izin vermeniz gerekiyor.');
        return;
      }

      const location = await locationService.getCurrentLocation();
      setFormData({
        ...formData,
        latitude: location.latitude,
        longitude: location.longitude,
      });
      
      Alert.alert('Başarılı', 'Cihaz konumu başarıyla eklendi');
    } catch (error: any) {
      console.error('Konum alınamadı:', error);
      const errorMessage = error.userFriendlyMessage || error.message || 'Konum alınamadı. Lütfen konum ayarlarınızı kontrol edin.';
      Alert.alert('Hata', errorMessage);
    } finally {
      setLocationLoading(false);
    }
  };

  const handleAddTag = () => {
    if (tagInput.trim() && !formData.tags?.includes(tagInput.trim())) {
      setFormData({
        ...formData,
        tags: [...(formData.tags || []), tagInput.trim()],
      });
      setTagInput('');
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setFormData({
      ...formData,
      tags: formData.tags?.filter(tag => tag !== tagToRemove) || [],
    });
  };

  const handleTimeWindowToggle = (value: boolean) => {
    setShowTimeWindow(value);
    if (value) {
      setFormData({
        ...formData,
        timeWindow: {
          start: timeWindowStart,
          end: timeWindowEnd,
        },
      });
    } else {
      setFormData({
        ...formData,
        timeWindow: undefined,
      });
    }
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    try {
      setLoading(true);

      // WhatsApp numarası formatını temizle
      let whatsAppNumber = formData.whatsApp;
      if (!whatsAppNumber && formData.phone) {
        whatsAppNumber = formatPhoneForWhatsApp(formData.phone);
      }

      const submitData: CreateCustomerRequest = {
        ...formData,
        whatsApp: whatsAppNumber,
        tags: formData.tags?.length ? formData.tags : undefined,
        timeWindow: showTimeWindow ? { start: timeWindowStart, end: timeWindowEnd } : undefined,
      };

      if (isOnline) {
        await customerService.update(customerId, submitData);
        Alert.alert('Başarılı', 'Müşteri başarıyla güncellendi');
        navigation.goBack();
      } else {
        await offlineQueueService.addToQueue({
          type: 'CUSTOMER_CREATE', // Offline queue sadece CREATE destekliyor, UPDATE için de CREATE kullanıyoruz
          data: { ...submitData, id: customerId },
          journeyId: 0, // Customer update için gerekli değil
        });
        Alert.alert('Taslak Kaydedildi', 'İnternet bağlantısı gelince otomatik olarak gönderilecek');
        navigation.goBack();
      }
    } catch (error: any) {
      console.error('Update customer error:', error);
      
      // userFriendlyMessage öncelikli hata mesajını kullan
      const errorMessage = error.userFriendlyMessage || 
        error.response?.data?.message || 
        error.response?.data?.error || 
        error.message || 
        'Müşteri güncellenirken bir hata oluştu';
      
      Alert.alert('Hata', errorMessage, [{ text: 'Tamam' }]);
    } finally {
      setLoading(false);
    }
  };

  if (initialLoading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Müşteri bilgileri yükleniyor...</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      {/* Temel Bilgiler */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Temel Bilgiler</Text>
          
          <TextInput
            label="Müşteri Kodu"
            value={formData.code}
            style={[styles.input, styles.disabledInput]}
            mode="outlined"
            disabled
            editable={false}
            right={
              <TextInput.Icon 
                icon={() => <Icon name="lock" size={16} color="#6B7280" />} 
              />
            }
          />
          <HelperText type="info" visible={true}>
            Müşteri kodu düzenlenemez
          </HelperText>

          <TextInput
            label="Müşteri Adı *"
            value={formData.name}
            onChangeText={(text) => setFormData({ ...formData, name: text })}
            style={styles.input}
            error={!!errors.name}
            mode="outlined"
          />
          <HelperText type="error" visible={!!errors.name}>
            {errors.name}
          </HelperText>

          <Text style={styles.fieldLabel}>Öncelik</Text>
          <SegmentedButtons
            value={formData.priority || 'normal'}
            onValueChange={(value) => setFormData({ ...formData, priority: value as any })}
            buttons={[
              { value: 'low', label: 'Düşük' },
              { value: 'normal', label: 'Normal' },
              { value: 'high', label: 'Yüksek' },
            ]}
            style={styles.segmentedButton}
          />
        </Card.Content>
      </Card>

      {/* İletişim Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>İletişim Bilgileri</Text>
          
          <TextInput
            label="Telefon"
            value={formData.phone}
            onChangeText={(text) => {
              setFormData({ 
                ...formData, 
                phone: text,
                // Telefon değiştiğinde WhatsApp'ı da güncelle (eğer WhatsApp boşsa)
                whatsApp: formData.whatsApp || formatPhoneForWhatsApp(text)
              });
            }}
            style={styles.input}
            error={!!errors.phone}
            mode="outlined"
            keyboardType="phone-pad"
            placeholder="5321234567"
          />
          <HelperText type="error" visible={!!errors.phone}>
            {errors.phone}
          </HelperText>

          {/* WhatsApp Alanı */}
          <TextInput
            label="WhatsApp Numarası"
            value={formData.whatsApp}
            onChangeText={(text) => {
              // Sadece rakam girişine izin ver
              const value = text.replace(/[^\d]/g, '');
              setFormData({ ...formData, whatsApp: value });
            }}
            onBlur={() => {
              // Eğer boşsa ve telefon varsa, telefonu WhatsApp formatında ekle
              if (!formData.whatsApp && formData.phone) {
                setFormData({
                  ...formData,
                  whatsApp: formatPhoneForWhatsApp(formData.phone)
                });
              }
            }}
            style={styles.input}
            mode="outlined"
            keyboardType="phone-pad"
            placeholder="5321234567"
            maxLength={10}
          />
          <HelperText type="info" visible={true}>
            Başında 0 olmadan, sadece rakamlardan oluşan 10 haneli numara
          </HelperText>

          <TextInput
            label="E-posta"
            value={formData.email}
            onChangeText={(text) => setFormData({ ...formData, email: text })}
            style={styles.input}
            error={!!errors.email}
            mode="outlined"
            keyboardType="email-address"
            autoCapitalize="none"
          />
          <HelperText type="error" visible={!!errors.email}>
            {errors.email}
          </HelperText>
        </Card.Content>
      </Card>

      {/* WhatsApp Bildirimleri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>WhatsApp Bildirimleri</Text>
          
          {/* WhatsApp Opt-in */}
          <View style={styles.switchRow}>
            <View style={styles.switchContent}>
              <Text style={styles.switchLabel}>WhatsApp bildirimleri gönder</Text>
              <Text style={styles.switchHelper}>
                Müşteri teslimat bildirimleri için WhatsApp mesajı almayı kabul ediyor
              </Text>
            </View>
            <Switch
              value={formData.whatsAppOptIn || false}
              onValueChange={(value) => {
                setFormData({
                  ...formData,
                  whatsAppOptIn: value,
                  whatsAppOptInDate: value && !formData.whatsAppOptInDate ? new Date().toISOString() : formData.whatsAppOptInDate
                });
              }}
              color="#25D366"
            />
          </View>

          {/* WhatsApp Verification Status - sadece opt-in aktifse göster */}
          {formData.whatsAppOptIn && (
            <View style={styles.switchRow}>
              <View style={styles.switchContent}>
                <Text style={styles.switchLabel}>Numara doğrulandı</Text>
                <Text style={styles.switchHelper}>
                  WhatsApp numarası müşteri tarafından doğrulandı
                </Text>
              </View>
              <Switch
                value={formData.whatsAppVerified || false}
                onValueChange={(value) => setFormData({ ...formData, whatsAppVerified: value })}
                color="#2196F3"
              />
            </View>
          )}

          {/* Onay tarihi gösterimi */}
          {formData.whatsAppOptIn && formData.whatsAppOptInDate && (
            <View style={styles.infoBox}>
              <Text style={styles.infoText}>
                Onay tarihi: {new Date(formData.whatsAppOptInDate).toLocaleDateString('tr-TR', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </Text>
            </View>
          )}
        </Card.Content>
      </Card>

      {/* İletişim Bilgileri devamı */}
      <Card style={styles.card}>
        <Card.Content>

          {/* Google Places Toggle */}
          <View style={styles.switchRow}>
            <Text style={styles.switchLabel}>
              <Icon name="google-maps" size={16} color="#3B82F6" /> Google İşletme Araması
            </Text>
            <Switch
              value={useGooglePlaces}
              onValueChange={setUseGooglePlaces}
            />
          </View>

          {useGooglePlaces ? (
            <View style={styles.autocompleteContainer}>
              <Text style={styles.fieldLabel}>
                <Icon name="magnify" size={14} color="#6B7280" /> İşletme Ara (Google)
              </Text>
              <GooglePlacesComponent
                placeholder="İşletme adı yazın (örn: Migros, Starbucks)"
                onPlaceSelect={(place, details) => {
                  console.log('GooglePlaces onPlaceSelect:', { place, details });
                  if (details) {
                    // Web implementasyonuna uygun veri işleme
                    const businessName = details.name || place.structured_formatting?.main_text || '';
                    
                    // Müşteri adını güncelle
                    setFormData(prev => ({ ...prev, name: businessName }));
                    
                    // Adresi güncelle
                    setFormData(prev => ({
                      ...prev,
                      address: details.formatted_address || '',
                      latitude: details.geometry?.location?.lat,
                      longitude: details.geometry?.location?.lng
                    }));
                    
                    // Telefon numarasını güncelle (varsa)
                    if (details.formatted_phone_number || details.international_phone_number) {
                      const phone = details.international_phone_number || details.formatted_phone_number || '';
                      const cleanPhone = phone.replace(/\D/g, '').slice(-10); // Son 10 haneli
                      setFormData(prev => ({
                        ...prev,
                        phone: cleanPhone,
                        // WhatsApp numarası da güncellenir
                        whatsApp: prev.whatsApp || formatPhoneForWhatsApp(cleanPhone)
                      }));
                    }
                    
                    // Website'i notlara ekle (varsa)
                    if (details.website) {
                      const websiteNote = `Website: ${details.website}`;
                      setFormData(prev => {
                        let newNotes = prev.notes || '';
                        if (newNotes.trim()) {
                          newNotes = `${newNotes.trim()}\n${websiteNote}`;
                        } else {
                          newNotes = websiteNote;
                        }
                        return { ...prev, notes: newNotes };
                      });
                    }
                    
                    // İşletme türüne göre otomatik tag ekle
                    if (details.types) {
                      const typeMapping: Record<string, string> = {
                        'restaurant': 'Restoran',
                        'food': 'Yemek',
                        'store': 'Mağaza', 
                        'supermarket': 'Süpermarket',
                        'gas_station': 'Benzin İstasyonu',
                        'hospital': 'Hastane',
                        'pharmacy': 'Eczane',
                        'bank': 'Banka',
                        'atm': 'ATM',
                        'shopping_mall': 'AVM',
                        'cafe': 'Kafe',
                        'bakery': 'Fırın',
                        'clothing_store': 'Giyim',
                        'electronics_store': 'Elektronik',
                      };

                      const newTags = details.types
                        .map((type: string) => typeMapping[type])
                        .filter(Boolean)
                        .slice(0, 2); // İlk 2 tag

                      if (newTags.length > 0) {
                        setFormData(prev => ({
                          ...prev,
                          tags: [...(prev.tags || []), ...newTags].filter((tag, index, arr) => 
                            arr.indexOf(tag) === index // Dublicate'leri kaldır
                          )
                        }));
                      }
                    }
                  }
                }}
              />
              <Text style={styles.googleHelperText}>
                <Icon name="information" size={12} color="#3B82F6" /> İşletme seçtiğinizde adres, telefon ve konum bilgileri otomatik doldurulacak
              </Text>
            </View>
          ) : (
            <TextInput
              label="Adres"
              value={formData.address}
              onChangeText={(text) => setFormData({ ...formData, address: text })}
              style={styles.input}
              mode="outlined"
              multiline
              numberOfLines={3}
            />
          )}
        </Card.Content>
      </Card>

      {/* Konum Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <View style={styles.sectionHeader}>
            <Text style={styles.sectionTitle}>Konum Bilgileri</Text>
            <Button
              mode="outlined"
              onPress={handleUseCurrentLocation}
              loading={locationLoading}
              disabled={locationLoading}
              compact
            >
              <Icon name="crosshairs-gps" size={16} />
              {locationLoading ? ' Alınıyor...' : ' Cihaz Konumu'}
            </Button>
          </View>

          <View style={styles.coordinateRow}>
            <TextInput
              label="Enlem"
              value={formData.latitude?.toString() || ''}
              onChangeText={(text) => setFormData({ 
                ...formData, 
                latitude: text ? parseFloat(text) : undefined 
              })}
              style={[styles.input, styles.halfInput]}
              mode="outlined"
              keyboardType="numeric"
              placeholder="-90 ile 90 arası"
            />
            <TextInput
              label="Boylam"
              value={formData.longitude?.toString() || ''}
              onChangeText={(text) => setFormData({ 
                ...formData, 
                longitude: text ? parseFloat(text) : undefined 
              })}
              style={[styles.input, styles.halfInput]}
              mode="outlined"
              keyboardType="numeric"
              placeholder="-180 ile 180 arası"
            />
          </View>
          <HelperText type="error" visible={!!errors.location}>
            {errors.location}
          </HelperText>
        </Card.Content>
      </Card>

      {/* Hizmet Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Hizmet Bilgileri</Text>
          
          <TextInput
            label="Tahmini Hizmet Süresi (dakika)"
            value={formData.estimatedServiceTime?.toString() || ''}
            onChangeText={(text) => setFormData({ 
              ...formData, 
              estimatedServiceTime: text ? parseInt(text) : undefined 
            })}
            style={styles.input}
            error={!!errors.estimatedServiceTime}
            mode="outlined"
            keyboardType="numeric"
          />
          <HelperText type="error" visible={!!errors.estimatedServiceTime}>
            {errors.estimatedServiceTime}
          </HelperText>

          <View style={styles.switchRow}>
            <Text style={styles.switchLabel}>Zaman Penceresi Belirle</Text>
            <Switch
              value={showTimeWindow}
              onValueChange={handleTimeWindowToggle}
            />
          </View>

          {showTimeWindow && (
            <View style={styles.timeWindowRow}>
              <TextInput
                label="Başlangıç Saati"
                value={timeWindowStart}
                onChangeText={setTimeWindowStart}
                style={[styles.input, styles.halfInput]}
                mode="outlined"
                placeholder="09:00"
              />
              <TextInput
                label="Bitiş Saati"
                value={timeWindowEnd}
                onChangeText={setTimeWindowEnd}
                style={[styles.input, styles.halfInput]}
                mode="outlined"
                placeholder="17:00"
              />
            </View>
          )}

          <TextInput
            label="Notlar"
            value={formData.notes}
            onChangeText={(text) => setFormData({ ...formData, notes: text })}
            style={styles.input}
            mode="outlined"
            multiline
            numberOfLines={3}
          />
        </Card.Content>
      </Card>

      {/* Etiketler */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Etiketler</Text>
          
          <View style={styles.tagInputRow}>
            <TextInput
              label="Yeni Etiket"
              value={tagInput}
              onChangeText={setTagInput}
              style={[styles.input, styles.tagInput]}
              mode="outlined"
            />
            <Button
              mode="outlined"
              onPress={handleAddTag}
              disabled={!tagInput.trim()}
              style={styles.addTagButton}
            >
              Ekle
            </Button>
          </View>

          {formData.tags && formData.tags.length > 0 && (
            <View style={styles.tagsContainer}>
              {formData.tags.map((tag, index) => (
                <Chip
                  key={index}
                  style={styles.tag}
                  onClose={() => handleRemoveTag(tag)}
                >
                  {tag}
                </Chip>
              ))}
            </View>
          )}
        </Card.Content>
      </Card>

      {/* Offline Indicator */}
      {!isOnline && (
        <Card style={[styles.card, styles.offlineCard]}>
          <Card.Content>
            <View style={styles.offlineIndicator}>
              <Icon name="wifi-off" size={20} color="#EF4444" />
              <Text style={styles.offlineText}>
                Çevrimdışı - Değişiklikler taslak olarak kaydedilecek
              </Text>
            </View>
          </Card.Content>
        </Card>
      )}

      {/* Submit Button */}
      <View style={styles.submitContainer}>
        <Button
          mode="contained"
          onPress={handleSubmit}
          loading={loading}
          disabled={loading}
          style={styles.submitButton}
        >
          {loading ? 'Güncelleniyor...' : isOnline ? 'Müşteriyi Güncelle' : 'Taslak Kaydet'}
        </Button>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  contentContainer: {
    padding: wp(4),
    paddingBottom: hp(4),
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#6B7280',
  },
  card: {
    marginBottom: hp(2),
    borderRadius: 12,
    elevation: 2,
    zIndex: 1, // Düşük zIndex - autocomplete'in arkasında kalacak
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: hp(1.5),
  },
  sectionHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: hp(1.5),
  },
  input: {
    marginBottom: hp(1),
    backgroundColor: 'white',
  },
  fieldLabel: {
    fontSize: 14,
    color: '#374151',
    marginBottom: hp(1),
    marginTop: hp(1),
  },
  segmentedButton: {
    marginBottom: hp(1),
  },
  coordinateRow: {
    flexDirection: 'row',
    gap: wp(2),
  },
  halfInput: {
    flex: 1,
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: hp(1.5),
  },
  switchLabel: {
    fontSize: 14,
    color: '#374151',
  },
  timeWindowRow: {
    flexDirection: 'row',
    gap: wp(2),
    marginBottom: hp(1.5),
  },
  tagInputRow: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: wp(2),
  },
  tagInput: {
    flex: 1,
  },
  addTagButton: {
    marginBottom: hp(1),
  },
  tagsContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
    marginTop: hp(1),
  },
  tag: {
    backgroundColor: '#EFF6FF',
  },
  offlineCard: {
    backgroundColor: '#FEF2F2',
    borderColor: '#FECACA',
    borderWidth: 1,
  },
  offlineIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  offlineText: {
    marginLeft: 8,
    color: '#DC2626',
    fontSize: 14,
  },
  submitContainer: {
    marginTop: hp(2),
  },
  submitButton: {
    paddingVertical: hp(0.8),
    backgroundColor: '#3B82F6',
  },
  autocompleteContainer: {
    marginBottom: hp(15), // Büyük margin - dropdown için alan bırak
    position: 'relative',
    zIndex: 1000, // Çok yüksek zIndex
    elevation: 1000, // Android için
    backgroundColor: 'transparent', // Şeffaf background
  },
  googleAutocompleteContainer: {
    flex: 0,
    width: '100%',
  },
  googleAutocompleteInput: {
    height: 56,
    borderColor: '#79747E',
    borderWidth: 1,
    borderRadius: 4,
    paddingHorizontal: 16,
    fontSize: 16,
    backgroundColor: 'white',
  },
  googleAutocompleteList: {
    maxHeight: 200,
    borderColor: '#79747E',
    borderWidth: 1,
    borderTopWidth: 0,
    borderRadius: 4,
    backgroundColor: 'white',
  },
  googleAutocompleteRow: {
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  googleAutocompleteText: {
    fontSize: 14,
    color: '#333',
  },
  googleHelperText: {
    fontSize: 12,
    color: '#3B82F6',
    marginTop: hp(0.5),
    fontStyle: 'italic',
  },
  switchContent: {
    flex: 1,
    marginRight: wp(3),
  },
  switchHelper: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 2,
  },
  infoBox: {
    backgroundColor: '#F3F4F6',
    padding: wp(3),
    borderRadius: 8,
    marginTop: hp(1),
  },
  infoText: {
    fontSize: 12,
    color: '#6B7280',
  },
  disabledInput: {
    backgroundColor: '#F9FAFB',
    opacity: 0.8,
  },
});

export default EditCustomerScreen;
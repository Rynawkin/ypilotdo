// C:\Projects\RotaAppMobile\src\screens\journeys\CompleteJourneyScreen.tsx

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Alert,
  ActivityIndicator,
  Platform,
  ActionSheetIOS,
  Pressable,
} from 'react-native';
import { Button, TextInput, Card, HelperText } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation } from '@react-navigation/native';
import { Picker } from '@react-native-picker/picker';
import journeyService from '../../services/journeyService';
import networkService from '../../services/networkService';
import offlineQueueService from '../../services/offlineQueueService';

interface RouteParams {
  journeyId: number;
  stopId: number;
  depotName?: string;
  depotAddress?: string;
}

const CompleteJourneyScreen: React.FC = () => {
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { journeyId, stopId, depotName, depotAddress } = route.params as RouteParams;

  const [endKilometer, setEndKilometer] = useState('');
  const [fuelLevel, setFuelLevel] = useState('');
  const [vehicleCondition, setVehicleCondition] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [isOnline, setIsOnline] = useState(true);

  const fuelLevelOptions: Array<{ label: string; value: string }> = [
    { label: 'Seçiniz...', value: '' },
    { label: 'Tam (Full)', value: 'full' },
    { label: '3/4', value: 'three_quarters' },
    { label: '1/2', value: 'half' },
    { label: '1/4', value: 'quarter' },
    { label: 'Boş', value: 'empty' },
  ];

  const vehicleConditionOptions: Array<{ label: string; value: string }> = [
    { label: 'Seçiniz...', value: '' },
    { label: 'İyi Durumda', value: 'good' },
    { label: 'Temizlik Gerekli', value: 'needs_cleaning' },
    { label: 'Bakım Gerekli', value: 'needs_maintenance' },
    { label: 'Hasar Var', value: 'damaged' },
  ];

  const getFuelLevelLabel = (value: string) =>
    fuelLevelOptions.find((o) => o.value === value)?.label ?? '';

  const getVehicleConditionLabel = (value: string) =>
    vehicleConditionOptions.find((o) => o.value === value)?.label ?? '';

  const openFuelLevelSheet = () => {
    if (Platform.OS !== 'ios') return;
    const labels = fuelLevelOptions.map((o) => o.label);
    const cancelButtonIndex = labels.length;
    ActionSheetIOS.showActionSheetWithOptions(
      {
        title: 'Yakıt Seviyesi',
        options: [...labels, 'İptal'],
        cancelButtonIndex,
      },
      (buttonIndex) => {
        if (buttonIndex === cancelButtonIndex) return;
        setFuelLevel(fuelLevelOptions[buttonIndex].value);
      }
    );
  };

  const openVehicleConditionSheet = () => {
    if (Platform.OS !== 'ios') return;
    const labels = vehicleConditionOptions.map((o) => o.label);
    const cancelButtonIndex = labels.length;
    ActionSheetIOS.showActionSheetWithOptions(
      {
        title: 'Araç Durumu',
        options: [...labels, 'İptal'],
        cancelButtonIndex,
      },
      (buttonIndex) => {
        if (buttonIndex === cancelButtonIndex) return;
        setVehicleCondition(vehicleConditionOptions[buttonIndex].value);
        setValidationError(null);
      }
    );
  };

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

  // Validasyon kontrolü
  const validateForm = (): boolean => {
    // Bitiş kilometresi zorunlu ve sayı olmalı
    const km = parseFloat(endKilometer);
    if (!endKilometer || isNaN(km) || km <= 0) {
      setValidationError('Geçerli bir bitiş kilometresi girmelisiniz.');
      return false;
    }

    // Araç durumu zorunlu
    if (!vehicleCondition) {
      setValidationError('Araç durumunu seçmelisiniz.');
      return false;
    }

    setValidationError(null);
    return true;
  };

  // Seferi tamamlama işlemi
  const handleCompleteJourney = async () => {
    if (!validateForm()) {
      return;
    }

    // Offline uyarısı
    if (!isOnline) {
      Alert.alert(
        'Çevrimdışı Mod',
        'Sefer bilgileri kaydedilecek ve internet bağlantısı kurulduğunda gönderilecek. Devam etmek istiyor musunuz?',
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
        'Seferi Tamamla',
        'Seferi tamamlamak istediğinize emin misiniz?',
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
        Alert.alert('Eksik Bilgi', validationError || 'Bitiş kilometresini girin');
        setLoading(false);
        return;
      }

      const completionData = {
        endKilometer: parseFloat(endKilometer),
        fuelLevel: fuelLevel || undefined,
        vehicleCondition: vehicleCondition || undefined,
        notes: notes.trim() || undefined,
      };

      // Offline durumda queue'ya ekle
      if (!isOnline) {
        try {
          await offlineQueueService.addToQueue({
            type: 'COMPLETE_JOURNEY',
            journeyId,
            stopId,
            data: completionData
          });

          Alert.alert(
            'Çevrimdışı Kaydedildi',
            'Sefer bilgileri kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
            [{ text: 'Tamam', onPress: () => navigation.navigate('Journeys') }]
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

      // Online durumda: depo durağını tamamla
      // FormData oluştur ve yeni backend alanlarını kullan
      const formData = new FormData();

      // Depo dönüşü için özel alanlar
      formData.append('endKilometer', endKilometer);
      if (fuelLevel) {
        formData.append('fuelLevel', fuelLevel);
      }
      if (vehicleCondition) {
        formData.append('vehicleCondition', vehicleCondition);
      }
      if (notes.trim()) {
        formData.append('notes', notes.trim());
      }

      await journeyService.completeStopWithFiles(journeyId, stopId, formData);

      Alert.alert(
        'Başarılı',
        'Sefer başarıyla tamamlandı.',
        [
          {
            text: 'Tamam',
            onPress: () => {
              navigation.navigate('Journeys');
            },
          },
        ]
      );
    } catch (error: any) {
      console.error('Complete journey error:', error);

      const errorMessage = error.response?.data?.message ||
        error.response?.data?.errors?.Notes?.[0] ||
        'Sefer tamamlanırken bir hata oluştu.';

      Alert.alert('Hata', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
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

      {/* Depo bilgileri */}
      <Card style={styles.infoCard}>
        <Card.Content>
          <Text style={styles.depotName}>🏠 {depotName || 'Depo Dönüşü'}</Text>
          {depotAddress && (
            <Text style={styles.depotAddress}>{depotAddress}</Text>
          )}
          <Text style={styles.label}>Seferi Tamamla</Text>
        </Card.Content>
      </Card>

      {/* Bitiş Kilometresi */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>
            Bitiş Kilometresi <Text style={styles.required}>*</Text>
          </Text>
          <TextInput
            mode="outlined"
            label="Kilometre"
            value={endKilometer}
            onChangeText={(text) => {
              setEndKilometer(text);
              setValidationError(null);
            }}
            keyboardType="decimal-pad"
            style={styles.input}
            placeholder="Örn: 45678.5"
            error={!!validationError && !endKilometer}
          />
          {validationError && !endKilometer && (
            <HelperText type="error" visible={true}>
              {validationError}
            </HelperText>
          )}
        </Card.Content>
      </Card>

      {/* Yakıt Seviyesi */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>
            Bitiş Yakıt Seviyesi (Opsiyonel)
          </Text>
          {Platform.OS === 'ios' && (
            <Pressable onPress={openFuelLevelSheet} disabled={loading}>
              <View pointerEvents="none">
                <TextInput
                  mode="outlined"
                  label="Yakıt Seviyesi"
                  value={fuelLevel ? getFuelLevelLabel(fuelLevel) : ''}
                  editable={false}
                  right={<TextInput.Icon icon="chevron-down" />}
                  style={styles.input}
                  placeholder="Seçiniz..."
                />
              </View>
            </Pressable>
          )}

          {Platform.OS !== 'ios' && (
          <View style={styles.pickerContainer} pointerEvents="box-none">
            <Picker
              selectedValue={fuelLevel}
              onValueChange={(itemValue) => setFuelLevel(itemValue)}
              style={styles.picker}
            >
              <Picker.Item label="Seçiniz..." value="" />
              <Picker.Item label="🟢 Tam (Full)" value="full" />
              <Picker.Item label="🟢 3/4" value="three_quarters" />
              <Picker.Item label="🟡 1/2" value="half" />
              <Picker.Item label="🟠 1/4" value="quarter" />
              <Picker.Item label="🔴 Boş" value="empty" />
            </Picker>
          </View>
          )}
        </Card.Content>
      </Card>

      {/* Araç Durumu */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>
            Araç Durumu <Text style={styles.required}>*</Text>
          </Text>
          {Platform.OS === 'ios' && (
            <Pressable onPress={openVehicleConditionSheet} disabled={loading}>
              <View pointerEvents="none">
                <TextInput
                  mode="outlined"
                  label="Araç Durumu"
                  value={vehicleCondition ? getVehicleConditionLabel(vehicleCondition) : ''}
                  editable={false}
                  right={<TextInput.Icon icon="chevron-down" />}
                  style={styles.input}
                  placeholder="Seçiniz..."
                  error={!!validationError && !vehicleCondition}
                />
              </View>
            </Pressable>
          )}

          {Platform.OS !== 'ios' && (
          <View
            style={[styles.pickerContainer, !vehicleCondition && validationError ? styles.pickerError : null]}
            pointerEvents="box-none"
          >
            <Picker
              selectedValue={vehicleCondition}
              onValueChange={(itemValue) => {
                setVehicleCondition(itemValue);
                setValidationError(null);
              }}
              style={styles.picker}
            >
              <Picker.Item label="Seçiniz..." value="" />
              <Picker.Item label="✅ İyi Durumda" value="good" />
              <Picker.Item label="🧹 Temizlik Gerekli" value="needs_cleaning" />
              <Picker.Item label="🔧 Bakım Gerekli" value="needs_maintenance" />
              <Picker.Item label="⚠️ Hasar Var" value="damaged" />
            </Picker>
          </View>
          )}
          {validationError && !vehicleCondition && (
            <HelperText type="error" visible={true}>
              {validationError}
            </HelperText>
          )}
        </Card.Content>
      </Card>

      {/* Notlar */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>
            Sefer Notları (Opsiyonel)
          </Text>
          <TextInput
            mode="outlined"
            label="Notlarınız"
            value={notes}
            onChangeText={setNotes}
            multiline
            numberOfLines={4}
            style={styles.input}
            placeholder="Sefer sırasında yaşanan önemli olaylar, araç durumu, vb."
          />
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
          onPress={handleCompleteJourney}
          style={styles.completeButton}
          buttonColor={!isOnline ? '#F59E0B' : '#10B981'}
          loading={loading}
          disabled={loading}
          icon={!isOnline ? "cloud-off-outline" : "check-circle"}
        >
          {!isOnline ? 'Çevrimdışı Kaydet' : 'Seferi Tamamla'}
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
  depotName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  depotAddress: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  label: {
    fontSize: 14,
    color: '#10B981',
    fontWeight: '600',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 12,
  },
  required: {
    color: '#EF4444',
  },
  input: {
    backgroundColor: 'white',
  },
  pickerContainer: {
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 4,
    backgroundColor: 'white',
  },
  pickerError: {
    borderColor: '#EF4444',
    borderWidth: 2,
  },
  picker: {
    height: 50,
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
    marginBottom: 20,
  },
  cancelButton: {
    flex: 1,
  },
  completeButton: {
    flex: 1,
  },
});

export default CompleteJourneyScreen;

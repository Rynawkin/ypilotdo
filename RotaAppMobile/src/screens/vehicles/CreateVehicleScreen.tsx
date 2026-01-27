import React, { useState } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Alert,
  Dimensions,
} from 'react-native';
import {
  TextInput,
  Button,
  Card,
  Text,
  HelperText,
  SegmentedButtons,
  ActivityIndicator,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation } from '@react-navigation/native';
import { CreateVehicleRequest, VehicleFormData, VehicleFormErrors } from '../../types/vehicle.types';
import vehicleService from '../../services/vehicleService';
import offlineQueueService from '../../services/offlineQueueService';
import networkService from '../../services/networkService';
import { isLargeScreen } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth } = Dimensions.get('window');

const CreateVehicleScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  
  const [formData, setFormData] = useState<VehicleFormData>({
    plateNumber: '',
    type: 'car',
    brand: '',
    model: '',
    year: new Date().getFullYear().toString(),
    capacity: '1000',
    status: 'active',
    fuelType: 'diesel',
  });

  const [errors, setErrors] = useState<VehicleFormErrors>({});
  const [loading, setLoading] = useState(false);

  const validateForm = (): boolean => {
    const newErrors: VehicleFormErrors = {};

    // Plaka numarası doğrulama
    if (!formData.plateNumber.trim()) {
      newErrors.plateNumber = 'Plaka numarası gerekli';
    } else if (!vehicleService.validatePlateNumber(formData.plateNumber)) {
      newErrors.plateNumber = 'Geçerli bir Türkiye plaka numarası girin (örn: 34 ABC 123)';
    }

    // Marka doğrulama
    if (!formData.brand.trim()) {
      newErrors.brand = 'Marka gerekli';
    } else if (formData.brand.trim().length < 2) {
      newErrors.brand = 'Marka en az 2 karakter olmalı';
    }

    // Model doğrulama
    if (!formData.model.trim()) {
      newErrors.model = 'Model gerekli';
    } else if (formData.model.trim().length < 2) {
      newErrors.model = 'Model en az 2 karakter olmalı';
    }

    // Yıl doğrulama
    const year = parseInt(formData.year);
    if (!year || isNaN(year)) {
      newErrors.year = 'Geçerli bir yıl girin';
    } else if (year < 1900 || year > new Date().getFullYear() + 1) {
      newErrors.year = `Yıl 1900-${new Date().getFullYear() + 1} arasında olmalı`;
    }

    // Kapasite doğrulama
    const capacity = parseFloat(formData.capacity);
    if (!formData.capacity || isNaN(capacity)) {
      newErrors.capacity = 'Geçerli bir kapasite girin';
    } else if (capacity <= 0) {
      newErrors.capacity = 'Kapasite 0\'dan büyük olmalı';
    } else if (capacity > 50000) {
      newErrors.capacity = 'Kapasite 50.000 kg\'dan fazla olamaz';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) return;

    setLoading(true);
    try {
      const isOnline = await networkService.isConnected();
      
      const vehicleData: CreateVehicleRequest = {
        plateNumber: vehicleService.formatPlateNumber(formData.plateNumber.trim()),
        type: formData.type,
        brand: formData.brand.trim(),
        model: formData.model.trim(),
        year: parseInt(formData.year),
        capacity: parseFloat(formData.capacity),
        status: formData.status,
        fuelType: formData.fuelType,
      };

      if (isOnline) {
        await vehicleService.create(vehicleData);
        Alert.alert(
          'Başarılı',
          'Araç başarıyla eklendi.',
          [{ text: 'Tamam', onPress: () => navigation.goBack() }]
        );
      } else {
        await offlineQueueService.addOperation('VEHICLE_CREATE', vehicleData);
        Alert.alert(
          'Çevrimdışı Mod',
          'Araç çevrimdışı kuyruğa eklendi. İnternet bağlantısı sağlandığında otomatik olarak kaydedilecek.',
          [{ text: 'Tamam', onPress: () => navigation.goBack() }]
        );
      }
    } catch (error: any) {
      console.error('Create vehicle error:', error);
      
      // userFriendlyMessage öncelikli hata mesajını kullan
      const errorMessage = error.userFriendlyMessage || 
        error?.response?.data?.message || 
        error.message || 
        'Araç oluşturulurken bir hata oluştu.';
      
      // Specific error handling for UI feedback
      if (error?.response?.status === 409) {
        setErrors({ plateNumber: 'Bu plaka numarası zaten kullanılıyor' });
      }
      
      Alert.alert('Hata', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const vehicleTypes = [
    { value: 'car', label: 'Otomobil', icon: 'car' },
    { value: 'van', label: 'Van', icon: 'van-passenger' },
    { value: 'truck', label: 'Kamyon', icon: 'truck' },
    { value: 'motorcycle', label: 'Motor', icon: 'motorbike' },
  ];

  const fuelTypes = [
    { value: 'diesel', label: 'Dizel' },
    { value: 'gasoline', label: 'Benzin' },
    { value: 'electric', label: 'Elektrik' },
    { value: 'hybrid', label: 'Hibrit' },
  ];

  const statusTypes = [
    { value: 'active', label: 'Aktif' },
    { value: 'inactive', label: 'Pasif' },
  ];

  const handlePlateNumberChange = (text: string) => {
    // Türkçe karakterleri İngilizce karakterlere çevir
    let formatted = text
      .replace(/[ğ]/gi, 'G')
      .replace(/[ü]/gi, 'U')
      .replace(/[ş]/gi, 'S')
      .replace(/[ı]/gi, 'I')
      .replace(/[ö]/gi, 'O')
      .replace(/[ç]/gi, 'C')
      .toUpperCase();

    // Sadece rakam, harf ve boşluk karakterlerine izin ver
    formatted = formatted.replace(/[^0-9A-Z\s]/g, '');

    setFormData(prev => ({ ...prev, plateNumber: formatted }));
    
    // Hata varsa temizle
    if (errors.plateNumber) {
      setErrors(prev => ({ ...prev, plateNumber: undefined }));
    }
  };

  return (
    <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
      <Card style={styles.formCard}>
        <Card.Content>
          <View style={styles.header}>
            <Icon name="car-plus" size={32} color="#3B82F6" />
            <Text style={styles.title}>Yeni Araç Ekle</Text>
            <Text style={styles.subtitle}>Yeni bir araç kaydı oluşturun</Text>
          </View>

          {/* Plaka Numarası */}
          <View style={styles.inputContainer}>
            <TextInput
              label="Plaka Numarası *"
              value={formData.plateNumber}
              onChangeText={handlePlateNumberChange}
              mode="outlined"
              style={styles.input}
              placeholder="34 ABC 123"
              maxLength={10}
              error={!!errors.plateNumber}
              left={<TextInput.Icon icon="identifier" />}
            />
            <HelperText type="error" visible={!!errors.plateNumber}>
              {errors.plateNumber}
            </HelperText>
          </View>

          {/* Araç Tipi */}
          <View style={styles.inputContainer}>
            <Text style={styles.fieldLabel}>Araç Tipi *</Text>
            <View style={styles.typeGrid}>
              {vehicleTypes.map((type) => (
                <SegmentedButtons
                  key={type.value}
                  value={formData.type}
                  onValueChange={(value) => setFormData(prev => ({ ...prev, type: value as any }))}
                  buttons={[
                    {
                      value: type.value,
                      label: type.label,
                      icon: type.icon,
                    },
                  ]}
                  style={styles.typeButton}
                />
              ))}
            </View>
            <View style={styles.typeSelector}>
              <SegmentedButtons
                value={formData.type}
                onValueChange={(value) => setFormData(prev => ({ ...prev, type: value as any }))}
                buttons={vehicleTypes}
                style={styles.segmentedButtons}
              />
            </View>
          </View>

          {/* Marka */}
          <View style={styles.inputContainer}>
            <TextInput
              label="Marka *"
              value={formData.brand}
              onChangeText={(text) => {
                setFormData(prev => ({ ...prev, brand: text }));
                if (errors.brand) setErrors(prev => ({ ...prev, brand: undefined }));
              }}
              mode="outlined"
              style={styles.input}
              placeholder="Örn: Ford, Mercedes, Toyota"
              error={!!errors.brand}
              left={<TextInput.Icon icon="car-info" />}
            />
            <HelperText type="error" visible={!!errors.brand}>
              {errors.brand}
            </HelperText>
          </View>

          {/* Model */}
          <View style={styles.inputContainer}>
            <TextInput
              label="Model *"
              value={formData.model}
              onChangeText={(text) => {
                setFormData(prev => ({ ...prev, model: text }));
                if (errors.model) setErrors(prev => ({ ...prev, model: undefined }));
              }}
              mode="outlined"
              style={styles.input}
              placeholder="Örn: Transit, Sprinter, Hiace"
              error={!!errors.model}
              left={<TextInput.Icon icon="car-settings" />}
            />
            <HelperText type="error" visible={!!errors.model}>
              {errors.model}
            </HelperText>
          </View>

          {/* Yıl ve Kapasite */}
          <View style={styles.rowContainer}>
            <View style={[styles.inputContainer, styles.halfWidth]}>
              <TextInput
                label="Yıl *"
                value={formData.year}
                onChangeText={(text) => {
                  setFormData(prev => ({ ...prev, year: text }));
                  if (errors.year) setErrors(prev => ({ ...prev, year: undefined }));
                }}
                mode="outlined"
                style={styles.input}
                keyboardType="numeric"
                placeholder="2024"
                error={!!errors.year}
                left={<TextInput.Icon icon="calendar" />}
              />
              <HelperText type="error" visible={!!errors.year}>
                {errors.year}
              </HelperText>
            </View>

            <View style={[styles.inputContainer, styles.halfWidth]}>
              <TextInput
                label="Kapasite (kg) *"
                value={formData.capacity}
                onChangeText={(text) => {
                  setFormData(prev => ({ ...prev, capacity: text }));
                  if (errors.capacity) setErrors(prev => ({ ...prev, capacity: undefined }));
                }}
                mode="outlined"
                style={styles.input}
                keyboardType="numeric"
                placeholder="1000"
                error={!!errors.capacity}
                left={<TextInput.Icon icon="weight-kilogram" />}
              />
              <HelperText type="error" visible={!!errors.capacity}>
                {errors.capacity}
              </HelperText>
            </View>
          </View>

          {/* Yakıt Tipi */}
          <View style={styles.inputContainer}>
            <Text style={styles.fieldLabel}>Yakıt Tipi</Text>
            <SegmentedButtons
              value={formData.fuelType}
              onValueChange={(value) => setFormData(prev => ({ ...prev, fuelType: value as any }))}
              buttons={fuelTypes}
              style={styles.segmentedButtons}
            />
          </View>

          {/* Durum */}
          <View style={styles.inputContainer}>
            <Text style={styles.fieldLabel}>Durum</Text>
            <SegmentedButtons
              value={formData.status}
              onValueChange={(value) => setFormData(prev => ({ ...prev, status: value as any }))}
              buttons={statusTypes}
              style={styles.segmentedButtons}
            />
          </View>

          {/* Submit Button */}
          <View style={styles.buttonContainer}>
            <Button
              mode="contained"
              onPress={handleSubmit}
              style={styles.submitButton}
              contentStyle={styles.submitButtonContent}
              loading={loading}
              disabled={loading}
              icon="plus"
            >
              {loading ? 'Kaydediliyor...' : 'Araç Ekle'}
            </Button>
          </View>
        </Card.Content>
      </Card>

      <View style={styles.footerSpacing} />
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  formCard: {
    margin: wp(4),
    borderRadius: 16,
    elevation: 3,
  },
  header: {
    alignItems: 'center',
    marginBottom: hp(3),
    paddingBottom: hp(2),
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#111827',
    marginTop: hp(1),
  },
  subtitle: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: hp(0.5),
  },
  inputContainer: {
    marginBottom: hp(2),
  },
  input: {
    backgroundColor: '#FFFFFF',
  },
  fieldLabel: {
    fontSize: 16,
    fontWeight: '600',
    color: '#374151',
    marginBottom: hp(1),
  },
  rowContainer: {
    flexDirection: 'row',
    gap: wp(3),
  },
  halfWidth: {
    flex: 1,
  },
  typeGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: wp(2),
    marginBottom: hp(1),
  },
  typeButton: {
    flex: 1,
    minWidth: '45%',
  },
  typeSelector: {
    marginTop: hp(1),
  },
  segmentedButtons: {
    backgroundColor: '#F9FAFB',
  },
  buttonContainer: {
    marginTop: hp(2),
    paddingTop: hp(2),
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  submitButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 12,
  },
  submitButtonContent: {
    height: hp(6),
  },
  footerSpacing: {
    height: hp(3),
  },
});

export default CreateVehicleScreen;
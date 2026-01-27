import React, { useState, useEffect } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Alert,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import {
  TextInput,
  Button,
  Text,
  Surface,
  HelperText,
  Menu,
  IconButton,
  ActivityIndicator,
  List,
} from 'react-native-paper';
import { useNavigation, useRoute } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { Driver, UpdateDriverRequest, DriverFormErrors } from '../../types/driver.types';
import driverService from '../../services/driverService';
import vehicleService from '../../services/vehicleService';
import { Vehicle } from '../../types/vehicle.types';
import { wp, hp } from '../../utils/dimensions';

interface RouteParams {
  driverId: number;
}

interface EditFormData {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  licenseNumber: string;
  licenseExpiry: string;
  vehicleId: string;
  emergencyContact: string;
  emergencyPhone: string;
  address: string;
  status: 'active' | 'inactive' | 'on_leave';
}

const EditDriverScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute();
  const { driverId } = route.params as RouteParams;

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [driver, setDriver] = useState<Driver | null>(null);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [showVehicleMenu, setShowVehicleMenu] = useState(false);

  const [formData, setFormData] = useState<EditFormData>({
    firstName: '',
    lastName: '',
    phone: '',
    email: '',
    licenseNumber: '',
    licenseExpiry: '',
    vehicleId: '',
    emergencyContact: '',
    emergencyPhone: '',
    address: '',
    status: 'active',
  });

  const [errors, setErrors] = useState<DriverFormErrors>({});

  useEffect(() => {
    loadData();
  }, [driverId]);

  const loadData = async () => {
    try {
      setLoading(true);
      const [driverData, vehicleData] = await Promise.all([
        driverService.getById(driverId),
        vehicleService.getAll(),
      ]);

      if (!driverData) {
        Alert.alert('Hata', 'Sürücü bulunamadı.', [
          { text: 'Tamam', onPress: () => navigation.goBack() }
        ]);
        return;
      }

      // Debug için driver objesini logla
      console.log('Loaded driver data:', JSON.stringify(driverData, null, 2));

      setDriver(driverData);
      
      // Available vehicles + current vehicle (if assigned)
      const availableVehicles = vehicleData.filter(v => 
        !v.driverId || v.driverId === driverId
      );
      setVehicles(availableVehicles);

      // Backend'den name olarak geliyor olabilir, firstName/lastName'e parse et
      let firstName = '';
      let lastName = '';
      
      if (driverData.firstName && driverData.lastName) {
        // Backend'de firstName/lastName ayrı geliyorsa
        firstName = driverData.firstName;
        lastName = driverData.lastName;
      } else if (driverData.name) {
        // Backend'de name olarak tek field geliyorsa parse et
        const nameParts = driverData.name.trim().split(' ');
        firstName = nameParts[0] || '';
        lastName = nameParts.slice(1).join(' ') || '';
      }

      console.log('Parsed names:', { firstName, lastName, originalName: driverData.name });

      // Populate form
      setFormData({
        firstName,
        lastName,
        phone: driverData.phone,
        email: driverData.email || '',
        licenseNumber: driverData.licenseNumber,
        licenseExpiry: driverData.licenseExpiry ? 
          formatDateForDisplay(new Date(driverData.licenseExpiry)) : '',
        vehicleId: driverData.vehicleId?.toString() || '',
        emergencyContact: driverData.emergencyContact || '',
        emergencyPhone: driverData.emergencyPhone || '',
        address: driverData.address || '',
        status: driverData.status,
      });
    } catch (error: any) {
      console.error('Load driver error:', error);
      Alert.alert('Hata', 'Sürücü bilgileri yüklenemedi.', [
        { text: 'Tamam', onPress: () => navigation.goBack() }
      ]);
    } finally {
      setLoading(false);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: DriverFormErrors = {};

    // Required fields
    if (!formData.firstName.trim()) {
      newErrors.firstName = 'İsim gereklidir';
    }

    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Soyisim gereklidir';
    }

    if (!formData.phone.trim()) {
      newErrors.phone = 'Telefon numarası gereklidir';
    } else if (!driverService.validatePhone(formData.phone)) {
      newErrors.phone = 'Geçerli bir telefon numarası giriniz (10 haneli)';
    }

    if (!formData.licenseNumber.trim()) {
      newErrors.licenseNumber = 'Ehliyet numarası gereklidir';
    }

    // Email validation (read-only, but still validate if changed somehow)
    if (formData.email.trim() && !driverService.validateEmail(formData.email)) {
      newErrors.email = 'Geçerli bir email adresi giriniz';
    }

    // Emergency phone validation (optional but if provided, must be valid)
    if (formData.emergencyPhone.trim() && !driverService.validatePhone(formData.emergencyPhone)) {
      newErrors.emergencyPhone = 'Geçerli bir telefon numarası giriniz (10 haneli)';
    }

    // License expiry validation (optional but if provided, must be valid)
    if (formData.licenseExpiry.trim() && !validateDateFormat(formData.licenseExpiry)) {
      newErrors.licenseExpiry = 'Geçerli bir tarih giriniz (GG/AA/YYYY)';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async () => {
    if (!validateForm()) {
      return;
    }

    setSaving(true);
    try {
      // Web formatında payload oluştur
      const updateData = {
        name: `${formData.firstName.trim()} ${formData.lastName.trim()}`, // Web'de name olarak tek field
        phone: formData.phone.replace(/\D/g, ''), // Remove non-digits
        licenseNumber: formData.licenseNumber.trim(),
        licenseExpiry: formData.licenseExpiry ? parseDisplayDate(formData.licenseExpiry) : undefined,
        vehicleId: formData.vehicleId ? parseInt(formData.vehicleId) : undefined,
        emergencyContact: formData.emergencyContact.trim() || undefined,
        emergencyPhone: formData.emergencyPhone.trim() || undefined,
        address: formData.address.trim() || undefined,
        status: formData.status,
      };

      // Debug için payload'ı logla
      console.log('Updating driver with payload:', JSON.stringify(updateData, null, 2));

      await driverService.update(driverId, updateData);
      
      console.log('Driver updated successfully');
      
      Alert.alert(
        'Başarılı',
        'Sürücü bilgileri güncellendi.',
        [
          {
            text: 'Tamam',
            onPress: () => navigation.goBack(),
          },
        ]
      );
    } catch (error: any) {
      console.error('Update driver error:', error);
      
      // Detaylı hata logu
      if (error.response) {
        console.log('Error status:', error.response.status);
        console.log('Error data:', JSON.stringify(error.response.data, null, 2));
      }
      
      // Kullanıcıya detaylı hata mesajı göster
      const errorMessage = error?.response?.data?.message || 
                          error?.response?.data?.error ||
                          `${error.message} (${error.response?.status || 'Network Error'})`;
      
      Alert.alert(
        'Hata',
        errorMessage
      );
    } finally {
      setSaving(false);
    }
  };

  const updateFormData = (field: keyof EditFormData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // Clear error for this field
    if (errors[field as keyof DriverFormErrors]) {
      setErrors(prev => ({ ...prev, [field]: undefined }));
    }
  };

  const getSelectedVehicle = () => {
    return vehicles.find(v => v.id.toString() === formData.vehicleId);
  };

  const formatVehicleDisplay = (vehicle: Vehicle) => {
    return `${vehicle.plateNumber} - ${vehicle.brand} ${vehicle.model}`;
  };

  // Date formatting functions
  const formatDateForDisplay = (date: Date): string => {
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear().toString();
    return `${day}/${month}/${year}`;
  };

  const parseDisplayDate = (dateStr: string): Date | null => {
    if (!dateStr || !validateDateFormat(dateStr)) return null;
    const [day, month, year] = dateStr.split('/').map(Number);
    return new Date(year, month - 1, day);
  };

  const validateDateFormat = (dateStr: string): boolean => {
    const regex = /^\d{2}\/\d{2}\/\d{4}$/;
    if (!regex.test(dateStr)) return false;
    
    const [day, month, year] = dateStr.split('/').map(Number);
    if (month < 1 || month > 12) return false;
    if (day < 1 || day > 31) return false;
    if (year < 1900 || year > 2100) return false;
    
    // Check if date is valid
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && 
           date.getMonth() === month - 1 && 
           date.getDate() === day;
  };

  const formatDateInput = (input: string): string => {
    // Remove all non-digits
    const digits = input.replace(/\D/g, '');
    
    // Apply mask: XX/XX/XXXX
    if (digits.length >= 8) {
      return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4, 8)}`;
    } else if (digits.length >= 4) {
      return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    } else if (digits.length >= 2) {
      return `${digits.slice(0, 2)}/${digits.slice(2)}`;
    }
    return digits;
  };

  const handleLicenseExpiryChange = (text: string) => {
    const formatted = formatDateInput(text);
    updateFormData('licenseExpiry', formatted);
  };

  const handleDeleteDriver = () => {
    Alert.alert(
      'Sürücü Sil',
      'Bu sürücüyü silmek istediğinizden emin misiniz? Bu işlem geri alınamaz.',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Sil',
          style: 'destructive',
          onPress: async () => {
            try {
              await driverService.delete(driverId);
              Alert.alert('Başarılı', 'Sürücü silindi.', [
                { text: 'Tamam', onPress: () => navigation.goBack() }
              ]);
            } catch (error) {
              Alert.alert('Hata', 'Sürücü silinirken bir hata oluştu.');
            }
          },
        },
      ]
    );
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Sürücü bilgileri yükleniyor...</Text>
      </View>
    );
  }

  if (!driver) {
    return null;
  }

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView 
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <Surface style={styles.formContainer} elevation={2}>
          <Text style={styles.sectionTitle}>Kişisel Bilgiler</Text>
          
          <View style={styles.row}>
            <View style={styles.halfColumn}>
              <TextInput
                label="İsim *"
                value={formData.firstName}
                onChangeText={(text) => updateFormData('firstName', text)}
                error={!!errors.firstName}
                style={styles.input}
                mode="outlined"
              />
              <HelperText type="error" visible={!!errors.firstName}>
                {errors.firstName}
              </HelperText>
            </View>
            
            <View style={styles.halfColumn}>
              <TextInput
                label="Soyisim *"
                value={formData.lastName}
                onChangeText={(text) => updateFormData('lastName', text)}
                error={!!errors.lastName}
                style={styles.input}
                mode="outlined"
              />
              <HelperText type="error" visible={!!errors.lastName}>
                {errors.lastName}
              </HelperText>
            </View>
          </View>

          <TextInput
            label="Telefon Numarası *"
            value={formData.phone}
            onChangeText={(text) => updateFormData('phone', text)}
            error={!!errors.phone}
            keyboardType="phone-pad"
            style={styles.input}
            mode="outlined"
            placeholder="5551234567"
          />
          <HelperText type="error" visible={!!errors.phone}>
            {errors.phone}
          </HelperText>

          <TextInput
            label="Email Adresi"
            value={formData.email}
            editable={false}
            style={[styles.input, styles.readOnlyInput]}
            mode="outlined"
            right={<TextInput.Icon icon="lock" />}
          />
          <HelperText type="info" visible={true}>
            Email adresi değiştirilemez
          </HelperText>
        </Surface>

        <Surface style={styles.formContainer} elevation={2}>
          <Text style={styles.sectionTitle}>Sürücü Bilgileri</Text>
          
          <TextInput
            label="Ehliyet Numarası *"
            value={formData.licenseNumber}
            onChangeText={(text) => updateFormData('licenseNumber', text)}
            error={!!errors.licenseNumber}
            style={styles.input}
            mode="outlined"
          />
          <HelperText type="error" visible={!!errors.licenseNumber}>
            {errors.licenseNumber}
          </HelperText>

          <TextInput
            label="Ehliyet Son Kullanma Tarihi"
            value={formData.licenseExpiry}
            onChangeText={handleLicenseExpiryChange}
            error={!!errors.licenseExpiry}
            keyboardType="numeric"
            style={styles.input}
            mode="outlined"
            placeholder="GG/AA/YYYY"
            maxLength={10}
          />
          <HelperText type="error" visible={!!errors.licenseExpiry}>
            {errors.licenseExpiry}
          </HelperText>

          <Menu
            visible={showVehicleMenu}
            onDismiss={() => setShowVehicleMenu(false)}
            anchor={
              <TextInput
                label="Araç Ataması"
                value={getSelectedVehicle() ? formatVehicleDisplay(getSelectedVehicle()!) : ''}
                editable={false}
                right={
                  <TextInput.Icon 
                    icon="chevron-down" 
                    onPress={() => setShowVehicleMenu(true)}
                  />
                }
                style={styles.input}
                mode="outlined"
                onPressIn={() => setShowVehicleMenu(true)}
                placeholder="Araç seçin (opsiyonel)"
              />
            }
          >
            <Menu.Item 
              onPress={() => {
                updateFormData('vehicleId', '');
                setShowVehicleMenu(false);
              }} 
              title="Araç ataması yok" 
            />
            {vehicles.map(vehicle => (
              <Menu.Item
                key={vehicle.id}
                onPress={() => {
                  updateFormData('vehicleId', vehicle.id.toString());
                  setShowVehicleMenu(false);
                }}
                title={formatVehicleDisplay(vehicle)}
              />
            ))}
          </Menu>
        </Surface>

        <Surface style={styles.formContainer} elevation={2}>
          <Text style={styles.sectionTitle}>Acil Durum & Adres</Text>
          
          <View style={styles.row}>
            <View style={styles.halfColumn}>
              <TextInput
                label="Acil Durum Kişisi"
                value={formData.emergencyContact}
                onChangeText={(text) => updateFormData('emergencyContact', text)}
                style={styles.input}
                mode="outlined"
              />
            </View>
            
            <View style={styles.halfColumn}>
              <TextInput
                label="Acil Durum Telefonu"
                value={formData.emergencyPhone}
                onChangeText={(text) => updateFormData('emergencyPhone', text)}
                error={!!errors.emergencyPhone}
                keyboardType="phone-pad"
                style={styles.input}
                mode="outlined"
                placeholder="5551234567"
              />
              <HelperText type="error" visible={!!errors.emergencyPhone}>
                {errors.emergencyPhone}
              </HelperText>
            </View>
          </View>

          <TextInput
            label="Adres"
            value={formData.address}
            onChangeText={(text) => updateFormData('address', text)}
            multiline
            numberOfLines={3}
            style={styles.input}
            mode="outlined"
          />
        </Surface>

        <Surface style={styles.formContainer} elevation={2}>
          <Text style={styles.sectionTitle}>Durum</Text>
          
          <List.Item
            title="Durum"
            description={driverService.getStatusLabel(formData.status)}
            left={(props) => <List.Icon {...props} icon="account-check" />}
            right={() => (
              <IconButton
                icon="chevron-down"
                onPress={() => {
                  Alert.alert(
                    'Durum Seç',
                    '',
                    [
                      { 
                        text: 'Aktif', 
                        onPress: () => updateFormData('status', 'active')
                      },
                      { 
                        text: 'Pasif', 
                        onPress: () => updateFormData('status', 'inactive')
                      },
                      { 
                        text: 'İzinli', 
                        onPress: () => updateFormData('status', 'on_leave')
                      },
                      { text: 'İptal', style: 'cancel' }
                    ]
                  );
                }}
              />
            )}
          />
        </Surface>

        <View style={styles.buttonContainer}>
          <Button
            mode="contained"
            onPress={handleSubmit}
            loading={saving}
            disabled={saving}
            style={styles.saveButton}
            contentStyle={styles.buttonContent}
          >
            Değişiklikleri Kaydet
          </Button>

          <Button
            mode="outlined"
            onPress={handleDeleteDriver}
            disabled={saving}
            style={styles.deleteButton}
            contentStyle={styles.buttonContent}
            buttonColor="#FEF2F2"
            textColor="#DC2626"
          >
            Sürücüyü Sil
          </Button>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F3F4F6',
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
  scrollContent: {
    padding: wp(4),
    paddingBottom: hp(4),
  },
  formContainer: {
    padding: wp(4),
    marginBottom: hp(2),
    borderRadius: 12,
    backgroundColor: 'white',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: hp(2),
  },
  input: {
    backgroundColor: 'white',
    marginBottom: hp(0.5),
  },
  readOnlyInput: {
    backgroundColor: '#F9FAFB',
  },
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  halfColumn: {
    flex: 0.48,
  },
  buttonContainer: {
    marginTop: hp(2),
    gap: hp(1.5),
  },
  saveButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 8,
  },
  deleteButton: {
    borderRadius: 8,
    borderColor: '#DC2626',
  },
  buttonContent: {
    height: hp(6),
  },
});

export default EditDriverScreen;
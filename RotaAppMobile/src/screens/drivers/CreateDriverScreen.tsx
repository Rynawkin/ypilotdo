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
  Switch,
  Surface,
  HelperText,
  Divider,
  List,
  Menu,
  IconButton,
} from 'react-native-paper';
import { useNavigation } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { CreateDriverRequest, DriverFormData, DriverFormErrors } from '../../types/driver.types';
import driverService from '../../services/driverService';
import vehicleService from '../../services/vehicleService';
import { Vehicle } from '../../types/vehicle.types';
import { wp, hp } from '../../utils/dimensions';

const CreateDriverScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [loading, setLoading] = useState(false);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [showVehicleMenu, setShowVehicleMenu] = useState(false);
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [generateRandomPassword, setGenerateRandomPassword] = useState(true); // Varsayılan olarak otomatik

  const [formData, setFormData] = useState<DriverFormData>({
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
    sendInviteEmail: false,
  });

  const [errors, setErrors] = useState<DriverFormErrors>({});

  useEffect(() => {
    loadVehicles();
    // Otomatik şifre oluştur (varsayılan olarak aktif)
    if (generateRandomPassword) {
      setPassword(generateAutoPassword());
    }
  }, []);

  const loadVehicles = async () => {
    try {
      const vehicleData = await vehicleService.getAll();
      setVehicles(vehicleData.filter(v => !v.driverId)); // Only unassigned vehicles
    } catch (error: any) {
      console.error('Error loading vehicles:', error);
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

    // Email validation (zorunlu - backend ApplicationUser için gerekli)
    if (!formData.email.trim()) {
      newErrors.email = 'Email adresi zorunludur';
    } else if (!driverService.validateEmail(formData.email)) {
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

    setLoading(true);
    try {
      // Backend'in beklediği formata uygun payload oluştur
      const driverData = {
        name: `${formData.firstName.trim()} ${formData.lastName.trim()}`, // Web'de name olarak tek field
        phone: formData.phone.replace(/\D/g, ''), // Remove non-digits
        email: formData.email.trim(), // Zorunlu - backend ApplicationUser için
        licenseNumber: formData.licenseNumber.trim(),
        licenseExpiry: formData.licenseExpiry ? parseDisplayDate(formData.licenseExpiry) : undefined,
        vehicleId: formData.vehicleId ? parseInt(formData.vehicleId) : undefined,
        emergencyContact: formData.emergencyContact.trim() || undefined,
        emergencyPhone: formData.emergencyPhone.trim() || undefined,
        address: formData.address.trim() || undefined,
        status: convertStatusToWebFormat(formData.status), // Web formatı: available/busy/offline
        // Backend ApplicationUser için gerekli alanlar
        password: password || generateAutoPassword(), // Kullanıcının girdiği veya otomatik oluşturulan şifre
        generateRandomPassword: generateRandomPassword, // Switch durumu
        sendCredentialsByEmail: true, // Email gönder
        sendCredentialsBySms: false,
        rating: 0,
        totalDeliveries: 0
      };

      // Debug için payload'ı logla
      console.log('Creating driver with payload:', JSON.stringify(driverData, null, 2));

      const response = await driverService.create(driverData);
      
      console.log('Driver created successfully:', response);
      
      Alert.alert(
        'Başarılı',
        `Sürücü başarıyla oluşturuldu.${response.isUserCreated ? ' Kullanıcı hesabı da otomatik olarak oluşturuldu ve hoş geldin emaili gönderildi.' : ''}`,
        [
          {
            text: 'Tamam',
            onPress: () => navigation.goBack(),
          },
        ]
      );
    } catch (error: any) {
      console.error('Create driver error:', error);
      
      // Detaylı hata logu
      if (error.response) {
        console.log('Error status:', error.response.status);
        console.log('Error data:', JSON.stringify(error.response.data, null, 2));
        console.log('Error headers:', error.response.headers);
      }
      
      // userFriendlyMessage öncelikli hata mesajını kullan
      const errorMessage = error.userFriendlyMessage || 
                          error?.response?.data?.message || 
                          error?.response?.data?.error ||
                          error.message || 
                          'Sürücü oluşturulurken bir hata oluştu';
      
      Alert.alert(
        'Hata',
        errorMessage
      );
    } finally {
      setLoading(false);
    }
  };

  const updateFormData = (field: keyof DriverFormData, value: string | boolean) => {
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

  // Otomatik güvenli şifre oluştur (web DriverForm.tsx'ten uyarlandı)
  const generateAutoPassword = (): string => {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$';
    let pwd = '';
    for (let i = 0; i < 10; i++) {
      pwd += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return pwd;
  };

  // Mobile status'u web formatına çevir (web'de available/busy/offline kullanılıyor)
  const convertStatusToWebFormat = (status: string): string => {
    switch (status) {
      case 'active':
        return 'available';
      case 'inactive':
        return 'offline';
      case 'on_leave':
        return 'offline'; // İzinli durumu da offline olarak gönderilebilir
      default:
        return 'available';
    }
  };

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
            label="Email Adresi *"
            value={formData.email}
            onChangeText={(text) => updateFormData('email', text)}
            error={!!errors.email}
            keyboardType="email-address"
            autoCapitalize="none"
            style={styles.input}
            mode="outlined"
            placeholder="ornek@email.com"
          />
          <HelperText type="error" visible={!!errors.email}>
            {errors.email}
          </HelperText>

          {/* Şifre Alanı */}
          <View style={styles.passwordSection}>
            <Text style={styles.passwordSectionTitle}>Şifre Bilgileri</Text>
            
            <View style={styles.passwordContainer}>
              <TextInput
                label="Şifre"
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
                style={[styles.input, styles.passwordInput]}
                mode="outlined"
                placeholder="Şifre giriniz"
                editable={!generateRandomPassword}
                right={
                  <TextInput.Icon 
                    icon={showPassword ? "eye-off" : "eye"}
                    onPress={() => setShowPassword(!showPassword)}
                  />
                }
              />
              
              <IconButton
                icon="dice-5"
                mode="contained"
                size={24}
                style={styles.generateButton}
                onPress={() => {
                  const newPassword = generateAutoPassword();
                  setPassword(newPassword);
                  setGenerateRandomPassword(true);
                }}
                iconColor="#3B82F6"
              />
            </View>
            
            <View style={styles.passwordOptionContainer}>
              <View style={styles.switchContainer}>
                <View>
                  <Text style={styles.switchLabel}>Otomatik Şifre Oluştur</Text>
                  <Text style={styles.switchDescription}>
                    Sistem güvenli bir şifre otomatik oluşturacak
                  </Text>
                </View>
                <Switch
                  value={generateRandomPassword}
                  onValueChange={(value) => {
                    setGenerateRandomPassword(value);
                    if (value) {
                      const newPassword = generateAutoPassword();
                      setPassword(newPassword);
                    } else {
                      setPassword('');
                    }
                  }}
                />
              </View>
            </View>
          </View>
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
              <Menu
                visible={false}
                onDismiss={() => {}}
                anchor={
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
                }
              />
            )}
          />
        </Surface>

        <View style={styles.submitContainer}>
          <Button
            mode="contained"
            onPress={handleSubmit}
            loading={loading}
            disabled={loading}
            style={styles.submitButton}
            contentStyle={styles.submitButtonContent}
          >
            Sürücü Oluştur
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
  row: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  halfColumn: {
    flex: 0.48,
  },
  switchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: hp(1),
    paddingHorizontal: wp(2),
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
    marginBottom: hp(1),
  },
  switchLabel: {
    flex: 1,
    fontSize: 14,
    color: '#374151',
  },
  switchDescription: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 2,
  },
  submitContainer: {
    marginTop: hp(2),
  },
  submitButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 8,
  },
  submitButtonContent: {
    height: hp(6),
  },
  passwordSection: {
    marginTop: hp(2),
    paddingTop: hp(2),
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  passwordSectionTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
    marginBottom: hp(1.5),
  },
  passwordContainer: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: hp(1),
  },
  passwordInput: {
    flex: 1,
    marginRight: wp(2),
  },
  generateButton: {
    marginTop: hp(0.5),
    backgroundColor: '#EFF6FF',
    borderWidth: 1,
    borderColor: '#DBEAFE',
  },
  passwordOptionContainer: {
    marginTop: hp(1),
  },
});

export default CreateDriverScreen;
// C:\Projects\RotaAppMobile\src\screens\routes\CreateRouteScreen.tsx

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Modal,
  FlatList,
  Switch,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';

import routeService from '../../services/routeService';
import depotService, { Depot } from '../../services/depotService';
import driverService, { Driver } from '../../services/driverService';
import vehicleService, { Vehicle } from '../../services/vehicleService';

const CreateRouteScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  
  // Form state
  const [name, setName] = useState('');
  const [dateString, setDateString] = useState(format(new Date(), 'dd/MM/yyyy'));
  const [timeString, setTimeString] = useState('08:00');
  const [notes, setNotes] = useState('');
  const [avoidTolls, setAvoidTolls] = useState(false);
  
  // Selections
  const [selectedDepot, setSelectedDepot] = useState<Depot | null>(null);
  const [selectedDriver, setSelectedDriver] = useState<Driver | null>(null);
  const [selectedVehicle, setSelectedVehicle] = useState<Vehicle | null>(null);
  
  // Data
  const [depots, setDepots] = useState<Depot[]>([]);
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  
  // Modal states
  const [showDepotModal, setShowDepotModal] = useState(false);
  const [showDriverModal, setShowDriverModal] = useState(false);
  const [showVehicleModal, setShowVehicleModal] = useState(false);
  
  // UI State
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [depotsData, driversData, vehiclesData] = await Promise.all([
        depotService.getAll(),
        driverService.getAll(),
        vehicleService.getAll(),
      ]);
      
      setDepots(depotsData);
      setDrivers(driversData);
      setVehicles(vehiclesData);
      
      // Set default depot
      const defaultDepot = depotsData.find(d => d.isDefault);
      if (defaultDepot) {
        setSelectedDepot(defaultDepot);
        
        if (defaultDepot.startWorkingHours) {
          const [hours, minutes] = defaultDepot.startWorkingHours.split(':');
          setTimeString(`${hours}:${minutes}`);
        }
      }
    } catch (error: any) {
      console.error('Error loading data:', error);
      const errorMessage = error.userFriendlyMessage || error.message || 'Veriler yüklenirken bir hata oluştu';
      Alert.alert('Hata', errorMessage);
    } finally {
      setLoading(false);
    }
  };

  // Otomatik tarih formatlaması
  const formatDateInput = (text: string) => {
    // Sadece rakamları al
    const numbers = text.replace(/[^\d]/g, '');
    
    if (numbers.length <= 2) {
      return numbers;
    } else if (numbers.length <= 4) {
      return `${numbers.slice(0, 2)}/${numbers.slice(2)}`;
    } else if (numbers.length <= 8) {
      return `${numbers.slice(0, 2)}/${numbers.slice(2, 4)}/${numbers.slice(4, 8)}`;
    }
    return `${numbers.slice(0, 2)}/${numbers.slice(2, 4)}/${numbers.slice(4, 8)}`;
  };

  // Otomatik saat formatlaması
  const formatTimeInput = (text: string) => {
    // Sadece rakamları al
    const numbers = text.replace(/[^\d]/g, '');
    
    if (numbers.length <= 2) {
      return numbers;
    } else if (numbers.length <= 4) {
      return `${numbers.slice(0, 2)}:${numbers.slice(2, 4)}`;
    }
    return `${numbers.slice(0, 2)}:${numbers.slice(2, 4)}`;
  };

  const parseDate = (dateStr: string): Date | null => {
    const parts = dateStr.split('/');
    if (parts.length === 3) {
      const day = parseInt(parts[0]);
      const month = parseInt(parts[1]) - 1;
      const year = parseInt(parts[2]);
      
      if (!isNaN(day) && !isNaN(month) && !isNaN(year)) {
        return new Date(year, month, day);
      }
    }
    return null;
  };

  // Time validation ve formatting
  const validateAndFormatTime = (time: string): string | null => {
    const timePattern = /^([01]?[0-9]|2[0-3]):([0-5][0-9])$/;
    if (!timePattern.test(time)) {
      return null;
    }
    // Backend için :00 ekle
    return `${time}:00`;
  };

  const handleCreateRoute = async () => {
    if (!name.trim()) {
      Alert.alert('Uyarı', 'Lütfen rota adı girin');
      return;
    }
    
    if (!selectedDepot) {
      Alert.alert('Uyarı', 'Lütfen depo seçin');
      return;
    }
    
    const parsedDate = parseDate(dateString);
    if (!parsedDate) {
      Alert.alert('Uyarı', 'Geçerli bir tarih girin (GG/AA/YYYY)');
      return;
    }
    
    const formattedStartTime = validateAndFormatTime(timeString);
    if (!formattedStartTime) {
      Alert.alert('Uyarı', 'Geçerli bir saat girin (SS:DD)');
      return;
    }
    
    setSaving(true);
    
    try {
      // Backend en az 1 stop istiyor
      const dummyStop = {
        customerId: null,
        name: 'Geçici Durak',
        address: selectedDepot.address,
        latitude: selectedDepot.latitude,
        longitude: selectedDepot.longitude,
        order: 1,
        type: 10,
        orderType: 20,
        proofOfDeliveryRequired: false,
        serviceTime: '00:10:00'
      };
      
      const routeData = {
        name,
        date: parsedDate.toISOString(),
        depotId: selectedDepot.id,
        driverId: selectedDriver?.id || null,
        vehicleId: selectedVehicle?.id || null,
        notes,
        optimized: false,
        totalDistance: 0,
        totalDuration: 0,
        stops: [dummyStop],
        AvoidTolls: avoidTolls,
        startDetails: {
          startTime: formattedStartTime,
          name: selectedDepot.name,
          address: selectedDepot.address,
          latitude: selectedDepot.latitude,
          longitude: selectedDepot.longitude,
        },
      };
      
      console.log('Sending route data:', routeData);
      
      const route = await routeService.create(routeData);
      
      Alert.alert('Başarılı', 'Rota oluşturuldu. Şimdi müşteri ekleyebilirsiniz.', [
        {
          text: 'Tamam',
          onPress: () => navigation.navigate('AddStopsToRoute', { routeId: route.id })
        }
      ]);
    } catch (error: any) {
      console.error('Error creating route:', error);
      const errorMessage = error.userFriendlyMessage || error.message || 'Rota oluşturulamadı';
      Alert.alert('Hata', errorMessage);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Yükleniyor...</Text>
      </View>
    );
  }

  return (
    <KeyboardAvoidingView 
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.scrollContent}>
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Temel Bilgiler</Text>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Rota Adı *</Text>
            <TextInput
              style={styles.input}
              value={name}
              onChangeText={setName}
              placeholder="Örn: Kadıköy Sabah Turu"
              placeholderTextColor="#9CA3AF"
            />
          </View>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Tarih * (GG/AA/YYYY)</Text>
            <View style={styles.dateInputContainer}>
              <Icon name="calendar" size={20} color="#6B7280" style={styles.inputIcon} />
              <TextInput
                style={styles.dateInput}
                value={dateString}
                onChangeText={(text) => setDateString(formatDateInput(text))}
                placeholder="15/01/2025"
                placeholderTextColor="#9CA3AF"
                keyboardType="numeric"
                maxLength={10}
              />
            </View>
            <Text style={styles.helperText}>Örnek: {format(new Date(), 'dd/MM/yyyy')}</Text>
          </View>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Başlangıç Saati * (SS:DD)</Text>
            <View style={styles.dateInputContainer}>
              <Icon name="clock-outline" size={20} color="#6B7280" style={styles.inputIcon} />
              <TextInput
                style={styles.dateInput}
                value={timeString}
                onChangeText={(text) => setTimeString(formatTimeInput(text))}
                placeholder="08:00"
                placeholderTextColor="#9CA3AF"
                keyboardType="numeric"
                maxLength={5}
              />
            </View>
            <Text style={styles.helperText}>24 saat formatında (Örnek: 14:30)</Text>
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Atamalar</Text>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Depo *</Text>
            <TouchableOpacity
              style={styles.selectButton}
              onPress={() => setShowDepotModal(true)}
            >
              <Text style={selectedDepot ? styles.selectButtonTextSelected : styles.selectButtonText}>
                {selectedDepot ? `${selectedDepot.name} ${selectedDepot.isDefault ? '(Varsayılan)' : ''}` : 'Depo Seçin'}
              </Text>
              <Icon name="chevron-down" size={20} color="#6B7280" />
            </TouchableOpacity>
          </View>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Sürücü (İsteğe Bağlı)</Text>
            <TouchableOpacity
              style={styles.selectButton}
              onPress={() => setShowDriverModal(true)}
            >
              <Text style={selectedDriver ? styles.selectButtonTextSelected : styles.selectButtonText}>
                {selectedDriver ? selectedDriver.name : 'Sürücü Seçin'}
              </Text>
              <Icon name="chevron-down" size={20} color="#6B7280" />
            </TouchableOpacity>
            {selectedDriver && (
              <TouchableOpacity
                style={styles.clearButton}
                onPress={() => setSelectedDriver(null)}
              >
                <Text style={styles.clearButtonText}>Temizle</Text>
              </TouchableOpacity>
            )}
          </View>
          
          <View style={styles.inputGroup}>
            <Text style={styles.label}>Araç (İsteğe Bağlı)</Text>
            <TouchableOpacity
              style={styles.selectButton}
              onPress={() => setShowVehicleModal(true)}
            >
              <Text style={selectedVehicle ? styles.selectButtonTextSelected : styles.selectButtonText}>
                {selectedVehicle ? `${selectedVehicle.plateNumber} - ${selectedVehicle.brand} ${selectedVehicle.model}` : 'Araç Seçin'}
              </Text>
              <Icon name="chevron-down" size={20} color="#6B7280" />
            </TouchableOpacity>
            {selectedVehicle && (
              <TouchableOpacity
                style={styles.clearButton}
                onPress={() => setSelectedVehicle(null)}
              >
                <Text style={styles.clearButtonText}>Temizle</Text>
              </TouchableOpacity>
            )}
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Notlar</Text>
          <TextInput
            style={[styles.input, styles.textArea]}
            value={notes}
            onChangeText={setNotes}
            placeholder="Rota ile ilgili notlarınız..."
            placeholderTextColor="#9CA3AF"
            multiline
            numberOfLines={4}
            textAlignVertical="top"
          />
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Rota Ayarları</Text>

          <View style={styles.switchContainer}>
            <View style={styles.switchLabelContainer}>
              <Icon name="toll" size={20} color="#374151" style={styles.switchIcon} />
              <View style={styles.switchTextContainer}>
                <Text style={styles.switchLabel}>Ücretli Yollardan Kaçın</Text>
                <Text style={styles.switchDescription}>Optimize edildiğinde ücretli yollar kullanılmaz</Text>
              </View>
            </View>
            <Switch
              value={avoidTolls}
              onValueChange={setAvoidTolls}
              trackColor={{ false: '#E5E7EB', true: '#3B82F6' }}
              thumbColor={avoidTolls ? '#FFFFFF' : '#9CA3AF'}
              ios_backgroundColor="#E5E7EB"
            />
          </View>
        </View>

        <View style={styles.buttonContainer}>
          <TouchableOpacity
            style={[styles.button, styles.cancelButton]}
            onPress={() => navigation.goBack()}
          >
            <Text style={styles.cancelButtonText}>İptal</Text>
          </TouchableOpacity>
          
          <TouchableOpacity
            style={[styles.button, styles.createButton]}
            onPress={handleCreateRoute}
            disabled={saving}
          >
            {saving ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <>
                <Icon name="arrow-right" size={20} color="#fff" />
                <Text style={styles.createButtonText}>İleri</Text>
              </>
            )}
          </TouchableOpacity>
        </View>
      </ScrollView>

      {/* Depo Modal */}
      <Modal visible={showDepotModal} transparent animationType="slide">
        <TouchableOpacity 
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowDepotModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Depo Seçin</Text>
              <TouchableOpacity onPress={() => setShowDepotModal(false)}>
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>
            <FlatList
              data={depots}
              keyExtractor={(item) => item.id.toString()}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={styles.modalItem}
                  onPress={() => {
                    setSelectedDepot(item);
                    setShowDepotModal(false);
                  }}
                >
                  <View style={styles.modalItemContent}>
                    <Text style={styles.modalItemText}>
                      {item.name} {item.isDefault && '(Varsayılan)'}
                    </Text>
                    {selectedDepot?.id === item.id && (
                      <Icon name="check" size={20} color="#10B981" />
                    )}
                  </View>
                </TouchableOpacity>
              )}
            />
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Sürücü Modal */}
      <Modal visible={showDriverModal} transparent animationType="slide">
        <TouchableOpacity 
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowDriverModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Sürücü Seçin</Text>
              <TouchableOpacity onPress={() => setShowDriverModal(false)}>
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>
            <FlatList
              data={drivers}
              keyExtractor={(item) => item.id.toString()}
              ListHeaderComponent={
                <TouchableOpacity
                  style={[styles.modalItem, { backgroundColor: '#F3F4F6' }]}
                  onPress={() => {
                    setSelectedDriver(null);
                    setShowDriverModal(false);
                  }}
                >
                  <Text style={[styles.modalItemText, { fontStyle: 'italic' }]}>
                    Sürücü Seçme (Temizle)
                  </Text>
                </TouchableOpacity>
              }
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={styles.modalItem}
                  onPress={() => {
                    setSelectedDriver(item);
                    setShowDriverModal(false);
                  }}
                >
                  <View style={styles.modalItemContent}>
                    <Text style={styles.modalItemText}>
                      {item.name} {item.status !== 'available' && `(${item.status})`}
                    </Text>
                    {selectedDriver?.id === item.id && (
                      <Icon name="check" size={20} color="#10B981" />
                    )}
                  </View>
                </TouchableOpacity>
              )}
            />
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Araç Modal */}
      <Modal visible={showVehicleModal} transparent animationType="slide">
        <TouchableOpacity 
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowVehicleModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Araç Seçin</Text>
              <TouchableOpacity onPress={() => setShowVehicleModal(false)}>
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>
            <FlatList
              data={vehicles}
              keyExtractor={(item) => item.id.toString()}
              ListHeaderComponent={
                <TouchableOpacity
                  style={[styles.modalItem, { backgroundColor: '#F3F4F6' }]}
                  onPress={() => {
                    setSelectedVehicle(null);
                    setShowVehicleModal(false);
                  }}
                >
                  <Text style={[styles.modalItemText, { fontStyle: 'italic' }]}>
                    Araç Seçme (Temizle)
                  </Text>
                </TouchableOpacity>
              }
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={styles.modalItem}
                  onPress={() => {
                    setSelectedVehicle(item);
                    setShowVehicleModal(false);
                  }}
                >
                  <View style={styles.modalItemContent}>
                    <Text style={styles.modalItemText}>
                      {item.plateNumber} - {item.brand} {item.model}
                    </Text>
                    {selectedVehicle?.id === item.id && (
                      <Icon name="check" size={20} color="#10B981" />
                    )}
                  </View>
                </TouchableOpacity>
              )}
            />
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>
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
    backgroundColor: '#F3F4F6',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: '#6B7280',
  },
  scrollContent: {
    padding: 16,
  },
  section: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 16,
  },
  inputGroup: {
    marginBottom: 16,
  },
  label: {
    fontSize: 14,
    fontWeight: '500',
    color: '#374151',
    marginBottom: 8,
  },
  input: {
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 16,
    color: '#111827',
    backgroundColor: '#fff',
  },
  dateInputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    backgroundColor: '#fff',
  },
  inputIcon: {
    marginLeft: 12,
  },
  dateInput: {
    flex: 1,
    paddingHorizontal: 8,
    paddingVertical: 10,
    fontSize: 16,
    color: '#111827',
  },
  helperText: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 4,
  },
  textArea: {
    minHeight: 100,
    textAlignVertical: 'top',
  },
  selectButton: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 12,
    backgroundColor: '#fff',
  },
  selectButtonText: {
    fontSize: 16,
    color: '#9CA3AF',
    flex: 1,
  },
  selectButtonTextSelected: {
    fontSize: 16,
    color: '#111827',
    flex: 1,
  },
  clearButton: {
    alignSelf: 'flex-start',
    marginTop: 4,
  },
  clearButtonText: {
    fontSize: 12,
    color: '#EF4444',
    textDecorationLine: 'underline',
  },
  buttonContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 16,
  },
  button: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    paddingHorizontal: 24,
    borderRadius: 8,
    flex: 1,
  },
  cancelButton: {
    backgroundColor: '#E5E7EB',
    marginRight: 8,
  },
  cancelButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#374151',
  },
  createButton: {
    backgroundColor: '#3B82F6',
    marginLeft: 8,
  },
  createButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#fff',
    marginLeft: 8,
  },
  modalContainer: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#fff',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    maxHeight: '70%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
  },
  modalItem: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  modalItemContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  modalItemText: {
    fontSize: 16,
    color: '#374151',
    flex: 1,
  },
  switchContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
  },
  switchLabelContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  switchIcon: {
    marginRight: 12,
  },
  switchTextContainer: {
    flex: 1,
  },
  switchLabel: {
    fontSize: 16,
    fontWeight: '500',
    color: '#374151',
    marginBottom: 2,
  },
  switchDescription: {
    fontSize: 13,
    color: '#6B7280',
    lineHeight: 16,
  },
});

export default CreateRouteScreen;
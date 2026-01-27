// C:\Projects\RotaAppMobile\src\screens\routes\AddStopsToRouteScreen.tsx

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
  FlatList,
  Modal,
  Switch,
} from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';

import routeService, { Route, RouteStop } from '../../services/routeService';
import customerService, { Customer } from '../../services/customerService';
import journeyService from '../../services/journeyService';
import { normalizeSearchText } from '../../utils/string';

const AddStopsToRouteScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const routeParams = useRoute<any>();
  const routeId = routeParams.params?.routeId;

  const [route, setRoute] = useState<Route | null>(null);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedCustomers, setSelectedCustomers] = useState<Customer[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [filteredCustomers, setFilteredCustomers] = useState<Customer[]>([]);
  const [showCustomerModal, setShowCustomerModal] = useState(false);
  const [showBulkModal, setShowBulkModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<Customer | null>(null);
  const [loading, setLoading] = useState(true);
  const [optimizing, setOptimizing] = useState(false);
  const [creatingJourney, setCreatingJourney] = useState(false);
  const [bulkSelectedCustomers, setBulkSelectedCustomers] = useState<number[]>([]);

  // Stop override değerleri
  const [stopOverrides, setStopOverrides] = useState<{
    [customerId: string]: {
      timeWindowStart?: string;
      timeWindowEnd?: string;
      serviceTime?: number;
      signatureRequired?: boolean;
      photoRequired?: boolean;
      notes?: string;
      positionConstraint?: 'first' | 'none'; // Position constraint sistemi
    }
  }>({});

  // Edit modal state'leri
  const [editTimeWindowStart, setEditTimeWindowStart] = useState('');
  const [editTimeWindowEnd, setEditTimeWindowEnd] = useState('');
  const [editServiceTime, setEditServiceTime] = useState('10');
  const [editSignatureRequired, setEditSignatureRequired] = useState(false);
  const [editPhotoRequired, setEditPhotoRequired] = useState(false);
  const [editNotes, setEditNotes] = useState('');
  const [editPositionConstraint, setEditPositionConstraint] = useState<'first' | 'none'>('none'); // Position constraint

  useEffect(() => {
    loadData();
  }, []);

  useEffect(() => {
    const normalizedQuery = normalizeSearchText(searchQuery);
    if (normalizedQuery) {
      const filtered = customers.filter(customer =>
        !selectedCustomers.find(sc => sc.id === customer.id) && (
          normalizeSearchText(customer.name).includes(normalizedQuery) ||
          normalizeSearchText(customer.code).includes(normalizedQuery) ||
          normalizeSearchText(customer.address).includes(normalizedQuery)
        )
      );
      setFilteredCustomers(filtered);
    } else {
      setFilteredCustomers([]);
    }
  }, [searchQuery, customers, selectedCustomers]);

  const loadData = async () => {
    try {
      const [routeData, customersData] = await Promise.all([
        routeService.getById(routeId),
        customerService.getAll(),
      ]);
      
      setRoute(routeData);
      setCustomers(customersData);
      
      if (routeData.stops && routeData.stops.length > 0) {
        const existingCustomers = routeData.stops
          .map(stop => customersData.find(c => c.id === stop.customerId))
          .filter(Boolean) as Customer[];
        setSelectedCustomers(existingCustomers);
      }
    } catch (error) {
      console.error('Error loading data:', error);
      Alert.alert('Hata', 'Veriler yüklenirken bir hata oluştu');
    } finally {
      setLoading(false);
    }
  };

  // Saat formatı için otomatik iki nokta ekleme fonksiyonu
  const formatTimeInput = (text: string): string => {
    // Sadece rakamları al
    const numbers = text.replace(/[^\d]/g, '');
    
    if (numbers.length <= 2) {
      return numbers;
    } else if (numbers.length <= 4) {
      return `${numbers.slice(0, 2)}:${numbers.slice(2)}`;
    }
    return `${numbers.slice(0, 2)}:${numbers.slice(2, 4)}`;
  };

  // Time window formatını backend için düzeltme
  const formatTimeForBackend = (time: string): string | undefined => {
    if (!time) return undefined;
    
    // "09:00" formatını kontrol et ve gerekirse "09:00:00" yap
    const parts = time.split(':');
    if (parts.length === 2) {
      return `${time}:00`;
    }
    return time;
  };

  const handleAddCustomer = (customer: Customer) => {
    if (!selectedCustomers.find(c => c.id === customer.id)) {
      setSelectedCustomers([...selectedCustomers, customer]);
      setSearchQuery('');
      setShowCustomerModal(false);
    }
  };

  const handleBulkToggleCustomer = (customerId: number) => {
    setBulkSelectedCustomers(prev => {
      if (prev.includes(customerId)) {
        return prev.filter(id => id !== customerId);
      } else {
        return [...prev, customerId];
      }
    });
  };

  const handleBulkAddCustomers = () => {
    const newCustomers = customers.filter(customer =>
      bulkSelectedCustomers.includes(customer.id) &&
      !selectedCustomers.find(sc => sc.id === customer.id)
    );

    if (newCustomers.length > 0) {
      setSelectedCustomers([...selectedCustomers, ...newCustomers]);
      setBulkSelectedCustomers([]);
      setShowBulkModal(false);
    }
  };

  const handleBulkSelectAll = () => {
    const availableCustomers = customers.filter(customer =>
      !selectedCustomers.find(sc => sc.id === customer.id)
    );
    setBulkSelectedCustomers(availableCustomers.map(c => c.id));
  };

  const handleBulkDeselectAll = () => {
    setBulkSelectedCustomers([]);
  };

  const handleRemoveCustomer = (customerId: number) => {
    setSelectedCustomers(selectedCustomers.filter(c => c.id !== customerId));
    // Override'ları da temizle
    const newOverrides = { ...stopOverrides };
    delete newOverrides[customerId.toString()];
    setStopOverrides(newOverrides);
  };

  const handleEditCustomer = (customer: Customer) => {
    setEditingCustomer(customer);
    
    // Mevcut override değerlerini yükle
    const overrides = stopOverrides[customer.id.toString()] || {};
    setEditTimeWindowStart(overrides.timeWindowStart || customer.timeWindowStart || '');
    setEditTimeWindowEnd(overrides.timeWindowEnd || customer.timeWindowEnd || '');
    setEditServiceTime((overrides.serviceTime || customer.estimatedServiceTime || 10).toString());
    setEditSignatureRequired(overrides.signatureRequired || false);
    setEditPhotoRequired(overrides.photoRequired || false);
    setEditNotes(overrides.notes || '');
    setEditPositionConstraint(overrides.positionConstraint || 'none'); // Position constraint
    
    setShowEditModal(true);
  };

  const handleSaveEdit = () => {
    if (!editingCustomer) return;

    const customerId = editingCustomer.id.toString();

    // Position constraint validation
    if (editPositionConstraint !== 'none') {
      const existingConstraints = Object.entries(stopOverrides)
        .filter(([id, override]) => id !== customerId && override.positionConstraint === editPositionConstraint);

      if (existingConstraints.length > 0) {
        Alert.alert(
          'Uyarı',
          `Sadece bir durak İlk Durak olarak işaretlenebilir. Önce diğer İlk Durak işaretini kaldırın.`
        );
        return;
      }
    }

    const newOverrides = { ...stopOverrides };
    
    // Sadece değişen değerleri override olarak sakla
    newOverrides[customerId] = {};
    
    if (editTimeWindowStart !== (editingCustomer.timeWindowStart || '')) {
      newOverrides[customerId].timeWindowStart = editTimeWindowStart;
    }
    if (editTimeWindowEnd !== (editingCustomer.timeWindowEnd || '')) {
      newOverrides[customerId].timeWindowEnd = editTimeWindowEnd;
    }
    if (parseInt(editServiceTime) !== (editingCustomer.estimatedServiceTime || 10)) {
      newOverrides[customerId].serviceTime = parseInt(editServiceTime);
    }
    if (editSignatureRequired) {
      newOverrides[customerId].signatureRequired = true;
    }
    if (editPhotoRequired) {
      newOverrides[customerId].photoRequired = true;
    }
    if (editNotes) {
      newOverrides[customerId].notes = editNotes;
    }
    if (editPositionConstraint !== 'none') {
      newOverrides[customerId].positionConstraint = editPositionConstraint;
    }

    // Eğer hiç override yoksa sil
    if (Object.keys(newOverrides[customerId]).length === 0) {
      delete newOverrides[customerId];
    }

    setStopOverrides(newOverrides);
    setShowEditModal(false);
    setEditingCustomer(null);
  };

  const handleMoveUp = (index: number) => {
    if (index > 0) {
      const newCustomers = [...selectedCustomers];
      [newCustomers[index], newCustomers[index - 1]] = [newCustomers[index - 1], newCustomers[index]];
      setSelectedCustomers(newCustomers);
    }
  };

  const handleMoveDown = (index: number) => {
    if (index < selectedCustomers.length - 1) {
      const newCustomers = [...selectedCustomers];
      [newCustomers[index], newCustomers[index + 1]] = [newCustomers[index + 1], newCustomers[index]];
      setSelectedCustomers(newCustomers);
    }
  };

  const handleOptimizeRoute = async () => {
    if (selectedCustomers.length < 2) {
      Alert.alert('Uyarı', 'Optimizasyon için en az 2 müşteri eklemelisiniz');
      return;
    }

    if (!route) {
      Alert.alert('Hata', 'Rota bilgisi bulunamadı');
      return;
    }

    setOptimizing(true);

    try {
      const stops: RouteStop[] = selectedCustomers.map((customer, index) => {
        const overrides = stopOverrides[customer.id.toString()] || {};
        
        return {
          customerId: customer.id,
          customer,
          order: index + 1,
          name: customer.name,
          address: customer.address,
          latitude: customer.latitude,
          longitude: customer.longitude,
          contactFullName: customer.name,
          contactPhone: customer.phone,
          contactEmail: customer.email,
          type: 10,
          orderType: overrides.positionConstraint === 'first' ? 10 : 20, // Position constraint to OrderType (10=First, 20=Auto)
          proofOfDeliveryRequired: false,
          signatureRequired: overrides.signatureRequired || false,
          photoRequired: overrides.photoRequired || false,
          serviceTime: `00:${(overrides.serviceTime || customer.estimatedServiceTime || 10).toString().padStart(2, '0')}:00`,
          arriveBetweenStart: formatTimeForBackend(overrides.timeWindowStart || ''),
          arriveBetweenEnd: formatTimeForBackend(overrides.timeWindowEnd || ''),
          notes: overrides.notes,
        };
      });

      await routeService.update(routeId, { stops });

      const optimizedRoute = await routeService.optimize(routeId, 'distance');

      if (optimizedRoute.success) {
        Alert.alert(
          'Başarılı',
          `Rota optimize edildi!\nToplam Mesafe: ${optimizedRoute.totalDistance.toFixed(1)} km\nTahmini Süre: ${Math.floor(optimizedRoute.totalDuration / 60)} saat ${optimizedRoute.totalDuration % 60} dakika`,
          [
            {
              text: 'Sefer Oluştur',
              onPress: handleCreateJourney
            },
            {
              text: 'Tamam'
            }
          ]
        );
      } else {
        throw new Error(optimizedRoute.message || 'Optimizasyon başarısız');
      }
    } catch (error: any) {
      console.error('Error optimizing route:', error);
      Alert.alert('Hata', error.message || 'Rota optimize edilemedi');
    } finally {
      setOptimizing(false);
    }
  };

  const handleCreateJourney = async () => {
    if (!route || !route.driverId) {
      Alert.alert('Hata', 'Sefer oluşturmak için sürücü ataması gerekli');
      return;
    }

    setCreatingJourney(true);

    try {
      const journey = await journeyService.createFromRoute(routeId, route.driverId);
      
      Alert.alert('Başarılı', 'Sefer oluşturuldu', [
        {
          text: 'Sefer Detayına Git',
          onPress: () => navigation.navigate('JourneyDetail', { journeyId: journey.id })
        },
        {
          text: 'Rotalar Sayfasına Git',
          onPress: () => {
            navigation.navigate('Routes');
            // Stack'i temizle ve ana routes sayfasına dön
            navigation.reset({
              index: 0,
              routes: [{ name: 'Routes' }],
            });
          }
        }
      ]);
    } catch (error: any) {
      console.error('Error creating journey:', error);
      Alert.alert('Hata', 'Sefer oluşturulamadı: ' + (error.message || 'Bilinmeyen hata'));
    } finally {
      setCreatingJourney(false);
    }
  };

  const getPositionLabel = (position: 'first' | 'none') => {
    switch(position) {
      case 'first': return 'İlk Durak';
      case 'none': return 'Serbest';
      default: return 'Serbest';
    }
  };

  const getPositionColor = (position: 'first' | 'none') => {
    switch(position) {
      case 'first': return '#10B981';
      case 'none': return '#6B7280';
      default: return '#6B7280';
    }
  };

  const renderCustomerItem = ({ item, index }: { item: Customer; index: number }) => {
    const overrides = stopOverrides[item.id.toString()] || {};
    const hasOverrides = Object.keys(overrides).length > 0;
    
    return (
      <View style={styles.customerItem}>
        <View style={styles.customerHandle}>
          <Text style={styles.customerOrder}>{index + 1}</Text>
        </View>
        
        <View style={styles.customerInfo}>
          <Text style={styles.customerName}>{item.name}</Text>
          <Text style={styles.customerAddress} numberOfLines={1}>
            {item.address}
          </Text>
          
          {/* Override göstergeleri */}
          {hasOverrides && (
            <View style={styles.overrideIndicators}>
              {overrides.timeWindowStart && (
                <View style={styles.overrideBadge}>
                  <Icon name="clock-outline" size={12} color="#F97316" />
                  <Text style={styles.overrideText}>
                    {overrides.timeWindowStart} - {overrides.timeWindowEnd}
                  </Text>
                </View>
              )}
              {overrides.positionConstraint && overrides.positionConstraint !== 'none' && (
                <View style={[styles.overrideBadge, { backgroundColor: `${getPositionColor(overrides.positionConstraint)}20` }]}>
                  <Text style={[styles.overrideText, { color: getPositionColor(overrides.positionConstraint) }]}>
                    {getPositionLabel(overrides.positionConstraint)}
                  </Text>
                </View>
              )}
              {overrides.signatureRequired && (
                <View style={styles.overrideBadge}>
                  <Icon name="draw" size={12} color="#8B5CF6" />
                  <Text style={styles.overrideText}>İmza</Text>
                </View>
              )}
              {overrides.photoRequired && (
                <View style={styles.overrideBadge}>
                  <Icon name="camera" size={12} color="#6366F1" />
                  <Text style={styles.overrideText}>Fotoğraf</Text>
                </View>
              )}
            </View>
          )}
        </View>

        <View style={styles.customerActions}>
          <TouchableOpacity
            style={styles.actionButton}
            onPress={() => handleEditCustomer(item)}
          >
            <Icon name="pencil" size={16} color="#6B7280" />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.moveButton}
            onPress={() => handleMoveUp(index)}
            disabled={index === 0}
          >
            <Icon name="chevron-up" size={20} color={index === 0 ? '#D1D5DB' : '#6B7280'} />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.moveButton}
            onPress={() => handleMoveDown(index)}
            disabled={index === selectedCustomers.length - 1}
          >
            <Icon name="chevron-down" size={20} color={index === selectedCustomers.length - 1 ? '#D1D5DB' : '#6B7280'} />
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.removeButton}
            onPress={() => handleRemoveCustomer(item.id)}
          >
            <Icon name="close" size={20} color="#EF4444" />
          </TouchableOpacity>
        </View>
      </View>
    );
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
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{route?.name}</Text>
        <Text style={styles.subtitle}>Müşteri ekleyin ve rotayı optimize edin</Text>
      </View>

      <View style={styles.addButtonsContainer}>
        <TouchableOpacity
          style={[styles.addButton, styles.addButtonHalf]}
          onPress={() => setShowCustomerModal(true)}
        >
          <Icon name="plus-circle" size={20} color="#3B82F6" />
          <Text style={styles.addButtonText}>Tekli Ekle</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={[styles.addButton, styles.addButtonHalf]}
          onPress={() => setShowBulkModal(true)}
        >
          <Icon name="playlist-plus" size={20} color="#059669" />
          <Text style={[styles.addButtonText, { color: '#059669' }]}>Toplu Ekle</Text>
        </TouchableOpacity>
      </View>

      {selectedCustomers.length > 0 ? (
        <View style={styles.listContainer}>
          <View style={styles.listHeader}>
            <Text style={styles.listTitle}>
              Seçili Müşteriler ({selectedCustomers.length})
            </Text>
            <Text style={styles.dragHint}>Düzenlemek için kalem ikonuna tıklayın</Text>
          </View>
          
          <FlatList
            data={selectedCustomers}
            renderItem={renderCustomerItem}
            keyExtractor={(item) => item.id.toString()}
            contentContainerStyle={styles.listContent}
          />
        </View>
      ) : (
        <View style={styles.emptyState}>
          <Icon name="map-marker-plus" size={64} color="#D1D5DB" />
          <Text style={styles.emptyText}>Henüz müşteri eklenmedi</Text>
          <Text style={styles.emptySubtext}>
            Yukarıdaki butonu kullanarak müşteri ekleyin
          </Text>
        </View>
      )}

      {selectedCustomers.length >= 2 && (
        <TouchableOpacity
          style={styles.optimizeButton}
          onPress={handleOptimizeRoute}
          disabled={optimizing}
        >
          {optimizing ? (
            <ActivityIndicator size="small" color="#fff" />
          ) : (
            <>
              <Icon name="map-marker-path" size={24} color="#fff" />
              <Text style={styles.optimizeButtonText}>Rotayı Optimize Et</Text>
            </>
          )}
        </TouchableOpacity>
      )}

      {/* Customer Selection Modal */}
      <Modal
        visible={showCustomerModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowCustomerModal(false)}
      >
        <TouchableOpacity 
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowCustomerModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Müşteri Seç</Text>
              <TouchableOpacity
                onPress={() => setShowCustomerModal(false)}
                style={styles.modalCloseButton}
              >
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>
            
            <View style={styles.searchContainer}>
              <Icon name="magnify" size={20} color="#9CA3AF" />
              <TextInput
                style={styles.searchInput}
                placeholder="Müşteri ara..."
                placeholderTextColor="#9CA3AF"
                value={searchQuery}
                onChangeText={setSearchQuery}
              />
            </View>
            
            <FlatList
              data={filteredCustomers}
              keyExtractor={(item) => item.id.toString()}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={styles.customerOption}
                  onPress={() => handleAddCustomer(item)}
                >
                  <View style={styles.customerOptionInfo}>
                    <Text style={styles.customerOptionName}>{item.name}</Text>
                    <Text style={styles.customerOptionAddress} numberOfLines={1}>
                      {item.address}
                    </Text>
                  </View>
                  <Icon name="plus-circle" size={24} color="#10B981" />
                </TouchableOpacity>
              )}
              ListEmptyComponent={
                <View style={styles.noResults}>
                  <Text style={styles.noResultsText}>
                    {searchQuery ? 'Sonuç bulunamadı' : 'Aramaya başlayın'}
                  </Text>
                </View>
              }
            />
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Edit Customer Modal */}
      <Modal
        visible={showEditModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowEditModal(false)}
      >
        <TouchableOpacity 
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowEditModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>
                {editingCustomer?.name} - Durak Ayarları
              </Text>
              <TouchableOpacity
                onPress={() => setShowEditModal(false)}
                style={styles.modalCloseButton}
              >
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>

            <ScrollView style={styles.editForm}>
              <Text style={styles.editSectionTitle}>Durak Sırası</Text>
              <View style={styles.priorityContainer}>
                {[
                  { value: 'none', label: 'Serbest', color: '#6B7280' },
                  { value: 'first', label: 'İlk Durak', color: '#10B981' }
                ].map((position) => (
                  <TouchableOpacity
                    key={position.value}
                    style={[
                      styles.priorityButton,
                      editPositionConstraint === position.value && {
                        backgroundColor: position.color,
                        borderColor: position.color
                      }
                    ]}
                    onPress={() => setEditPositionConstraint(position.value as 'first' | 'none')}
                  >
                    <Text style={[
                      styles.priorityButtonText,
                      editPositionConstraint === position.value && styles.priorityButtonTextActive
                    ]}>
                      {position.label}
                    </Text>
                  </TouchableOpacity>
                ))}
              </View>

              <Text style={styles.editSectionTitle}>Zaman Penceresi</Text>
              <View style={styles.timeWindowRow}>
                <View style={styles.timeInputContainer}>
                  <Text style={styles.inputLabel}>Başlangıç</Text>
                  <TextInput
                    style={styles.timeInput}
                    value={editTimeWindowStart}
                    onChangeText={(text) => setEditTimeWindowStart(formatTimeInput(text))}
                    placeholder="08:00"
                    placeholderTextColor="#9CA3AF"
                    keyboardType="numeric"
                    maxLength={5}
                  />
                </View>
                <View style={styles.timeInputContainer}>
                  <Text style={styles.inputLabel}>Bitiş</Text>
                  <TextInput
                    style={styles.timeInput}
                    value={editTimeWindowEnd}
                    onChangeText={(text) => setEditTimeWindowEnd(formatTimeInput(text))}
                    placeholder="17:00"
                    placeholderTextColor="#9CA3AF"
                    keyboardType="numeric"
                    maxLength={5}
                  />
                </View>
              </View>

              <Text style={styles.editSectionTitle}>Servis Süresi (dakika)</Text>
              <TextInput
                style={styles.input}
                value={editServiceTime}
                onChangeText={setEditServiceTime}
                keyboardType="numeric"
                placeholder="10"
                placeholderTextColor="#9CA3AF"
              />

              <Text style={styles.editSectionTitle}>Teslimat Gereksinimleri</Text>
              <View style={styles.switchRow}>
                <Text style={styles.switchLabel}>İmza Zorunlu</Text>
                <Switch
                  value={editSignatureRequired}
                  onValueChange={setEditSignatureRequired}
                  trackColor={{ false: '#D1D5DB', true: '#8B5CF6' }}
                  thumbColor={editSignatureRequired ? '#fff' : '#f4f3f4'}
                />
              </View>

              <View style={styles.switchRow}>
                <Text style={styles.switchLabel}>Fotoğraf Zorunlu</Text>
                <Switch
                  value={editPhotoRequired}
                  onValueChange={setEditPhotoRequired}
                  trackColor={{ false: '#D1D5DB', true: '#6366F1' }}
                  thumbColor={editPhotoRequired ? '#fff' : '#f4f3f4'}
                />
              </View>

              <Text style={styles.editSectionTitle}>Notlar</Text>
              <TextInput
                style={[styles.input, styles.textArea]}
                value={editNotes}
                onChangeText={setEditNotes}
                placeholder="Bu durak için özel notlar..."
                placeholderTextColor="#9CA3AF"
                multiline
                numberOfLines={3}
              />

              <TouchableOpacity
                style={styles.saveButton}
                onPress={handleSaveEdit}
              >
                <Icon name="content-save" size={20} color="#fff" />
                <Text style={styles.saveButtonText}>Kaydet</Text>
              </TouchableOpacity>
            </ScrollView>
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>

      {/* Bulk Customer Selection Modal */}
      <Modal
        visible={showBulkModal}
        animationType="slide"
        transparent={true}
        onRequestClose={() => setShowBulkModal(false)}
      >
        <TouchableOpacity
          style={styles.modalContainer}
          activeOpacity={1}
          onPress={() => setShowBulkModal(false)}
        >
          <TouchableOpacity
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Toplu Müşteri Seçimi</Text>
              <TouchableOpacity
                onPress={() => setShowBulkModal(false)}
                style={styles.modalCloseButton}
              >
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>

            <View style={styles.bulkActionsHeader}>
              <TouchableOpacity
                style={styles.bulkActionButton}
                onPress={handleBulkSelectAll}
              >
                <Icon name="checkbox-marked" size={16} color="#059669" />
                <Text style={styles.bulkActionText}>Tümünü Seç</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.bulkActionButton}
                onPress={handleBulkDeselectAll}
              >
                <Icon name="checkbox-blank-outline" size={16} color="#EF4444" />
                <Text style={styles.bulkActionText}>Tümünü Temizle</Text>
              </TouchableOpacity>

              <Text style={styles.selectedCountText}>
                {bulkSelectedCustomers.length} seçili
              </Text>
            </View>

            <FlatList
              data={customers.filter(customer =>
                !selectedCustomers.find(sc => sc.id === customer.id)
              )}
              keyExtractor={(item) => item.id.toString()}
              renderItem={({ item }) => (
                <TouchableOpacity
                  style={styles.bulkCustomerOption}
                  onPress={() => handleBulkToggleCustomer(item.id)}
                >
                  <TouchableOpacity
                    style={styles.checkboxContainer}
                    onPress={() => handleBulkToggleCustomer(item.id)}
                  >
                    <Icon
                      name={bulkSelectedCustomers.includes(item.id) ? "checkbox-marked" : "checkbox-blank-outline"}
                      size={20}
                      color={bulkSelectedCustomers.includes(item.id) ? "#059669" : "#9CA3AF"}
                    />
                  </TouchableOpacity>

                  <View style={styles.bulkCustomerInfo}>
                    <Text style={styles.customerOptionName}>{item.name}</Text>
                    <Text style={styles.customerOptionAddress} numberOfLines={1}>
                      {item.address}
                    </Text>
                    {item.code && (
                      <Text style={styles.customerCode}>Kod: {item.code}</Text>
                    )}
                  </View>
                </TouchableOpacity>
              )}
              ListEmptyComponent={
                <View style={styles.noResults}>
                  <Text style={styles.noResultsText}>
                    Tüm müşteriler zaten eklendi
                  </Text>
                </View>
              }
            />

            {bulkSelectedCustomers.length > 0 && (
              <View style={styles.bulkAddButtonContainer}>
                <TouchableOpacity
                  style={styles.bulkAddButton}
                  onPress={handleBulkAddCustomers}
                >
                  <Icon name="playlist-plus" size={20} color="#fff" />
                  <Text style={styles.bulkAddButtonText}>
                    {bulkSelectedCustomers.length} Müşteriyi Ekle
                  </Text>
                </TouchableOpacity>
              </View>
            )}
          </TouchableOpacity>
        </TouchableOpacity>
      </Modal>
    </View>
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
  header: {
    backgroundColor: '#fff',
    paddingHorizontal: 16,
    paddingVertical: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 4,
  },
  subtitle: {
    fontSize: 14,
    color: '#6B7280',
  },
  addButtonsContainer: {
    flexDirection: 'row',
    marginHorizontal: 16,
    marginTop: 16,
    gap: 8,
  },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#fff',
    padding: 16,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderStyle: 'dashed',
  },
  addButtonHalf: {
    flex: 1,
  },
  addButtonText: {
    fontSize: 16,
    fontWeight: '600',
    color: '#3B82F6',
    marginLeft: 8,
  },
  listContainer: {
    flex: 1,
    marginHorizontal: 16,
    marginTop: 16,
  },
  listHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  listTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#374151',
  },
  dragHint: {
    fontSize: 12,
    color: '#6B7280',
  },
  listContent: {
    paddingBottom: 20,
  },
  customerItem: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: 12,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 2,
    elevation: 2,
  },
  customerHandle: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  customerOrder: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  customerInfo: {
    flex: 1,
  },
  customerName: {
    fontSize: 15,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 2,
  },
  customerAddress: {
    fontSize: 13,
    color: '#6B7280',
  },
  customerActions: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  actionButton: {
    padding: 4,
  },
  moveButton: {
    padding: 4,
  },
  removeButton: {
    padding: 4,
  },
  overrideIndicators: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginTop: 4,
    gap: 4,
  },
  overrideBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FEF3C7',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 4,
    gap: 2,
  },
  overrideText: {
    fontSize: 10,
    color: '#92400E',
  },
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 60,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#374151',
    marginTop: 16,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 4,
  },
  optimizeButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#3B82F6',
    marginHorizontal: 16,
    marginBottom: 16,
    paddingVertical: 16,
    borderRadius: 12,
  },
  optimizeButtonText: {
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
    maxHeight: '80%',
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
  modalCloseButton: {
    padding: 4,
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F3F4F6',
    marginHorizontal: 16,
    marginVertical: 12,
    paddingHorizontal: 12,
    paddingVertical: 10,
    borderRadius: 10,
  },
  searchInput: {
    flex: 1,
    marginLeft: 8,
    fontSize: 16,
    color: '#111827',
  },
  customerOption: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  customerOptionInfo: {
    flex: 1,
  },
  customerOptionName: {
    fontSize: 15,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 2,
  },
  customerOptionAddress: {
    fontSize: 13,
    color: '#6B7280',
  },
  noResults: {
    paddingVertical: 40,
    alignItems: 'center',
  },
  noResultsText: {
    fontSize: 14,
    color: '#6B7280',
  },
  editForm: {
    padding: 16,
  },
  editSectionTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#374151',
    marginTop: 16,
    marginBottom: 8,
  },
  priorityContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 8,
  },
  priorityButton: {
    flex: 1,
    paddingVertical: 8,
    borderRadius: 6,
    borderWidth: 1,
    borderColor: '#E5E7EB',
    alignItems: 'center',
  },
  priorityButtonText: {
    fontSize: 13,
    color: '#6B7280',
    fontWeight: '500',
  },
  priorityButtonTextActive: {
    color: '#fff',
  },
  timeWindowRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 12,
  },
  timeInputContainer: {
    flex: 1,
  },
  inputLabel: {
    fontSize: 12,
    color: '#6B7280',
    marginBottom: 4,
  },
  timeInput: {
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 14,
    color: '#111827',
  },
  input: {
    borderWidth: 1,
    borderColor: '#E5E7EB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 8,
    fontSize: 14,
    color: '#111827',
  },
  textArea: {
    minHeight: 80,
    textAlignVertical: 'top',
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 8,
  },
  switchLabel: {
    fontSize: 14,
    color: '#374151',
  },
  saveButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#3B82F6',
    borderRadius: 8,
    paddingVertical: 12,
    marginTop: 24,
    marginBottom: 16,
  },
  saveButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
    marginLeft: 8,
  },
  bulkActionsHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    backgroundColor: '#F9FAFB',
  },
  bulkActionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 6,
    marginRight: 12,
  },
  bulkActionText: {
    fontSize: 13,
    fontWeight: '500',
    color: '#374151',
    marginLeft: 4,
  },
  selectedCountText: {
    fontSize: 13,
    color: '#6B7280',
    marginLeft: 'auto',
    fontWeight: '500',
  },
  bulkCustomerOption: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  checkboxContainer: {
    marginRight: 12,
    padding: 4,
  },
  bulkCustomerInfo: {
    flex: 1,
  },
  customerCode: {
    fontSize: 11,
    color: '#9CA3AF',
    marginTop: 2,
  },
  bulkAddButtonContainer: {
    padding: 16,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    backgroundColor: '#F9FAFB',
  },
  bulkAddButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#059669',
    borderRadius: 8,
    paddingVertical: 12,
  },
  bulkAddButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
    marginLeft: 8,
  },
});

export default AddStopsToRouteScreen;

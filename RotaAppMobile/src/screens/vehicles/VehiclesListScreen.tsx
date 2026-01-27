import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  View,
  StyleSheet,
  FlatList,
  RefreshControl,
  Alert,
  Dimensions,
  InteractionManager,
  TouchableOpacity,
} from 'react-native';
import {
  Card,
  Text,
  Searchbar,
  FAB,
  ActivityIndicator,
  Surface,
  Button,
  Menu,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { Vehicle } from '../../types/vehicle.types';
import vehicleService from '../../services/vehicleService';
import { getFlatListOptimizations, isLargeScreen, isLowEndDevice } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

const SEARCH_DEBOUNCE = isLowEndDevice() ? 500 : 300;

const VehiclesListScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [filteredVehicles, setFilteredVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState<'all' | 'car' | 'van' | 'truck' | 'motorcycle'>('all');
  const [selectedStatus, setSelectedStatus] = useState<'all' | 'active' | 'maintenance' | 'inactive'>('all');
  const [searchTimeout, setSearchTimeout] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [showTypeMenu, setShowTypeMenu] = useState(false);
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  const flatListOptimizations = useMemo(() => getFlatListOptimizations(), []);

  const loadVehicles = useCallback(async (showLoader = true) => {
    try {
      if (showLoader) setLoading(true);

      const data = await vehicleService.getAll();
      setVehicles(data);
      filterVehicles(data, searchQuery, selectedType, selectedStatus);
    } catch (error: any) {
      console.error('Load vehicles error:', error);
      
      setVehicles([]);
      setFilteredVehicles([]);
      Alert.alert(
        'Hata', 
        'Araçlar yüklenemedi. Lütfen internet bağlantınızı kontrol edin.',
        [
          { text: 'Tekrar Dene', onPress: () => loadVehicles(showLoader) },
          { text: 'İptal', style: 'cancel' }
        ]
      );
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [searchQuery, selectedType, selectedStatus]);

  useEffect(() => {
    const task = InteractionManager.runAfterInteractions(() => {
      loadVehicles(true);
    });
    
    return () => task.cancel();
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadVehicles(false);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadVehicles(false);
  };

  const filterVehicles = (vehicleList: Vehicle[], query: string, type: string, status: string) => {
    let filtered = [...vehicleList];

    // Search filter
    if (query.trim()) {
      const searchLower = query.toLowerCase();
      filtered = filtered.filter(vehicle => {
        const plateNumber = vehicle.plateNumber?.toLowerCase() || '';
        const brand = vehicle.brand?.toLowerCase() || '';
        const model = vehicle.model?.toLowerCase() || '';
        
        return plateNumber.includes(searchLower) || 
               brand.includes(searchLower) || 
               model.includes(searchLower);
      });
    }

    // Type filter
    if (type !== 'all') {
      filtered = filtered.filter(vehicle => vehicle.type === type);
    }

    // Status filter
    if (status !== 'all') {
      filtered = filtered.filter(vehicle => vehicle.status === status);
    }
    
    setFilteredVehicles(filtered);
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
    
    const timeout = setTimeout(() => {
      filterVehicles(vehicles, query, selectedType, selectedStatus);
    }, SEARCH_DEBOUNCE);
    
    setSearchTimeout(timeout);
  };

  const handleTypeFilter = (type: typeof selectedType) => {
    setSelectedType(type);
    filterVehicles(vehicles, searchQuery, type, selectedStatus);
    setShowTypeMenu(false);
  };

  const handleStatusFilter = (status: typeof selectedStatus) => {
    setSelectedStatus(status);
    filterVehicles(vehicles, searchQuery, selectedType, status);
    setShowStatusMenu(false);
  };

  const getTypeLabel = (type: string) => {
    switch (type) {
      case 'car': return 'Otomobil';
      case 'van': return 'Van';
      case 'truck': return 'Kamyon';
      case 'motorcycle': return 'Motor';
      default: return 'Tüm Tipler';
    }
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'active': return 'Aktif';
      case 'maintenance': return 'Bakımda';
      case 'inactive': return 'Pasif';
      default: return 'Tüm Durumlar';
    }
  };

  const getVehicleIcon = (type: string) => {
    switch (type) {
      case 'truck':
        return 'truck';
      case 'van':
        return 'van-passenger';
      case 'motorcycle':
        return 'motorbike';
      default:
        return 'car';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return '#10B981';
      case 'maintenance':
        return '#F59E0B';
      case 'inactive':
        return '#6B7280';
      default:
        return '#6B7280';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'active':
        return 'check-circle';
      case 'maintenance':
        return 'wrench';
      case 'inactive':
        return 'close-circle';
      default:
        return 'help-circle';
    }
  };

  const getFuelTypeIcon = (fuelType: string) => {
    switch (fuelType) {
      case 'electric':
        return 'lightning-bolt';
      case 'hybrid':
        return 'leaf';
      case 'gasoline':
        return 'gas-station';
      case 'diesel':
      default:
        return 'fuel';
    }
  };


  const handleStatusChange = async (vehicleId: number, newStatus: 'active' | 'maintenance' | 'inactive') => {
    try {
      await vehicleService.updateStatus(vehicleId, newStatus);
      loadVehicles(false);
    } catch (error) {
      Alert.alert('Hata', 'Durum güncellenirken bir hata oluştu.');
    }
  };

  const renderVehicleCard = useCallback(({ item }: { item: Vehicle }) => {
    return (
      <Card 
        style={[
          styles.vehicleCard,
          isLargeScreen && styles.vehicleCardLarge
        ]}
        onPress={() => navigation.navigate('VehicleDetail', { vehicleId: item.id })}
      >
        <Card.Content>
          <View style={styles.cardHeader}>
            <View style={styles.headerLeft}>
              <View style={styles.vehicleInfo}>
                <Icon 
                  name={getVehicleIcon(item.type)} 
                  size={24} 
                  color="#3B82F6" 
                  style={styles.vehicleIcon}
                />
                <View style={styles.vehicleDetails}>
                  <Text style={[
                    styles.vehicleName,
                    isLargeScreen && styles.vehicleNameLarge
                  ]} numberOfLines={1}>
                    {item.brand} {item.model}
                  </Text>
                  <Text style={styles.plateNumber}>{item.plateNumber}</Text>
                </View>
              </View>
            </View>
            <View style={styles.headerRight}>
              <TouchableOpacity
                style={styles.editButton}
                onPress={(e) => {
                  e.stopPropagation();
                  navigation.navigate('EditVehicle', { vehicleId: item.id });
                }}
              >
                <Icon name="pencil" size={16} color="#3B82F6" />
              </TouchableOpacity>
              <View style={[
                styles.statusBadge, 
                { backgroundColor: `${getStatusColor(item.status)}20`, marginLeft: wp(2) }
              ]}>
                <Icon 
                  name={getStatusIcon(item.status)} 
                  size={12} 
                  color={getStatusColor(item.status)} 
                />
                <Text style={[
                  styles.statusBadgeText,
                  { color: getStatusColor(item.status), marginLeft: 4 }
                ]} numberOfLines={1}>
                  {vehicleService.getStatusLabel(item.status)}
                </Text>
              </View>
            </View>
          </View>

          <View style={styles.vehicleSpecs}>
            <View style={styles.specItem}>
              <Icon name="calendar" size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>{item.year}</Text>
            </View>
            <View style={styles.specItem}>
              <Icon name="weight-kilogram" size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>{item.capacity} kg</Text>
            </View>
            <View style={styles.specItem}>
              <Icon name={getFuelTypeIcon(item.fuelType)} size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>{vehicleService.getFuelTypeLabel(item.fuelType)}</Text>
            </View>
            <View style={styles.specItem}>
              <Icon name="tag" size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>{vehicleService.getVehicleTypeLabel(item.type)}</Text>
            </View>
          </View>

          <View style={styles.cardActions}>
            <TouchableOpacity
              style={[styles.actionButton, styles.statusButton]}
              onPress={(e) => {
                e.stopPropagation();
                const nextStatus = item.status === 'active' ? 'maintenance' : 
                                 item.status === 'maintenance' ? 'inactive' : 'active';
                handleStatusChange(item.id, nextStatus);
              }}
            >
              <Icon name="swap-horizontal" size={16} color="#F59E0B" />
              <Text style={[styles.actionButtonText, { color: '#F59E0B', marginLeft: 4 }]}>Durum</Text>
            </TouchableOpacity>
          </View>
        </Card.Content>
      </Card>
    );
  }, [navigation]);

  const EmptyComponent = () => (
    <View style={styles.emptyContainer}>
      <Icon name="car-multiple" size={64} color="#9CA3AF" />
      <Text style={styles.emptyText}>Araç Bulunamadı</Text>
      <Text style={styles.emptySubtext}>
        {searchQuery || selectedType !== 'all' || selectedStatus !== 'all' 
          ? 'Filtrelerinizi değiştirmeyi deneyin.'
          : 'Henüz hiç araç eklenmemiş.'
        }
      </Text>
    </View>
  );

  const keyExtractor = useCallback((item: Vehicle) => item.id.toString(), []);

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Araçlar yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Surface style={styles.searchContainer} elevation={1}>
        <Searchbar
          placeholder="Araç ara (plaka, marka, model)..."
          onChangeText={handleSearch}
          value={searchQuery}
          style={styles.searchBar}
          inputStyle={styles.searchInput}
        />

        <View style={styles.filtersRow}>
          <Menu
            visible={showTypeMenu}
            onDismiss={() => setShowTypeMenu(false)}
            anchor={
              <Button
                mode="outlined"
                onPress={() => setShowTypeMenu(true)}
                style={styles.filterDropdown}
                contentStyle={styles.filterDropdownContent}
                icon="chevron-down"
                compact
              >
                {getTypeLabel(selectedType)}
              </Button>
            }
          >
            <Menu.Item onPress={() => handleTypeFilter('all')} title="Tüm Tipler" />
            <Menu.Item onPress={() => handleTypeFilter('car')} title="Otomobil" leadingIcon="car" />
            <Menu.Item onPress={() => handleTypeFilter('van')} title="Van" leadingIcon="van-passenger" />
            <Menu.Item onPress={() => handleTypeFilter('truck')} title="Kamyon" leadingIcon="truck" />
            <Menu.Item onPress={() => handleTypeFilter('motorcycle')} title="Motor" leadingIcon="motorbike" />
          </Menu>

          <Menu
            visible={showStatusMenu}
            onDismiss={() => setShowStatusMenu(false)}
            anchor={
              <Button
                mode="outlined"
                onPress={() => setShowStatusMenu(true)}
                style={styles.filterDropdown}
                contentStyle={styles.filterDropdownContent}
                icon="chevron-down"
                compact
              >
                {getStatusLabel(selectedStatus)}
              </Button>
            }
          >
            <Menu.Item onPress={() => handleStatusFilter('all')} title="Tüm Durumlar" />
            <Menu.Item onPress={() => handleStatusFilter('active')} title="Aktif" leadingIcon="check-circle" />
            <Menu.Item onPress={() => handleStatusFilter('maintenance')} title="Bakımda" leadingIcon="wrench" />
            <Menu.Item onPress={() => handleStatusFilter('inactive')} title="Pasif" leadingIcon="close-circle" />
          </Menu>
        </View>
      </Surface>

      <FlatList
        data={filteredVehicles}
        renderItem={renderVehicleCard}
        keyExtractor={keyExtractor}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#3B82F6']} />
        }
        contentContainerStyle={[
          styles.listContent,
          isLargeScreen && styles.listContentLarge
        ]}
        ListEmptyComponent={EmptyComponent}
        {...flatListOptimizations}
      />

      <FAB
        icon="plus"
        style={styles.fab}
        onPress={() => navigation.navigate('CreateVehicle')}
        label="Yeni Araç"
      />
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
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#6B7280',
  },
  searchContainer: {
    backgroundColor: 'white',
    paddingHorizontal: wp(4),
    paddingVertical: hp(1.5),
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  searchBar: {
    backgroundColor: '#F9FAFB',
    elevation: 0,
    borderRadius: 8,
    height: hp(isLargeScreen ? 5 : 5.5),
    marginBottom: hp(2),
  },
  searchInput: {
    fontSize: isLargeScreen ? 13 : 14,
  },
  filtersRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: hp(1),
    minHeight: hp(6),
    backgroundColor: '#F3F4F6',
    borderRadius: 8,
    paddingHorizontal: wp(2),
  },
  filterDropdown: {
    flex: 1,
    marginHorizontal: wp(1),
    borderColor: '#D1D5DB',
    borderWidth: 1,
    borderRadius: 8,
    backgroundColor: '#FFFFFF',
  },
  filterDropdownContent: {
    height: hp(5),
    justifyContent: 'center',
  },
  listContent: {
    padding: wp(4),
    paddingBottom: hp(10),
  },
  listContentLarge: {
    padding: wp(3),
    paddingBottom: hp(12),
  },
  vehicleCard: {
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  vehicleCardLarge: {
    marginBottom: hp(2),
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: hp(1.5),
  },
  headerLeft: {
    flex: 1,
    marginRight: wp(2),
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  vehicleInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  vehicleIcon: {
    marginRight: wp(3),
  },
  vehicleDetails: {
    flex: 1,
  },
  vehicleName: {
    fontSize: isLargeScreen ? 15 : 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 2,
  },
  vehicleNameLarge: {
    fontSize: 15,
  },
  plateNumber: {
    fontSize: 13,
    color: '#6B7280',
    fontWeight: '500',
  },
  editButton: {
    padding: wp(2),
    borderRadius: 6,
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
  },
  statusBadge: {
    paddingHorizontal: wp(2),
    paddingVertical: hp(0.8),
    borderRadius: 12,
    flexDirection: 'row',
    alignItems: 'center',
    minWidth: wp(isLargeScreen ? 18 : 20),
  },
  statusBadgeText: {
    fontSize: isLargeScreen ? 10 : 11,
    fontWeight: '600',
  },
  vehicleSpecs: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginBottom: hp(1.5),
  },
  specItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: wp(4),
  },
  specText: {
    fontSize: 12,
    color: '#6B7280',
  },
  cardActions: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: hp(0.5),
  },
  actionButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: hp(1),
    borderRadius: 6,
  },
  statusButton: {
    backgroundColor: 'rgba(245, 158, 11, 0.1)',
  },
  actionButtonText: {
    fontSize: 12,
    fontWeight: '500',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: hp(12),
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    marginTop: hp(2),
  },
  emptySubtext: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: hp(1),
    textAlign: 'center',
    paddingHorizontal: wp(10),
  },
  fab: {
    position: 'absolute',
    margin: 16,
    right: 0,
    bottom: 0,
    backgroundColor: '#3B82F6',
  },
});

export default VehiclesListScreen;
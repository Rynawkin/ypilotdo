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
import { Driver } from '../../types/driver.types';
import driverService from '../../services/driverService';
import { getFlatListOptimizations, isLargeScreen, isLowEndDevice } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth } = Dimensions.get('window');

const SEARCH_DEBOUNCE = isLowEndDevice() ? 500 : 300;

const DriversListScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [filteredDrivers, setFilteredDrivers] = useState<Driver[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedStatus, setSelectedStatus] = useState<'all' | 'active' | 'inactive' | 'on_leave'>('all');
  const [searchTimeout, setSearchTimeout] = useState<ReturnType<typeof setTimeout> | null>(null);
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  const flatListOptimizations = useMemo(() => getFlatListOptimizations(), []);

  const loadDrivers = useCallback(async (showLoader = true) => {
    try {
      if (showLoader) setLoading(true);

      const data = await driverService.getAll();
      setDrivers(data);
      filterDrivers(data, searchQuery, selectedStatus);
    } catch (error: any) {
      console.error('Load drivers error:', error);
      
      setDrivers([]);
      setFilteredDrivers([]);
      Alert.alert(
        'Hata', 
        'Sürücüler yüklenemedi. Lütfen internet bağlantınızı kontrol edin.',
        [
          { text: 'Tekrar Dene', onPress: () => loadDrivers(showLoader) },
          { text: 'İptal', style: 'cancel' }
        ]
      );
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [searchQuery, selectedStatus]);

  useEffect(() => {
    const task = InteractionManager.runAfterInteractions(() => {
      loadDrivers(true);
    });
    
    return () => task.cancel();
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadDrivers(false);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadDrivers(false);
  };

  const filterDrivers = (driverList: Driver[], query: string, status: string) => {
    let filtered = [...driverList];

    // Search filter
    if (query.trim()) {
      const searchLower = query.toLowerCase();
      filtered = filtered.filter(driver => {
        const name = driver.name?.toLowerCase() || '';
        const phone = driver.phone?.toLowerCase() || '';
        const email = driver.email?.toLowerCase() || '';
        const licenseNumber = driver.licenseNumber?.toLowerCase() || '';
        
        return name.includes(searchLower) || 
               phone.includes(searchLower) || 
               email.includes(searchLower) ||
               licenseNumber.includes(searchLower);
      });
    }

    // Status filter
    if (status !== 'all') {
      filtered = filtered.filter(driver => driver.status === status);
    }
    
    setFilteredDrivers(filtered);
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
    
    const timeout = setTimeout(() => {
      filterDrivers(drivers, query, selectedStatus);
    }, SEARCH_DEBOUNCE);
    
    setSearchTimeout(timeout);
  };

  const handleStatusFilter = (status: typeof selectedStatus) => {
    setSelectedStatus(status);
    filterDrivers(drivers, searchQuery, status);
    setShowStatusMenu(false);
  };

  const getStatusLabel = (status: string) => {
    return driverService.getStatusLabel(status);
  };

  const getDriverIcon = (driver: Driver) => {
    if (driver.vehicle) {
      return 'car';
    }
    return 'account';
  };

  const getStatusColor = (status: string) => {
    return driverService.getStatusColor(status);
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'active':
        return 'account-check';
      case 'inactive':
        return 'account-off';
      case 'on_leave':
        return 'account-clock';
      default:
        return 'account';
    }
  };

  const getActivityStatusIcon = (driver: Driver) => {
    const activityStatus = driverService.getActivityStatus(driver);
    switch (activityStatus) {
      case 'online':
        return 'circle';
      case 'idle':
        return 'circle-outline';
      case 'offline':
      default:
        return 'circle-off-outline';
    }
  };

  const getActivityStatusColor = (driver: Driver) => {
    const activityStatus = driverService.getActivityStatus(driver);
    switch (activityStatus) {
      case 'online':
        return '#10B981';
      case 'idle':
        return '#F59E0B';
      case 'offline':
      default:
        return '#6B7280';
    }
  };

  const handleStatusChange = async (driverId: number, newStatus: 'active' | 'inactive' | 'on_leave') => {
    try {
      await driverService.updateStatus(driverId, newStatus);
      loadDrivers(false);
    } catch (error) {
      Alert.alert('Hata', 'Durum güncellenirken bir hata oluştu.');
    }
  };


  const renderDriverCard = useCallback(({ item }: { item: Driver }) => {
    return (
      <Card 
        style={[
          styles.driverCard,
          isLargeScreen && styles.driverCardLarge
        ]}
        onPress={() => navigation.navigate('DriverDetail', { driverId: item.id })}
      >
        <Card.Content>
          <View style={styles.cardHeader}>
            <View style={styles.headerLeft}>
              <View style={styles.driverInfo}>
                <Icon 
                  name={getDriverIcon(item)} 
                  size={24} 
                  color="#3B82F6" 
                  style={styles.driverIcon}
                />
                <View style={styles.driverDetails}>
                  <Text style={[
                    styles.driverName,
                    isLargeScreen && styles.driverNameLarge
                  ]} numberOfLines={1}>
                    {item.name}
                  </Text>
                  <Text style={styles.phoneNumber}>{driverService.formatPhone(item.phone)}</Text>
                </View>
              </View>
            </View>
            <View style={styles.headerRight}>
              <View style={styles.statusContainer}>
                <View style={[
                  styles.activityIndicator,
                  { backgroundColor: getActivityStatusColor(item) }
                ]}>
                  <Icon 
                    name={getActivityStatusIcon(item)} 
                    size={8} 
                    color="#FFFFFF"
                  />
                </View>
                <TouchableOpacity
                  style={styles.editButton}
                  onPress={(e) => {
                    e.stopPropagation();
                    navigation.navigate('EditDriver', { driverId: item.id });
                  }}
                >
                  <Icon name="pencil" size={16} color="#3B82F6" />
                </TouchableOpacity>
                <View style={[
                  styles.statusBadge, 
                  { backgroundColor: `${getStatusColor(item.status)}20` }
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
                    {getStatusLabel(item.status)}
                  </Text>
                </View>
              </View>
            </View>
          </View>

          <View style={styles.driverSpecs}>
            <View style={styles.specItem}>
              <Icon name="email" size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]} numberOfLines={1}>
                {item.email || 'Email yok'}
              </Text>
            </View>
            <View style={styles.specItem}>
              <Icon name="card-account-details" size={14} color="#6B7280" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>
                {item.licenseNumber}
              </Text>
            </View>
            {item.vehicle && (
              <View style={styles.specItem}>
                <Icon name="car" size={14} color="#6B7280" />
                <Text style={[styles.specText, { marginLeft: 4 }]}>
                  {item.vehicle.plateNumber}
                </Text>
              </View>
            )}
            <View style={styles.specItem}>
              <Icon name="star" size={14} color="#F59E0B" />
              <Text style={[styles.specText, { marginLeft: 4 }]}>
                {(item.rating || 0).toFixed(1)}
              </Text>
            </View>
          </View>

          <View style={styles.cardActions}>
            <TouchableOpacity
              style={[styles.actionButton, styles.statusButton]}
              onPress={(e) => {
                e.stopPropagation();
                const nextStatus = item.status === 'active' ? 'inactive' : 
                                 item.status === 'inactive' ? 'on_leave' : 'active';
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
      <Icon name="account-multiple" size={64} color="#9CA3AF" />
      <Text style={styles.emptyText}>Sürücü Bulunamadı</Text>
      <Text style={styles.emptySubtext}>
        {searchQuery || selectedStatus !== 'all' 
          ? 'Filtrelerinizi değiştirmeyi deneyin.'
          : 'Henüz hiç sürücü eklenmemiş.'
        }
      </Text>
    </View>
  );

  const keyExtractor = useCallback((item: Driver) => item.id.toString(), []);

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Sürücüler yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Surface style={styles.searchContainer} elevation={1}>
        <Searchbar
          placeholder="Sürücü ara (isim, telefon, email, ehliyet)..."
          onChangeText={handleSearch}
          value={searchQuery}
          style={styles.searchBar}
          inputStyle={styles.searchInput}
        />

        <View style={styles.filtersRow}>
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
                {getStatusLabel(selectedStatus === 'all' ? 'all' : selectedStatus)}
              </Button>
            }
          >
            <Menu.Item onPress={() => handleStatusFilter('all')} title="Tüm Durumlar" />
            <Menu.Item onPress={() => handleStatusFilter('active')} title="Aktif" leadingIcon="account-check" />
            <Menu.Item onPress={() => handleStatusFilter('inactive')} title="Pasif" leadingIcon="account-off" />
            <Menu.Item onPress={() => handleStatusFilter('on_leave')} title="İzinli" leadingIcon="account-clock" />
          </Menu>
        </View>
      </Surface>

      <FlatList
        data={filteredDrivers}
        renderItem={renderDriverCard}
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
        onPress={() => navigation.navigate('CreateDriver')}
        label="Yeni Sürücü"
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
  driverCard: {
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  driverCardLarge: {
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
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  driverInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  driverIcon: {
    marginRight: wp(3),
  },
  driverDetails: {
    flex: 1,
  },
  driverName: {
    fontSize: isLargeScreen ? 15 : 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 2,
  },
  driverNameLarge: {
    fontSize: 15,
  },
  phoneNumber: {
    fontSize: 13,
    color: '#6B7280',
    fontWeight: '500',
  },
  activityIndicator: {
    width: 12,
    height: 12,
    borderRadius: 6,
    marginRight: wp(2),
    justifyContent: 'center',
    alignItems: 'center',
  },
  editButton: {
    padding: wp(2),
    borderRadius: 6,
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
    marginRight: wp(2),
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
  driverSpecs: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginBottom: hp(1.5),
  },
  specItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: wp(4),
    marginBottom: hp(0.5),
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
    marginHorizontal: wp(1),
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

export default DriversListScreen;
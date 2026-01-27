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
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { Customer } from '../../types/customer.types';
import customerService from '../../services/customerService';
import { getFlatListOptimizations, isLargeScreen, isLowEndDevice } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';
import { normalizeSearchText } from '../../utils/string';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

const BATCH_SIZE = 20;
const SEARCH_DEBOUNCE = isLowEndDevice() ? 500 : 300;

const CustomersListScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [filteredCustomers, setFilteredCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [searchTimeout, setSearchTimeout] = useState<ReturnType<typeof setTimeout> | null>(null);

  const flatListOptimizations = useMemo(() => getFlatListOptimizations(), []);

  const loadCustomers = useCallback(async (showLoader = true, pageNum = 1, append = false) => {
    try {
      if (showLoader && pageNum === 1) setLoading(true);
      if (pageNum > 1) setLoadingMore(true);

      const data = await customerService.getAll();

      // BUGFIX: Use functional state update to avoid infinite loop
      if (append) {
        setCustomers(prevCustomers => {
          const newCustomers = [...prevCustomers, ...data];
          // Use latest searchQuery via closure
          const query = searchQuery;
          filterCustomers(newCustomers, query);
          return newCustomers;
        });
      } else {
        setCustomers(data);
        filterCustomers(data, searchQuery);
      }

      setHasMore(false);
    } catch (error: any) {
      console.error('Load customers error:', error);

      if (pageNum === 1) {
        setCustomers([]);
        setFilteredCustomers([]);
      }
      Alert.alert(
        'Hata',
        'Müşteriler yüklenemedi. Lütfen internet bağlantınızı kontrol edin.',
        [
          { text: 'Tekrar Dene', onPress: () => loadCustomers(showLoader, pageNum, append) },
          { text: 'İptal', style: 'cancel' }
        ]
      );
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  }, [searchQuery]); // BUGFIX: Removed 'customers' dependency to prevent infinite loop

  useEffect(() => {
    const task = InteractionManager.runAfterInteractions(() => {
      setPage(1);
      setHasMore(true);
      loadCustomers(true, 1, false);
    });
    
    return () => task.cancel();
  }, []);

  useFocusEffect(
    useCallback(() => {
      loadCustomers(false, 1, false);
    }, [])
  );

  const onRefresh = () => {
    setRefreshing(true);
    setPage(1);
    setHasMore(true);
    loadCustomers(false, 1, false);
  };

  const filterCustomers = (customerList: Customer[], query: string) => {
    const normalizedQuery = normalizeSearchText(query);
    if (!normalizedQuery) {
      setFilteredCustomers(customerList);
      return;
    }

    const filtered = customerList.filter(customer => {
      const customerName = normalizeSearchText(customer.name);
      const customerCode = normalizeSearchText(customer.code);
      const customerPhone = normalizeSearchText(customer.phone);
      
      return customerName.includes(normalizedQuery) || 
             customerCode.includes(normalizedQuery) || 
             customerPhone.includes(normalizedQuery);
    });
    
    setFilteredCustomers(filtered);
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
    
    const timeout = setTimeout(() => {
      filterCustomers(customers, query);
    }, SEARCH_DEBOUNCE);
    
    setSearchTimeout(timeout);
  };

  const getPriorityColor = (priority?: string) => {
    switch (priority?.toLowerCase()) {
      case 'high':
        return '#EF4444';
      case 'normal':
        return '#3B82F6';
      case 'low':
        return '#10B981';
      default:
        return '#6B7280';
    }
  };

  const getPriorityText = (priority?: string) => {
    switch (priority?.toLowerCase()) {
      case 'high':
        return 'Yüksek';
      case 'normal':
        return 'Normal';
      case 'low':
        return 'Düşük';
      default:
        return 'Normal';
    }
  };

  const renderCustomerCard = useCallback(({ item }: { item: Customer }) => {
    return (
      <Card 
        style={[
          styles.customerCard,
          isLargeScreen && styles.customerCardLarge
        ]}
        onPress={() => navigation.navigate('CustomerDetail', { customerId: item.id })}
      >
        <Card.Content>
          <View style={styles.cardHeader}>
            <View style={styles.headerLeft}>
              <Text style={[
                styles.customerName,
                isLargeScreen && styles.customerNameLarge
              ]} numberOfLines={1}>
                {item.name}
              </Text>
              <View style={styles.customerMeta}>
                <Icon name="identifier" size={14} color="#6B7280" />
                <Text style={styles.customerCode}>{item.code}</Text>
                {item.phone && (
                  <>
                    <Icon name="phone" size={14} color="#6B7280" style={{ marginLeft: 8 }} />
                    <Text style={styles.customerPhone}>{item.phone}</Text>
                  </>
                )}
              </View>
            </View>
            <View style={styles.headerRight}>
              <TouchableOpacity
                style={styles.editButton}
                onPress={(e) => {
                  e.stopPropagation();
                  navigation.navigate('EditCustomer', { customerId: item.id });
                }}
              >
                <Icon name="pencil" size={16} color="#3B82F6" />
              </TouchableOpacity>
              <View style={[
                styles.priorityBadge, 
                { backgroundColor: `${getPriorityColor(item.priority)}20` }
              ]}>
                <Text style={[
                  styles.priorityBadgeText,
                  { color: getPriorityColor(item.priority) }
                ]} numberOfLines={1}>
                  {getPriorityText(item.priority)}
                </Text>
              </View>
            </View>
          </View>

          {item.address && (
            <View style={styles.addressSection}>
              <Icon name="map-marker" size={14} color="#6B7280" />
              <Text style={styles.addressText} numberOfLines={2}>
                {item.address}
              </Text>
            </View>
          )}

          <View style={styles.cardFooter}>
            {item.email && (
              <View style={styles.footerItem}>
                <Icon name="email-outline" size={14} color="#6B7280" />
                <Text style={styles.footerText} numberOfLines={1}>
                  {item.email}
                </Text>
              </View>
            )}
            {item.estimatedServiceTime && (
              <View style={styles.footerItem}>
                <Icon name="clock-outline" size={14} color="#6B7280" />
                <Text style={styles.footerText}>
                  {item.estimatedServiceTime} dk
                </Text>
              </View>
            )}
          </View>
        </Card.Content>
      </Card>
    );
  }, [navigation]);

  const EmptyComponent = () => (
    <View style={styles.emptyContainer}>
      <Icon name="account-multiple-outline" size={64} color="#9CA3AF" />
      <Text style={styles.emptyText}>Müşteri Bulunamadı</Text>
      <Text style={styles.emptySubtext}>
        Henüz hiç müşteri eklenmemiş.
      </Text>
    </View>
  );

  const FooterComponent = () => {
    if (!loadingMore) return null;
    return (
      <View style={styles.footerLoader}>
        <ActivityIndicator size="small" color="#3B82F6" />
        <Text style={styles.footerLoaderText}>Daha fazla yükleniyor...</Text>
      </View>
    );
  };

  const keyExtractor = useCallback((item: Customer) => item.id.toString(), []);

  if (loading && page === 1) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Müşteriler yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <Surface style={styles.searchContainer} elevation={1}>
        <Searchbar
          placeholder="Müşteri ara (isim, kod, telefon)..."
          onChangeText={handleSearch}
          value={searchQuery}
          style={styles.searchBar}
          inputStyle={styles.searchInput}
        />
      </Surface>

      <FlatList
        data={filteredCustomers}
        renderItem={renderCustomerCard}
        keyExtractor={keyExtractor}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#3B82F6']} />
        }
        contentContainerStyle={[
          styles.listContent,
          isLargeScreen && styles.listContentLarge
        ]}
        ListEmptyComponent={EmptyComponent}
        ListFooterComponent={FooterComponent}
        {...flatListOptimizations}
      />

      <FAB
        icon="plus"
        style={styles.fab}
        onPress={() => navigation.navigate('CreateCustomer')}
        label="Yeni Müşteri"
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
  },
  searchInput: {
    fontSize: isLargeScreen ? 13 : 14,
  },
  listContent: {
    padding: wp(4),
    paddingBottom: hp(10),
  },
  listContentLarge: {
    padding: wp(3),
    paddingBottom: hp(12),
  },
  customerCard: {
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  customerCardLarge: {
    marginBottom: hp(2),
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: hp(1),
  },
  headerLeft: {
    flex: 1,
    marginRight: wp(2),
  },
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: wp(2),
  },
  editButton: {
    padding: wp(2),
    borderRadius: 6,
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
  },
  customerName: {
    fontSize: isLargeScreen ? 15 : 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 4,
  },
  customerNameLarge: {
    fontSize: 15,
  },
  customerMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  customerCode: {
    fontSize: 12,
    color: '#6B7280',
    marginLeft: 4,
  },
  customerPhone: {
    fontSize: 12,
    color: '#6B7280',
    marginLeft: 4,
  },
  priorityBadge: {
    paddingHorizontal: wp(2.5),
    paddingVertical: hp(0.8),
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: wp(isLargeScreen ? 18 : 20),
  },
  priorityBadgeText: {
    fontSize: isLargeScreen ? 10 : 11,
    fontWeight: '600',
  },
  addressSection: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: hp(1),
  },
  addressText: {
    fontSize: 13,
    color: '#4B5563',
    marginLeft: 6,
    flex: 1,
  },
  cardFooter: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: wp(4),
  },
  footerItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    flex: 1,
  },
  footerText: {
    fontSize: 12,
    color: '#6B7280',
    flex: 1,
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
  footerLoader: {
    paddingVertical: hp(2.5),
    alignItems: 'center',
    flexDirection: 'row',
    justifyContent: 'center',
  },
  footerLoaderText: {
    marginLeft: 8,
    fontSize: 13,
    color: '#6B7280',
  },
  fab: {
    position: 'absolute',
    margin: 16,
    right: 0,
    bottom: 0,
    backgroundColor: '#3B82F6',
  },
});

export default CustomersListScreen;

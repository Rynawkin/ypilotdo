// C:\Projects\RotaAppMobile\src\screens\routes\RoutesListScreen.tsx

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  Alert,
  RefreshControl,
  ActivityIndicator,
  Modal,
  TextInput,
  InteractionManager,
  Platform,
  Dimensions,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import routeService, { Route } from '../../services/routeService';
import journeyService from '../../services/journeyService';
import { getFlatListOptimizations, isLargeScreen, isLowEndDevice } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

const RoutesListScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [routes, setRoutes] = useState<Route[]>([]);
  const [filteredRoutes, setFilteredRoutes] = useState<Route[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [retryCount, setRetryCount] = useState(0);
  
  // Sefer oluşturma modal state'leri
  const [showJourneyModal, setShowJourneyModal] = useState(false);
  const [selectedRoute, setSelectedRoute] = useState<Route | null>(null);
  const [journeyName, setJourneyName] = useState('');
  const [creatingJourney, setCreatingJourney] = useState(false);

  // Debug log
  console.log('RoutesListScreen - Device Info:', {
    width: screenWidth,
    height: screenHeight,
    isLargeScreen: isLargeScreen,
    isLowEnd: isLowEndDevice()
  });

  // Performans optimizasyonu için memoized değerler
  const sevenDaysAgo = useMemo(() => {
    const date = new Date();
    date.setDate(date.getDate() - 7);
    date.setHours(0, 0, 0, 0);
    return date;
  }, []);

  // FlatList optimizasyonları
  const flatListOptimizations = useMemo(() => getFlatListOptimizations(), []);

  useEffect(() => {
    const task = InteractionManager.runAfterInteractions(() => {
      loadRoutes();
    });
    return () => task.cancel();
  }, []); // Boş array - sadece mount'ta çalışsın

  // Focus listener'ı ayrı useEffect'e al:
  useEffect(() => {
    const unsubscribe = navigation.addListener('focus', () => {
      loadRoutes();
    });
    return unsubscribe;
  }, [navigation]);

  const loadRoutes = useCallback(async () => {
    // Eski cihazlar ve büyük ekranlar için retry mekanizması
    const maxRetries = isLowEndDevice() ? 5 : 3;
    
    try {
      setLoading(true);
      
      // Büyük ekranlar için daha uzun timeout
      const timeoutDuration = isLargeScreen ? 45000 : 30000;
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), timeoutDuration);
      
      try {
        // Büyük ekranlar için sayfalama parametresi ekle
        const data = await routeService.getAll(); // Parametresiz dene
        clearTimeout(timeoutId);
        
        if (!data || !Array.isArray(data)) {
          throw new Error('Geçersiz veri formatı');
        }
        
        console.log('Loaded routes:', data.length);
        
        // Büyük ekranlar için daha fazla veri göster
        const daysToShow = isLargeScreen ? 14 : 7;
        const dateLimit = new Date();
        dateLimit.setDate(dateLimit.getDate() - daysToShow);
        dateLimit.setHours(0, 0, 0, 0);
        
        // Filtreleme işlemlerini optimize et
        const processedRoutes = data.filter(r => {
          if (!r || !r.date) return false;
          
          const routeDate = new Date(r.date);
          if (isNaN(routeDate.getTime())) return false;
          
          // Status kontrolü
          const validStatus = r.status === 'draft' || 
                            r.status === 'planned' ||
                            r.status === 'in_progress' || 
                            !r.status;
          
          // Tarih kontrolü
          const validDate = routeDate >= dateLimit;
          
          return validStatus && validDate;
        });
        
        console.log('Filtered routes:', processedRoutes.length);
        
        // Sıralama
        const sortedRoutes = processedRoutes.sort((a, b) => {
          const dateA = new Date(a.date).getTime();
          const dateB = new Date(b.date).getTime();
          return sortOrder === 'desc' ? dateB - dateA : dateA - dateB;
        });
        
        setRoutes(sortedRoutes);
        setFilteredRoutes(sortedRoutes);
        setRetryCount(0); // Başarılı olduğunda retry sayacını sıfırla
        
      } catch (fetchError: any) {
        clearTimeout(timeoutId);
        
        if (fetchError.name === 'AbortError') {
          throw new Error('Bağlantı zaman aşımına uğradı');
        }
        throw fetchError;
      }
      
    } catch (error: any) {
      console.error('Error loading routes:', error);
      
      // Retry mekanizması
      if (retryCount < maxRetries) {
        setRetryCount(prev => prev + 1);
        setTimeout(() => {
          loadRoutes();
        }, 2000 * (retryCount + 1)); // Exponential backoff
        
        Alert.alert(
          'Bağlantı Sorunu',
          `Rotalar yüklenmeye çalışılıyor... (Deneme: ${retryCount + 1}/${maxRetries})`,
          [{ text: 'Tamam' }]
        );
      } else {
        Alert.alert(
          'Hata',
          'Rotalar yüklenemedi. Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin.',
          [
            { text: 'Tekrar Dene', onPress: () => {
              setRetryCount(0);
              loadRoutes();
            }},
            { text: 'İptal', style: 'cancel' }
          ]
        );
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [sortOrder, sevenDaysAgo, retryCount]);

  const handleSearch = useCallback((text: string) => {
    setSearchQuery(text);
    
    if (!text.trim()) {
      setFilteredRoutes(routes);
      return;
    }
    
    const searchLower = text.toLowerCase().trim();
    const filtered = routes.filter(route => 
      route.name?.toLowerCase().includes(searchLower) || false
    );
    
    setFilteredRoutes(filtered);
  }, [routes]);

  const toggleSortOrder = useCallback(() => {
    const newOrder = sortOrder === 'desc' ? 'asc' : 'desc';
    setSortOrder(newOrder);
    
    const sorted = [...filteredRoutes].sort((a, b) => {
      const dateA = new Date(a.date).getTime();
      const dateB = new Date(b.date).getTime();
      return newOrder === 'desc' ? dateB - dateA : dateA - dateB;
    });
    
    setFilteredRoutes(sorted);
  }, [sortOrder, filteredRoutes]);

  const handleRefresh = useCallback(() => {
    setRefreshing(true);
    setRetryCount(0);
    loadRoutes();
  }, [loadRoutes]);

  const openJourneyModal = useCallback((route: Route) => {
    if (!route.driverId || !route.vehicleId) {
      Alert.alert('Uyarı', 'Bu rotaya sürücü ve araç ataması yapılmamış');
      return;
    }
    
    setSelectedRoute(route);
    const dateStr = format(new Date(route.date), 'dd.MM.yyyy');
    setJourneyName(`${route.name} - ${dateStr}`);
    setShowJourneyModal(true);
  }, []);

  const handleCreateJourney = useCallback(async () => {
    if (!selectedRoute || !journeyName.trim()) {
      Alert.alert('Uyarı', 'Lütfen sefer adı girin');
      return;
    }

    setCreatingJourney(true);
    try {
      const journey = await journeyService.createFromRoute(
        selectedRoute.id!,
        selectedRoute.driverId,
        journeyName
      );
      
      setShowJourneyModal(false);
      setSelectedRoute(null);
      setJourneyName('');
      
      Alert.alert('Başarılı', 'Sefer oluşturuldu', [
        {
          text: 'Seferler Sayfasına Git',
          onPress: () => navigation.navigate('Journeys')
        },
        {
          text: 'Tamam'
        }
      ]);
    } catch (error: any) {
      console.log('[RoutesListScreen] Journey creation error:', error);
      const errorMessage = error.userFriendlyMessage || error.message || 'Sefer oluşturulamadı';
      Alert.alert('Hata', errorMessage);
    } finally {
      setCreatingJourney(false);
    }
  }, [selectedRoute, journeyName, navigation]);

  const renderRouteItem = useCallback(({ item }: { item: Route }) => {
    const isOptimized = item.optimized;
    const hasDriver = !!item.driverId;
    const hasVehicle = !!item.vehicleId;
    const canCreateJourney = isOptimized && hasDriver && hasVehicle;

    return (
      <TouchableOpacity
        style={[
          styles.routeCard,
          isLargeScreen && styles.routeCardLarge
        ]}
        onPress={() => navigation.navigate('RouteDetail', { routeId: item.id })}
        activeOpacity={0.7}
      >
        <View style={styles.routeHeader}>
          <View style={styles.routeInfo}>
            <Text style={styles.routeName} numberOfLines={1}>
              {item.name || 'İsimsiz Rota'}
            </Text>
            <Text style={styles.routeDate}>
              {format(new Date(item.date), 'dd MMMM yyyy EEEE', { locale: tr })}
            </Text>
          </View>
          <View style={styles.routeStatus}>
            {isOptimized ? (
              <Icon name="check-circle" size={24} color="#10B981" />
            ) : (
              <Icon name="alert-circle" size={24} color="#F59E0B" />
            )}
          </View>
        </View>

        <View style={styles.routeDetails}>
          <View style={styles.detailRow}>
            <Icon name="map-marker-multiple" size={16} color="#6B7280" />
            <Text style={styles.detailText}>{item.stops?.length || 0} durak</Text>
          </View>
          
          {item.totalDistance > 0 && (
            <View style={styles.detailRow}>
              <Icon name="road-variant" size={16} color="#6B7280" />
              <Text style={styles.detailText}>{item.totalDistance.toFixed(1)} km</Text>
            </View>
          )}
          
          {item.totalDuration > 0 && (
            <View style={styles.detailRow}>
              <Icon name="clock-outline" size={16} color="#6B7280" />
              <Text style={styles.detailText}>
                {Math.floor(item.totalDuration / 60)} saat {item.totalDuration % 60} dk
              </Text>
            </View>
          )}
        </View>

        <View style={styles.routeAssignments}>
          <View style={styles.assignmentRow}>
            <Icon 
              name="account" 
              size={16} 
              color={hasDriver ? '#10B981' : '#EF4444'} 
            />
            <Text style={[styles.assignmentText, !hasDriver && styles.unassigned]}>
              {hasDriver ? 'Sürücü atandı' : 'Sürücü atanmadı'}
            </Text>
          </View>
          
          <View style={styles.assignmentRow}>
            <Icon 
              name="truck" 
              size={16} 
              color={hasVehicle ? '#10B981' : '#EF4444'} 
            />
            <Text style={[styles.assignmentText, !hasVehicle && styles.unassigned]}>
              {hasVehicle ? 'Araç atandı' : 'Araç atanmadı'}
            </Text>
          </View>
        </View>

        {canCreateJourney && (
          <TouchableOpacity
            style={styles.startButton}
            onPress={() => openJourneyModal(item)}
            activeOpacity={0.7}
          >
            <Icon name="truck-delivery" size={20} color="#fff" />
            <Text style={styles.startButtonText}>Sefer Oluştur</Text>
          </TouchableOpacity>
        )}

        {!isOptimized && (
          <TouchableOpacity
            style={[styles.startButton, styles.optimizeButton]}
            onPress={() => navigation.navigate('AddStopsToRoute', { routeId: item.id })}
            activeOpacity={0.7}
          >
            <Icon name="map-marker-path" size={20} color="#fff" />
            <Text style={styles.startButtonText}>Rotayı Optimize Et</Text>
          </TouchableOpacity>
        )}
      </TouchableOpacity>
    );
  }, [navigation, openJourneyModal]);

  // FlatList için optimizasyonlar
  const keyExtractor = useCallback((item: Route) => item.id?.toString() || '', []);
  
  // Büyük ekranlar için dinamik item layout
  const getItemLayout = useCallback((data: any, index: number) => {
    const itemHeight = isLargeScreen ? 280 : 250;
    return {
      length: itemHeight,
      offset: itemHeight * index,
      index,
    };
  }, []);

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Rotalar yükleniyor...</Text>
        {isLargeScreen && (
          <Text style={styles.debugText}>
            Ekran: {screenWidth}x{screenHeight}
          </Text>
        )}
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Arama ve Sıralama Toolbar */}
      <View style={styles.toolbar}>
        <View style={styles.searchContainer}>
          <Icon name="magnify" size={20} color="#6B7280" style={styles.searchIcon} />
          <TextInput
            style={styles.searchInput}
            placeholder="Rota ara..."
            placeholderTextColor="#9CA3AF"
            value={searchQuery}
            onChangeText={handleSearch}
            returnKeyType="search"
            autoCorrect={false}
          />
          {searchQuery.length > 0 && (
            <TouchableOpacity 
              onPress={() => handleSearch('')}
              hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
            >
              <Icon name="close-circle" size={20} color="#6B7280" />
            </TouchableOpacity>
          )}
        </View>
        
        <TouchableOpacity 
          style={styles.sortButton} 
          onPress={toggleSortOrder}
          activeOpacity={0.7}
        >
          <Icon 
            name={sortOrder === 'desc' ? 'sort-calendar-descending' : 'sort-calendar-ascending'} 
            size={24} 
            color="#3B82F6" 
          />
          <Text style={styles.sortText}>
            {sortOrder === 'desc' ? 'Yeni' : 'Eski'}
          </Text>
        </TouchableOpacity>
      </View>

      <FlatList
        data={filteredRoutes}
        keyExtractor={keyExtractor}
        renderItem={renderRouteItem}
        contentContainerStyle={[
          styles.listContent,
          isLargeScreen && styles.listContentLarge
        ]}
        refreshControl={
          <RefreshControl 
            refreshing={refreshing} 
            onRefresh={handleRefresh}
            colors={['#3B82F6']}
            tintColor="#3B82F6"
          />
        }
        {...flatListOptimizations}
        getItemLayout={getItemLayout}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Icon name="routes" size={64} color="#D1D5DB" />
            <Text style={styles.emptyText}>Henüz rota bulunmuyor</Text>
            <Text style={styles.emptySubtext}>
              Yeni rota oluşturmak için + butonuna basın
            </Text>
          </View>
        }
      />

      <TouchableOpacity
        style={styles.fab}
        onPress={() => navigation.navigate('CreateRoute')}
        activeOpacity={0.8}
      >
        <Icon name="plus" size={24} color="#fff" />
      </TouchableOpacity>

      {/* Sefer Adı Girme Modal'ı */}
      <Modal
        visible={showJourneyModal}
        transparent={true}
        animationType="slide"
        onRequestClose={() => setShowJourneyModal(false)}
      >
        <TouchableOpacity 
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setShowJourneyModal(false)}
        >
          <TouchableOpacity 
            style={styles.modalContent}
            activeOpacity={1}
            onPress={(e) => e.stopPropagation()}
          >
            <View style={styles.modalHeader}>
              <Text style={styles.modalTitle}>Yeni Sefer Oluştur</Text>
              <TouchableOpacity 
                onPress={() => setShowJourneyModal(false)}
                style={styles.modalCloseButton}
                hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
              >
                <Icon name="close" size={24} color="#374151" />
              </TouchableOpacity>
            </View>

            <Text style={styles.modalSubtitle}>
              {selectedRoute?.name} rotasından sefer oluşturulacak
            </Text>

            <View style={styles.inputContainer}>
              <Text style={styles.inputLabel}>Sefer Adı</Text>
              <TextInput
                style={styles.input}
                value={journeyName}
                onChangeText={setJourneyName}
                placeholder="Örn: Sabah Teslimatı - 04.09.2025"
                placeholderTextColor="#9CA3AF"
                autoFocus
                maxLength={100}
              />
              <Text style={styles.inputHint}>
                Seferi diğerlerinden ayırt edebilmek için açıklayıcı bir isim verin
              </Text>
            </View>

            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={[styles.modalButton, styles.cancelButton]}
                onPress={() => setShowJourneyModal(false)}
                activeOpacity={0.7}
              >
                <Text style={styles.cancelButtonText}>İptal</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.modalButton, styles.createButton]}
                onPress={handleCreateJourney}
                disabled={!journeyName.trim() || creatingJourney}
                activeOpacity={0.7}
              >
                {creatingJourney ? (
                  <ActivityIndicator size="small" color="#fff" />
                ) : (
                  <>
                    <Icon name="truck-delivery" size={20} color="#fff" />
                    <Text style={styles.createButtonText}>Oluştur</Text>
                  </>
                )}
              </TouchableOpacity>
            </View>
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
  toolbar: {
    flexDirection: 'row',
    paddingHorizontal: wp(4),
    paddingVertical: hp(1.5),
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    gap: wp(3),
  },
  searchContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#F3F4F6',
    borderRadius: 8,
    paddingHorizontal: wp(3),
    height: 40,
    maxHeight: 40,
  },
  searchIcon: {
    marginRight: wp(2),
  },
  searchInput: {
    flex: 1,
    fontSize: 14,
    color: '#111827',
    paddingVertical: 0,
  },
  sortButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: wp(3),
    paddingVertical: 8,
    backgroundColor: '#EBF5FF',
    borderRadius: 8,
    gap: 4,
  },
  sortText: {
    fontSize: 12,
    color: '#3B82F6',
    fontWeight: '500',
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
  debugText: {
    marginTop: 8,
    fontSize: 12,
    color: '#9CA3AF',
  },
  listContent: {
    padding: wp(4),
    paddingBottom: hp(10),
  },
  listContentLarge: {
    padding: wp(3),
    paddingBottom: hp(12),
  },
  routeCard: {
    backgroundColor: '#fff',
    borderRadius: 12,
    padding: wp(4),
    marginBottom: hp(1.5),
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  routeCardLarge: {
    padding: wp(3.5),
    marginBottom: hp(2),
  },
  routeHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: hp(1.5),
  },
  routeInfo: {
    flex: 1,
  },
  routeName: {
    fontSize: isLargeScreen ? 16 : 18,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 4,
  },
  routeDate: {
    fontSize: isLargeScreen ? 13 : 14,
    color: '#6B7280',
  },
  routeStatus: {
    marginLeft: wp(3),
  },
  routeDetails: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginBottom: hp(1.5),
    paddingBottom: hp(1.5),
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: wp(4),
    marginBottom: 4,
  },
  detailText: {
    marginLeft: 4,
    fontSize: isLargeScreen ? 13 : 14,
    color: '#6B7280',
  },
  routeAssignments: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginBottom: hp(1.5),
  },
  assignmentRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  assignmentText: {
    marginLeft: 4,
    fontSize: isLargeScreen ? 13 : 14,
    color: '#10B981',
  },
  unassigned: {
    color: '#EF4444',
  },
  startButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#10B981',
    paddingVertical: hp(1.2),
    borderRadius: 8,
  },
  optimizeButton: {
    backgroundColor: '#8B5CF6',
  },
  startButtonText: {
    marginLeft: 8,
    color: '#fff',
    fontSize: isLargeScreen ? 14 : 16,
    fontWeight: '600',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: hp(8),
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#6B7280',
    marginTop: hp(2),
  },
  emptySubtext: {
    fontSize: 14,
    color: '#9CA3AF',
    marginTop: hp(1),
    textAlign: 'center',
  },
  fab: {
    position: 'absolute',
    right: wp(4),
    bottom: hp(2),
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 8,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  modalContent: {
    backgroundColor: '#fff',
    borderRadius: 16,
    padding: wp(6),
    width: '90%',
    maxWidth: 400,
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: hp(1),
  },
  modalTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: '#111827',
  },
  modalCloseButton: {
    padding: 4,
  },
  modalSubtitle: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: hp(2.5),
  },
  inputContainer: {
    marginBottom: hp(2.5),
  },
  inputLabel: {
    fontSize: 14,
    fontWeight: '500',
    color: '#374151',
    marginBottom: hp(1),
  },
  input: {
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 8,
    paddingHorizontal: wp(3),
    paddingVertical: hp(1.2),
    fontSize: 16,
    color: '#111827',
  },
  inputHint: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: hp(0.5),
  },
  modalButtons: {
    flexDirection: 'row',
    gap: wp(3),
  },
  modalButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: hp(1.5),
    borderRadius: 8,
  },
  cancelButton: {
    backgroundColor: '#F3F4F6',
  },
  cancelButtonText: {
    color: '#6B7280',
    fontSize: 16,
    fontWeight: '500',
  },
  createButton: {
    backgroundColor: '#10B981',
  },
  createButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
    marginLeft: 6,
  },
});

export default RoutesListScreen;
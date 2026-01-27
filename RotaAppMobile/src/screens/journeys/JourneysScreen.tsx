// C:\Projects\RotaAppMobile\src\screens\journeys\JourneysScreen.tsx

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  View,
  StyleSheet,
  FlatList,
  RefreshControl,
  Alert,
  Platform,
  Dimensions,
  InteractionManager,
} from 'react-native';
import {
  Card,
  Text,
  Chip,
  Searchbar,
  FAB,
  Button,
  SegmentedButtons,
  Badge,
  Surface,
  ActivityIndicator,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import AsyncStorage from '@react-native-async-storage/async-storage';
import journeyService from '../../services/journeyService';
import notificationService from '../../services/notificationService';
import { JourneyResponse } from '../../types/journey.types';
import { useAuth } from '../../contexts/AuthContext';
import { getFlatListOptimizations, isLargeScreen, isLowEndDevice, isExtraLargeDevice } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

// Performans optimizasyonu için sabitler - Ekran boyutuna göre ayarla
const BATCH_SIZE = 10; // Sabit tut, tüm cihazlar için
const SEARCH_DEBOUNCE = isLowEndDevice() ? 500 : 300;
const API_TIMEOUT = isLargeScreen ? 30000 : (isLowEndDevice() ? 20000 : 15000);

const JourneysScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { user } = useAuth();
  const [journeys, setJourneys] = useState<JourneyResponse[]>([]);
  const [filteredJourneys, setFilteredJourneys] = useState<JourneyResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [dateFilter, setDateFilter] = useState('today');
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [searchTimeout, setSearchTimeout] = useState<NodeJS.Timeout | null>(null);
  const [retryCount, setRetryCount] = useState(0);

  // Debug log
  console.log('JourneysScreen - Device Info:', {
    width: screenWidth,
    height: screenHeight,
    isLargeScreen: isLargeScreen,
    isExtraLarge: isExtraLargeDevice,
    isLowEnd: isLowEndDevice(),
    batchSize: BATCH_SIZE
  });

  // FlatList optimizasyonları
  const flatListOptimizations = useMemo(() => getFlatListOptimizations(), []);

  const loadJourneys = useCallback(async (showLoader = true, pageNum = 1, append = false) => {
    const maxRetries = isLowEndDevice() ? 5 : 3;
    
    try {
      if (showLoader && pageNum === 1) setLoading(true);
      if (pageNum > 1) setLoadingMore(true);

      let params: any = {
        page: pageNum,
        limit: BATCH_SIZE
      };
      
      // Tarih filtreleme
      const today = new Date();
      const todayStr = format(today, 'yyyy-MM-dd');
      
      if (dateFilter === 'today') {
        params.from = todayStr;
        params.to = todayStr;
      } else if (dateFilter === 'week') {
        const weekAgo = new Date();
        weekAgo.setDate(weekAgo.getDate() - 7);
        params.from = format(weekAgo, 'yyyy-MM-dd');
        params.to = todayStr;
      }

      // Status filtreleme
      if (statusFilter !== 'all') {
        params.status = statusFilter;
      }

      // Timeout ekle - büyük ekranlar için daha uzun
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), API_TIMEOUT);

      try {
        const data = await journeyService.getJourneys(params, { signal: controller.signal });
        clearTimeout(timeoutId);
        
        // Büyük ekranlar daha fazla veri bekler
        const expectedSize = isLargeScreen ? BATCH_SIZE : BATCH_SIZE;
        if (!data || data.length < expectedSize) {
          setHasMore(false);
        }
        
        // Bildirim kontrolü - sadece ilk sayfada ve büyük ekranlarda daha hızlı
        if (pageNum === 1 && !isLowEndDevice()) {
          // InteractionManager ile ertele
          InteractionManager.runAfterInteractions(() => {
            handleNotifications(data);
          });
        }
        
        if (append) {
          // Duplicate check - aynı ID'ye sahip journey'leri filtrele
          const existingIds = new Set(journeys.map(j => j.id));
          const uniqueNewData = data.filter(j => !existingIds.has(j.id));
          const newJourneys = [...journeys, ...uniqueNewData];
          setJourneys(newJourneys);
          filterJourneys(newJourneys, searchQuery);
        } else {
          setJourneys(data);
          filterJourneys(data, searchQuery);
        }
        
        setRetryCount(0); // Başarılı olunca sıfırla
      } catch (error: any) {
        clearTimeout(timeoutId);
        if (error.name === 'AbortError') {
          throw new Error('Bağlantı zaman aşımına uğradı');
        } else {
          throw error;
        }
      }
    } catch (error: any) {
      console.error('Load journeys error:', error);
      
      // Retry mekanizması
      if (retryCount < maxRetries) {
        setRetryCount(prev => prev + 1);
        setTimeout(() => {
          loadJourneys(showLoader, pageNum, append);
        }, 2000 * (retryCount + 1)); // Exponential backoff
        
        Alert.alert(
          'Bağlantı Sorunu',
          `Seferler yüklenmeye çalışılıyor... (Deneme: ${retryCount + 1}/${maxRetries})`,
          [{ text: 'Tamam' }]
        );
      } else {
        // Hata durumunda boş liste göster, crash olmasın
        if (pageNum === 1) {
          setJourneys([]);
          setFilteredJourneys([]);
        }
        Alert.alert(
          'Hata', 
          'Seferler yüklenemedi. Lütfen internet bağlantınızı kontrol edin.',
          [
            { text: 'Tekrar Dene', onPress: () => {
              setRetryCount(0);
              loadJourneys(showLoader, pageNum, append);
            }},
            { text: 'İptal', style: 'cancel' }
          ]
        );
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  }, [dateFilter, statusFilter, searchQuery, journeys, retryCount]);

  // Bildirim kontrolünü ayrı fonksiyon yap - performans için
  const handleNotifications = async (data: JourneyResponse[]) => {
    try {
      const storedJourneyData = await AsyncStorage.getItem('knownJourneys');
      const knownJourneys = storedJourneyData ? JSON.parse(storedJourneyData) : {};
      
      for (const journey of data) {
        const knownJourney = knownJourneys[journey.id];
        const currentStatus = journey.status?.toLowerCase();
        
        if (!knownJourney) {
          if (currentStatus === 'planned' || currentStatus === 'assigned' || currentStatus === 'inprogress' || currentStatus === 'in_progress') {
            await notificationService.addNotification('route_assigned', {
              id: journey.id,
              stopCount: journey.stops?.length || journey.totalStops || 0,
              routeName: journey.name || journey.route?.name || `Sefer #${journey.id}`
            });
          }
        } else {
          const knownStatus = knownJourney.status?.toLowerCase();
          
          if (knownStatus !== 'cancelled' && currentStatus === 'cancelled') {
            await notificationService.addNotification('route_cancelled', {
              id: journey.id,
              routeName: journey.name || journey.route?.name || `Sefer #${journey.id}`
            });
          }
          
          if (knownStatus === 'cancelled' && (currentStatus === 'planned' || currentStatus === 'assigned')) {
            await notificationService.addNotification('route_assigned', {
              id: journey.id,
              stopCount: journey.stops?.length || journey.totalStops || 0,
              routeName: journey.name || journey.route?.name || `Sefer #${journey.id}`
            });
          }
        }
      }
      
      const updatedKnownJourneys: any = {};
      data.forEach((journey: JourneyResponse) => {
        updatedKnownJourneys[journey.id] = {
          id: journey.id,
          status: journey.status
        };
      });
      await AsyncStorage.setItem('knownJourneys', JSON.stringify(updatedKnownJourneys));
    } catch (error) {
      console.error('Notification handling error:', error);
    }
  };

  useEffect(() => {
    const task = InteractionManager.runAfterInteractions(() => {
      setPage(1);
      setHasMore(true);
      loadJourneys(true, 1, false);
    });
    
    return () => task.cancel();
  }, [dateFilter, statusFilter]); // loadJourneys'i ÇIKAR

  useFocusEffect(
    useCallback(() => {
      loadJourneys(false, 1, false);
    }, [dateFilter, statusFilter])
  );

  const onRefresh = () => {
    setRefreshing(true);
    setPage(1);
    setHasMore(true);
    setRetryCount(0);
    loadJourneys(false, 1, false);
  };

  const loadMore = () => {
    if (!loadingMore && hasMore) {
      const nextPage = page + 1;
      setPage(nextPage);
      loadJourneys(false, nextPage, true);
    }
  };

  const filterJourneys = (journeyList: JourneyResponse[], query: string) => {
    if (!query.trim()) {
      setFilteredJourneys(journeyList);
      return;
    }

    const filtered = journeyList.filter(journey => {
      const searchLower = query.toLowerCase();
      const journeyName = journey.name?.toLowerCase() || '';
      const routeName = journey.route?.name?.toLowerCase() || '';
      const journeyId = journey.id?.toString() || '';
      const vehiclePlate = journey.vehicle?.plate?.toLowerCase() || '';
      
      return journeyName.includes(searchLower) || routeName.includes(searchLower) || 
             journeyId.includes(searchLower) ||
             vehiclePlate.includes(searchLower);
    });
    
    setFilteredJourneys(filtered);
  };

  // Debounced search
  const handleSearch = (query: string) => {
    setSearchQuery(query);
    
    // Önceki timeout'u temizle
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }
    
    // Yeni timeout oluştur
    const timeout = setTimeout(() => {
      filterJourneys(journeys, query);
    }, SEARCH_DEBOUNCE);
    
    setSearchTimeout(timeout);
  };

  // Memoized değerler - gereksiz hesaplamaları önle
  const getStatusColor = useMemo(() => (status: string | undefined) => {
    const normalizedStatus = status?.toLowerCase();
    switch (normalizedStatus) {
      case 'completed':
        return '#10B981';
      case 'inprogress':
      case 'in_progress':
        return '#3B82F6';
      case 'cancelled':
        return '#EF4444';
      case 'planned':
      case 'preparing':
        return '#F59E0B';
      default:
        return '#6B7280';
    }
  }, []);

  const getStatusText = useMemo(() => (status: string | undefined) => {
    const normalizedStatus = status?.toLowerCase();
    switch (normalizedStatus) {
      case 'completed':
        return 'Tamamlandı';
      case 'inprogress':
      case 'in_progress':
        return 'Devam Ediyor';
      case 'cancelled':
        return 'İptal Edildi';
      case 'planned':
        return 'Planlandı';
      case 'preparing':
        return 'Hazırlanıyor';
      default:
        return status || 'Bilinmiyor';
    }
  }, []);

  const renderJourneyCard = useCallback(({ item }: { item: JourneyResponse }) => {
    const completedStops = item.stops?.filter(s => 
      s.status?.toLowerCase() === 'completed'
    ).length || 0;
    const failedStops = item.stops?.filter(s => 
      s.status?.toLowerCase() === 'failed'
    ).length || 0;
    const totalStops = item.stops?.length || 0;
    
    const totalProcessedStops = completedStops + failedStops;
    const overallProgress = totalStops > 0 
      ? Math.round((totalProcessedStops / totalStops) * 100) 
      : 0;
    
    const successRate = totalStops > 0 
      ? Math.round((completedStops / totalStops) * 100) 
      : 0;
    
    const failureRate = totalStops > 0 
      ? Math.round((failedStops / totalStops) * 100) 
      : 0;

    return (
      <Card 
        style={[
          styles.journeyCard,
          isLargeScreen && styles.journeyCardLarge
        ]}
        onPress={() => navigation.navigate('JourneyDetail', { journeyId: item.id })}
      >
        <Card.Content>
          <View style={styles.cardHeader}>
            <View style={styles.headerLeft}>
              <Text style={[
                styles.journeyTitle,
                isLargeScreen && styles.journeyTitleLarge
              ]} numberOfLines={1}>
                {item.name || item.route?.name || 'İsimsiz Sefer'}
              </Text>
              <View style={styles.journeyMeta}>
                <Icon name="identifier" size={14} color="#6B7280" />
                <Text style={styles.journeyId}>#{item.id}</Text>
                {item.vehicle && (
                  <>
                    <Icon name="truck-outline" size={14} color="#6B7280" style={{ marginLeft: 8 }} />
                    <Text style={styles.vehiclePlate}>{item.vehicle.plate}</Text>
                  </>
                )}
              </View>
            </View>
            <View style={[
              styles.statusBadge, 
              { backgroundColor: `${getStatusColor(item.status)}20` }
            ]}>
              <Text style={[
                styles.statusBadgeText,
                { color: getStatusColor(item.status) }
              ]} numberOfLines={1}>
                {getStatusText(item.status)}
              </Text>
            </View>
          </View>

          <View style={styles.progressSection}>
            <View style={styles.progressInfo}>
              <Text style={styles.progressLabel}>Duraklar</Text>
              <Text style={styles.progressText}>
                {totalProcessedStops} / {totalStops}
              </Text>
            </View>
            <Text style={styles.progressPercent}>{overallProgress}%</Text>
          </View>

          <View style={styles.progressBar}>
            {successRate > 0 && (
              <View 
                style={[styles.progressFill, { 
                  width: `${successRate}%`,
                  backgroundColor: '#10B981',
                  left: 0
                }]} 
              />
            )}
            {failureRate > 0 && (
              <View 
                style={[styles.progressFill, { 
                  width: `${failureRate}%`,
                  backgroundColor: '#EF4444',
                  left: `${successRate}%`
                }]} 
              />
            )}
          </View>

          <View style={styles.cardFooter}>
            <View style={styles.footerItem}>
              <Icon name="map-marker-distance" size={14} color="#6B7280" />
              <Text style={styles.footerText}>
                {item.totalDistance ? `${item.totalDistance.toFixed(1)} km` : '0 km'}
              </Text>
            </View>
            <View style={styles.footerItem}>
              <Icon name="clock-outline" size={14} color="#6B7280" />
              <Text style={styles.footerText}>
                {item.totalDuration ? `${Math.round(item.totalDuration / 60)} dk` : '0 dk'}
              </Text>
            </View>
            {item.createdAt && (
              <View style={styles.footerItem}>
                <Icon name="calendar" size={14} color="#6B7280" />
                <Text style={styles.footerText}>
                  {format(new Date(item.createdAt), 'dd MMM', { locale: tr })}
                </Text>
              </View>
            )}
          </View>
        </Card.Content>
      </Card>
    );
  }, [getStatusColor, getStatusText, navigation]);

  const EmptyComponent = () => (
    <View style={styles.emptyContainer}>
      <Icon name="truck-remove-outline" size={64} color="#9CA3AF" />
      <Text style={styles.emptyText}>Sefer Bulunamadı</Text>
      <Text style={styles.emptySubtext}>
        {dateFilter === 'today' 
          ? 'Bugün için atanmış sefer bulunmuyor'
          : 'Seçilen kriterlere uygun sefer bulunmuyor'}
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

  // FlatList için keyExtractor ve getItemLayout
  const keyExtractor = useCallback((item: JourneyResponse) => item.id.toString(), []);
  const getItemLayout = useCallback((data: any, index: number) => {
    const itemHeight = isLargeScreen ? 180 : 160;
    return {
      length: itemHeight,
      offset: itemHeight * index,
      index,
    };
  }, []);

  if (loading && page === 1) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Seferler yükleniyor...</Text>
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
      <Surface style={styles.filterContainer} elevation={1}>
        <Searchbar
          placeholder="Sefer ara..."
          onChangeText={handleSearch}
          value={searchQuery}
          style={styles.searchBar}
          inputStyle={styles.searchInput}
        />
        
        <View style={styles.filterRow}>
          <SegmentedButtons
            value={dateFilter}
            onValueChange={setDateFilter}
            buttons={[
              { value: 'today', label: 'Bugün' },
              { value: 'week', label: 'Bu Hafta' },
              { value: 'all', label: 'Tümü' },
            ]}
            style={styles.segmentedButton}
          />
        </View>

        <View style={styles.filterRow}>
          <SegmentedButtons
            value={statusFilter}
            onValueChange={setStatusFilter}
            buttons={[
              { value: 'all', label: 'Tümü' },
              { value: 'InProgress', label: 'Aktif' },
              { value: 'Completed', label: 'Biten' },
              { value: 'Planned', label: 'Bekleyen' },
            ]}
            style={styles.segmentedButton}
            density="small"
          />
        </View>
      </Surface>

      <FlatList
        data={filteredJourneys}
        renderItem={renderJourneyCard}
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
        onEndReached={loadMore}
        onEndReachedThreshold={0.5}
        {...flatListOptimizations}
        getItemLayout={getItemLayout}
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
  debugText: {
    marginTop: 8,
    fontSize: 12,
    color: '#9CA3AF',
  },
  statusBadge: {
    paddingHorizontal: wp(2.5),
    paddingVertical: hp(0.8),
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    minWidth: wp(isLargeScreen ? 20 : 24),
    maxWidth: wp(isLargeScreen ? 28 : 32),
  },
  statusBadgeText: {
    fontSize: isLargeScreen ? 10 : 11,
    fontWeight: '600',
  },
  filterContainer: {
    backgroundColor: 'white',
    paddingHorizontal: wp(4),
    paddingVertical: hp(1.5),
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  searchBar: {
    backgroundColor: '#F9FAFB',
    elevation: 0,
    marginBottom: hp(1.5),
    borderRadius: 8,
    height: hp(isLargeScreen ? 5 : 5.5),
  },
  searchInput: {
    fontSize: isLargeScreen ? 13 : 14,
  },
  filterRow: {
    marginBottom: hp(1),
  },
  segmentedButton: {
    height: hp(isLargeScreen ? 4 : 4.5),
  },
  listContent: {
    padding: wp(4),
    paddingBottom: hp(3),
  },
  listContentLarge: {
    padding: wp(3),
    paddingBottom: hp(4),
  },
  journeyCard: {
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  journeyCardLarge: {
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
  journeyTitle: {
    fontSize: isLargeScreen ? 15 : 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 4,
  },
  journeyTitleLarge: {
    fontSize: 15,
  },
  journeyMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
  },
  journeyId: {
    fontSize: 12,
    color: '#6B7280',
    marginLeft: 4,
  },
  vehiclePlate: {
    fontSize: 12,
    color: '#6B7280',
    marginLeft: 4,
  },
  progressSection: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: hp(1),
  },
  progressInfo: {
    flex: 1,
  },
  progressLabel: {
    fontSize: 11,
    color: '#6B7280',
    marginBottom: 2,
  },
  progressText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#111827',
  },
  progressPercent: {
    fontSize: isLargeScreen ? 16 : 18,
    fontWeight: '600',
    color: '#3B82F6',
  },
  progressBar: {
    height: hp(1),
    backgroundColor: '#E5E7EB',
    borderRadius: 4,
    overflow: 'hidden',
    marginBottom: hp(1.5),
    position: 'relative',
  },
  progressFill: {
    position: 'absolute',
    height: '100%',
    borderRadius: 4,
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
  },
  footerText: {
    fontSize: 12,
    color: '#6B7280',
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
});

export default JourneysScreen;
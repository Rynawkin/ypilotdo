// C:\Projects\RotaAppMobile\src\screens\journeys\JourneyDetailScreen.tsx

import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  RefreshControl,
  ActivityIndicator,
  Linking,
  Dimensions,
  PixelRatio,
  Platform,
} from 'react-native';

// Responsive helpers - DÜZELTME
const { width: screenWidth, height: screenHeight } = Dimensions.get('window');
const baseWidth = 375;
const baseHeight = 812;
const scale = screenWidth / baseWidth;

const normalize = (size: number) => {
  const newSize = size * scale;
  if (Platform.OS === 'ios') {
    return Math.round(PixelRatio.roundToNearestPixel(newSize));
  } else {
    return Math.round(PixelRatio.roundToNearestPixel(newSize)) - 2;
  }
};

// wp fonksiyonu düzeltildi - sadece number alıyor
const wp = (percentage: number) => {
  const value = (percentage * screenWidth) / 100;
  return isNaN(value) ? 0 : value; // NaN kontrolü eklendi
};

const hp = (percentage: number) => {
  const value = (percentage * screenHeight) / 100;
  return isNaN(value) ? 0 : value; // NaN kontrolü eklendi
};

const isSmallDevice = screenWidth < 375;
import { Card, Button, Menu, Divider, Portal, Modal } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation, useFocusEffect } from '@react-navigation/native';
import { format, parseISO } from 'date-fns';
import { tr } from 'date-fns/locale';
import journeyService from '../../services/journeyService';
import navigationService from '../../services/navigationService';
import networkService from '../../services/networkService';
import offlineQueueService from '../../services/offlineQueueService';
import locationService from '../../services/locationService';
import { JourneyMapView } from '../../components/JourneyMapView';
import { useAuth } from '../../contexts/AuthContext';
import {
  JourneyResponse,
  JourneyStopResponse,
  getJourneyStatusColor,
  getStopStatusColor,
  formatTime,
  DelayReasonCategory,
} from '../../types/journey.types';
import { DelayReasonModal } from '../../components/DelayReasonModal';

// Custom Chip Component to replace react-native-paper Chip - DÜZELTME
const CustomChip = ({ 
  children, 
  icon, 
  style, 
  textStyle,
  backgroundColor,
  mode = 'flat'
}: {
  children: React.ReactNode;
  icon?: string;
  style?: any;
  textStyle?: any;
  backgroundColor?: string;
  mode?: 'flat' | 'outlined';
}) => {
  return (
    <View style={[
      styles.customChip,
      backgroundColor && { backgroundColor },
      style
    ]}>
      {icon && (
        <Icon 
          name={icon} 
          size={14} // 16'dan 14'e düşürdük
          color={textStyle?.color || 'white'} 
          style={{ marginRight: 4 }}
        />
      )}
      <Text 
        style={[styles.customChipText, textStyle]}
        numberOfLines={1} // Tek satırda kalmasını sağla
        adjustsFontSizeToFit={true} // Otomatik font küçültme
        minimumFontScale={0.8} // Minimum %80'e kadar küçülebilir
      >
        {children}
      </Text>
    </View>
  );
};

interface RouteParams {
  journeyId: number;
}

const isStopExcluded = (stop: JourneyStopResponse): boolean => {
  // Depo durağı (routeStop == null) excluded değildir
  if (stop.routeStop == null) {
    return false;
  }

  return stop.routeStop?.isExcluded === true ||
         stop.order === 0 ||
         stop.routeStop?.order === 0;
};

const calculateJourneyProgress = (stops: JourneyStopResponse[]): number => {
  if (!stops || stops.length === 0) return 0;
  
  // Excluded olmayan stops'ları say
  const includedStops = stops.filter(s => !isStopExcluded(s));
  
  if (includedStops.length === 0) return 0;
  
  const completedStops = includedStops.filter(
    stop => stop.status === 'completed' || stop.status === 'failed'
  );
  
  return Math.round((completedStops.length / includedStops.length) * 100);
};

const JourneyDetailScreen: React.FC = () => {
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { user } = useAuth();
  const { journeyId } = route.params as RouteParams;

  const [journey, setJourney] = useState<JourneyResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedStop, setSelectedStop] = useState<JourneyStopResponse | null>(null);
  const [modalVisible, setModalVisible] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);
  const [isOnline, setIsOnline] = useState(true);
  const [queueStatus, setQueueStatus] = useState<any>(null);

  // Delay reason modal state
  const [delayReasonModalVisible, setDelayReasonModalVisible] = useState(false);
  const [delayReasonStopId, setDelayReasonStopId] = useState<number | null>(null);
  const [delayReasonData, setDelayReasonData] = useState<{
    newDelay: number;
    cumulativeDelay: number;
    stopName: string;
  } | null>(null);

  // Component mount kontrolü için ref
  const isMounted = useRef(true);

  // Component mount olduğunda navigasyon servisini başlat
  useEffect(() => {
    const initNavigationService = async () => {
      await navigationService.detectInstalledApps();
      await navigationService.loadPreference();
    };
    initNavigationService();
  }, []);

  // Journey'yi yükle - journey'yi dependency'den çıkardık
  const loadJourney = useCallback(async (showLoader = true) => {
    try {
      console.log('Loading journey with ID:', journeyId);
      if (showLoader) setLoading(true);
      const data = await journeyService.getJourney(journeyId);
      console.log('Journey loaded successfully:', data);

      // Component hala mount edilmişse state'i güncelle
      if (isMounted.current) {
        setJourney(data);
      }

      return data; // Return the fresh journey data
    } catch (error: any) {
      console.log('Load journey error:', error?.message || 'Unknown error');

      // Offline ise ve journey yoksa hata göster
      if (!networkService.getIsConnected()) {
        Alert.alert('Çevrimdışı', 'İnternet bağlantısı yok. Sefer detayları yüklenemiyor.');
      } else {
        Alert.alert('Hata', 'Sefer detayları yüklenirken bir hata oluştu.');
        navigation.goBack();
      }
      return null;
    } finally {
      if (isMounted.current) {
        setLoading(false);
        setRefreshing(false);
      }
    }
  }, [journeyId, navigation]);

  // Component unmount kontrolü
  useEffect(() => {
    isMounted.current = true;
    return () => {
      isMounted.current = false;
    };
  }, []);

  // Network durumunu dinle
  useEffect(() => {
    const handleConnectionChange = (connected: boolean) => {
      if (isMounted.current) {
        setIsOnline(connected);
        if (connected && journey) {
          loadJourney(false);
        }
      }
    };

    const updateQueueStatus = async () => {
      const status = await offlineQueueService.getQueueStatus();
      if (isMounted.current) {
        setQueueStatus(status);
      }
    };

    // Initial checks
    setIsOnline(networkService.getIsConnected());
    updateQueueStatus();

    // Listeners
    networkService.on('connectionChange', handleConnectionChange);
    const queueInterval = setInterval(updateQueueStatus, 5000);

    return () => {
      networkService.removeListener('connectionChange', handleConnectionChange);
      clearInterval(queueInterval);
    };
  }, [loadJourney, journey]);

  useEffect(() => {
    loadJourney();
  }, [loadJourney]);

  // CompleteStop veya FailStop'tan döndüğünde yenile
  useFocusEffect(
    useCallback(() => {
      // Sayfa odaklandığında ve loading değilse yenile
      if (!loading && isMounted.current) {
        loadJourney(false);
      }
      return () => {}; // cleanup
    }, [loading, loadJourney])
  );

  // Pull to refresh
  const onRefresh = useCallback(() => {
    setRefreshing(true);
    loadJourney(false);
  }, [loadJourney]);

  // Yardımcı fonksiyon ekle (handleStartJourney'nin üstüne)
  const checkTimeDeviation = (plannedStartTime: string): number => {
    const planned = new Date(`1970-01-01T${plannedStartTime}`);
    const actual = new Date(`1970-01-01T${format(new Date(), 'HH:mm:ss')}`);
    const diffMinutes = Math.abs((actual.getTime() - planned.getTime()) / 60000);
    return diffMinutes;
  };

  // Journey başlat
  const handleStartJourney = async () => {
    // Planlanan başlama saatini kontrol et
    const plannedStartTime = journey?.route?.startDetails?.startTime || '09:00:00';
    const deviation = checkTimeDeviation(plannedStartTime);


    if (deviation > 30) {
      // 30 dakikadan fazla sapma var
      Alert.alert(
        '⚠️ Zaman Sapması Tespit Edildi',
        `Planlanan başlama saati: ${plannedStartTime.substring(0, 5)}\n` +
        `Şu anki saat: ${format(new Date(), 'HH:mm')}\n` +
        `Sapma: ${Math.round(deviation)} dakika\n\n` +
        'Teslimat saatleri güncellenmeli. Rota yeniden optimize edilecek.',
        [
          { text: 'İptal', style: 'cancel' },
          {
            text: 'Optimize Et ve Başlat',
            style: 'default',
            onPress: async () => {
              try {
                setActionLoading(true);
                
                // Önce optimize et
                const now = new Date();
                const actualStartTime = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:00`;

                const result = await journeyService.optimizeForDeviation(
                  journeyId,
                  actualStartTime // ActualStartTime gönder
                );

                // Journey'i yeniden yükle
                await loadJourney();
                
                console.log('Journey optimized successfully');
                
                // YENİ: Optimize sonrası excluded stops kontrolü
                if (journey?.stops) {
                  const previousNormalStops = journey.stops.filter(s => !isStopExcluded(s));
                  
                  // Yeni journey'yi tekrar yükle ve kontrol et
                  const updatedJourney = await journeyService.getJourney(journeyId);
                  if (isMounted.current) {
                    setJourney(updatedJourney);
                  }
                  
                  const currentExcludedStops = updatedJourney.stops.filter(isStopExcluded);
                  const currentNormalStops = updatedJourney.stops.filter(s => !isStopExcluded(s));
                  
                  // Önceki normal stops'lardan hangilerinin excluded olduğunu bul
                  const newlyExcludedStops = previousNormalStops.filter(prevStop => {
                    return !currentNormalStops.some(currStop => 
                      currStop.routeStop?.customer?.id === prevStop.routeStop?.customer?.id
                    );
                  });

                  if (newlyExcludedStops.length > 0) {
                    // Excluded durakları listele
                    const excludedList = newlyExcludedStops.map((stop, index) => 
                      `${index + 1}. ${stop.routeStop?.customer?.name || stop.routeStop?.address || 'İsimsiz Durak'}`
                    ).join('\n');

                    Alert.alert(
                      '🚫 Kaldırılan Duraklar',
                      `Zaman penceresi uyumsuzluğu nedeniyle ${newlyExcludedStops.length} durak rotadan çıkarıldı:\n\n${excludedList}\n\nBu duraklar daha sonra ayrı bir rotada değerlendirilebilir.\n\nDevam etmek istiyor musunuz?`,
                      [
                        { 
                          text: 'İptal', 
                          style: 'cancel',
                          onPress: () => setActionLoading(false)
                        },
                        { 
                          text: 'Devam Et',
                          onPress: async () => {
                            try {
                              await journeyService.startJourney(journeyId);
                              Alert.alert('Başarılı', 'Sefer başlatıldı!');
                              // Journey'yi tam olarak yeniden yükle
                              const freshJourney = await journeyService.getJourney(journeyId);
                              if (isMounted.current) {
                                setJourney(freshJourney);
                              }
                            } catch (err) {
                              console.log('Start journey error:', err);
                              Alert.alert('Hata', 'Sefer başlatılamadı');
                            } finally {
                              setActionLoading(false);
                            }
                          }
                        }
                      ]
                    );
                    return;
                  }
                }
                
                // Excluded stops yoksa direkt başlat
                await journeyService.startJourney(journeyId);
                
                Alert.alert(
                  'Başarılı', 
                  'Rota yeni başlama saatine göre optimize edildi ve sefer başlatıldı.'
                );
                
                // Journey'yi yenile
                await loadJourney();
              } catch (error: any) {
                console.log('Optimize and start error:', error);
                Alert.alert(
                  'Hata', 
                  error.response?.data?.message || 'İşlem sırasında bir hata oluştu.'
                );
              } finally {
                setActionLoading(false);
              }
            },
          },
        ]
      );
    } else {
      // Normal başlatma akışı
      Alert.alert(
        'Seferi Başlat',
        'Seferi başlatmak istediğinize emin misiniz?',
        [
          { text: 'İptal', style: 'cancel' },
          {
            text: 'Başlat',
            onPress: async () => {
              try {
                setActionLoading(true);
                
                if (!isOnline) {
                  Alert.alert(
                    'Çevrimdışı Mod',
                    'Sefer başlatma isteği kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
                    [{ text: 'Tamam' }]
                  );
                  if (journey && isMounted.current) {
                    setJourney({...journey, status: 'in_progress'});
                  }
                }
                
                await journeyService.startJourney(journeyId);
                
                if (isOnline) {
                  Alert.alert('Başarılı', 'Sefer başlatıldı.');
                  loadJourney();
                }
              } catch (error) {
                Alert.alert('Hata', 'Sefer başlatılırken bir hata oluştu.');
              } finally {
                setActionLoading(false);
              }
            },
          },
        ]
      );
    }
  };

  // YENİ: Seferde sapma kontrolü ve optimize fonksiyonu
  const handleOptimizeForDeviation = async () => {
    Alert.alert(
      '🔄 Rota Yeniden Optimizasyonu',
      'Zaman sapması nedeniyle rota yeniden optimize edilecek. Bazı duraklar kaldırılabilir.\n\nDevam etmek istiyor musunuz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Optimize Et',
          style: 'default',
          onPress: async () => {
            try {
              setActionLoading(true);
              
              // Mevcut stops'ları sakla
              const previousStops = journey?.stops || [];
              const previousNormalStops = previousStops.filter(s => !isStopExcluded(s));
              
              // Optimize et
              const now = new Date();
              const actualStartTime = journey?.startedAt
                ? format(parseISO(journey.startedAt), 'HH:mm:ss')
                : `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:00`;

              await journeyService.optimizeForDeviation(
                journeyId,
                actualStartTime
              );

              // Journey'i yeniden yükle
              const updatedJourney = await journeyService.getJourney(journeyId);
              if (isMounted.current) {
                setJourney(updatedJourney);
              }
              
              // Yeni excluded stops'ları bul
              const currentNormalStops = updatedJourney.stops.filter(s => !isStopExcluded(s));
              
              const newlyExcludedStops = previousNormalStops.filter(prevStop => {
                return !currentNormalStops.some(currStop => 
                  currStop.routeStop?.customer?.id === prevStop.routeStop?.customer?.id
                );
              });

              if (newlyExcludedStops.length > 0) {
                const excludedList = newlyExcludedStops.map((stop, index) => 
                  `• ${stop.routeStop?.customer?.name || stop.routeStop?.address || 'İsimsiz Durak'}`
                ).join('\n');

                Alert.alert(
                  '📋 Optimizasyon Tamamlandı',
                  `${newlyExcludedStops.length} durak zaman penceresi uyumsuzluğu nedeniyle kaldırıldı:\n\n${excludedList}\n\nBu duraklar başka bir rotada değerlendirilebilir.`,
                  [{ text: 'Tamam' }]
                );
              } else {
                Alert.alert(
                  'Başarılı',
                  'Rota başarıyla optimize edildi. Tüm duraklar korundu.',
                  [{ text: 'Tamam' }]
                );
              }
            } catch (error: any) {
              console.log('Optimize for deviation error:', error);
              Alert.alert(
                'Hata',
                error.response?.data?.message || 'Optimizasyon sırasında bir hata oluştu.'
              );
            } finally {
              setActionLoading(false);
            }
          }
        }
      ]
    );
  };

  // ✅ YENİ: Yeni durak eklendikten sonra optimize et
  const handleReoptimizeJourney = async () => {
    Alert.alert(
      '🔄 Rota Optimizasyonu',
      'Yeni eklenen duraklar için rota yeniden optimize edilecek. Anlık konumunuz kullanılacak.\n\nDevam etmek istiyor musunuz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Optimize Et',
          style: 'default',
          onPress: async () => {
            try {
              setActionLoading(true);

              // Mevcut konumu al
              const location = await locationService.getCurrentLocation();
              if (!location) {
                Alert.alert('Hata', 'Konum bilgisi alınamadı. Lütfen konum izinlerini kontrol edin.');
                return;
              }

              // Optimize et
              await journeyService.reoptimizeJourney(
                journeyId,
                location.latitude,
                location.longitude
              );

              // Journey'i yeniden yükle
              await loadJourney(false);

              Alert.alert(
                'Başarılı',
                'Rota başarıyla optimize edildi. Yeni ETA\'lar hesaplandı.',
                [{ text: 'Tamam' }]
              );
            } catch (error: any) {
              console.log('Reoptimize journey error:', error);
              Alert.alert(
                'Hata',
                error.message || 'Optimizasyon sırasında bir hata oluştu.'
              );
            } finally {
              setActionLoading(false);
            }
          }
        }
      ]
    );
  };

  // Journey bitir
  const handleFinishJourney = async () => {
    Alert.alert(
      'Seferi Bitir',
      'Seferi bitirmek istediğinize emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Bitir',
          style: 'destructive',
          onPress: async () => {
            try {
              setActionLoading(true);
              
              if (!isOnline) {
                Alert.alert(
                  'Çevrimdışı Mod',
                  'Sefer bitirme isteği kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
                  [{ text: 'Tamam' }]
                );
                // Local state güncelle
                if (journey && isMounted.current) {
                  setJourney({...journey, status: 'completed'});
                }
              }
              
              await journeyService.finishJourney(journeyId);
              
              if (isOnline) {
                Alert.alert('Başarılı', 'Sefer tamamlandı.');
                loadJourney();
              }
            } catch (error: any) {
              Alert.alert('Hata', error.response?.data?.message || 'Sefer bitirilirken bir hata oluştu.');
            } finally {
              setActionLoading(false);
            }
          },
        },
      ]
    );
  };

  // Stop'a check-in yap
  const handleCheckIn = async (stop: JourneyStopResponse) => {
    try {
      setActionLoading(true);

      if (!isOnline) {
        Alert.alert(
          'Çevrimdışı Mod',
          'Varış bildirimi kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
          [{ text: 'Tamam' }]
        );

        // Local state güncelle
        if (journey && isMounted.current) {
          const updatedJourney = { ...journey };
          const stopIndex = updatedJourney.stops.findIndex(s => s.id === stop.id);
          if (stopIndex >= 0) {
            updatedJourney.stops[stopIndex].status = 'in_progress';
            updatedJourney.stops[stopIndex].checkInTime = new Date().toISOString();
          }
          setJourney(updatedJourney);
        }
      }

      const checkInResponse = await journeyService.checkInStop(journeyId, stop.id);

      // Online ise normal akış
      if (isOnline) {
        await loadJourney(false);
      }

      setActionLoading(false);

      // Gecikme sebebi gerekiyor mu kontrol et
      if (checkInResponse.requiresDelayReason) {
        setDelayReasonStopId(stop.id);
        setDelayReasonData({
          newDelay: checkInResponse.newDelay,
          cumulativeDelay: checkInResponse.cumulativeDelay,
          stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası'
        });
        setDelayReasonModalVisible(true);
        return; // Modal kapanınca devam edilecek
      }

      // Depo durağı mı kontrol et (routeStop yoksa VEYA son durak ise depo dönüşüdür)
      const isLastStop = journey?.stops && stop.order === Math.max(...journey.stops.map(s => s.order));
      const isDepotReturn = stop.routeStop == null || isLastStop;

      // ✅ DEBUG LOG
      console.log('[CHECK_IN_COMPLETE] Stop ID:', stop.id);
      console.log('[CHECK_IN_COMPLETE] Stop order:', stop.order);
      console.log('[CHECK_IN_COMPLETE] Stop routeStopId:', stop.routeStopId);
      console.log('[CHECK_IN_COMPLETE] Stop routeStop:', stop.routeStop);
      console.log('[CHECK_IN_COMPLETE] isLastStop:', isLastStop);
      console.log('[CHECK_IN_COMPLETE] isDepotReturn:', isDepotReturn);

      if (isDepotReturn) {
        // Depo dönüşü için direkt CompleteJourney ekranına git
        navigation.navigate('CompleteJourney', {
          journeyId,
          stopId: stop.id,
          depotName: journey?.route?.endDetails?.name || journey?.route?.depot?.name || 'Depo',
          depotAddress: journey?.route?.endDetails?.address || journey?.route?.depot?.address
        });
      } else {
        // Normal teslimat durağı için mevcut alert
        Alert.alert(
          'Varış Kaydedildi',
          isOnline
            ? 'Durağa varış kaydedildi. Teslimatı tamamlamak veya başarısız olarak işaretlemek için seçim yapın.'
            : 'Varış çevrimdışı kaydedildi. Teslimat bilgilerini girebilirsiniz.',
          [
            {
              text: 'Teslimatı Tamamla',
              onPress: () => {
                navigation.navigate('CompleteStop', {
                  journeyId,
                  stopId: stop.id,
                  stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası',
                  requiresProof: stop.routeStop?.proofOfDeliveryRequired,
                  signatureRequired: stop.routeStop?.signatureRequired || false,
                  photoRequired: stop.routeStop?.photoRequired || false,
                  customerId: stop.routeStop?.customerId,
                  currentLatitude: stop.endLatitude,
                  currentLongitude: stop.endLongitude,
                  currentAddress: stop.routeStop?.address
                });
              },
            },
            {
              text: 'Başarısız Teslimat',
              onPress: () => {
                navigation.navigate('FailStop', {
                  journeyId,
                  stopId: stop.id,
                  stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası',
                });
              },
              style: 'destructive',
          },
            {
              text: 'Varışı İptal Et',
              onPress: () => {
                handleCancelCheckIn(stop);
              },
              style: 'cancel',
            },
          ],
          { cancelable: false }
        );
      }

    } catch (error) {
      Alert.alert('Hata', 'Check-in yapılırken bir hata oluştu.');
      setActionLoading(false);
    }
  };

  // Gecikme sebebi gönder
  const handleSubmitDelayReason = async (category: DelayReasonCategory, reason: string) => {
    if (!delayReasonStopId) return;

    try {
      await journeyService.submitDelayReason(journeyId, delayReasonStopId, category, reason);

      Alert.alert('Başarılı', 'Gecikme sebebi kaydedildi.');

      // Journey'yi yeniden yükle ve fresh data'yı al
      const freshJourney = await loadJourney(false);
      if (!freshJourney) return;

      // Fresh journey data'dan stop bilgisini al
      const stop = freshJourney.stops.find(s => s.id === delayReasonStopId);
      if (!stop) return;

      // Depo durağı mı kontrol et (routeStop yoksa VEYA son durak ise depo dönüşüdür)
      const isLastStop = freshJourney?.stops && stop.order === Math.max(...freshJourney.stops.map(s => s.order));
      const isDepotReturn = stop.routeStop == null || isLastStop;

      console.log('[DELAY_REASON_SUBMIT] Stop ID:', stop.id);
      console.log('[DELAY_REASON_SUBMIT] Stop order:', stop.order);
      console.log('[DELAY_REASON_SUBMIT] Stop routeStopId:', stop.routeStopId);
      console.log('[DELAY_REASON_SUBMIT] Stop routeStop:', stop.routeStop);
      console.log('[DELAY_REASON_SUBMIT] isLastStop:', isLastStop);
      console.log('[DELAY_REASON_SUBMIT] isDepotReturn:', isDepotReturn);

      if (isDepotReturn) {
        navigation.navigate('CompleteJourney', {
          journeyId,
          stopId: stop.id,
          depotName: freshJourney?.route?.endDetails?.name || freshJourney?.route?.depot?.name || 'Depo',
          depotAddress: freshJourney?.route?.endDetails?.address || freshJourney?.route?.depot?.address
        });
      } else {
        // Normal teslimat durağı için alert
        Alert.alert(
          'Varış Kaydedildi',
          'Teslimatı tamamlamak veya başarısız olarak işaretlemek için seçim yapın.',
          [
            {
              text: 'Teslimatı Tamamla',
              onPress: () => {
                navigation.navigate('CompleteStop', {
                  journeyId,
                  stopId: stop.id,
                  stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası',
                  requiresProof: stop.routeStop?.proofOfDeliveryRequired,
                  signatureRequired: stop.routeStop?.signatureRequired || false,
                  photoRequired: stop.routeStop?.photoRequired || false,
                  customerId: stop.routeStop?.customerId,
                  currentLatitude: stop.endLatitude,
                  currentLongitude: stop.endLongitude,
                  currentAddress: stop.routeStop?.address
                });
              },
            },
            {
              text: 'Başarısız Teslimat',
              onPress: () => {
                navigation.navigate('FailStop', {
                  journeyId,
                  stopId: stop.id,
                  stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası',
                });
              },
              style: 'destructive',
            },
          ],
          { cancelable: false }
        );
      }
    } catch (error: any) {
      Alert.alert('Hata', error.message || 'Gecikme sebebi kaydedilemedi.');
    }
  };

  // Check-in iptali
  const handleCancelCheckIn = async (stop: JourneyStopResponse) => {
    try {
      setActionLoading(true);
      
      if (!isOnline) {
        Alert.alert(
          'Çevrimdışı Mod',
          'İptal isteği kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
          [{ text: 'Tamam' }]
        );
      }
      
      await journeyService.resetStop(journeyId, stop.id);
      
      if (isOnline) {
        Alert.alert('Başarılı', 'Varış bildirimi iptal edildi.');
        await loadJourney(false);
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'İşlem sırasında bir hata oluştu.';
      Alert.alert('Hata', errorMessage);
    } finally {
      setActionLoading(false);
    }
  };

  // Stop'u tamamla
  const handleCompleteStop = (stop: JourneyStopResponse) => {
    // Depo durağı mı kontrol et (routeStop yoksa VEYA son durak ise depo dönüşüdür)
    const isLastStop = journey?.stops && stop.order === Math.max(...journey.stops.map(s => s.order));
    const isDepotReturn = stop.routeStop == null || isLastStop;

    if (isDepotReturn) {
      // Depo dönüşü için direkt CompleteJourney ekranına git
      navigation.navigate('CompleteJourney', {
        journeyId,
        stopId: stop.id,
        depotName: journey?.route?.endDetails?.name || journey?.route?.depot?.name || 'Depo',
        depotAddress: journey?.route?.endDetails?.address || journey?.route?.depot?.address
      });
    } else {
      // Normal durak için CompleteStop ekranına git
      navigation.navigate('CompleteStop', {
        journeyId,
        stopId: stop.id,
        stopName: stop.routeStop?.name || stop.routeStop?.address || 'Teslimat Noktası',
        requiresProof: stop.routeStop?.proofOfDeliveryRequired,
        signatureRequired: stop.routeStop?.signatureRequired || false,
        photoRequired: stop.routeStop?.photoRequired || false,
        customerId: stop.routeStop?.customerId,
        currentLatitude: stop.endLatitude,
        currentLongitude: stop.endLongitude,
        currentAddress: stop.routeStop?.address
      });
    }
  };

  // Stop'u başarısız işaretle
  const handleFailStop = (stop: JourneyStopResponse) => {
    navigation.navigate('FailStop', {
      journeyId,
      stopId: stop.id,
      stopName: stop.routeStop?.name || stop.routeStop?.address || journey?.route?.endDetails?.name || journey?.route?.depot?.name || 'Depo Dönüşü',
    });
  };

  // Başarısız durağı tekrar deneme
  const handleRetryStop = async (stop: JourneyStopResponse) => {
    if (journey?.status !== 'in_progress') {
      Alert.alert(
        'Uyarı',
        'Sadece devam eden seferlerde duraklar tekrar denenebilir.',
        [{ text: 'Tamam' }]
      );
      return;
    }
    
    Alert.alert(
      'Durağı Tekrar Dene',
      'Bu durağı tekrar denemek istediğinize emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Tekrar Dene',
          onPress: async () => {
            try {
              setActionLoading(true);
              
              if (!isOnline) {
                Alert.alert(
                  'Çevrimdışı Mod',
                  'Tekrar deneme isteği kaydedildi ve internet bağlantısı kurulduğunda gönderilecek.',
                  [{ text: 'Tamam' }]
                );
              }
              
              await journeyService.resetStop(journeyId, stop.id);
              
              if (isOnline) {
                Alert.alert('Başarılı', 'Durak tekrar denemeye hazır.');
                await loadJourney(false);
              }
            } catch (error: any) {
              const errorMessage = error.response?.data?.message || 'İşlem sırasında bir hata oluştu.';
              Alert.alert('Hata', errorMessage);
            } finally {
              setActionLoading(false);
            }
          },
        },
      ]
    );
  };

  // Haritada göster - GÜNCELLENEN FONKSİYON
  const openInMaps = async (stop: JourneyStopResponse, longPress: boolean = false) => {
    const latitude = stop.endLatitude;
    const longitude = stop.endLongitude;
    const label = stop.routeStop?.name || stop.routeStop?.address;
    
    // Uzun basma veya tercih yoksa seçim modal'ını göster
    const preferredApp = navigationService.getPreferredApp();
    
    if (longPress || !preferredApp) {
      await navigationService.showAppSelectionModal(latitude, longitude, label);
    } else {
      // Tercih edilen uygulama varsa direkt aç
      await navigationService.openNavigation(latitude, longitude, label);
    }
  };

  // Müşteriyi ara
  const callCustomer = (phone: string) => {
    Linking.openURL(`tel:${phone}`);
  };

  // Durak detayını görüntüle
  const showStopDetails = (stop: JourneyStopResponse) => {
    setSelectedStop(stop);
    setModalVisible(true);
  };

  // Mevcut aktif stop'u bul
  const getCurrentActiveStop = () => {
    if (!journey?.stops) return null;
    
    // Excluded olmayan stops'ları filtrele
    const activeStops = journey.stops.filter(s => !isStopExcluded(s));
    
    // Önce in_progress durumunda bir stop var mı kontrol et
    const inProgressStop = activeStops.find(s => s.status === 'in_progress');
    if (inProgressStop) return inProgressStop;
    
    // Yoksa ilk pending stop'u bul
    const sortedStops = [...activeStops].sort((a, b) => a.order - b.order);
    return sortedStops.find(s => s.status === 'pending');
  };

  // Stop durumuna göre aksiyon butonları
  const renderStopActions = (stop: JourneyStopResponse) => {
    const journeyInProgress = journey?.status === 'in_progress';
    
    if (!journeyInProgress) {
      // Journey aktif değilse sadece status chip göster
      if (stop.status === 'completed' || stop.status === 'failed') {
        return (
          <View style={styles.stopActions}>
            <CustomChip 
              style={[
                styles.statusChip,
                { backgroundColor: getStopStatusColor(stop.status) }
              ]}
              textStyle={{ color: 'white' }}
              icon={stop.status === 'completed' ? 'check' : 'close'}
            >
              {stop.status === 'completed' ? 'Tamamlandı' : 'Başarısız'}
            </CustomChip>
          </View>
        );
      }
      return null;
    }

    const normalizedStatus = stop.status?.toLowerCase();
    
    // IN-PROGRESS DURUMU renderStop İÇİNDE HANDLE EDİLİYOR
    if (normalizedStatus === 'in_progress' || normalizedStatus === 'inprogress') {
      return null; // renderStop içinde handle ediliyor
    }
    
    // Pending durumu için
    const currentActiveStop = getCurrentActiveStop();
    const isActiveStop = currentActiveStop?.id === stop.id;
    
    // Önceki durakların tamamlanma kontrolü
    const activeStops = journey.stops.filter(s => !isStopExcluded(s));
    const sortedStops = [...activeStops].sort((a, b) => a.order - b.order);
    const previousStopsCompleted = sortedStops
      .filter(s => s.order < stop.order && !isStopExcluded(s))
      .every(s => {
        const sStatus = s.status?.toLowerCase();
        return sStatus === 'completed' || sStatus === 'failed' || sStatus === 'skipped';
      });
    
    if (normalizedStatus === 'pending' && isActiveStop && previousStopsCompleted) {
      // Depo durağı mı kontrol et
      const isDepotReturn = stop.routeStop == null;

      return (
        <View style={styles.stopActions}>
          <Button
            mode="contained"
            onPress={() => handleCheckIn(stop)}
            style={styles.actionButton}
            buttonColor={isDepotReturn ? "#10B981" : "#3B82F6"}
            loading={actionLoading}
            icon={isDepotReturn ? "home-variant" : (!isOnline ? "cloud-off-outline" : "map-marker-check")}
          >
            {isDepotReturn
              ? 'Depoya Varış & Sefer Tamamla'
              : (!isOnline ? 'Çevrimdışı Varış' : 'Varış Bildir')
            }
          </Button>
        </View>
      );
    }

    // Completed durumu
    if (normalizedStatus === 'completed') {
      return (
        <View style={styles.stopActions}>
          <CustomChip 
            style={[
              styles.statusChip,
              { backgroundColor: getStopStatusColor(stop.status) }
            ]}
            textStyle={{ color: 'white' }}
            icon="check"
          >
            Tamamlandı
          </CustomChip>
        </View>
      );
    }

    // Failed durumu
    if (normalizedStatus === 'failed') {
      return (
        <View style={styles.failedContainer}>
          <CustomChip 
            style={[
              styles.statusChip,
              { backgroundColor: getStopStatusColor(stop.status) }
            ]}
            textStyle={{ color: 'white' }}
            icon="close"
          >
            Başarısız
          </CustomChip>
          <Button
            mode="text"
            onPress={() => handleRetryStop(stop)}
            style={styles.retryButtonSmall}
            textColor="#F59E0B"
            icon="restart"
            compact
          >
            Tekrar Dene
          </Button>
        </View>
      );
    }

    return null;
  };

  // Stop kartı
  const renderStop = (stop: JourneyStopResponse, index: number) => {
    const currentActiveStop = getCurrentActiveStop();
    const isActiveStop = currentActiveStop?.id === stop.id;
    const normalizedStatus = stop.status?.toLowerCase();
    const statusColor = getStopStatusColor(stop.status);
    const journeyInProgress = journey?.status === 'in_progress';
    const isExcluded = isStopExcluded(stop); // YENİ
    
    return (
      <TouchableOpacity 
        key={stop.id}
        onPress={() => showStopDetails(stop)}
        activeOpacity={0.8}
        style={[
          isExcluded && styles.excludedStopWrapper
        ]}
      >
        <Card 
          style={[
            styles.stopCard,
            isActiveStop && journeyInProgress && styles.currentStopCard,
            (normalizedStatus === 'in_progress' || normalizedStatus === 'inprogress') && styles.inProgressStopCard,
            isExcluded && styles.excludedStop
          ]}
        >
          <Card.Content>
            {/* Excluded stop uyarısı */}
            {isExcluded && (
              <View style={styles.excludedWarning}>
                <Icon name="alert-circle" size={16} color="#f44336" />
                <Text style={styles.excludedWarningText}>
                  Bu durak time window uyumsuzluğu nedeniyle hariç tutuldu
                </Text>
              </View>
            )}

            {/* Stop header */}
            <View style={styles.stopHeader}>
              <View style={styles.stopNumber}>
                <Text style={styles.stopNumberText}>
                  {isExcluded ? '—' : stop.order}
                </Text>
              </View>
              <View style={styles.stopInfo}>
                <Text style={styles.stopName}>
                  {stop.routeStop?.name || stop.routeStop?.customer?.name || 'İsimsiz Durak'}
                </Text>
                <Text style={styles.stopAddress} numberOfLines={2}>
                  {stop.routeStop?.address}
                </Text>
              </View>
              <View style={styles.stopStatusContainer}>
                {/* Status indicator dot veya bar */}
                {(normalizedStatus === 'in_progress' || normalizedStatus === 'inprogress') ? (
                  <View style={styles.inProgressBar} />
                ) : (
                  <View 
                    style={[styles.stopStatusDot, { backgroundColor: statusColor }]} 
                  />
                )}
                {isActiveStop && journeyInProgress && normalizedStatus === 'pending' && (
                  <CustomChip style={styles.currentChip} textStyle={styles.currentChipText}>
                    Sıradaki
                  </CustomChip>
                )}
              </View>
            </View>

            {/* Stop details */}
            <View style={styles.stopDetails}>
              <View style={styles.stopDetailRow}>
                <Icon name="clock-outline" size={16} color="#666" />
                <Text style={styles.stopDetailText}>
                  Tahmini: {formatTime(stop.estimatedArrivalTime)}
                  {stop.estimatedDepartureTime && ` - ${formatTime(stop.estimatedDepartureTime)}`}
                </Text>
              </View>
              
              {stop.checkInTime && (
                <View style={styles.stopDetailRow}>
                  <Icon name="location-enter" size={16} color="#10B981" />
                  <Text style={[styles.stopDetailText, { color: '#10B981', fontWeight: '600' }]}>
                    Varış: {format(parseISO(stop.checkInTime), 'HH:mm')}
                  </Text>
                </View>
              )}
              
              {stop.checkOutTime && (
                <View style={styles.stopDetailRow}>
                  <Icon name="location-exit" size={16} color="#3B82F6" />
                  <Text style={[styles.stopDetailText, { color: '#3B82F6', fontWeight: '600' }]}>
                    Ayrılış: {format(parseISO(stop.checkOutTime), 'HH:mm')}
                  </Text>
                </View>
              )}
              
              <View style={styles.stopDetailRow}>
                <Icon name="map-marker-distance" size={16} color="#666" />
                <Text style={styles.stopDetailText}>
                  {stop.distance.toFixed(1)} km
                </Text>
              </View>
            </View>

            {/* Contact info */}
            {stop.routeStop?.contactPhone && (
              <View style={styles.contactRow}>
                <Icon name="phone" size={16} color="#666" />
                <Text style={styles.contactText}>{stop.routeStop.contactPhone}</Text>
                <Button
                  mode="text"
                  icon="phone"
                  onPress={() => callCustomer(stop.routeStop.contactPhone)}
                  compact
                >
                  Ara
                </Button>
              </View>
            )}

            {/* Notes */}
            {stop.routeStop?.notes && (
              <View style={styles.notesContainer}>
                <Text style={styles.notesTitle}>Notlar:</Text>
                <Text style={styles.notesText} numberOfLines={2}>
                  {stop.routeStop.notes}
                </Text>
              </View>
            )}

            {/* Excluded stop ise aksiyonları gösterme */}
            {!isExcluded && (
              <>
                {/* In-Progress durumu için özel layout */}
                {(normalizedStatus === 'in_progress' || normalizedStatus === 'inprogress') && journeyInProgress ? (
                  <>
                    {/* Depo durağı kontrolü */}
                    {(() => {
                      const isLastStop = journey?.stops && stop.order === Math.max(...journey.stops.map(s => s.order));
                      const isDepotReturn = stop.routeStop == null || isLastStop;

                      return (
                        <>
                          {/* Navigation button - full width */}
                          <View style={styles.navigationFullWidth}>
                            <Button
                              mode="elevated"
                              onPress={() => openInMaps(stop)}
                              onLongPress={() => openInMaps(stop, true)}
                              style={styles.navigationButtonFullWidth}
                              icon="navigation"
                              buttonColor="#DBEAFE"
                              textColor="#1E40AF"
                              contentStyle={styles.buttonContent}
                            >
                              Yol Tarifi Al
                            </Button>
                            <Text style={styles.navigationHint}>
                              Uygulama seçimi için uzun basın
                            </Text>
                          </View>

                          {/* Action buttons - full width */}
                          <View style={styles.inProgressActions}>
                            <Button
                              mode="contained"
                              onPress={() => handleCompleteStop(stop)}
                              style={styles.inProgressButton}
                              buttonColor="#10B981"
                              loading={actionLoading}
                              icon={isDepotReturn ? "home-check" : "check-circle"}
                            >
                              {isDepotReturn ? 'Seferi Tamamla' : 'Tamamla'}
                            </Button>
                            {!isDepotReturn && (
                              <Button
                                mode="outlined"
                                onPress={() => handleFailStop(stop)}
                                style={styles.inProgressButton}
                                textColor="#EF4444"
                                loading={actionLoading}
                              >
                                Başarısız
                              </Button>
                            )}
                            <Button
                              mode="text"
                              onPress={() => handleCancelCheckIn(stop)}
                              style={styles.inProgressButtonSmall}
                              textColor="#6B7280"
                              loading={actionLoading}
                              icon="undo"
                            >
                              İptal
                            </Button>
                          </View>
                        </>
                      );
                    })()}
                  </>
                ) : (
                  /* Normal durum için eski layout */
                  <View style={styles.stopFooter}>
                    <View style={styles.navigationContainer}>
                      <Button
                        mode="elevated"
                        onPress={() => openInMaps(stop)}
                        onLongPress={() => openInMaps(stop, true)}
                        style={styles.navigationButton}
                        icon="navigation"
                        buttonColor="#DBEAFE"
                        textColor="#1E40AF"
                        contentStyle={styles.buttonContent}
                        labelStyle={styles.navigationButtonLabel}
                      >
                        Yol Tarifi Al
                      </Button>
                      <Text style={styles.navigationHintSmall}>
                        Uzun basın: Uygulama seç
                      </Text>
                    </View>
                    
                    {renderStopActions(stop)}
                  </View>
                )}
              </>
            )}
            
            {/* Detay gösterge */}
            <View style={styles.detailIndicator}>
              <Text style={styles.detailIndicatorText}>Detaylar için dokunun</Text>
              <Icon name="chevron-right" size={16} color="#999" />
            </View>
          </Card.Content>
        </Card>
      </TouchableOpacity>
    );
  };


  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Sefer detayları yükleniyor...</Text>
      </View>
    );
  }

  if (!journey) {
    return null;
  }

  const progress = calculateJourneyProgress(journey.stops);
  const statusColor = getJourneyStatusColor(journey.status);
  
  const includedStops = journey.stops.filter(s => !isStopExcluded(s));
  const excludedStops = journey.stops.filter(isStopExcluded); // YENİ
  const completedStops = includedStops.filter(s => s.status === 'completed').length;
  const failedStops = includedStops.filter(s => s.status === 'failed').length;
  const totalStops = includedStops.length;
  const successRate = totalStops > 0 ? Math.round((completedStops * 100) / totalStops) : 0;
  const totalProgressPercent = totalStops > 0 
    ? Math.round(((completedStops + failedStops) * 100) / totalStops) 
    : 0;

  // YENİ: Sapma kontrolü için sefer başlama zamanını kontrol et
  const shouldShowDeviationButton = () => {
    if (journey.status !== 'in_progress') return false;
    if (!journey.startedAt) return false;

    const startTime = parseISO(journey.startedAt);
    const now = new Date();
    const diffMinutes = Math.abs((now.getTime() - startTime.getTime()) / 60000);

    // Eğer sefer 30 dakikadan fazla süre önce başladıysa optimize butonu göster
    return diffMinutes > 30;
  };

  return (
    <>
      <ScrollView 
        style={styles.container}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            colors={['#3B82F6']}
          />
        }
      >
        {/* Journey header card */}
        <Card style={styles.headerCard}>
          <Card.Content>
            <View style={styles.headerRow}>
              <View style={{ flex: 1 }}>
                <Text style={styles.journeyId}>Sefer #{journey.id}</Text>
                <Text style={styles.routeName}>
                  {journey.name || journey.route?.name || 'Sefer Detayı'}
                </Text>
              </View>
              <View style={styles.statusContainer}>
                <CustomChip
                  style={[styles.statusChip, { backgroundColor: statusColor }]}
                  textStyle={{ color: 'white' }}
                >
                  {getStatusLabel(journey.status)}
                </CustomChip>
                {!isOnline && (
                  <View style={styles.offlineChip}>
                    <Icon name="cloud-off-outline" size={14} color="#EF4444" />
                    <Text style={styles.offlineText}>Çevrimdışı</Text>
                  </View>
                )}
                {queueStatus && queueStatus.count > 0 && (
                  <View style={styles.queueChip}>
                    <Icon name="cloud-upload" size={14} color="#F59E0B" />
                    <Text style={styles.queueText}>{queueStatus.count}</Text>
                  </View>
                )}
              </View>
            </View>

            <Divider style={styles.divider} />

            {/* Journey info */}
            <View style={styles.infoGrid}>
              <View style={styles.infoItem}>
                <Icon name="account" size={20} color="#666" />
                <Text style={styles.infoLabel}>Sürücü</Text>
                <Text style={styles.infoValue}>{journey.driver?.fullName}</Text>
              </View>
              
              {journey.route?.vehicle && (
                <View style={styles.infoItem}>
                  <Icon name="truck" size={20} color="#666" />
                  <Text style={styles.infoLabel}>Araç</Text>
                  <Text style={styles.infoValue}>{journey.route.vehicle.plateNumber}</Text>
                </View>
              )}
              
              <View style={styles.infoItem}>
                <Icon name="calendar" size={20} color="#666" />
                <Text style={styles.infoLabel}>Tarih</Text>
                <Text style={styles.infoValue}>
                  {format(parseISO(journey.date), 'dd MMM yyyy', { locale: tr })}
                </Text>
              </View>
            </View>

            {/* Route Notes */}
            {journey.route?.notes && (
              <View style={styles.routeNotesContainer}>
                <Text style={styles.routeNotesTitle}>
                  <Icon name="note-text" size={16} color="#666" /> Rota Notları
                </Text>
                <Text style={styles.routeNotesText}>
                  {journey.route.notes}
                </Text>
              </View>
            )}

            {/* YENİ: Excluded stops uyarısı */}
            {excludedStops.length > 0 && (
              <View style={styles.excludedNotification}>
                <Icon name="alert-circle-outline" size={20} color="#F59E0B" />
                <Text style={styles.excludedNotificationText}>
                  {excludedStops.length} durak zaman uyumsuzluğu nedeniyle hariç tutuldu
                </Text>
              </View>
            )}

            {/* ✅ YENİ: Reoptimization Banner - Only for active journeys */}
            {journey.needsReoptimization && journey.status === 'in_progress' && (
              <View style={styles.reoptimizationBanner}>
                <View style={{ flex: 1, flexDirection: 'row', alignItems: 'center' }}>
                  <Icon name="map-marker-alert" size={24} color="#F59E0B" style={{ marginRight: 8 }} />
                  <View style={{ flex: 1 }}>
                    <Text style={styles.reoptimizationTitle}>Optimizasyon Gerekiyor</Text>
                    <Text style={styles.reoptimizationText}>
                      Yeni durak eklendi. Rotayı optimize edin.
                    </Text>
                  </View>
                </View>
                <TouchableOpacity
                  style={styles.reoptimizeButton}
                  onPress={handleReoptimizeJourney}
                  disabled={actionLoading}
                >
                  {actionLoading ? (
                    <ActivityIndicator size="small" color="#FFFFFF" />
                  ) : (
                    <>
                      <Icon name="refresh" size={16} color="#FFFFFF" />
                      <Text style={styles.reoptimizeButtonText}>Optimize Et</Text>
                    </>
                  )}
                </TouchableOpacity>
              </View>
            )}

            {/* Progress */}
            <View style={styles.progressSection}>
              <View style={styles.progressHeader}>
                <Text style={styles.progressTitle}>İlerleme</Text>
                <Text style={styles.progressText}>{totalProgressPercent}%</Text>
              </View>
              <View style={styles.progressBar}>
                {successRate > 0 && (
                  <View 
                    style={[
                      styles.progressFill,
                      { 
                        width: `${successRate}%`, 
                        backgroundColor: '#10B981',
                        position: 'absolute',
                        left: 0,
                        top: 0,
                        bottom: 0
                      }
                    ]}
                  />
                )}
                {failedStops > 0 && (
                  <View 
                    style={[
                      styles.progressFill,
                      { 
                        width: `${(failedStops * 100) / totalStops}%`, 
                        backgroundColor: '#EF4444',
                        position: 'absolute',
                        left: `${successRate}%`,
                        top: 0,
                        bottom: 0
                      }
                    ]}
                  />
                )}
              </View>
              <Text style={styles.progressSubtext}>
                {completedStops} başarılı{failedStops > 0 && `, ${failedStops} başarısız`} / {totalStops} durak
                {excludedStops.length > 0 && ` (${excludedStops.length} hariç)`}
              </Text>
            </View>

            {/* Action buttons */}
            {journey.status?.toLowerCase() === 'planned' && (
              <Button
                mode="contained"
                onPress={handleStartJourney}
                style={styles.startButton}
                buttonColor="#10B981"
                loading={actionLoading}
                icon={!isOnline ? "cloud-off-outline" : "play"}
              >
                {!isOnline ? 'Çevrimdışı Başlat' : 'Seferi Başlat'}
              </Button>
            )}
            
            {(journey.status?.toLowerCase() === 'in_progress' || journey.status?.toLowerCase() === 'inprogress') && (
              <>
                {/* YENİ: Sapma durumunda optimize butonu */}
                {shouldShowDeviationButton() && (
                  <Button
                    mode="outlined"
                    onPress={handleOptimizeForDeviation}
                    style={styles.optimizeButton}
                    textColor="#F59E0B"
                    loading={actionLoading}
                    icon="update"
                  >
                    Zaman Sapması - Yeniden Optimize Et
                  </Button>
                )}
                
                <Button
                  mode="contained"
                  onPress={handleFinishJourney}
                  style={styles.finishButton}
                  buttonColor="#EF4444"
                  loading={actionLoading}
                  icon={!isOnline ? "cloud-off-outline" : "stop"}
                >
                  {!isOnline ? 'Çevrimdışı Bitir' : 'Seferi Bitir'}
                </Button>
              </>
            )}
          </Card.Content>
        </Card>

        
        {journey.route?.optimized && (
          <View style={{ paddingHorizontal: 16 }}>
            <Text style={styles.sectionTitle}>Rota Haritası</Text>
            <JourneyMapView 
              journeyId={journeyId}
              stopsVersion={journey.stops?.map(s => `${s.id}-${s.status}`).join(',')} // YENİ
              onPress={() => {
                Alert.alert(
                  'Harita Seçenekleri',
                  'Ne yapmak istersiniz?',
                  [
                    {
                      text: 'Haritayı Paylaş',
                      onPress: async () => {
                        Alert.alert('Bilgi', 'Paylaşım özelliği yakında eklenecek');
                      }
                    },
                    { text: 'Kapat', style: 'cancel' }
                  ]
                );
              }}
            />
          </View>
        )}
        
        {/* Başlangıç Deposu */}
        {journey.route?.depot && (
          <View style={styles.stopsSection}>
            <Text style={styles.sectionTitle}>Başlangıç Deposu</Text>
            <Card style={styles.depotCard}>
              <Card.Content>
                <View style={styles.stopHeader}>
                  <View style={[styles.stopNumber, { backgroundColor: '#3B82F6' }]}>
                    <Icon name="home-variant" size={20} color="white" />
                  </View>
                  <View style={styles.stopInfo}>
                    <Text style={styles.stopName}>
                      🏭 {journey.route.depot.name}
                    </Text>
                    <Text style={styles.stopAddress} numberOfLines={2}>
                      {journey.route.depot.address}
                    </Text>
                  </View>
                </View>

                <View style={styles.stopDetails}>
                  {journey.route.startDetails?.startTime && (
                    <View style={styles.stopDetailRow}>
                      <Icon name="clock-start" size={16} color="#3B82F6" />
                      <Text style={[styles.stopDetailText, { color: '#3B82F6', fontWeight: '600' }]}>
                        Planlanan Çıkış: {journey.route.startDetails.startTime.substring(0, 5)}
                      </Text>
                    </View>
                  )}

                  {journey.startedAt && (
                    <View style={styles.stopDetailRow}>
                      <Icon name="location-exit" size={16} color="#10B981" />
                      <Text style={[styles.stopDetailText, { color: '#10B981', fontWeight: '600' }]}>
                        Gerçek Çıkış: {format(parseISO(journey.startedAt), 'HH:mm')}
                      </Text>
                    </View>
                  )}
                </View>
              </Card.Content>
            </Card>
          </View>
        )}

        {/* Stops section */}
        <View style={styles.stopsSection}>
          <Text style={styles.sectionTitle}>Duraklar ({journey.stops.filter(s => !isStopExcluded(s)).length})</Text>
          {journey.stops
            .filter(stop => !isStopExcluded(stop)) // Excluded olanları gösterme
            .sort((a, b) => (a.order || 0) - (b.order || 0))
            .map((stop, index) => renderStop(stop, index))}
        </View>
      </ScrollView>

      {/* Delay Reason Modal */}
      {delayReasonData && (
        <DelayReasonModal
          visible={delayReasonModalVisible}
          onDismiss={() => {
            setDelayReasonModalVisible(false);
            setDelayReasonStopId(null);
            setDelayReasonData(null);
          }}
          onSubmit={handleSubmitDelayReason}
          newDelay={delayReasonData.newDelay}
          cumulativeDelay={delayReasonData.cumulativeDelay}
          stopName={delayReasonData.stopName}
        />
      )}

      {/* Stop Detail Modal */}
      <Portal>
        <Modal
          visible={modalVisible}
          onDismiss={() => setModalVisible(false)}
          contentContainerStyle={styles.modalContent}
        >
          {selectedStop && (
            <ScrollView>
              <View style={styles.modalHeader}>
                <Text style={styles.modalTitle}>Durak Detayları</Text>
                <TouchableOpacity onPress={() => setModalVisible(false)}>
                  <Icon name="close" size={24} color="#666" />
                </TouchableOpacity>
              </View>

              <Divider style={styles.modalDivider} />

              <View style={styles.modalBody}>
                <Text style={styles.modalSectionTitle}>Genel Bilgiler</Text>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Durak No:</Text>
                  <Text style={styles.modalValue}>{selectedStop.order}</Text>
                </View>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Durum:</Text>
                  <View style={[
                      styles.modalStatusBadge,
                      { backgroundColor: getStopStatusColor(selectedStop.status) }
                    ]}>
                      <Text style={styles.modalStatusBadgeText}>
                        {getStatusLabel(selectedStop.status)}
                      </Text>
                    </View>
                  </View>

                <Text style={styles.modalSectionTitle}>Müşteri Bilgileri</Text>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>İsim:</Text>
                  <Text style={styles.modalValue}>
                    {selectedStop.routeStop?.name || 'Belirtilmemiş'}
                  </Text>
                </View>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Adres:</Text>
                  <Text style={styles.modalValue}>
                    {selectedStop.routeStop?.address}
                  </Text>
                </View>
                {selectedStop.routeStop?.contactFullName && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>İletişim:</Text>
                    <Text style={styles.modalValue}>
                      {selectedStop.routeStop.contactFullName}
                    </Text>
                  </View>
                )}
                {selectedStop.routeStop?.contactPhone && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>Telefon:</Text>
                    <Text style={styles.modalValue}>
                      {selectedStop.routeStop.contactPhone}
                    </Text>
                  </View>
                )}
                {selectedStop.routeStop?.contactEmail && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>E-posta:</Text>
                    <Text style={styles.modalValue}>
                      {selectedStop.routeStop.contactEmail}
                    </Text>
                  </View>
                )}

                <Text style={styles.modalSectionTitle}>Zaman Bilgileri</Text>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Tahmini Varış:</Text>
                  <Text style={styles.modalValue}>
                    {formatTime(selectedStop.estimatedArrivalTime)}
                  </Text>
                </View>
                {selectedStop.estimatedDepartureTime && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>Tahmini Ayrılış:</Text>
                    <Text style={styles.modalValue}>
                      {formatTime(selectedStop.estimatedDepartureTime)}
                    </Text>
                  </View>
                )}
                {selectedStop.checkInTime && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>Gerçek Varış:</Text>
                    <Text style={styles.modalValue}>
                      {format(parseISO(selectedStop.checkInTime), 'HH:mm')}
                    </Text>
                  </View>
                )}
                {selectedStop.checkOutTime && (
                  <View style={styles.modalInfoRow}>
                    <Text style={styles.modalLabel}>Gerçek Ayrılış:</Text>
                    <Text style={styles.modalValue}>
                      {format(parseISO(selectedStop.checkOutTime), 'HH:mm')}
                    </Text>
                  </View>
                )}

                <Text style={styles.modalSectionTitle}>Teslimat Bilgileri</Text>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Mesafe:</Text>
                  <Text style={styles.modalValue}>
                    {selectedStop.distance.toFixed(1)} km
                  </Text>
                </View>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>Sipariş Tipi:</Text>
                  <Text style={styles.modalValue}>
                    {selectedStop.routeStop?.orderType || 'Standart'}
                  </Text>
                </View>
                <View style={styles.modalInfoRow}>
                  <Text style={styles.modalLabel}>İmza Gerekli:</Text>
                  <Text style={styles.modalValue}>
                    {selectedStop.routeStop?.proofOfDeliveryRequired ? 'Evet' : 'Hayır'}
                  </Text>
                </View>

                {selectedStop.routeStop?.notes && (
                  <>
                    <Text style={styles.modalSectionTitle}>Notlar</Text>
                    <View style={styles.modalNotesBox}>
                      <Text style={styles.modalNotesText}>
                        {selectedStop.routeStop.notes}
                      </Text>
                    </View>
                  </>
                )}

                <View style={styles.modalActions}>
                  <Button
                    mode="contained"
                    onPress={async () => {
                      setModalVisible(false);
                      await openInMaps(selectedStop);
                    }}
                    icon="navigation"
                    buttonColor="#3B82F6"
                    style={{ flex: 1, marginRight: 8 }}
                  >
                    Haritada Göster
                  </Button>
                  {selectedStop.routeStop?.contactPhone && (
                    <Button
                      mode="outlined"
                      onPress={() => {
                        setModalVisible(false);
                        callCustomer(selectedStop.routeStop.contactPhone);
                      }}
                      icon="phone"
                      style={{ flex: 1, marginLeft: 8 }}
                    >
                      Ara
                    </Button>
                  )}
                </View>
                {user?.isDriver && selectedStop.routeStop?.customerId && (
                  <Button
                    mode="outlined"
                    icon="map-marker-alert"
                    onPress={() => {
                      const currentLatitude = selectedStop.routeStop?.latitude ?? selectedStop.endLatitude;
                      const currentLongitude = selectedStop.routeStop?.longitude ?? selectedStop.endLongitude;
                      const currentAddress = selectedStop.routeStop?.address ?? selectedStop.endAddress;

                      setModalVisible(false);
                      navigation.navigate('RequestLocationUpdate', {
                        journeyId,
                        stopId: selectedStop.id,
                        customerId: selectedStop.routeStop?.customerId,
                        currentLatitude,
                        currentLongitude,
                        currentAddress
                      });
                    }}
                    style={styles.locationRequestButton}
                  >
                    Konum Güncelleme Talebi
                  </Button>
                )}
              </View>
            </ScrollView>
          )}
        </Modal>
      </Portal>
    </>
  );
};

// Status label helper - DÜZELTME
const getStatusLabel = (status: string): string => {
  const normalizedStatus = status?.toLowerCase() || '';
  const useShortLabels = screenWidth < 380; // 375'ten 380'e çıkardık
  
  switch (normalizedStatus) {
    case 'planned': return 'Planlandı';
    case 'preparing': return 'Hazırlanıyor';
    case 'in_progress': 
    case 'inprogress': return useShortLabels ? 'Devam' : 'Devam Ediyor';
    case 'completed': return useShortLabels ? 'Tamam' : 'Tamamlandı'; // Kısa label eklendi
    case 'cancelled': return useShortLabels ? 'İptal' : 'İptal Edildi';
    case 'on_hold':
    case 'onhold': return 'Beklemede';
    case 'pending': return 'Bekliyor';
    case 'failed': return 'Başarısız';
    case 'skipped': return 'Atlandı';
    default: return status || 'Bilinmiyor';
  }
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    fontSize: 14,
    color: '#666',
  },
  // Custom Chip styles - DÜZELTME
  customChip: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 10, // 12'den 10'a düşürdük
    paddingVertical: 5, // 6'dan 5'e düşürdük
    borderRadius: 14,
    backgroundColor: '#e0e0e0',
    minHeight: 26, // Minimum yükseklik
  },
  customChipText: {
    fontSize: 11, // 12'den 11'e düşürdük
    fontWeight: '600',
    flexShrink: 1, // Yazının daralabilmesini sağla
  },
  modalStatusBadge: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modalStatusBadgeText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  headerCard: {
    margin: 16,
    elevation: 3,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  journeyId: {
    fontSize: normalize(12),
    color: '#666',
  },
  routeName: {
    fontSize: normalize(18),
    fontWeight: '600',
    color: '#333',
    marginTop: normalize(2),
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: normalize(4),
    flexWrap: 'wrap', // Sığmazsa alt satıra geçsin
    maxWidth: wp(45), // Maksimum genişlik - DÜZELTME: Sayı olarak
  },
  statusChip: { // DÜZELTME
    height: normalize(28),
    paddingHorizontal: normalize(10), // 12'den 10'a
    minWidth: isSmallDevice ? 85 : 100, // Genişliği artırdık
    borderRadius: 14,
    justifyContent: 'center',
    alignItems: 'center',
  },
  offlineChip: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FEE2E2',
    paddingHorizontal: normalize(8),
    paddingVertical: normalize(4),
    borderRadius: normalize(12),
  },
  offlineText: {
    fontSize: 11,
    color: '#EF4444',
    marginLeft: 4,
    fontWeight: '600',
  },
  queueChip: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FEF3C7',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  queueText: {
    fontSize: 11,
    color: '#F59E0B',
    marginLeft: 4,
    fontWeight: '600',
  },
  divider: {
    marginVertical: 16,
  },
  infoGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginBottom: 16,
  },
  infoItem: {
    alignItems: 'center',
  },
  infoLabel: {
    fontSize: 11,
    color: '#999',
    marginTop: 4,
  },
  infoValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginTop: 2,
  },
  routeNotesContainer: {
    backgroundColor: '#FEF3C7',
    padding: 12,
    borderRadius: 8,
    marginBottom: 16,
  },
  routeNotesTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#92400E',
    marginBottom: 6,
  },
  routeNotesText: {
    fontSize: 13,
    color: '#78350F',
    lineHeight: 18,
  },
  excludedNotification: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#FFF3CD',
    padding: 12,
    borderRadius: 8,
    marginBottom: 16,
  },
  excludedNotificationText: {
    fontSize: 13,
    color: '#856404',
    marginLeft: 8,
    flex: 1,
  },
  // ✅ YENİ: Reoptimization Banner Styles
  reoptimizationBanner: {
    backgroundColor: '#FEF3C7',
    padding: 14,
    borderRadius: 10,
    marginBottom: 16,
    borderLeftWidth: 4,
    borderLeftColor: '#F59E0B',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  reoptimizationTitle: {
    fontSize: 15,
    fontWeight: '700',
    color: '#92400E',
    marginBottom: 4,
  },
  reoptimizationText: {
    fontSize: 13,
    color: '#78350F',
    lineHeight: 18,
  },
  reoptimizeButton: {
    backgroundColor: '#F59E0B',
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderRadius: 8,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    marginLeft: 12,
  },
  reoptimizeButtonText: {
    color: '#FFFFFF',
    fontSize: 13,
    fontWeight: '600',
    marginLeft: 4,
  },
  progressSection: {
    marginVertical: 16,
  },
  progressHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  progressTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
  },
  progressText: {
    fontSize: 14,
    fontWeight: '600',
    color: '#3B82F6',
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e5e5e5',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: 4,
  },
  progressSubtext: {
    fontSize: 12,
    color: '#666',
    marginTop: 6,
  },
  startButton: {
    marginTop: 16,
  },
  optimizeButton: {
    marginTop: 16,
    borderColor: '#F59E0B',
  },
  finishButton: {
    marginTop: 16,
  },
  stopsSection: {
    paddingHorizontal: 16,
    paddingBottom: 24,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 12,
  },
  stopCard: {
    marginBottom: 12,
    elevation: 2,
  },
  currentStopCard: {
    borderColor: '#3B82F6',
    borderWidth: 2,
  },
  inProgressStopCard: {
    borderColor: '#3B82F6',
    borderWidth: 1,
    borderStyle: 'solid',
  },
  excludedStopWrapper: {
    opacity: 0.7,
  },
  excludedStop: {
    backgroundColor: '#ffebee',
    borderLeftColor: '#f44336',
    borderLeftWidth: 4,
  },
  excludedWarning: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#fff3cd',
    padding: 8,
    borderRadius: 4,
    marginBottom: 12,
  },
  excludedWarningText: {
    fontSize: 12,
    color: '#856404',
    marginLeft: 8,
    flex: 1,
  },
  endPointCard: {
    marginBottom: 12,
    elevation: 2,
    backgroundColor: '#F9FAFB',
  },
  depotCard: {
    marginBottom: 12,
    elevation: 2,
    backgroundColor: '#EBF8FF',
    borderLeftWidth: 4,
    borderLeftColor: '#3B82F6',
  },
  stopHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 12,
  },
  stopNumber: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  stopNumberText: {
    color: 'white',
    fontWeight: '600',
    fontSize: 14,
  },
  stopInfo: {
    flex: 1,
  },
  stopName: {
    fontSize: 15,
    fontWeight: '600',
    color: '#333',
    marginBottom: 2,
  },
  stopAddress: {
    fontSize: 13,
    color: '#666',
    lineHeight: 18,
  },
  stopStatusContainer: {
    alignItems: 'center',
  },
  stopStatusDot: {
    width: 12,
    height: 12,
    borderRadius: 6,
    marginBottom: 4,
  },
  inProgressBar: {
    width: 4,
    height: 40,
    backgroundColor: '#3B82F6',
    borderRadius: 2,
  },
  currentChip: {
    height: 20,
    backgroundColor: '#3B82F6',
  },
  currentChipText: {
    fontSize: 10,
    color: 'white',
  },
  stopDetails: {
    marginLeft: 44,
    marginBottom: 12,
  },
  stopDetailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  stopDetailText: {
    fontSize: 13,
    color: '#666',
    marginLeft: 6,
  },
  contactRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginLeft: 44,
    marginBottom: 12,
  },
  contactText: {
    fontSize: 13,
    color: '#666',
    marginLeft: 6,
    flex: 1,
  },
  notesContainer: {
    marginLeft: 44,
    marginBottom: 12,
    padding: 8,
    backgroundColor: '#f9f9f9',
    borderRadius: 4,
  },
  notesTitle: {
    fontSize: 12,
    fontWeight: '600',
    color: '#666',
    marginBottom: 4,
  },
  notesText: {
    fontSize: 13,
    color: '#333',
    lineHeight: 18,
  },
  stopFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start', // flex-start yaparak üstten hizala
    marginTop: 8,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: '#e5e5e5',
    gap: 8,
    minHeight: 42, // Minimum yükseklik belirle
  },
  navigationContainer: {
    flex: 1.5,
    marginRight: 8,
    alignItems: 'flex-start', // Üstten hizala
  },
  navigationButton: {
    height: 42,
  },
  navigationButtonFullWidth: {
    height: 42,
  },
  navigationButtonLabel: {
    fontSize: 13,
  },
  buttonContent: {
    paddingHorizontal: 4,
  },
  navigationFullWidth: {
    marginTop: 12,
    marginBottom: 8,
  },
  navigationHint: {
    fontSize: 10,
    color: '#999',
    textAlign: 'center',
    marginTop: 4,
    fontStyle: 'italic',
  },
  navigationHintSmall: {
    fontSize: 9,
    color: '#999',
    textAlign: 'center',
    marginTop: 2,
    fontStyle: 'italic',
  },
  inProgressActions: {
    flexDirection: 'row',
    gap: 8,
    marginBottom: 8,
  },
  inProgressButton: {
    flex: 1,
  },
  inProgressButtonSmall: {
    minWidth: 70,
  },
  stopActions: {
    flexDirection: 'row',
    gap: 8,
    flex: 1.5,
    justifyContent: 'flex-end',
    alignItems: 'flex-start', // Üstten hizala
    minHeight: 42, // Minimum yükseklik
  },
  actionButton: {
    minWidth: 60,
  },
  failedContainer: {
    flexDirection: 'row',
    gap: 8,
    alignItems: 'center',
  },
  retryButtonSmall: {
    marginLeft: 4,
  },
  detailIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 8,
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: '#f0f0f0',
  },
  detailIndicatorText: {
    fontSize: 11,
    color: '#999',
  },
  // Modal styles
  modalContent: {
    backgroundColor: 'white',
    margin: 20,
    borderRadius: 12,
    maxHeight: '80%',
  },
  modalHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  modalDivider: {
    marginHorizontal: 16,
  },
  modalBody: {
    padding: 16,
  },
  modalSectionTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#333',
    marginTop: 16,
    marginBottom: 8,
  },
  modalInfoRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 6,
    minHeight: 32,
  },
  modalLabel: {
    fontSize: 13,
    color: '#666',
    flex: 1,
    marginRight: 8,
  },
  modalValue: {
    fontSize: 13,
    color: '#333',
    flex: 2,
    textAlign: 'right',
    flexWrap: 'wrap',
  },
  modalNotesBox: {
    backgroundColor: '#f9f9f9',
    padding: 12,
    borderRadius: 8,
    marginTop: 8,
  },
  modalNotesText: {
    fontSize: 13,
    color: '#333',
    lineHeight: 18,
  },
  modalActions: {
    flexDirection: 'row',
    marginTop: 24,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: '#e5e5e5',
  },
  locationRequestButton: {
    marginTop: 12,
  },
});

export default JourneyDetailScreen;

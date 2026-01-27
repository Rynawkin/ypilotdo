// C:\Projects\RotaAppMobile\src\screens\dashboard\DashboardScreen.tsx

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  RefreshControl,
  TouchableOpacity,
  Alert,
  Dimensions,
  Platform,
  Text as RNText,
} from 'react-native';
import {
  Card,
  Text,
  Surface,
  Button,
  Chip,
  ActivityIndicator,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import { useAuth } from '../../contexts/AuthContext';
import journeyService from '../../services/journeyService';
import subscriptionService from '../../services/subscriptionService';
import { JourneyResponse, JourneySummaryResponse } from '../../types/journey.types';
import {
  widthPercentageToDP as wp,
  heightPercentageToDP as hp,
} from 'react-native-responsive-screen';

const { width: screenWidth } = Dimensions.get('window');
const isSmallDevice = screenWidth < 375;
const isVerySmallDevice = screenWidth < 350;

// Responsive Text Component
const AutoText: React.FC<{
  children: React.ReactNode;
  style?: any;
  numberOfLines?: number;
}> = ({ children, style, numberOfLines = 1 }) => {
  if (Platform.OS === 'ios') {
    return (
      <RNText
        style={style}
        numberOfLines={numberOfLines}
        adjustsFontSizeToFit
        minimumFontScale={0.6}
        allowFontScaling={false}
      >
        {children}
      </RNText>
    );
  }
  return <Text style={style} numberOfLines={numberOfLines}>{children}</Text>;
};

const DashboardScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [summary, setSummary] = useState<JourneySummaryResponse | null>(null);
  const [activeJourney, setActiveJourney] = useState<JourneyResponse | null>(null);
  const [todaysJourneys, setTodaysJourneys] = useState<JourneyResponse[]>([]);

  // Dashboard verilerini yükle
  const loadDashboardData = useCallback(async (showLoader = true) => {
    try {
      if (showLoader) setLoading(true);

      // Bugünün tarihini al
      const today = new Date();
      const todayStr = today.toISOString().split('T')[0];

      // Özet verileri al - timeout ekle
      const summaryPromise = journeyService.getJourneysSummary({
        from: todayStr,
        to: todayStr
      }).catch(() => null);

      // Bugünün journey'lerini al - timeout ekle
      const journeysPromise = journeyService.getTodaysJourneys()
        .catch(() => []);

      const [summaryData, journeys] = await Promise.all([
        summaryPromise,
        journeysPromise
      ]);

      if (summaryData) setSummary(summaryData);
      setTodaysJourneys(journeys || []);

      // Trial limit kontrolü (sadece Admin/Dispatcher için)
      console.log('[Dashboard] User roles:', { isAdmin: user?.isAdmin, isDispatcher: user?.isDispatcher });
      if (user?.isAdmin || user?.isDispatcher) {
        console.log('[Dashboard] Starting trial limit check...');
        try {
          const limitResult = await subscriptionService.checkTrialLimits();
          console.log('[Dashboard] Trial limit result:', limitResult);
          
          if (limitResult.isTrialUser && limitResult.usage) {
            const { usage, stopsExceeded, whatsAppExceeded, stopsNearLimit, whatsAppNearLimit } = limitResult;
            console.log('[Dashboard] Trial user detected, checking alerts...', {
              stopsExceeded, whatsAppExceeded, stopsNearLimit, whatsAppNearLimit
            });
            
            // Limit aşıldıysa kritik uyarı
            if (stopsExceeded || whatsAppExceeded) {
              Alert.alert(
                'Trial Limit Aşıldı',
                `Trial limitiniz aşıldı:\n` +
                `• Duraklar: ${usage.currentMonthStops}/${usage.includedMonthlyStops}\n` +
                `• WhatsApp: ${usage.currentMonthWhatsAppMessages}/${usage.includedWhatsAppMessages}\n\n` +
                `Plan yükseltmek için web panelini kullanın.`,
                [{ text: 'Tamam', style: 'default' }]
              );
            }
            // Limite yaklaşıldıysa uyarı
            else if (stopsNearLimit || whatsAppNearLimit) {
              Alert.alert(
                'Trial Limite Yaklaşıyorsunuz',
                `Trial limitinize yaklaşıyorsunuz:\n` +
                `• Duraklar: ${usage.currentMonthStops}/${usage.includedMonthlyStops}\n` +
                `• WhatsApp: ${usage.currentMonthWhatsAppMessages}/${usage.includedWhatsAppMessages}\n\n` +
                `Plan yükseltmeyi düşünün.`,
                [{ text: 'Tamam', style: 'default' }]
              );
            }
          }
        } catch (error) {
          console.error('[Dashboard] Trial limit check error:', error);
        }
      } else {
        console.log('[Dashboard] User is not Admin or Dispatcher, skipping trial limit check');
      }

      // Aktif seferi bul (InProgress olanı)
      const activeOnes = (journeys || []).filter(j => 
        j.status?.toLowerCase() === 'inprogress' || 
        j.status?.toLowerCase() === 'in_progress'
      );
      
      if (activeOnes.length > 0) {
        setActiveJourney(activeOnes[0]);
      } else {
        setActiveJourney(null);
      }

    } catch (error) {
      console.error('Dashboard data error:', error);
      // Hata durumunda boş data göster
      setTodaysJourneys([]);
      setActiveJourney(null);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadDashboardData();
  }, [loadDashboardData]);

  // Ekrana focus olduğunda yenile
  useFocusEffect(
    useCallback(() => {
      loadDashboardData(false);
    }, [loadDashboardData])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadDashboardData(false);
  };

  // Aktif sefer kartı
  const renderActiveJourney = () => {
    if (!activeJourney) {
      return (
        <Card style={styles.card}>
          <Card.Content style={styles.emptyCard}>
            <Icon name="truck-check-outline" size={isSmallDevice ? 40 : 48} color="#9CA3AF" />
            <AutoText style={styles.emptyText}>Aktif sefer bulunmuyor</AutoText>
            <AutoText style={styles.emptySubtext} numberOfLines={2}>
              Yeni sefer atandığında burada görünecek
            </AutoText>
          </Card.Content>
        </Card>
      );
    }

    const completedStops = activeJourney.stops?.filter(s => 
      s.status?.toLowerCase() === 'completed'
    ).length || 0;
    const totalStops = activeJourney.stops?.length || 0;
    const progress = totalStops > 0 ? Math.round((completedStops / totalStops) * 100) : 0;

    return (
      <TouchableOpacity
        onPress={() => {
          navigation.navigate('Journeys', {
            screen: 'JourneyDetail',
            params: { journeyId: activeJourney.id }
          });
        }}
        activeOpacity={0.8}
      >
        <Card style={[styles.card, styles.activeCard]}>
          <Card.Content>
            <View style={styles.cardHeader}>
              <View style={{ flex: 1 }}>
                <Text style={styles.activeTitle}>Aktif Sefer</Text>
                <AutoText style={styles.activeRoute} numberOfLines={1}>
                  {activeJourney.route?.name || 'İsimsiz Rota'}
                </AutoText>
              </View>
              <Chip mode="flat" style={styles.activeChip}>
                #{activeJourney.id}
              </Chip>
            </View>

            <View style={styles.progressSection}>
              <View style={styles.progressInfo}>
                <Text style={styles.progressLabel}>İlerleme</Text>
                <Text style={styles.progressText}>
                  {completedStops} / {totalStops} durak
                </Text>
              </View>
              <Text style={styles.progressPercent}>{progress}%</Text>
            </View>

            <View style={styles.progressBar}>
              <View style={[styles.progressFill, { width: `${progress}%` }]} />
            </View>

            <View style={styles.activeMetrics}>
              <View style={styles.metric}>
                <Icon name="clock-outline" size={isSmallDevice ? 14 : 16} color="#6B7280" />
                <AutoText style={styles.metricText}>
                  ~{Math.round(activeJourney.totalDuration / 60)} dk
                </AutoText>
              </View>
              <View style={styles.metric}>
                <Icon name="map-marker-distance" size={isSmallDevice ? 14 : 16} color="#6B7280" />
                <AutoText style={styles.metricText}>
                  {activeJourney.totalDistance?.toFixed(1) || '0'} km
                </AutoText>
              </View>
            </View>

            <Button
              mode="contained"
              onPress={() => {
                navigation.navigate('Journeys', {
                  screen: 'JourneyDetail',
                  params: { journeyId: activeJourney.id }
                });
              }}
              style={styles.continueButton}
              labelStyle={{ fontSize: isSmallDevice ? 13 : 14 }}
              icon="arrow-right"
              compact={isSmallDevice}
            >
              Devam Et
            </Button>
          </Card.Content>
        </Card>
      </TouchableOpacity>
    );
  };

  // Özet kartları - Gerçek verilerle
  const renderSummaryCards = () => {
    // Bugünkü journey'lerden hesapla
    const totalJourneys = todaysJourneys.length;
    const completedJourneys = todaysJourneys.filter(j => 
      j.status?.toLowerCase() === 'completed'
    ).length;
    const inProgressJourneys = todaysJourneys.filter(j => 
      j.status?.toLowerCase() === 'inprogress' || 
      j.status?.toLowerCase() === 'in_progress'
    ).length;
    const plannedJourneys = todaysJourneys.filter(j => 
      j.status?.toLowerCase() === 'planned' || 
      j.status?.toLowerCase() === 'preparing'
    ).length;

    const cardData = [
      {
        icon: "package-variant",
        value: totalJourneys,
        label: "Toplam\nSefer",
        color: "#3B82F6"
      },
      {
        icon: "check-circle",
        value: completedJourneys,
        label: "Tamamlanan",
        color: "#10B981"
      },
      {
        icon: "truck-delivery",
        value: inProgressJourneys,
        label: "Devam\nEden",
        color: "#F59E0B"
      },
      {
        icon: "clock-outline",
        value: plannedJourneys,
        label: "Bekleyen",
        color: "#8B5CF6"
      }
    ];

    return (
      <View style={styles.summaryContainer}>
        {cardData.map((card, index) => (
          <View key={index} style={styles.summaryCard}>
            <Surface style={styles.summaryCardSurface} elevation={1}>
              <Icon name={card.icon} size={20} color={card.color} />
              <AutoText style={styles.summaryValue}>{card.value}</AutoText>
              <AutoText style={styles.summaryLabel} numberOfLines={2}>
                {card.label}
              </AutoText>
            </Surface>
          </View>
        ))}
      </View>
    );
  };

  // Hızlı istatistikler
  const renderQuickStats = () => {
    // Bugünkü verilerden hesapla
    const totalStops = todaysJourneys.reduce((sum, j) => 
      sum + (j.stops?.length || 0), 0
    );
    const completedStops = todaysJourneys.reduce((sum, j) => 
      sum + (j.stops?.filter(s => s.status?.toLowerCase() === 'completed').length || 0), 0
    );
    const successRate = totalStops > 0 
      ? Math.round((completedStops / totalStops) * 100) 
      : 0;

    const totalHours = todaysJourneys.reduce((sum, j) => 
      sum + (j.totalDuration || 0), 0
    ) / 60;

    return (
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Bugünkü Performans</Text>
          
          <View style={styles.statRow}>
            <View style={styles.statItem}>
              <AutoText style={styles.statValue}>{totalStops}</AutoText>
              <AutoText style={styles.statLabel}>Teslimat</AutoText>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <AutoText style={styles.statValue}>{successRate}%</AutoText>
              <AutoText style={styles.statLabel}>Başarı</AutoText>
            </View>
            <View style={styles.statDivider} />
            <View style={styles.statItem}>
              <AutoText style={styles.statValue}>{totalHours.toFixed(1)}</AutoText>
              <AutoText style={styles.statLabel}>Saat</AutoText>
            </View>
          </View>
        </Card.Content>
      </Card>
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
    <ScrollView
      style={styles.container}
      refreshControl={
        <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#3B82F6']} />
      }
    >
      {/* Hoşgeldin başlığı */}
      <View style={styles.header}>
        <Text style={styles.greeting}>Merhaba, {user?.fullName?.split(' ')[0] || 'Kullanıcı'}</Text>
        <Text style={styles.date}>
          {format(new Date(), 'dd MMMM yyyy, EEEE', { locale: tr })}
        </Text>
      </View>

      {/* Aktif sefer */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Aktif Sefer</Text>
        {renderActiveJourney()}
      </View>

      {/* Özet kartları */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Bugünkü Özet</Text>
        {renderSummaryCards()}
      </View>

      {/* Hızlı istatistikler */}
      <View style={styles.section}>
        {renderQuickStats()}
      </View>

      {/* Alt boşluk */}
      <View style={{ height: 20 }} />
    </ScrollView>
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
    fontSize: isSmallDevice ? 12 : 14,
    color: '#6B7280',
  },
  header: {
    padding: wp('5%'),
    paddingBottom: hp('1%'),
  },
  greeting: {
    fontSize: isVerySmallDevice ? 18 : isSmallDevice ? 20 : 24,
    fontWeight: '600',
    color: '#111827',
  },
  date: {
    fontSize: isSmallDevice ? 12 : 14,
    color: '#6B7280',
    marginTop: 4,
  },
  section: {
    paddingHorizontal: wp('5%'),
    marginBottom: hp('2.5%'),
  },
  sectionTitle: {
    fontSize: isVerySmallDevice ? 14 : isSmallDevice ? 16 : 18,
    fontWeight: '600',
    color: '#111827',
    marginBottom: hp('1.5%'),
  },
  card: {
    borderRadius: 12,
    elevation: 2,
    backgroundColor: 'white',
  },
  activeCard: {
    borderLeftWidth: 4,
    borderLeftColor: '#3B82F6',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: hp('2%'),
  },
  activeTitle: {
    fontSize: isSmallDevice ? 11 : 12,
    color: '#6B7280',
    marginBottom: 4,
  },
  activeRoute: {
    fontSize: isSmallDevice ? 14 : 16,
    fontWeight: '600',
    color: '#111827',
  },
  activeChip: {
    backgroundColor: '#EFF6FF',
  },
  progressSection: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: hp('1%'),
  },
  progressInfo: {
    flex: 1,
  },
  progressLabel: {
    fontSize: isSmallDevice ? 11 : 12,
    color: '#6B7280',
  },
  progressText: {
    fontSize: isSmallDevice ? 13 : 14,
    fontWeight: '500',
    color: '#111827',
  },
  progressPercent: {
    fontSize: isVerySmallDevice ? 16 : isSmallDevice ? 18 : 20,
    fontWeight: '600',
    color: '#3B82F6',
  },
  progressBar: {
    height: 8,
    backgroundColor: '#E5E7EB',
    borderRadius: 4,
    overflow: 'hidden',
    marginBottom: hp('2%'),
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#3B82F6',
    borderRadius: 4,
  },
  activeMetrics: {
    flexDirection: 'row',
    gap: wp('5%'),
    marginBottom: hp('2%'),
  },
  metric: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  metricText: {
    fontSize: isSmallDevice ? 12 : 13,
    color: '#6B7280',
  },
  continueButton: {
    borderRadius: 8,
  },
  emptyCard: {
    alignItems: 'center',
    paddingVertical: hp('3%'),
  },
  emptyText: {
    fontSize: isSmallDevice ? 14 : 16,
    fontWeight: '500',
    color: '#111827',
    marginTop: hp('1.5%'),
  },
  emptySubtext: {
    fontSize: isSmallDevice ? 12 : 14,
    color: '#6B7280',
    marginTop: 4,
    textAlign: 'center',
  },
  summaryContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    marginHorizontal: -6,
  },
  summaryCard: {
    width: '50%',
    paddingHorizontal: 6,
    marginBottom: 12,
  },
  summaryCardSurface: {
    padding: 16,
    borderRadius: 12,
    backgroundColor: 'white',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 100,
  },
  summaryValue: {
    fontSize: 22,
    fontWeight: '600',
    color: '#111827',
    marginTop: 8,
    marginBottom: 4,
  },
  summaryLabel: {
    fontSize: 11,
    color: '#6B7280',
    textAlign: 'center',
    lineHeight: 14,
  },
  statRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingTop: hp('1.5%'),
  },
  statItem: {
    flex: 1,
    alignItems: 'center',
    paddingHorizontal: wp('1%'),
  },
  statValue: {
    fontSize: isVerySmallDevice ? 14 : isSmallDevice ? 16 : 20,
    fontWeight: '600',
    color: '#111827',
  },
  statLabel: {
    fontSize: isVerySmallDevice ? 9 : isSmallDevice ? 10 : 12,
    color: '#6B7280',
    marginTop: 4,
    textAlign: 'center',
  },
  statDivider: {
    width: 1,
    backgroundColor: '#E5E7EB',
  },
});

export default DashboardScreen;
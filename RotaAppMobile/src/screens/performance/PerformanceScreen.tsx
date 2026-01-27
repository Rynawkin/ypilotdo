// C:\Projects\RotaAppMobile\src\screens\performance\PerformanceScreen.tsx

import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  RefreshControl,
  Alert,
  Dimensions,
  Platform,
  PixelRatio,
  Text as RNText,
} from 'react-native';
import {
  Card,
  Text,
  SegmentedButtons,
  ActivityIndicator,
  Surface,
  Chip,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useFocusEffect } from '@react-navigation/native';
import { useAuth } from '../../contexts/AuthContext';
import reportService, { FailureReasonResponse } from '../../services/reportService';
import journeyService from '../../services/journeyService';
import { format, startOfWeek, endOfWeek, startOfMonth, endOfMonth } from 'date-fns';

// Responsive helpers
const { width: screenWidth, height: screenHeight } = Dimensions.get('window');
const baseWidth = 375;
const baseHeight = 812;
const scaleWidth = screenWidth / baseWidth;
const scaleHeight = screenHeight / baseHeight;
const scale = Math.min(scaleWidth, scaleHeight);

const normalize = (size: number) => {
  const newSize = size * scale;
  if (Platform.OS === 'ios') {
    return Math.round(PixelRatio.roundToNearestPixel(newSize));
  } else {
    return Math.round(PixelRatio.roundToNearestPixel(newSize)) - 2;
  }
};

const wp = (percentage: number) => {
  return (percentage * screenWidth) / 100;
};

const hp = (percentage: number) => {
  return (percentage * screenHeight) / 100;
};

const isSmallDevice = screenWidth < 375;
const isVerySmallDevice = screenWidth < 350;
const isTablet = screenWidth >= 768;

// Auto sizing text component
const AutoText: React.FC<{
  children: React.ReactNode;
  style?: any;
  numberOfLines?: number;
  adjustToFit?: boolean;
}> = ({ children, style, numberOfLines = 1, adjustToFit = true }) => {
  if (Platform.OS === 'ios' && adjustToFit) {
    return (
      <RNText
        style={style}
        numberOfLines={numberOfLines}
        adjustsFontSizeToFit
        minimumFontScale={0.5}
        allowFontScaling={false}
      >
        {children}
      </RNText>
    );
  }
  return <Text style={style} numberOfLines={numberOfLines}>{children}</Text>;
};

interface PerformanceData {
  totalDeliveries: number;
  completedDeliveries: number;
  failedDeliveries: number;
  pendingDeliveries: number;
  successRate: number;
  totalDistance: number;
  totalDuration: number;
  failureReasons: {
    reason: string;
    count: number;
  }[];
  weeklyData: {
    day: string;
    deliveries: number;
    completed: number;
  }[];
  monthlyData: {
    week: string;
    deliveries: number;
    completed: number;
  }[];
  avgStopsPerJourney: number;
  peakHour: string;
}

const PerformanceScreen: React.FC = () => {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [period, setPeriod] = useState('today');
  const [performanceData, setPerformanceData] = useState<PerformanceData | null>(null);

  const loadPerformanceData = useCallback(async (showLoader = true) => {
    try {
      if (showLoader) setLoading(true);

      let from: Date;
      let to: Date = new Date();
      
      if (period === 'today') {
        from = new Date();
        from.setHours(0, 0, 0, 0);
        to.setHours(23, 59, 59, 999);
      } else if (period === 'week') {
        from = startOfWeek(new Date(), { weekStartsOn: 1 });
        to = endOfWeek(new Date(), { weekStartsOn: 1 });
      } else {
        from = startOfMonth(new Date());
        to = endOfMonth(new Date());
      }

      // Timeout ekle
      const timeoutPromise = new Promise((_, reject) => 
        setTimeout(() => reject(new Error('Timeout')), 15000)
      );

      const dataPromise = Promise.all([
        reportService.getSummaryStats(period === 'today' ? 'day' : period),
        journeyService.getJourneys({
          from: format(from, 'yyyy-MM-dd'),
          to: format(to, 'yyyy-MM-dd')
        }),
        reportService.getFailureReasons(period === 'today' ? 'today' : period, from, to)
      ]);

      const [summaryStats, journeys, failureReasonsAPI] = await Promise.race([
        dataPromise,
        timeoutPromise
      ]) as [any, any, FailureReasonResponse[]];

      let totalDeliveries = 0;
      let completedDeliveries = 0;
      let failedDeliveries = 0;
      let pendingDeliveries = 0;
      let totalDistance = 0;
      let totalDuration = 0;
      let totalJourneys = 0;

      const hourlyCompletions = new Map<number, number>();
      const weeklyDataMap = new Map<string, { deliveries: number; completed: number }>();
      const dayNames = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
      const monthlyDataMap = new Map<string, { deliveries: number; completed: number }>();

      // Backend'den failure reasons geliyor - mockdata kaldırıldı

      journeys.forEach((journey: any) => {
        totalJourneys++;
        const journeyDate = new Date(journey.date || journey.createdAt);
        const dayIndex = journeyDate.getDay();
        const dayName = dayNames[dayIndex === 0 ? 6 : dayIndex - 1];
        const weekOfMonth = Math.ceil(journeyDate.getDate() / 7);
        const weekName = `${weekOfMonth}. Hafta`;

        if (journey.stops) {
          journey.stops.forEach((stop: any) => {
            totalDeliveries++;
            
            const stopStatus = stop.status?.toLowerCase();
            if (stopStatus === 'completed') {
              completedDeliveries++;
              
              if (stop.checkOutTime) {
                const hour = new Date(stop.checkOutTime).getHours();
                hourlyCompletions.set(hour, (hourlyCompletions.get(hour) || 0) + 1);
              }
              
              if (period === 'week') {
                const existing = weeklyDataMap.get(dayName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                existing.completed++;
                weeklyDataMap.set(dayName, existing);
              }
              
              if (period === 'month') {
                const existing = monthlyDataMap.get(weekName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                existing.completed++;
                monthlyDataMap.set(weekName, existing);
              }
            } else if (stopStatus === 'failed') {
              failedDeliveries++;
              
              // Failure reason'ları journey'dan değil, backend'den alıyoruz
              // Bu kısım sadece failed delivery sayısını hesaplıyor
              
              if (period === 'week') {
                const existing = weeklyDataMap.get(dayName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                weeklyDataMap.set(dayName, existing);
              }
              
              if (period === 'month') {
                const existing = monthlyDataMap.get(weekName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                monthlyDataMap.set(weekName, existing);
              }
            } else if (stopStatus === 'pending' || stopStatus === 'inprogress' || stopStatus === 'in_progress') {
              pendingDeliveries++;
              
              if (period === 'week') {
                const existing = weeklyDataMap.get(dayName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                weeklyDataMap.set(dayName, existing);
              }
              
              if (period === 'month') {
                const existing = monthlyDataMap.get(weekName) || { deliveries: 0, completed: 0 };
                existing.deliveries++;
                monthlyDataMap.set(weekName, existing);
              }
            }
          });
        }

        totalDistance += journey.totalDistance || 0;
        totalDuration += journey.totalDuration || 0;
      });

      let peakHour = '-';
      if (hourlyCompletions.size > 0) {
        const maxHour = Array.from(hourlyCompletions.entries())
          .sort((a, b) => b[1] - a[1])[0];
        if (maxHour) {
          peakHour = `${maxHour[0].toString().padStart(2, '0')}:00`;
        }
      }

      const avgStopsPerJourney = totalJourneys > 0 
        ? totalDeliveries / totalJourneys 
        : 0;

      // Backend'den gelen failure reasons'ı kullan (API'den gelen veriler)
      const failureReasons = failureReasonsAPI.slice(0, 5).map(fr => ({ reason: fr.reason, count: fr.count }));

      const weeklyData = dayNames.map(day => {
        const data = weeklyDataMap.get(day) || { deliveries: 0, completed: 0 };
        return { day, ...data };
      });

      const monthlyData = ['1. Hafta', '2. Hafta', '3. Hafta', '4. Hafta', '5. Hafta'].map(week => {
        const data = monthlyDataMap.get(week) || { deliveries: 0, completed: 0 };
        return { week, ...data };
      });

      const successRate = totalDeliveries > 0 
        ? (completedDeliveries / totalDeliveries) * 100 
        : 0;

      if (summaryStats) {
        totalDeliveries = summaryStats.totalJourneys || totalDeliveries;
        totalDistance = summaryStats.totalDistance || totalDistance;
        totalDuration = summaryStats.totalDuration || totalDuration;
      }

      const performanceData: PerformanceData = {
        totalDeliveries,
        completedDeliveries,
        failedDeliveries,
        pendingDeliveries,
        successRate,
        totalDistance,
        totalDuration,
        failureReasons,
        weeklyData: period === 'week' ? weeklyData : [],
        monthlyData: period === 'month' ? monthlyData : [],
        avgStopsPerJourney,
        peakHour
      };

      setPerformanceData(performanceData);
    } catch (error) {
      console.error('Performance data error:', error);
      
      setPerformanceData({
        totalDeliveries: 0,
        completedDeliveries: 0,
        failedDeliveries: 0,
        pendingDeliveries: 0,
        successRate: 0,
        totalDistance: 0,
        totalDuration: 0,
        failureReasons: [],
        weeklyData: [],
        monthlyData: [],
        avgStopsPerJourney: 0,
        peakHour: '-'
      });
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [period]);

  useEffect(() => {
    loadPerformanceData();
  }, [loadPerformanceData]);

  useFocusEffect(
    useCallback(() => {
      loadPerformanceData(false);
    }, [loadPerformanceData])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadPerformanceData(false);
  };

  const renderMainMetrics = () => {
    if (!performanceData) return null;

    const metricsData = [
      {
        icon: "package-variant",
        label: "Toplam Teslimat",
        value: performanceData.totalDeliveries,
        color: "#3B82F6",
        fullWidth: true
      },
      {
        icon: "check-circle",
        label: "Başarılı",
        value: performanceData.completedDeliveries,
        color: "#10B981",
        chip: `%${performanceData.successRate.toFixed(1)}`,
        borderColor: "#10B981"
      },
      {
        icon: "close-circle",
        label: "Başarısız",
        value: performanceData.failedDeliveries,
        color: "#EF4444",
        borderColor: "#EF4444"
      },
      {
        icon: "truck-delivery",
        label: "Sefer Başına Ortalama",
        value: performanceData.avgStopsPerJourney.toFixed(1),
        unit: "durak",
        color: "#8B5CF6",
        fullWidth: true
      }
    ];

    return (
      <View style={styles.metricsContainer}>
        {metricsData.map((metric, index) => (
          <View 
            key={index} 
            style={[
              styles.metricCardWrapper, 
              metric.fullWidth ? { width: '100%' } : { width: '48%' }
            ]}
          >
            <Surface 
              style={[
                styles.metricCard,
                metric.borderColor && { borderLeftWidth: 3, borderLeftColor: metric.borderColor }
              ]} 
              elevation={1}
            >
              <View style={styles.metricHeader}>
                <Icon name={metric.icon} size={18} color={metric.color} />
                <AutoText style={styles.metricLabel} numberOfLines={2}>
                  {metric.label}
                </AutoText>
              </View>
              <View style={styles.metricValueContainer}>
                <AutoText style={styles.metricValue}>
                  {metric.value}
                </AutoText>
                {metric.unit && (
                  <AutoText style={styles.metricUnit}>{metric.unit}</AutoText>
                )}
              </View>
              {metric.chip && (
                <Chip mode="flat" style={styles.successChip} textStyle={styles.chipText}>
                  {metric.chip}
                </Chip>
              )}
            </Surface>
          </View>
        ))}
      </View>
    );
  };

  const renderQuickInsights = () => {
    if (!performanceData) return null;

    const periodText = period === 'today' ? 'Bugün' : period === 'week' ? 'Bu Hafta' : 'Bu Ay';

    return (
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.cardTitle}>Hızlı Bilgiler</Text>
          
          <View style={styles.insightItem}>
            <View style={styles.insightIcon}>
              <Icon name="clock-alert-outline" size={18} color="#F59E0B" />
            </View>
            <View style={styles.insightContent}>
              <AutoText style={styles.insightLabel}>En Yoğun Saat</AutoText>
              <AutoText style={styles.insightValue}>{performanceData.peakHour}</AutoText>
            </View>
          </View>
          
          <View style={styles.insightItem}>
            <View style={styles.insightIcon}>
              <Icon name="check-all" size={18} color="#10B981" />
            </View>
            <View style={styles.insightContent}>
              <AutoText style={styles.insightLabel}>{periodText} Tamamlanan</AutoText>
              <AutoText style={styles.insightValue}>
                {performanceData.completedDeliveries} teslimat
              </AutoText>
            </View>
          </View>

          <View style={styles.insightItem}>
            <View style={styles.insightIcon}>
              <Icon name="close-circle-outline" size={18} color="#EF4444" />
            </View>
            <View style={styles.insightContent}>
              <AutoText style={styles.insightLabel}>{periodText} Başarısız</AutoText>
              <AutoText style={styles.insightValue}>
                {performanceData.failedDeliveries} teslimat
              </AutoText>
            </View>
          </View>

          {period === 'today' && (
            <View style={styles.insightItem}>
              <View style={styles.insightIcon}>
                <Icon name="clock-outline" size={18} color="#6B7280" />
              </View>
              <View style={styles.insightContent}>
                <AutoText style={styles.insightLabel}>Bekleyen</AutoText>
                <AutoText style={styles.insightValue}>
                  {performanceData.pendingDeliveries} teslimat
                </AutoText>
              </View>
            </View>
          )}
        </Card.Content>
      </Card>
    );
  };

  const renderWeeklyChart = () => {
    if (!performanceData || period !== 'week' || performanceData.weeklyData.length === 0) {
      return null;
    }

    const maxValue = Math.max(...performanceData.weeklyData.map(d => d.deliveries), 1);

    return (
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.cardTitle}>Haftalık Performans</Text>
          <View style={styles.chart}>
            {performanceData.weeklyData.map((day, index) => {
              const height = (day.deliveries / maxValue) * hp(15);
              const successHeight = (day.completed / maxValue) * hp(15);
              
              return (
                <View key={index} style={styles.chartBar}>
                  <AutoText style={styles.chartValue} adjustToFit={false}>
                    {day.deliveries}
                  </AutoText>
                  <View style={styles.barContainer}>
                    <View style={[styles.bar, { height: Math.max(height, 1) }]}>
                      <View style={[styles.successBar, { height: Math.max(successHeight, 1) }]} />
                    </View>
                  </View>
                  <AutoText style={styles.chartLabel}>{day.day}</AutoText>
                </View>
              );
            })}
          </View>
          <View style={styles.chartLegend}>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: '#E5E7EB' }]} />
              <Text style={styles.legendText}>Toplam</Text>
            </View>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: '#10B981' }]} />
              <Text style={styles.legendText}>Başarılı</Text>
            </View>
          </View>
        </Card.Content>
      </Card>
    );
  };

  const renderMonthlyChart = () => {
    if (!performanceData || period !== 'month' || performanceData.monthlyData.length === 0) {
      return null;
    }

    const maxValue = Math.max(...performanceData.monthlyData.map(d => d.deliveries), 1);

    return (
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.cardTitle}>Aylık Performans</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false}>
            <View style={[styles.chart, { width: performanceData.monthlyData.filter(w => w.deliveries > 0).length * 80 }]}>
              {performanceData.monthlyData.filter(week => week.deliveries > 0).map((week, index) => {
                const height = (week.deliveries / maxValue) * hp(15);
                const successHeight = (week.completed / maxValue) * hp(15);
                
                return (
                  <View key={index} style={[styles.chartBar, { width: 70 }]}>
                    <AutoText style={styles.chartValue} adjustToFit={false}>
                      {week.deliveries}
                    </AutoText>
                    <View style={styles.barContainer}>
                      <View style={[styles.bar, { height: Math.max(height, 1) }]}>
                        <View style={[styles.successBar, { height: Math.max(successHeight, 1) }]} />
                      </View>
                    </View>
                    <AutoText style={styles.chartLabel} numberOfLines={2}>
                      {week.week}
                    </AutoText>
                  </View>
                );
              })}
            </View>
          </ScrollView>
          <View style={styles.chartLegend}>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: '#E5E7EB' }]} />
              <Text style={styles.legendText}>Toplam</Text>
            </View>
            <View style={styles.legendItem}>
              <View style={[styles.legendDot, { backgroundColor: '#10B981' }]} />
              <Text style={styles.legendText}>Başarılı</Text>
            </View>
          </View>
        </Card.Content>
      </Card>
    );
  };

  const renderFailureReasons = () => {
    if (!performanceData || performanceData.failureReasons.length === 0) return null;

    const total = performanceData.failureReasons.reduce((sum, r) => sum + r.count, 0);

    return (
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.cardTitle}>Başarısızlık Nedenleri</Text>
          {performanceData.failureReasons.map((reason, index) => {
            const percentage = total > 0 ? (reason.count / total) * 100 : 0;
            
            return (
              <View key={index} style={styles.reasonItem}>
                <View style={styles.reasonHeader}>
                  <AutoText style={styles.reasonText}>{reason.reason}</AutoText>
                  <Text style={styles.reasonCount}>{reason.count}</Text>
                </View>
                <View style={styles.reasonBar}>
                  <View 
                    style={[styles.reasonProgress, { width: `${percentage}%` }]} 
                  />
                </View>
              </View>
            );
          })}
        </Card.Content>
      </Card>
    );
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Performans verileri yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.periodSelector}>
        <SegmentedButtons
          value={period}
          onValueChange={setPeriod}
          buttons={[
            { value: 'today', label: isVerySmallDevice ? 'Gün' : 'Bugün' },
            { value: 'week', label: isVerySmallDevice ? 'Hafta' : 'Bu Hafta' },
            { value: 'month', label: isVerySmallDevice ? 'Ay' : 'Bu Ay' },
          ]}
        />
      </View>

      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={['#3B82F6']} />
        }
      >
        {renderMainMetrics()}
        {renderQuickInsights()}
        {renderWeeklyChart()}
        {renderMonthlyChart()}
        {renderFailureReasons()}
        <View style={{ height: hp(3) }} />
      </ScrollView>
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
    marginTop: normalize(12),
    fontSize: normalize(isVerySmallDevice ? 12 : 14),
    color: '#6B7280',
  },
  periodSelector: {
    padding: normalize(16),
    backgroundColor: 'white',
    elevation: 2,
  },
  scrollContent: {
    padding: normalize(16),
  },
  metricsContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
    marginBottom: 16,
    marginHorizontal: -6,
  },
  metricCardWrapper: {
    paddingHorizontal: 6,
    marginBottom: 12,
  },
  metricCard: {
    padding: 16,
    borderRadius: 12,
    backgroundColor: 'white',
    minHeight: 90,
    justifyContent: 'space-between',
  },
  metricHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    marginBottom: 8,
  },
  metricLabel: {
    fontSize: 12,
    color: '#6B7280',
    flex: 1,
    lineHeight: 16,
  },
  metricValueContainer: {
    flexDirection: 'row',
    alignItems: 'baseline',
    gap: 4,
    marginBottom: 4,
  },
  metricValue: {
    fontSize: 22,
    fontWeight: '600',
    color: '#111827',
  },
  metricUnit: {
    fontSize: 12,
    color: '#6B7280',
  },
  successChip: {
    alignSelf: 'flex-start',
    backgroundColor: '#DCFCE7',
    marginTop: 4,
  },
  chipText: {
    fontSize: 10,
    color: '#166534',
    fontWeight: '500',
  },
  card: {
    borderRadius: normalize(12),
    marginBottom: normalize(16),
    backgroundColor: 'white',
  },
  cardTitle: {
    fontSize: normalize(isVerySmallDevice ? 14 : 15),
    fontWeight: '600',
    color: '#111827',
    marginBottom: normalize(14),
  },
  insightItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  insightIcon: {
    width: 36,
    height: 36,
    borderRadius: 18,
    backgroundColor: '#F3F4F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  insightContent: {
    flex: 1,
  },
  insightLabel: {
    fontSize: 12,
    color: '#6B7280',
    marginBottom: 2,
  },
  insightValue: {
    fontSize: 14,
    fontWeight: '600',
    color: '#111827',
  },
  chart: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-end',
    height: hp(18),
    marginBottom: normalize(12),
  },
  chartBar: {
    flex: 1,
    alignItems: 'center',
  },
  chartValue: {
    fontSize: 10,
    color: '#6B7280',
    marginBottom: 4,
  },
  barContainer: {
    flex: 1,
    width: '60%',
    justifyContent: 'flex-end',
  },
  bar: {
    width: '100%',
    backgroundColor: '#E5E7EB',
    borderRadius: normalize(4),
    overflow: 'hidden',
  },
  successBar: {
    position: 'absolute',
    bottom: 0,
    width: '100%',
    backgroundColor: '#10B981',
    borderRadius: normalize(4),
  },
  chartLabel: {
    fontSize: 10,
    color: '#6B7280',
    marginTop: 4,
    textAlign: 'center',
  },
  chartLegend: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: normalize(20),
    marginTop: normalize(8),
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: normalize(6),
  },
  legendDot: {
    width: normalize(8),
    height: normalize(8),
    borderRadius: normalize(4),
  },
  legendText: {
    fontSize: normalize(11),
    color: '#6B7280',
  },
  reasonItem: {
    marginBottom: normalize(12),
  },
  reasonHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: normalize(4),
  },
  reasonText: {
    fontSize: 13,
    color: '#374151',
    flex: 1,
  },
  reasonCount: {
    fontSize: 13,
    fontWeight: '500',
    color: '#111827',
    marginLeft: 8,
  },
  reasonBar: {
    height: normalize(6),
    backgroundColor: '#E5E7EB',
    borderRadius: normalize(3),
    overflow: 'hidden',
  },
  reasonProgress: {
    height: '100%',
    backgroundColor: '#EF4444',
    borderRadius: normalize(3),
  },
});

export default PerformanceScreen;
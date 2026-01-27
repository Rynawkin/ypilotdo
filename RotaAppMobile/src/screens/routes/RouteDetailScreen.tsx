// C:\Projects\RotaAppMobile\src\screens\routes\RouteDetailScreen.tsx

import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';

import routeService, { Route } from '../../services/routeService';
import journeyService from '../../services/journeyService';

const RouteDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const routeParams = useRoute<any>();
  const routeId = routeParams.params?.routeId;

  const [route, setRoute] = useState<Route | null>(null);
  const [loading, setLoading] = useState(true);
  const [starting, setStarting] = useState(false);

  useEffect(() => {
    loadRoute();
  }, [routeId]);

  const loadRoute = async () => {
    try {
      setLoading(true);
      const data = await routeService.getById(routeId);
      setRoute(data);
    } catch (error) {
      console.error('Error loading route:', error);
      Alert.alert('Hata', 'Rota yüklenirken bir hata oluştu');
      navigation.goBack();
    } finally {
      setLoading(false);
    }
  };

  const handleStartJourney = async () => {
    if (!route) return;
    
    if (!route.driverId || !route.vehicleId) {
      Alert.alert('Uyarı', 'Sefer başlatmak için sürücü ve araç ataması yapmalısınız');
      return;
    }

    Alert.alert(
      'Seferi Başlat',
      'Bu rotayı sefer olarak başlatmak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Başlat',
          onPress: async () => {
            setStarting(true);
            try {
              const journey = await journeyService.startFromRoute(routeId, route.driverId);
              Alert.alert('Başarılı', 'Sefer başlatıldı', [
                {
                  text: 'Sefere Git',
                  onPress: () => {
                    navigation.reset({
                      index: 0,
                      routes: [
                        { name: 'MainTabs' },
                        { 
                          name: 'Journeys',
                          params: {
                            screen: 'JourneyDetail',
                            params: { journeyId: journey.id }
                          }
                        }
                      ]
                    });
                  }
                }
              ]);
            } catch (error: any) {
              console.log('[RouteDetail] Journey creation error:', error);
              const errorMessage = error.userFriendlyMessage || error.message || 'Sefer başlatılamadı';
              Alert.alert('Hata', errorMessage);
            } finally {
              setStarting(false);
            }
          }
        }
      ]
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

  if (!route) {
    return (
      <View style={styles.errorContainer}>
        <Text style={styles.errorText}>Rota bulunamadı</Text>
      </View>
    );
  }

  const isOptimized = route.optimized;
  const hasDriver = !!route.driverId;
  const hasVehicle = !!route.vehicleId;
  const canStartJourney = isOptimized && hasDriver && hasVehicle;

  return (
    <ScrollView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{route.name}</Text>
        <Text style={styles.date}>
          {format(new Date(route.date), 'dd MMMM yyyy EEEE', { locale: tr })}
        </Text>
        
        <View style={styles.statusContainer}>
          {isOptimized ? (
            <View style={styles.statusBadge}>
              <Icon name="check-circle" size={16} color="#10B981" />
              <Text style={styles.statusText}>Optimize Edildi</Text>
            </View>
          ) : (
            <View style={[styles.statusBadge, styles.statusBadgeWarning]}>
              <Icon name="alert-circle" size={16} color="#F59E0B" />
              <Text style={[styles.statusText, styles.statusTextWarning]}>
                Optimize Edilmedi
              </Text>
            </View>
          )}
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Rota Bilgileri</Text>
        
        <View style={styles.infoRow}>
          <Icon name="map-marker-multiple" size={20} color="#6B7280" />
          <Text style={styles.infoLabel}>Durak Sayısı:</Text>
          <Text style={styles.infoValue}>{route.stops?.length || 0}</Text>
        </View>
        
        {route.totalDistance > 0 && (
          <View style={styles.infoRow}>
            <Icon name="road-variant" size={20} color="#6B7280" />
            <Text style={styles.infoLabel}>Toplam Mesafe:</Text>
            <Text style={styles.infoValue}>{route.totalDistance.toFixed(1)} km</Text>
          </View>
        )}
        
        {route.totalDuration > 0 && (
          <View style={styles.infoRow}>
            <Icon name="clock-outline" size={20} color="#6B7280" />
            <Text style={styles.infoLabel}>Tahmini Süre:</Text>
            <Text style={styles.infoValue}>
              {Math.floor(route.totalDuration / 60)} saat {route.totalDuration % 60} dk
            </Text>
          </View>
        )}
        
        {route.startDetails && (
          <View style={styles.infoRow}>
            <Icon name="clock-start" size={20} color="#6B7280" />
            <Text style={styles.infoLabel}>Başlangıç Saati:</Text>
            <Text style={styles.infoValue}>
              {route.startDetails.startTime.substring(0, 5)}
            </Text>
          </View>
        )}
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Atamalar</Text>
        
        <View style={styles.assignmentRow}>
          <Icon 
            name="account" 
            size={20} 
            color={hasDriver ? '#10B981' : '#EF4444'} 
          />
          <Text style={styles.infoLabel}>Sürücü:</Text>
          <Text style={[styles.infoValue, !hasDriver && styles.unassigned]}>
            {hasDriver ? 'Atandı' : 'Atanmadı'}
          </Text>
        </View>
        
        <View style={styles.assignmentRow}>
          <Icon 
            name="truck" 
            size={20} 
            color={hasVehicle ? '#10B981' : '#EF4444'} 
          />
          <Text style={styles.infoLabel}>Araç:</Text>
          <Text style={[styles.infoValue, !hasVehicle && styles.unassigned]}>
            {hasVehicle ? 'Atandı' : 'Atanmadı'}
          </Text>
        </View>
      </View>

      {route.notes && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Notlar</Text>
          <Text style={styles.notes}>{route.notes}</Text>
        </View>
      )}

      {/* Başlangıç Deposu */}
      {route.depot && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Başlangıç Deposu</Text>
          <View style={styles.depotItem}>
            <View style={styles.depotIcon}>
              <Icon name="home-variant" size={20} color="#fff" />
            </View>
            <View style={styles.stopInfo}>
              <Text style={styles.stopName}>🏭 {route.depot.name}</Text>
              <Text style={styles.stopAddress} numberOfLines={2}>
                {route.depot.address}
              </Text>
              {route.startDetails?.startTime && (
                <Text style={styles.stopEta}>
                  Planlanan Çıkış: {route.startDetails.startTime.substring(0, 5)}
                </Text>
              )}
            </View>
          </View>
        </View>
      )}

      {route.stops && route.stops.length > 0 && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Duraklar ({route.stops.length})</Text>
          {route.stops
            .sort((a, b) => a.order - b.order)
            .map((stop, index) => (
              <View key={stop.id || index} style={styles.stopItem}>
                <View style={styles.stopOrder}>
                  <Text style={styles.stopOrderText}>{stop.order}</Text>
                </View>
                <View style={styles.stopInfo}>
                  <Text style={styles.stopName}>{stop.name}</Text>
                  <Text style={styles.stopAddress} numberOfLines={2}>
                    {stop.address}
                  </Text>
                  {stop.estimatedArrivalTime && (
                    <Text style={styles.stopEta}>
                      Tahmini Varış: {stop.estimatedArrivalTime.substring(0, 5)}
                    </Text>
                  )}
                </View>
              </View>
          ))}
        </View>
      )}

      {/* Bitiş Deposu - Optimize edilmişse göster */}
      {route.optimized && route.endDetails && (
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Bitiş Deposu</Text>
          <View style={styles.depotItem}>
            <View style={[styles.depotIcon, { backgroundColor: '#10B981' }]}>
              <Icon name="home-variant" size={20} color="#fff" />
            </View>
            <View style={styles.stopInfo}>
              <Text style={styles.stopName}>🏠 Depo Dönüşü</Text>
              <Text style={styles.stopAddress} numberOfLines={2}>
                {route.endDetails.address || route.depot?.address}
              </Text>
              {route.endDetails.estimatedArrivalTime && (
                <Text style={[styles.stopEta, { color: '#10B981' }]}>
                  Tahmini Dönüş: {route.endDetails.estimatedArrivalTime.substring(0, 5)}
                </Text>
              )}
            </View>
          </View>
        </View>
      )}

      <View style={styles.actionButtons}>
        {!isOptimized && (
          <TouchableOpacity
            style={[styles.button, styles.optimizeButton]}
            onPress={() => navigation.navigate('AddStopsToRoute', { routeId })}
          >
            <Icon name="map-marker-path" size={20} color="#fff" />
            <Text style={styles.buttonText}>Optimize Et</Text>
          </TouchableOpacity>
        )}
        
        {canStartJourney && (
          <TouchableOpacity
            style={[styles.button, styles.startButton]}
            onPress={handleStartJourney}
            disabled={starting}
          >
            {starting ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <>
                <Icon name="play-circle" size={20} color="#fff" />
                <Text style={styles.buttonText}>Seferi Başlat</Text>
              </>
            )}
          </TouchableOpacity>
        )}
        
        {!hasDriver && (
          <TouchableOpacity
            style={[styles.button, styles.assignButton]}
            onPress={() => Alert.alert('Bilgi', 'Sürücü atama özelliği yakında eklenecek')}
          >
            <Icon name="account-plus" size={20} color="#fff" />
            <Text style={styles.buttonText}>Sürücü Ata</Text>
          </TouchableOpacity>
        )}
        
        {!hasVehicle && (
          <TouchableOpacity
            style={[styles.button, styles.assignButton]}
            onPress={() => Alert.alert('Bilgi', 'Araç atama özelliği yakında eklenecek')}
          >
            <Icon name="truck-plus" size={20} color="#fff" />
            <Text style={styles.buttonText}>Araç Ata</Text>
          </TouchableOpacity>
        )}
      </View>
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
    fontSize: 16,
    color: '#6B7280',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorText: {
    fontSize: 16,
    color: '#EF4444',
  },
  header: {
    backgroundColor: '#fff',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  title: {
    fontSize: 24,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 4,
  },
  date: {
    fontSize: 16,
    color: '#6B7280',
    marginBottom: 12,
  },
  statusContainer: {
    flexDirection: 'row',
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#D1FAE5',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  statusBadgeWarning: {
    backgroundColor: '#FEF3C7',
  },
  statusText: {
    marginLeft: 4,
    fontSize: 14,
    fontWeight: '500',
    color: '#10B981',
  },
  statusTextWarning: {
    color: '#F59E0B',
  },
  section: {
    backgroundColor: '#fff',
    margin: 16,
    padding: 16,
    borderRadius: 12,
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
    marginBottom: 12,
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  assignmentRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  infoLabel: {
    flex: 1,
    marginLeft: 12,
    fontSize: 16,
    color: '#6B7280',
  },
  infoValue: {
    fontSize: 16,
    fontWeight: '500',
    color: '#111827',
  },
  unassigned: {
    color: '#EF4444',
  },
  notes: {
    fontSize: 16,
    color: '#374151',
    lineHeight: 24,
  },
  stopItem: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  stopOrder: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  stopOrderText: {
    color: '#fff',
    fontWeight: '600',
    fontSize: 14,
  },
  stopInfo: {
    flex: 1,
  },
  stopName: {
    fontSize: 16,
    fontWeight: '500',
    color: '#111827',
    marginBottom: 2,
  },
  stopAddress: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: 2,
  },
  stopEta: {
    fontSize: 12,
    color: '#3B82F6',
    marginTop: 2,
  },
  depotItem: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    paddingVertical: 12,
  },
  depotIcon: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  actionButtons: {
    padding: 16,
    paddingBottom: 32,
  },
  button: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
  },
  optimizeButton: {
    backgroundColor: '#8B5CF6',
  },
  startButton: {
    backgroundColor: '#10B981',
  },
  assignButton: {
    backgroundColor: '#3B82F6',
  },
  buttonText: {
    marginLeft: 8,
    fontSize: 16,
    fontWeight: '600',
    color: '#fff',
  },
});

export default RouteDetailScreen;
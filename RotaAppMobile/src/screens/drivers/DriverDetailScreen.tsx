import React, { useState, useEffect } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Alert,
  Dimensions,
  TouchableOpacity,
} from 'react-native';
import {
  Card,
  Text,
  ActivityIndicator,
  Button,
  Divider,
  Surface,
  Menu,
  Chip,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation } from '@react-navigation/native';
import { Driver } from '../../types/driver.types';
import driverService from '../../services/driverService';
import { isLargeScreen } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth } = Dimensions.get('window');

interface RouteParams {
  driverId: number;
}

const DriverDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute();
  const { driverId } = route.params as RouteParams;
  
  const [driver, setDriver] = useState<Driver | null>(null);
  const [loading, setLoading] = useState(true);
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  useEffect(() => {
    loadDriver();
  }, [driverId]);

  const loadDriver = async () => {
    try {
      setLoading(true);
      const data = await driverService.getById(driverId);
      if (data) {
        setDriver(data);
      } else {
        Alert.alert(
          'Hata',
          'Sürücü bulunamadı.',
          [{ text: 'Geri', onPress: () => navigation.goBack() }]
        );
      }
    } catch (error) {
      console.error('Load driver error:', error);
      Alert.alert(
        'Hata',
        'Sürücü bilgileri yüklenemedi.',
        [
          { text: 'Tekrar Dene', onPress: loadDriver },
          { text: 'Geri', onPress: () => navigation.goBack() }
        ]
      );
    } finally {
      setLoading(false);
    }
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

  const getActivityStatusLabel = (driver: Driver) => {
    const activityStatus = driverService.getActivityStatus(driver);
    switch (activityStatus) {
      case 'online':
        return 'Çevrimiçi';
      case 'idle':
        return 'Beklemede';
      case 'offline':
      default:
        return 'Çevrimdışı';
    }
  };

  const handleStatusChange = async (newStatus: 'active' | 'inactive' | 'on_leave') => {
    try {
      await driverService.updateStatus(driverId, newStatus);
      setDriver(prev => prev ? { ...prev, status: newStatus } : null);
      setShowStatusMenu(false);
    } catch (error) {
      Alert.alert('Hata', 'Durum güncellenirken bir hata oluştu.');
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Sürücü bilgileri yükleniyor...</Text>
      </View>
    );
  }

  if (!driver) {
    return (
      <View style={styles.errorContainer}>
        <Icon name="account-off" size={64} color="#9CA3AF" />
        <Text style={styles.errorText}>Sürücü bulunamadı</Text>
        <Button mode="contained" onPress={() => navigation.goBack()}>
          Geri Dön
        </Button>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} showsVerticalScrollIndicator={false}>
      {/* Header Card */}
      <Card style={styles.headerCard}>
        <Card.Content>
          <View style={styles.headerContent}>
            <View style={styles.driverIconContainer}>
              <Icon 
                name={getDriverIcon(driver)} 
                size={40} 
                color="#3B82F6" 
              />
              <View style={[
                styles.activityDot,
                { backgroundColor: getActivityStatusColor(driver) }
              ]} />
            </View>
            <View style={styles.headerInfo}>
              <Text style={styles.driverTitle}>
                {driver.name}
              </Text>
              <Text style={styles.phoneNumber}>{driverService.formatPhone(driver.phone)}</Text>
              {driver.email && (
                <Text style={styles.emailAddress}>{driver.email}</Text>
              )}
              <View style={styles.statusContainer}>
                <View style={[
                  styles.statusBadge,
                  { backgroundColor: `${getStatusColor(driver.status)}20` }
                ]}>
                  <Icon 
                    name={getStatusIcon(driver.status)} 
                    size={14} 
                    color={getStatusColor(driver.status)} 
                  />
                  <Text style={[
                    styles.statusText,
                    { color: getStatusColor(driver.status), marginLeft: 4 }
                  ]}>
                    {driverService.getStatusLabel(driver.status)}
                  </Text>
                </View>
                <Chip 
                  icon={() => <Icon name="circle" size={12} color={getActivityStatusColor(driver)} />}
                  style={[styles.activityChip, { backgroundColor: `${getActivityStatusColor(driver)}15` }]}
                  textStyle={{ color: getActivityStatusColor(driver), fontSize: 11 }}
                >
                  {getActivityStatusLabel(driver)}
                </Chip>
              </View>
            </View>
          </View>
        </Card.Content>
      </Card>

      {/* Basic Information */}
      <Card style={styles.infoCard}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Temel Bilgiler</Text>
          <Divider style={styles.divider} />
          
          <View style={styles.infoGrid}>
            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name="card-account-details" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Ehliyet No</Text>
              </View>
              <Text style={styles.infoValue}>{driver.licenseNumber}</Text>
            </View>

            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name="calendar-clock" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Ehliyet Bitiş</Text>
              </View>
              <Text style={styles.infoValue}>
                {driverService.formatLicenseExpiry(driver)}
                {driverService.isLicenseExpired(driver) && (
                  <Text style={styles.expiredText}> (Süresi Dolmuş)</Text>
                )}
                {driverService.isLicenseExpiringSoon(driver) && (
                  <Text style={styles.expiringSoonText}> (Yakında Dolacak)</Text>
                )}
              </Text>
            </View>

            {driver.emergencyContact && (
              <View style={styles.infoItem}>
                <View style={styles.infoLabel}>
                  <Icon name="account-heart" size={16} color="#6B7280" />
                  <Text style={styles.labelText}>Acil Durum Kişisi</Text>
                </View>
                <Text style={styles.infoValue}>{driver.emergencyContact}</Text>
              </View>
            )}

            {driver.emergencyPhone && (
              <View style={styles.infoItem}>
                <View style={styles.infoLabel}>
                  <Icon name="phone-alert" size={16} color="#6B7280" />
                  <Text style={styles.labelText}>Acil Durum Tel</Text>
                </View>
                <Text style={styles.infoValue}>{driverService.formatPhone(driver.emergencyPhone)}</Text>
              </View>
            )}

            {driver.address && (
              <View style={styles.infoItem}>
                <View style={styles.infoLabel}>
                  <Icon name="map-marker" size={16} color="#6B7280" />
                  <Text style={styles.labelText}>Adres</Text>
                </View>
                <Text style={styles.infoValue}>{driver.address}</Text>
              </View>
            )}

            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name="clock-outline" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Son Aktivite</Text>
              </View>
              <Text style={styles.infoValue}>{driverService.formatLastActivity(driver)}</Text>
            </View>
          </View>
        </Card.Content>
      </Card>

      {/* Vehicle Information */}
      <Card style={styles.infoCard}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Araç Bilgileri</Text>
          <Divider style={styles.divider} />
          
          {driver.vehicle ? (
            <View style={styles.vehicleInfo}>
              <View style={styles.vehicleHeader}>
                <Icon name="car" size={24} color="#3B82F6" />
                <View style={styles.vehicleDetails}>
                  <Text style={styles.vehicleName}>
                    {driver.vehicle.brand} {driver.vehicle.model}
                  </Text>
                  <Text style={styles.vehiclePlate}>{driver.vehicle.plateNumber}</Text>
                </View>
              </View>
            </View>
          ) : (
            <View style={styles.noVehicleContainer}>
              <Icon name="car-off" size={32} color="#9CA3AF" />
              <Text style={styles.noVehicleText}>Atanmış araç yok</Text>
            </View>
          )}
        </Card.Content>
      </Card>

      {/* Performance Metrics */}
      <Card style={styles.infoCard}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Performans Metrikleri</Text>
          <Divider style={styles.divider} />
          
          <View style={styles.metricsGrid}>
            <View style={styles.metricItem}>
              <Icon name="star" size={24} color="#F59E0B" />
              <Text style={styles.metricValue}>{(driver.rating || 0).toFixed(1)}</Text>
              <Text style={styles.metricLabel}>Puan</Text>
            </View>
            
            <View style={styles.metricItem}>
              <Icon name="package-variant" size={24} color="#10B981" />
              <Text style={styles.metricValue}>{driver.totalDeliveries || 0}</Text>
              <Text style={styles.metricLabel}>Teslimat</Text>
            </View>
            
            <View style={styles.metricItem}>
              <Icon name="clock-fast" size={24} color="#3B82F6" />
              <Text style={styles.metricValue}>
                {driver.avgDeliveryTime ? `${Math.round(driver.avgDeliveryTime)}dk` : '-'}
              </Text>
              <Text style={styles.metricLabel}>Ort. Süre</Text>
            </View>
          </View>
          
          <Text style={styles.metricsNote}>
            Performans metrikleri, tamamlanan rotalar üzerinden hesaplanır.
          </Text>
        </Card.Content>
      </Card>

      {/* Quick Actions */}
      <Card style={styles.actionsCard}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Hızlı İşlemler</Text>
          <Divider style={styles.divider} />
          
          <View style={styles.actionButtons}>
            <TouchableOpacity
              style={[styles.actionButton, styles.editButton]}
              onPress={() => navigation.navigate('EditDriver', { driverId: driver.id })}
            >
              <Icon name="pencil" size={20} color="#3B82F6" />
              <Text style={[styles.actionButtonText, { color: '#3B82F6' }]}>Düzenle</Text>
            </TouchableOpacity>

            <Menu
              visible={showStatusMenu}
              onDismiss={() => setShowStatusMenu(false)}
              anchor={
                <TouchableOpacity
                  style={[styles.actionButton, styles.statusButton]}
                  onPress={() => setShowStatusMenu(true)}
                >
                  <Icon name="swap-horizontal" size={20} color="#F59E0B" />
                  <Text style={[styles.actionButtonText, { color: '#F59E0B' }]}>Durum Değiştir</Text>
                </TouchableOpacity>
              }
            >
              <Menu.Item
                onPress={() => handleStatusChange('active')}
                title="Aktif"
                leadingIcon="account-check"
                disabled={driver.status === 'active'}
              />
              <Menu.Item
                onPress={() => handleStatusChange('inactive')}
                title="Pasif"
                leadingIcon="account-off"
                disabled={driver.status === 'inactive'}
              />
              <Menu.Item
                onPress={() => handleStatusChange('on_leave')}
                title="İzinli"
                leadingIcon="account-clock"
                disabled={driver.status === 'on_leave'}
              />
            </Menu>
          </View>
        </Card.Content>
      </Card>

      {/* Footer spacing */}
      <View style={styles.footerSpacing} />
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
    fontSize: 14,
    color: '#6B7280',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: wp(8),
  },
  errorText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    marginTop: hp(2),
    marginBottom: hp(3),
  },
  headerCard: {
    margin: wp(4),
    marginBottom: hp(1.5),
    borderRadius: 16,
    elevation: 3,
  },
  headerContent: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  driverIconContainer: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: wp(4),
    position: 'relative',
  },
  activityDot: {
    width: 16,
    height: 16,
    borderRadius: 8,
    position: 'absolute',
    bottom: -2,
    right: -2,
    borderWidth: 2,
    borderColor: 'white',
  },
  headerInfo: {
    flex: 1,
  },
  driverTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 4,
  },
  phoneNumber: {
    fontSize: 16,
    color: '#6B7280',
    fontWeight: '500',
    marginBottom: 4,
  },
  emailAddress: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: hp(1),
  },
  statusContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: wp(3),
    paddingVertical: hp(0.8),
    borderRadius: 12,
    marginRight: wp(2),
  },
  statusText: {
    fontSize: 13,
    fontWeight: '600',
  },
  activityChip: {
    height: 24,
  },
  infoCard: {
    marginHorizontal: wp(4),
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  actionsCard: {
    marginHorizontal: wp(4),
    marginBottom: hp(1.5),
    borderRadius: 12,
    elevation: 2,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: hp(1),
  },
  divider: {
    marginBottom: hp(2),
  },
  infoGrid: {
    gap: hp(2),
  },
  infoItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  infoLabel: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  labelText: {
    fontSize: 14,
    color: '#6B7280',
    fontWeight: '500',
    marginLeft: 8,
  },
  infoValue: {
    fontSize: 14,
    color: '#111827',
    fontWeight: '600',
    flex: 1.5,
    textAlign: 'right',
  },
  expiredText: {
    color: '#EF4444',
    fontWeight: 'bold',
  },
  expiringSoonText: {
    color: '#F59E0B',
    fontWeight: 'bold',
  },
  vehicleInfo: {
    padding: wp(4),
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
  },
  vehicleHeader: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  vehicleDetails: {
    marginLeft: wp(3),
  },
  vehicleName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
  },
  vehiclePlate: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 2,
  },
  noVehicleContainer: {
    alignItems: 'center',
    padding: wp(8),
  },
  noVehicleText: {
    fontSize: 14,
    color: '#9CA3AF',
    marginTop: hp(1),
  },
  metricsGrid: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginBottom: hp(2),
  },
  metricItem: {
    alignItems: 'center',
    padding: wp(2),
  },
  metricValue: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#111827',
    marginTop: hp(0.5),
  },
  metricLabel: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: hp(0.5),
  },
  metricsNote: {
    fontSize: 12,
    color: '#9CA3AF',
    textAlign: 'center',
    fontStyle: 'italic',
    marginTop: hp(1),
  },
  actionButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  actionButton: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: hp(1.5),
    borderRadius: 8,
    marginHorizontal: wp(1),
  },
  editButton: {
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
  },
  statusButton: {
    backgroundColor: 'rgba(245, 158, 11, 0.1)',
  },
  actionButtonText: {
    fontSize: 14,
    fontWeight: '600',
    marginLeft: 8,
  },
  footerSpacing: {
    height: hp(3),
  },
});

export default DriverDetailScreen;
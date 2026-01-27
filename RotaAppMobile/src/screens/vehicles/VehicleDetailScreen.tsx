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
import { Vehicle } from '../../types/vehicle.types';
import vehicleService from '../../services/vehicleService';
import { isLargeScreen } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth } = Dimensions.get('window');

interface RouteParams {
  vehicleId: number;
}

const VehicleDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute();
  const { vehicleId } = route.params as RouteParams;
  
  const [vehicle, setVehicle] = useState<Vehicle | null>(null);
  const [loading, setLoading] = useState(true);
  const [showStatusMenu, setShowStatusMenu] = useState(false);

  useEffect(() => {
    loadVehicle();
  }, [vehicleId]);

  const loadVehicle = async () => {
    try {
      setLoading(true);
      const data = await vehicleService.getById(vehicleId);
      if (data) {
        setVehicle(data);
      } else {
        Alert.alert(
          'Hata',
          'Araç bulunamadı.',
          [{ text: 'Geri', onPress: () => navigation.goBack() }]
        );
      }
    } catch (error) {
      console.error('Load vehicle error:', error);
      Alert.alert(
        'Hata',
        'Araç bilgileri yüklenemedi.',
        [
          { text: 'Tekrar Dene', onPress: loadVehicle },
          { text: 'Geri', onPress: () => navigation.goBack() }
        ]
      );
    } finally {
      setLoading(false);
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

  const getFuelTypeColor = (fuelType: string) => {
    switch (fuelType) {
      case 'electric':
        return '#10B981';
      case 'hybrid':
        return '#8B5CF6';
      case 'gasoline':
        return '#3B82F6';
      case 'diesel':
      default:
        return '#6B7280';
    }
  };


  const handleStatusChange = async (newStatus: 'active' | 'maintenance' | 'inactive') => {
    try {
      await vehicleService.updateStatus(vehicleId, newStatus);
      setVehicle(prev => prev ? { ...prev, status: newStatus } : null);
      setShowStatusMenu(false);
    } catch (error) {
      Alert.alert('Hata', 'Durum güncellenirken bir hata oluştu.');
    }
  };

  const handleMaintenanceMode = () => {
    Alert.alert(
      'Bakıma Al',
      'Bu aracı bakıma almak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Bakıma Al',
          onPress: () => handleStatusChange('maintenance'),
        },
      ]
    );
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Araç bilgileri yükleniyor...</Text>
      </View>
    );
  }

  if (!vehicle) {
    return (
      <View style={styles.errorContainer}>
        <Icon name="car-off" size={64} color="#9CA3AF" />
        <Text style={styles.errorText}>Araç bulunamadı</Text>
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
            <View style={styles.vehicleIconContainer}>
              <Icon 
                name={getVehicleIcon(vehicle.type)} 
                size={40} 
                color="#3B82F6" 
              />
            </View>
            <View style={styles.headerInfo}>
              <Text style={styles.vehicleTitle}>
                {vehicle.brand} {vehicle.model}
              </Text>
              <Text style={styles.plateNumber}>{vehicle.plateNumber}</Text>
              <View style={styles.statusContainer}>
                <View style={[
                  styles.statusBadge,
                  { backgroundColor: `${getStatusColor(vehicle.status)}20` }
                ]}>
                  <Icon 
                    name={getStatusIcon(vehicle.status)} 
                    size={14} 
                    color={getStatusColor(vehicle.status)} 
                  />
                  <Text style={[
                    styles.statusText,
                    { color: getStatusColor(vehicle.status) }
                  ]}>
                    {vehicleService.getStatusLabel(vehicle.status)}
                  </Text>
                </View>
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
                <Icon name="tag" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Tip</Text>
              </View>
              <Text style={styles.infoValue}>{vehicleService.getVehicleTypeLabel(vehicle.type)}</Text>
            </View>

            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name="calendar" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Yıl</Text>
              </View>
              <Text style={styles.infoValue}>{vehicle.year}</Text>
            </View>

            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name="weight-kilogram" size={16} color="#6B7280" />
                <Text style={styles.labelText}>Kapasite</Text>
              </View>
              <Text style={styles.infoValue}>{vehicle.capacity} kg</Text>
            </View>

            <View style={styles.infoItem}>
              <View style={styles.infoLabel}>
                <Icon name={getFuelTypeIcon(vehicle.fuelType)} size={16} color="#6B7280" />
                <Text style={styles.labelText}>Yakıt Tipi</Text>
              </View>
              <View style={styles.fuelTypeContainer}>
                <Chip
                  icon={getFuelTypeIcon(vehicle.fuelType)}
                  style={[
                    styles.fuelChip,
                    { backgroundColor: `${getFuelTypeColor(vehicle.fuelType)}20` }
                  ]}
                  textStyle={{ color: getFuelTypeColor(vehicle.fuelType) }}
                >
                  {vehicleService.getFuelTypeLabel(vehicle.fuelType)}
                </Chip>
              </View>
            </View>
          </View>
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
              onPress={() => navigation.navigate('EditVehicle', { vehicleId: vehicle.id })}
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
                leadingIcon="check-circle"
                disabled={vehicle.status === 'active'}
              />
              <Menu.Item
                onPress={() => handleStatusChange('maintenance')}
                title="Bakımda"
                leadingIcon="wrench"
                disabled={vehicle.status === 'maintenance'}
              />
              <Menu.Item
                onPress={() => handleStatusChange('inactive')}
                title="Pasif"
                leadingIcon="close-circle"
                disabled={vehicle.status === 'inactive'}
              />
            </Menu>

            {vehicle.status !== 'maintenance' && (
              <TouchableOpacity
                style={[styles.actionButton, styles.maintenanceButton]}
                onPress={handleMaintenanceMode}
              >
                <Icon name="wrench" size={20} color="#F59E0B" />
                <Text style={[styles.actionButtonText, { color: '#F59E0B' }]}>Bakıma Al</Text>
              </TouchableOpacity>
            )}

          </View>
        </Card.Content>
      </Card>

      {/* Usage Statistics */}
      <Card style={styles.statsCard}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Kullanım İstatistikleri</Text>
          <Divider style={styles.divider} />
          
          <View style={styles.statsGrid}>
            <View style={styles.statItem}>
              <Icon name="road" size={24} color="#3B82F6" />
              <Text style={styles.statValue}>-</Text>
              <Text style={styles.statLabel}>Toplam Rota</Text>
            </View>
            
            <View style={styles.statItem}>
              <Icon name="map-marker-distance" size={24} color="#10B981" />
              <Text style={styles.statValue}>-</Text>
              <Text style={styles.statLabel}>Toplam Mesafe</Text>
            </View>
            
            <View style={styles.statItem}>
              <Icon name="clock-outline" size={24} color="#F59E0B" />
              <Text style={styles.statValue}>-</Text>
              <Text style={styles.statLabel}>Çalışma Saati</Text>
            </View>
            
            <View style={styles.statItem}>
              <Icon name="chart-line" size={24} color="#8B5CF6" />
              <Text style={styles.statValue}>-</Text>
              <Text style={styles.statLabel}>Verimlilik</Text>
            </View>
          </View>
          
          <Text style={styles.statsNote}>
            İstatistikler, araç rotalara atandıktan sonra görüntülenecektir.
          </Text>
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
  vehicleIconContainer: {
    width: 60,
    height: 60,
    borderRadius: 30,
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: wp(4),
  },
  headerInfo: {
    flex: 1,
  },
  vehicleTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 4,
  },
  plateNumber: {
    fontSize: 16,
    color: '#6B7280',
    fontWeight: '600',
    marginBottom: hp(1),
  },
  statusContainer: {
    flexDirection: 'row',
  },
  statusBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: wp(3),
    paddingVertical: hp(0.8),
    borderRadius: 12,
    gap: 4,
  },
  statusText: {
    fontSize: 13,
    fontWeight: '600',
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
  statsCard: {
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
    alignItems: 'center',
  },
  infoLabel: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  labelText: {
    fontSize: 14,
    color: '#6B7280',
    fontWeight: '500',
  },
  infoValue: {
    fontSize: 14,
    color: '#111827',
    fontWeight: '600',
  },
  fuelTypeContainer: {
    alignItems: 'flex-end',
  },
  fuelChip: {
    height: 28,
  },
  actionButtons: {
    gap: hp(1.5),
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: hp(1.5),
    borderRadius: 8,
    gap: 8,
  },
  editButton: {
    backgroundColor: 'rgba(59, 130, 246, 0.1)',
  },
  statusButton: {
    backgroundColor: 'rgba(245, 158, 11, 0.1)',
  },
  maintenanceButton: {
    backgroundColor: 'rgba(245, 158, 11, 0.1)',
  },
  actionButtonText: {
    fontSize: 14,
    fontWeight: '600',
  },
  statsGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: wp(4),
    marginBottom: hp(2),
  },
  statItem: {
    flex: 1,
    minWidth: '40%',
    alignItems: 'center',
    padding: wp(3),
    backgroundColor: '#F9FAFB',
    borderRadius: 8,
  },
  statValue: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#111827',
    marginTop: hp(0.5),
  },
  statLabel: {
    fontSize: 12,
    color: '#6B7280',
    textAlign: 'center',
    marginTop: hp(0.5),
  },
  statsNote: {
    fontSize: 12,
    color: '#9CA3AF',
    textAlign: 'center',
    fontStyle: 'italic',
    marginTop: hp(1),
  },
  footerSpacing: {
    height: hp(3),
  },
});

export default VehicleDetailScreen;
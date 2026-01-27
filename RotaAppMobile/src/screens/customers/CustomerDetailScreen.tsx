import React, { useState, useEffect } from 'react';
import {
  View,
  StyleSheet,
  ScrollView,
  Alert,
  Dimensions,
  Linking,
} from 'react-native';
import {
  Card,
  Text,
  ActivityIndicator,
  Button,
  Divider,
  Surface,
} from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation } from '@react-navigation/native';
import { Customer } from '../../types/customer.types';
import customerService from '../../services/customerService';
import { isLargeScreen } from '../../utils/responsive';
import { wp, hp } from '../../utils/dimensions';

const { width: screenWidth } = Dimensions.get('window');

interface RouteParams {
  customerId: number;
}

const CustomerDetailScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute();
  const { customerId } = route.params as RouteParams;
  
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadCustomer();
  }, [customerId]);

  const loadCustomer = async () => {
    try {
      setLoading(true);
      const data = await customerService.getById(customerId);
      setCustomer(data);
    } catch (error) {
      console.error('Load customer error:', error);
      Alert.alert(
        'Hata',
        'Müşteri bilgileri yüklenemedi.',
        [
          { text: 'Tekrar Dene', onPress: loadCustomer },
          { text: 'Geri', onPress: () => navigation.goBack() }
        ]
      );
    } finally {
      setLoading(false);
    }
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

  const handleShowOnMap = (latitude: number, longitude: number) => {
    const url = `https://www.google.com/maps?q=${latitude},${longitude}`;
    Linking.openURL(url).catch(() => {
      Alert.alert('Hata', 'Haritalar uygulaması açılamadı');
    });
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Müşteri bilgileri yükleniyor...</Text>
      </View>
    );
  }

  if (!customer) {
    return (
      <View style={styles.errorContainer}>
        <Icon name="account-alert" size={64} color="#EF4444" />
        <Text style={styles.errorText}>Müşteri Bulunamadı</Text>
        <Button mode="contained" onPress={() => navigation.goBack()}>
          Geri Dön
        </Button>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.contentContainer}>
      {/* Temel Bilgiler */}
      <Card style={styles.card}>
        <Card.Content>
          <View style={styles.cardHeader}>
            <View style={styles.headerContent}>
              <Text style={styles.customerName}>{customer.name}</Text>
              <View style={styles.customerMeta}>
                <Icon name="identifier" size={16} color="#6B7280" />
                <Text style={styles.customerCode}>{customer.code}</Text>
              </View>
            </View>
            <View style={[
              styles.priorityBadge,
              { backgroundColor: `${getPriorityColor(customer.priority)}20` }
            ]}>
              <Text style={[
                styles.priorityText,
                { color: getPriorityColor(customer.priority) }
              ]}>
                {getPriorityText(customer.priority)}
              </Text>
            </View>
          </View>
        </Card.Content>
      </Card>

      {/* İletişim Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>İletişim Bilgileri</Text>
          <Divider style={styles.divider} />
          
          {customer.phone && (
            <View style={styles.infoRow}>
              <Icon name="phone" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Telefon</Text>
                <Text style={styles.infoValue}>{customer.phone}</Text>
              </View>
            </View>
          )}

          {customer.email && (
            <View style={styles.infoRow}>
              <Icon name="email" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>E-posta</Text>
                <Text style={styles.infoValue}>{customer.email}</Text>
              </View>
            </View>
          )}

          {customer.address && (
            <View style={styles.infoRow}>
              <Icon name="map-marker" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Adres</Text>
                <Text style={styles.infoValue}>{customer.address}</Text>
              </View>
            </View>
          )}
        </Card.Content>
      </Card>

      {/* Lokasyon */}
      {customer.latitude && customer.longitude && (
        <Card style={styles.card}>
          <Card.Content>
            <Text style={styles.sectionTitle}>Konum</Text>
            <Divider style={styles.divider} />
            
            <View style={styles.coordinatesRow}>
              <View style={styles.coordinateItem}>
                <Text style={styles.coordinateLabel}>Enlem</Text>
                <Text style={styles.coordinateValue}>{customer.latitude.toFixed(6)}</Text>
              </View>
              <View style={styles.coordinateItem}>
                <Text style={styles.coordinateLabel}>Boylam</Text>
                <Text style={styles.coordinateValue}>{customer.longitude.toFixed(6)}</Text>
              </View>
            </View>

            <View style={styles.mapButtonContainer}>
              <Button
                mode="contained"
                icon="map-marker"
                onPress={() => handleShowOnMap(customer.latitude!, customer.longitude!)}
                style={styles.mapButton}
                contentStyle={styles.mapButtonContent}
              >
                Haritada Göster
              </Button>
              <Text style={styles.mapButtonHelper}>
                Google Maps'te açılacak
              </Text>
            </View>
          </Card.Content>
        </Card>
      )}

      {/* Hizmet Bilgileri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Hizmet Bilgileri</Text>
          <Divider style={styles.divider} />
          
          {customer.estimatedServiceTime && (
            <View style={styles.infoRow}>
              <Icon name="clock-outline" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Tahmini Hizmet Süresi</Text>
                <Text style={styles.infoValue}>{customer.estimatedServiceTime} dakika</Text>
              </View>
            </View>
          )}

          {customer.timeWindow && (
            <View style={styles.infoRow}>
              <Icon name="clock-time-eight" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Zaman Penceresi</Text>
                <Text style={styles.infoValue}>
                  {customer.timeWindow.start} - {customer.timeWindow.end}
                </Text>
              </View>
            </View>
          )}

          {customer.tags && customer.tags.length > 0 && (
            <View style={styles.infoRow}>
              <Icon name="tag-multiple" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Etiketler</Text>
                <View style={styles.tagsContainer}>
                  {customer.tags.map((tag, index) => (
                    <View key={index} style={styles.tag}>
                      <Text style={styles.tagText}>{tag}</Text>
                    </View>
                  ))}
                </View>
              </View>
            </View>
          )}

          {customer.notes && (
            <View style={styles.infoRow}>
              <Icon name="note-text" size={20} color="#3B82F6" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Notlar</Text>
                <Text style={styles.infoValue}>{customer.notes}</Text>
              </View>
            </View>
          )}
        </Card.Content>
      </Card>

      {/* Oluşturma Tarihi */}
      {customer.createdAt && (
        <Card style={styles.card}>
          <Card.Content>
            <View style={styles.infoRow}>
              <Icon name="calendar-plus" size={20} color="#6B7280" />
              <View style={styles.infoContent}>
                <Text style={styles.infoLabel}>Oluşturma Tarihi</Text>
                <Text style={styles.infoValue}>
                  {new Date(customer.createdAt).toLocaleDateString('tr-TR')}
                </Text>
              </View>
            </View>
          </Card.Content>
        </Card>
      )}
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F3F4F6',
  },
  contentContainer: {
    padding: wp(4),
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
    marginVertical: hp(2),
  },
  card: {
    marginBottom: hp(2),
    borderRadius: 12,
    elevation: 2,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
  },
  headerContent: {
    flex: 1,
  },
  customerName: {
    fontSize: isLargeScreen ? 20 : 22,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 4,
  },
  customerMeta: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  customerCode: {
    fontSize: 14,
    color: '#6B7280',
    marginLeft: 6,
    fontWeight: '500',
  },
  priorityBadge: {
    paddingHorizontal: wp(3),
    paddingVertical: hp(1),
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
  },
  priorityText: {
    fontSize: 12,
    fontWeight: '600',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: hp(1),
  },
  divider: {
    marginBottom: hp(1.5),
  },
  infoRow: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: hp(1.5),
  },
  infoContent: {
    flex: 1,
    marginLeft: 12,
  },
  infoLabel: {
    fontSize: 12,
    color: '#6B7280',
    marginBottom: 2,
    fontWeight: '500',
  },
  infoValue: {
    fontSize: 14,
    color: '#111827',
    lineHeight: 20,
  },
  coordinatesRow: {
    flexDirection: 'row',
    marginBottom: hp(2),
  },
  coordinateItem: {
    flex: 1,
    alignItems: 'center',
  },
  coordinateLabel: {
    fontSize: 12,
    color: '#6B7280',
    marginBottom: 4,
  },
  coordinateValue: {
    fontSize: 14,
    color: '#111827',
    fontWeight: '500',
  },
  mapButtonContainer: {
    alignItems: 'center',
    marginTop: hp(1),
  },
  mapButton: {
    backgroundColor: '#10B981',
    borderRadius: 8,
  },
  mapButtonContent: {
    paddingVertical: hp(0.5),
  },
  mapButtonHelper: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: hp(0.5),
    textAlign: 'center',
  },
  tagsContainer: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 6,
  },
  tag: {
    backgroundColor: '#EFF6FF',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#DBEAFE',
  },
  tagText: {
    fontSize: 12,
    color: '#1E40AF',
    fontWeight: '500',
  },
});

export default CustomerDetailScreen;
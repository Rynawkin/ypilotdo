import React, { useCallback, useEffect, useState } from 'react';
import {
  View,
  StyleSheet,
  FlatList,
  RefreshControl,
  Alert,
} from 'react-native';
import {
  Card,
  Text,
  Button,
  Switch,
  ActivityIndicator,
  Divider,
  Portal,
  Dialog,
  TextInput,
} from 'react-native-paper';
import { format } from 'date-fns';
import { tr } from 'date-fns/locale';
import journeyService from '../../services/journeyService';
import networkService from '../../services/networkService';

type LocationUpdateRequest = {
  id: number;
  journeyId: number;
  journeyName: string;
  customerId: number;
  customerName: string;
  currentLatitude: number;
  currentLongitude: number;
  currentAddress: string;
  requestedLatitude: number;
  requestedLongitude: number;
  requestedAddress: string;
  reason: string;
  requestedByName: string;
  createdAt: string;
};

const LocationUpdateRequestsScreen: React.FC = () => {
  const [requests, setRequests] = useState<LocationUpdateRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [updateFutureStops, setUpdateFutureStops] = useState<Record<number, boolean>>({});
  const [rejectDialogVisible, setRejectDialogVisible] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [selectedRequest, setSelectedRequest] = useState<LocationUpdateRequest | null>(null);

  const loadRequests = useCallback(async (showLoader = true) => {
    if (!networkService.getIsConnected()) {
      setLoading(false);
      setRefreshing(false);
      return;
    }

    try {
      if (showLoader) setLoading(true);
      const data = await journeyService.getPendingLocationUpdateRequests();
      setRequests(data);
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Talepler yüklenemedi.';
      Alert.alert('Hata', message);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    loadRequests(true);
  }, [loadRequests]);

  const onRefresh = () => {
    setRefreshing(true);
    loadRequests(false);
  };

  const getUpdateFuture = (id: number) => updateFutureStops[id] ?? true;

  const toggleUpdateFuture = (id: number) => {
    setUpdateFutureStops(prev => ({
      ...prev,
      [id]: !getUpdateFuture(id)
    }));
  };

  const handleApprove = async (request: LocationUpdateRequest) => {
    try {
      await journeyService.approveLocationUpdateRequest(request.id, getUpdateFuture(request.id));
      Alert.alert('Başarılı', 'Konum güncelleme talebi onaylandı.');
      loadRequests(false);
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Onay işlemi başarısız.';
      Alert.alert('Hata', message);
    }
  };

  const handleReject = (request: LocationUpdateRequest) => {
    setSelectedRequest(request);
    setRejectReason('');
    setRejectDialogVisible(true);
  };

  const confirmReject = async () => {
    if (!selectedRequest) return;
    if (!rejectReason.trim()) {
      Alert.alert('Eksik Bilgi', 'Lütfen red nedeni girin.');
      return;
    }

    try {
      await journeyService.rejectLocationUpdateRequest(selectedRequest.id, rejectReason.trim());
      Alert.alert('Başarılı', 'Talep reddedildi.');
      setRejectDialogVisible(false);
      loadRequests(false);
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Red işlemi başarısız.';
      Alert.alert('Hata', message);
    }
  };

  const renderRequest = ({ item }: { item: LocationUpdateRequest }) => (
    <Card style={styles.card}>
      <Card.Content>
        <View style={styles.headerRow}>
          <Text variant="titleMedium">{item.customerName || 'Müşteri'}</Text>
          <Text style={styles.dateText}>
            {format(new Date(item.createdAt), 'dd MMM HH:mm', { locale: tr })}
          </Text>
        </View>

        <Text style={styles.subText}>Sefer: {item.journeyName || `#${item.journeyId}`}</Text>
        <Text style={styles.subText}>Talep Eden: {item.requestedByName}</Text>

        <Divider style={styles.divider} />

        <Text style={styles.sectionTitle}>Mevcut Konum</Text>
        <Text style={styles.bodyText}>{item.currentAddress || 'Adres belirtilmemiş'}</Text>
        <Text style={styles.coordText}>
          {Number(item.currentLatitude).toFixed(6)}, {Number(item.currentLongitude).toFixed(6)}
        </Text>

        <Text style={styles.sectionTitle}>İstenen Konum</Text>
        <Text style={styles.bodyText}>{item.requestedAddress || 'Adres belirtilmemiş'}</Text>
        <Text style={styles.coordText}>
          {Number(item.requestedLatitude).toFixed(6)}, {Number(item.requestedLongitude).toFixed(6)}
        </Text>

        <Text style={styles.sectionTitle}>Neden</Text>
        <Text style={styles.bodyText}>{item.reason}</Text>

        <View style={styles.switchRow}>
          <Text style={styles.switchLabel}>Gelecek durakları da güncelle</Text>
          <Switch
            value={getUpdateFuture(item.id)}
            onValueChange={() => toggleUpdateFuture(item.id)}
          />
        </View>

        <View style={styles.actions}>
          <Button
            mode="contained"
            onPress={() => handleApprove(item)}
            buttonColor="#10B981"
          >
            Onayla
          </Button>
          <Button
            mode="outlined"
            onPress={() => handleReject(item)}
            textColor="#EF4444"
          >
            Reddet
          </Button>
        </View>
      </Card.Content>
    </Card>
  );

  if (!networkService.getIsConnected()) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyText}>İnternet bağlantısı gerekli.</Text>
      </View>
    );
  }

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Talepler yükleniyor...</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={requests}
        keyExtractor={(item) => item.id.toString()}
        renderItem={renderRequest}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        contentContainerStyle={requests.length === 0 ? styles.emptyList : styles.list}
        ListEmptyComponent={
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>Bekleyen konum güncelleme talebi yok.</Text>
          </View>
        }
      />

      <Portal>
        <Dialog visible={rejectDialogVisible} onDismiss={() => setRejectDialogVisible(false)}>
          <Dialog.Title>Talebi Reddet</Dialog.Title>
          <Dialog.Content>
            <TextInput
              mode="outlined"
              label="Red nedeni"
              value={rejectReason}
              onChangeText={setRejectReason}
              multiline
            />
          </Dialog.Content>
          <Dialog.Actions>
            <Button onPress={() => setRejectDialogVisible(false)}>İptal</Button>
            <Button onPress={confirmReject} textColor="#EF4444">
              Reddet
            </Button>
          </Dialog.Actions>
        </Dialog>
      </Portal>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  list: {
    padding: 16,
  },
  emptyList: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 16,
  },
  card: {
    marginBottom: 16,
    elevation: 2,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  dateText: {
    fontSize: 12,
    color: '#6B7280',
  },
  subText: {
    fontSize: 12,
    color: '#6B7280',
  },
  divider: {
    marginVertical: 12,
  },
  sectionTitle: {
    marginTop: 6,
    fontSize: 13,
    fontWeight: '600',
    color: '#374151',
  },
  bodyText: {
    fontSize: 12,
    color: '#374151',
  },
  coordText: {
    fontSize: 11,
    color: '#6B7280',
    marginBottom: 6,
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  switchLabel: {
    fontSize: 12,
    color: '#374151',
  },
  actions: {
    flexDirection: 'row',
    gap: 12,
    marginTop: 12,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 8,
    color: '#6B7280',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  emptyText: {
    color: '#6B7280',
  },
});

export default LocationUpdateRequestsScreen;

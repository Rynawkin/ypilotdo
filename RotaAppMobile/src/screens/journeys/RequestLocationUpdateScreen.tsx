// C:\Projects\RotaAppMobile\src\screens\journeys\RequestLocationUpdateScreen.tsx
import React, { useEffect, useState } from 'react';
import { View, Alert, StyleSheet, ScrollView } from 'react-native';
import { useNavigation, useRoute } from '@react-navigation/native';
import { Button, Card, Text, TextInput, HelperText } from 'react-native-paper';
import MapPicker from '../../components/MapPicker'; // MapPicker komponentiniz
import offlineQueueService from '../../services/offlineQueueService';
import networkService from '../../services/networkService';
import journeyService from '../../services/journeyService';
import permissionService from '../../services/permissionService';
import locationService from '../../services/locationService';

type RouteParams = {
  journeyId: number;
  stopId: number;
  customerId?: number;
  currentLatitude?: number;
  currentLongitude?: number;
  currentAddress?: string;
};

type PickedLocation = {
  latitude: number;
  longitude: number;
  address?: string;
};

const RequestLocationUpdateScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute();
  const {
    journeyId,
    stopId,
    customerId,
    currentLatitude,
    currentLongitude,
    currentAddress
  } = route.params as RouteParams;

  const [isOnline, setIsOnline] = useState<boolean>(true);
  const [initialCenter, setInitialCenter] = useState<{ latitude: number; longitude: number } | null>(null);
  const [selectedLocation, setSelectedLocation] = useState<PickedLocation | null>(null);
  const [reason, setReason] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);

  // Ağ durumunu takip et
  useEffect(() => {
    setIsOnline(networkService.getIsConnected());
    const h = (connected: boolean) => setIsOnline(connected);
    networkService.on('connectionChange', h);
    return () => networkService.removeListener('connectionChange', h);
  }, []);

  // Başlangıç konumunu gerçek koordinatla al
  useEffect(() => {
    (async () => {
      try {
        const has = await permissionService.checkLocationPermission();
        if (!has) {
          Alert.alert('İzin gerekli', 'Harita üzerinde konum seçmek için konum izni vermelisiniz.');
          return;
        }
        const pos = await locationService.getCurrentPreciseLocation();
        setInitialCenter({ latitude: pos.latitude, longitude: pos.longitude });
      } catch (e: any) {
        if (currentLatitude && currentLongitude) {
          setInitialCenter({ latitude: currentLatitude, longitude: currentLongitude });
        } else {
          Alert.alert('Konum alınamadı', e?.message || 'Lütfen konum izinlerini ve ayarlarını kontrol edin.');
        }
      }
    })();
  }, [currentLatitude, currentLongitude]);

  const handleSubmit = async () => {
    if (!selectedLocation || !reason.trim()) return;
    if (currentLatitude === undefined || currentLongitude === undefined) {
      Alert.alert('Eksik Bilgi', 'Mevcut konum bilgisi eksik olduğu için talep gönderilemiyor.');
      return;
    }

    setLoading(true);
    try {
      if (!isOnline) {
        await offlineQueueService.addToQueue({
          type: 'LOCATION_UPDATE_REQUEST',
          journeyId,
          stopId,
          data: {
            customerId,
            currentLatitude,
            currentLongitude,
            currentAddress: currentAddress || '',
            requestedLatitude: selectedLocation.latitude,
            requestedLongitude: selectedLocation.longitude,
            reason,
          },
        });
        Alert.alert('Talep Kaydedildi', 'Bağlantı sağlanınca otomatik gönderilecek.');
        navigation.goBack();
        return;
      }

      // Online gönderim
      await journeyService.createLocationUpdateRequest({
        journeyId,
        stopId,
        customerId: customerId ?? 0,
        currentLatitude,
        currentLongitude,
        currentAddress: currentAddress || '',
        requestedLatitude: selectedLocation.latitude,
        requestedLongitude: selectedLocation.longitude,
        reason,
      });

      Alert.alert('Başarılı', 'Konum güncelleme talebiniz yöneticinize iletildi.');
      navigation.goBack();
    } catch (error: any) {
      Alert.alert('Hata', error?.message || 'Talep gönderilemedi. Tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <ScrollView contentContainerStyle={styles.scroll}>
        <Card style={styles.card}>
          <Card.Content>
            <Text variant="titleMedium">Haritada Doğru Konumu Seçin</Text>
            <HelperText type="info" visible>
              {currentAddress ? `Mevcut adres: ${currentAddress}` : 'Adres bilgisi mevcut değil.'}
            </HelperText>
            {initialCenter && (
              <MapPicker
                initialLocation={initialCenter}
                onLocationSelect={(loc: PickedLocation) => setSelectedLocation(loc)}
              />
            )}
            {!initialCenter && (
              <HelperText type="error" visible>
                Konum alınamadı. Lütfen izinleri kontrol edip tekrar deneyin.
              </HelperText>
            )}
          </Card.Content>
        </Card>

        <Card style={styles.card}>
          <Card.Content>
            <Text variant="titleMedium">
              Güncelleme Nedeni <Text style={{ color: '#EF4444' }}>*</Text>
            </Text>
            <TextInput
              mode="outlined"
              value={reason}
              onChangeText={setReason}
              multiline
              numberOfLines={4}
              placeholder="Örn: Kayıtlı adres yanlış; doğru konum şu an bulunduğum yer."
              style={{ backgroundColor: 'white', marginTop: 8 }}
            />
            <HelperText type="info" visible>
              Onaylandığında müşterinin koordinat/adresi veritabanında güncellenecek.
            </HelperText>
          </Card.Content>
        </Card>

        <View style={styles.actions}>
          <Button mode="outlined" onPress={() => navigation.goBack()}>
            İptal
          </Button>
          <Button
            mode="contained"
            onPress={handleSubmit}
            loading={loading}
            disabled={!selectedLocation || !reason.trim() || loading}
          >
            Talep Gönder
          </Button>
        </View>
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f5f5f5' },
  scroll: { padding: 16 },
  card: { marginBottom: 16, elevation: 2 },
  actions: { flexDirection: 'row', gap: 12, paddingTop: 8 },
});

export default RequestLocationUpdateScreen;

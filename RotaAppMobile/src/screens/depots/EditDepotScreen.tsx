import React, { useEffect, useState } from 'react';
import { View, StyleSheet, ScrollView, Alert } from 'react-native';
import { Button, Card, Text, TextInput, Switch, HelperText, ActivityIndicator } from 'react-native-paper';
import { useNavigation, useRoute } from '@react-navigation/native';
import depotService, { Depot } from '../../services/depotService';
import locationService from '../../services/locationService';
import GooglePlacesComponent from '../../components/SimpleGooglePlaces';
import { isValidLatLng } from '../../utils/validators';

const dayOrder = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday'];

const EditDepotScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const depotId = route.params?.depotId as number;

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [depot, setDepot] = useState<Depot | null>(null);

  const [name, setName] = useState('');
  const [address, setAddress] = useState('');
  const [latitude, setLatitude] = useState('');
  const [longitude, setLongitude] = useState('');
  const [startTime, setStartTime] = useState('08:00');
  const [endTime, setEndTime] = useState('18:00');
  const [isDefault, setIsDefault] = useState(false);
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    loadDepot();
  }, [depotId]);

  const loadDepot = async () => {
    try {
      setLoading(true);
      const data = await depotService.getById(depotId);
      setDepot(data);
      setName(data.name || '');
      setAddress(data.address || '');
      setLatitude(data.latitude?.toString() || '');
      setLongitude(data.longitude?.toString() || '');
      setIsDefault(!!data.isDefault);

      const hours = data.workingHours || {};
      const defaultDay = dayOrder.find(day => hours[day]?.open && hours[day]?.open !== 'closed');
      if (defaultDay) {
        setStartTime(hours[defaultDay].open);
        setEndTime(hours[defaultDay].close);
      }
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Depo yüklenemedi.';
      Alert.alert('Hata', message);
      navigation.goBack();
    } finally {
      setLoading(false);
    }
  };

  const validateTime = (time: string) => /^([01]?[0-9]|2[0-3]):([0-5][0-9])$/.test(time);

  const validateForm = () => {
    const nextErrors: { [key: string]: string } = {};
    if (!name.trim()) nextErrors.name = 'Depo adı zorunludur';
    if (!address.trim()) nextErrors.address = 'Adres zorunludur';

    const lat = parseFloat(latitude);
    const lng = parseFloat(longitude);
    if (!isValidLatLng(lat, lng)) nextErrors.location = 'Geçerli koordinat girin';
    if (!validateTime(startTime) || !validateTime(endTime)) nextErrors.time = 'Saat formatı HH:MM olmalıdır';

    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleUseCurrentLocation = async () => {
    try {
      const location = await locationService.getCurrentLocation();
      setLatitude(location.latitude.toString());
      setLongitude(location.longitude.toString());
    } catch (error: any) {
      Alert.alert('Hata', error.userFriendlyMessage || error.message || 'Konum alınamadı.');
    }
  };

  const handleSave = async () => {
    if (!validateForm() || !depot) return;

    setSaving(true);
    try {
      await depotService.update(depot.id, {
        name: name.trim(),
        address: address.trim(),
        latitude: parseFloat(latitude),
        longitude: parseFloat(longitude),
        isDefault,
        startWorkingHours: startTime,
        endWorkingHours: endTime,
      });
      Alert.alert('Başarılı', 'Depo güncellendi.');
      navigation.goBack();
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Depo güncellenemedi.';
      Alert.alert('Hata', message);
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.loadingContainer}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Depo yükleniyor...</Text>
      </View>
    );
  }

  if (!depot) {
    return null;
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Card style={styles.card}>
        <Card.Content>
          <Text variant="titleMedium">Depo Bilgileri</Text>
          <TextInput
            label="Depo Adı"
            mode="outlined"
            value={name}
            onChangeText={setName}
            style={styles.input}
            error={!!errors.name}
          />
          <HelperText type="error" visible={!!errors.name}>
            {errors.name}
          </HelperText>

          <TextInput
            label="Adres"
            mode="outlined"
            value={address}
            onChangeText={setAddress}
            style={styles.input}
            error={!!errors.address}
          />
          <HelperText type="error" visible={!!errors.address}>
            {errors.address}
          </HelperText>

          <GooglePlacesComponent
            placeholder="Adres ara"
            onPlaceSelect={(place, details) => {
              const location = details?.geometry?.location;
              if (!location) return;
              setAddress(details?.formatted_address || place.description);
              setLatitude(location.lat.toString());
              setLongitude(location.lng.toString());
            }}
            style={styles.places}
          />

          <View style={styles.locationRow}>
            <TextInput
              label="Enlem"
              mode="outlined"
              value={latitude}
              onChangeText={setLatitude}
              keyboardType="numeric"
              style={[styles.input, styles.locationInput]}
            />
            <TextInput
              label="Boylam"
              mode="outlined"
              value={longitude}
              onChangeText={setLongitude}
              keyboardType="numeric"
              style={[styles.input, styles.locationInput]}
            />
          </View>
          <HelperText type="error" visible={!!errors.location}>
            {errors.location}
          </HelperText>

          <Button mode="outlined" onPress={handleUseCurrentLocation} icon="crosshairs-gps">
            Mevcut Konumu Kullan
          </Button>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <Text variant="titleMedium">Çalışma Saatleri</Text>
          <View style={styles.timeRow}>
            <TextInput
              label="Başlangıç"
              mode="outlined"
              value={startTime}
              onChangeText={setStartTime}
              style={[styles.input, styles.timeInput]}
              placeholder="08:00"
            />
            <TextInput
              label="Bitiş"
              mode="outlined"
              value={endTime}
              onChangeText={setEndTime}
              style={[styles.input, styles.timeInput]}
              placeholder="18:00"
            />
          </View>
          <HelperText type="error" visible={!!errors.time}>
            {errors.time}
          </HelperText>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content style={styles.switchRow}>
          <Text>Varsayılan Depo</Text>
          <Switch value={isDefault} onValueChange={setIsDefault} />
        </Card.Content>
      </Card>

      <Button
        mode="contained"
        onPress={handleSave}
        loading={saving}
        buttonColor="#3B82F6"
        style={styles.saveButton}
      >
        Kaydet
      </Button>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  content: {
    padding: 16,
    paddingBottom: 40,
  },
  card: {
    marginBottom: 16,
    elevation: 2,
  },
  input: {
    marginTop: 12,
    backgroundColor: 'white',
  },
  places: {
    marginTop: 12,
  },
  locationRow: {
    flexDirection: 'row',
    gap: 12,
  },
  locationInput: {
    flex: 1,
  },
  timeRow: {
    flexDirection: 'row',
    gap: 12,
  },
  timeInput: {
    flex: 1,
  },
  switchRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  saveButton: {
    marginTop: 8,
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
});

export default EditDepotScreen;

import React, { useCallback, useEffect, useState } from 'react';
import { View, StyleSheet, Image } from 'react-native';
import { Button, HelperText, Text, ActivityIndicator } from 'react-native-paper';
import GooglePlacesComponent from './SimpleGooglePlaces';
import locationService from '../services/locationService';
import { GOOGLE_MAPS_API_KEY } from '../config/google';

type PickedLocation = {
  latitude: number;
  longitude: number;
  address?: string;
};

interface MapPickerProps {
  initialLocation?: { latitude: number; longitude: number } | null;
  onLocationSelect: (location: PickedLocation) => void;
}

const buildStaticMapUrl = (latitude: number, longitude: number) => {
  const center = `${latitude},${longitude}`;
  const marker = `color:red|label:X|${center}`;
  return `https://maps.googleapis.com/maps/api/staticmap?center=${center}&zoom=16&size=640x320&scale=2&markers=${encodeURIComponent(marker)}&key=${GOOGLE_MAPS_API_KEY}`;
};

const reverseGeocode = async (latitude: number, longitude: number): Promise<string | undefined> => {
  try {
    const response = await fetch(
      `https://maps.googleapis.com/maps/api/geocode/json?latlng=${latitude},${longitude}&key=${GOOGLE_MAPS_API_KEY}&language=tr`
    );
    const data = await response.json();
    return data?.results?.[0]?.formatted_address;
  } catch (error) {
    console.warn('[MapPicker] Reverse geocode failed', error);
    return undefined;
  }
};

const MapPicker: React.FC<MapPickerProps> = ({ initialLocation, onLocationSelect }) => {
  const [selectedLocation, setSelectedLocation] = useState<PickedLocation | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!initialLocation || selectedLocation) return;
    const nextLocation = {
      latitude: initialLocation.latitude,
      longitude: initialLocation.longitude
    };
    setSelectedLocation(nextLocation);
    onLocationSelect(nextLocation);
  }, [initialLocation, onLocationSelect, selectedLocation]);

  const handleLocationSelect = useCallback(
    async (location: PickedLocation) => {
      setSelectedLocation(location);
      onLocationSelect(location);
    },
    [onLocationSelect]
  );

  const handleUseCurrentLocation = async () => {
    setLoading(true);
    try {
      const position = await locationService.getCurrentLocation();
      const address = await reverseGeocode(position.latitude, position.longitude);
      await handleLocationSelect({
        latitude: position.latitude,
        longitude: position.longitude,
        address
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      <GooglePlacesComponent
        placeholder="Adres veya işletme ara"
        onPlaceSelect={(place, details) => {
          const location = details?.geometry?.location;
          if (!location) return;
          handleLocationSelect({
            latitude: location.lat,
            longitude: location.lng,
            address: details?.formatted_address || place.description
          });
        }}
      />

      <Button
        mode="outlined"
        onPress={handleUseCurrentLocation}
        style={styles.useLocationButton}
        icon="crosshairs-gps"
        disabled={loading}
      >
        Mevcut Konumu Kullan
      </Button>

      {loading && (
        <View style={styles.loadingRow}>
          <ActivityIndicator size="small" />
          <Text style={styles.loadingText}>Konum alınıyor...</Text>
        </View>
      )}

      {selectedLocation ? (
        <View style={styles.previewCard}>
          <Image
            source={{ uri: buildStaticMapUrl(selectedLocation.latitude, selectedLocation.longitude) }}
            style={styles.mapImage}
            resizeMode="cover"
          />
          <Text style={styles.addressText} numberOfLines={2}>
            {selectedLocation.address || `${selectedLocation.latitude.toFixed(6)}, ${selectedLocation.longitude.toFixed(6)}`}
          </Text>
        </View>
      ) : (
        <HelperText type="info" visible>
          Haritada bir konum seçin veya mevcut konumu kullanın.
        </HelperText>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
  useLocationButton: {
    alignSelf: 'flex-start',
  },
  loadingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  loadingText: {
    fontSize: 12,
    color: '#666',
  },
  previewCard: {
    backgroundColor: 'white',
    borderRadius: 8,
    overflow: 'hidden',
    borderWidth: 1,
    borderColor: '#E5E7EB',
  },
  mapImage: {
    width: '100%',
    height: 180,
  },
  addressText: {
    padding: 8,
    fontSize: 12,
    color: '#374151',
  },
});

export default MapPicker;

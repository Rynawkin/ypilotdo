import React, { useState, useEffect } from 'react';
import {
  View,
  Image,
  StyleSheet,
  TouchableOpacity,
  Text,
  ActivityIndicator,
  Dimensions,
} from 'react-native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import journeyService from '../services/journeyService';

const { width } = Dimensions.get('window');

interface JourneyMapViewProps {
  journeyId: number;
  stopsVersion?: string;
  onPress?: () => void;
}

export const JourneyMapView: React.FC<JourneyMapViewProps> = ({ 
  journeyId, 
  stopsVersion,
  onPress 
}) => {
  const [mapUrl, setMapUrl] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    loadMap();
  }, [journeyId, stopsVersion]);

  const loadMap = async () => {
    try {
      setLoading(true);
      setError(false);
      const url = await journeyService.getStaticMapUrl(journeyId);
      setMapUrl(url);
    } catch (err) {
      setError(true);
      console.error('Map load error:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <View style={styles.container}>
        <ActivityIndicator size="large" color="#3B82F6" />
        <Text style={styles.loadingText}>Harita yükleniyor...</Text>
      </View>
    );
  }

  if (error || !mapUrl) {
    return (
      <TouchableOpacity style={styles.errorContainer} onPress={loadMap}>
        <Icon name="map-marker-off" size={40} color="#999" />
        <Text style={styles.errorText}>Harita yüklenemedi</Text>
        <Text style={styles.retryText}>Tekrar denemek için dokunun</Text>
      </TouchableOpacity>
    );
  }

  return (
    <TouchableOpacity 
      style={styles.mapContainer} 
      onPress={onPress}
      activeOpacity={0.9}
    >
      <Image
        source={{ uri: mapUrl }}
        style={styles.mapImage}
        resizeMode="cover"
        onError={() => setError(true)}
      />
      
      <View style={styles.overlay}>
        <View style={styles.legendContainer}>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: '#10B981' }]} />
            <Text style={styles.legendText}>Başlangıç</Text>
          </View>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: '#FFA500' }]} />
            <Text style={styles.legendText}>Sıradaki</Text>
          </View>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: '#3B82F6' }]} />
            <Text style={styles.legendText}>Tamamlandı</Text>
          </View>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: '#EF4444' }]} />
            <Text style={styles.legendText}>Başarısız</Text>
          </View>
          <View style={styles.legendItem}>
            <View style={[styles.legendDot, { backgroundColor: '#6B7280' }]} />
            <Text style={styles.legendText}>Bekliyor</Text>
          </View>
        </View>
      </View>
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  container: {
    height: 250,
    backgroundColor: '#f5f5f5',
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 8,
    marginVertical: 8,
  },
  mapContainer: {
    height: 250,
    borderRadius: 8,
    overflow: 'hidden',
    marginVertical: 8,
  },
  mapImage: {
    width: '100%',
    height: '100%',
  },
  overlay: {
    position: 'absolute',
    bottom: 8,
    left: 8,
    right: 8,
  },
  legendContainer: {
    flexDirection: 'row',
    backgroundColor: 'rgba(255, 255, 255, 0.9)',
    padding: 8,
    borderRadius: 6,
    flexWrap: 'wrap',
  },
  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: 12,
  },
  legendDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    marginRight: 4,
  },
  legendText: {
    fontSize: 11,
    color: '#333',
  },
  loadingText: {
    marginTop: 8,
    fontSize: 14,
    color: '#666',
  },
  errorContainer: {
    height: 250,
    backgroundColor: '#f5f5f5',
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 8,
    marginVertical: 8,
  },
  errorText: {
    marginTop: 8,
    fontSize: 14,
    color: '#666',
  },
  retryText: {
    marginTop: 4,
    fontSize: 12,
    color: '#3B82F6',
  },
});
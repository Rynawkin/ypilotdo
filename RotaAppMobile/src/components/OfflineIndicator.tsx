import React, { useState, useEffect } from 'react';
import { View, Text, StyleSheet, Animated } from 'react-native';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import networkService from '../services/networkService';
import offlineQueueService from '../services/offlineQueueService';

const OfflineIndicator: React.FC = () => {
  const [isOnline, setIsOnline] = useState(true);
  const [queueCount, setQueueCount] = useState(0);
  const [fadeAnim] = useState(new Animated.Value(0));
  const [pulseAnim] = useState(new Animated.Value(1));

  useEffect(() => {
    // Network durumunu dinle
    const handleConnectionChange = (connected: boolean) => {
      setIsOnline(connected);
      
      // Animasyon başlat
      Animated.timing(fadeAnim, {
        toValue: connected ? 0 : 1,
        duration: 300,
        useNativeDriver: true,
      }).start();
    };

    // Queue sayısını periyodik kontrol et
    const updateQueueCount = async () => {
      const count = await offlineQueueService.getQueueCount();
      setQueueCount(count);
      
      // Pulse animasyonu başlat (queue'da item varsa)
      if (count > 0) {
        Animated.loop(
          Animated.sequence([
            Animated.timing(pulseAnim, {
              toValue: 1.2,
              duration: 1000,
              useNativeDriver: true,
            }),
            Animated.timing(pulseAnim, {
              toValue: 1,
              duration: 1000,
              useNativeDriver: true,
            }),
          ])
        ).start();
      }
    };

    // Initial check
    setIsOnline(networkService.getIsConnected());
    updateQueueCount();

    // Listeners
    networkService.on('connectionChange', handleConnectionChange);
    const interval = setInterval(updateQueueCount, 5000);

    return () => {
      networkService.removeListener('connectionChange', handleConnectionChange);
      clearInterval(interval);
    };
  }, [fadeAnim, pulseAnim]);

  if (isOnline && queueCount === 0) {
    return null;
  }

  return (
    <Animated.View 
      style={[
        styles.container,
        {
          opacity: fadeAnim,
          backgroundColor: isOnline ? '#F59E0B' : '#EF4444',
        }
      ]}
    >
      <Icon 
        name={isOnline ? 'cloud-upload' : 'cloud-off-outline'} 
        size={16} 
        color="white" 
      />
      <Text style={styles.text}>
        {!isOnline 
          ? 'Çevrimdışı' 
          : `${queueCount} işlem bekliyor`}
      </Text>
      {isOnline && queueCount > 0 && (
        <Animated.View 
          style={[
            styles.syncIndicator,
            { transform: [{ scale: pulseAnim }] }
          ]}
        >
          <Icon name="sync" size={14} color="white" />
        </Animated.View>
      )}
    </Animated.View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 12,
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 1000,
  },
  text: {
    color: 'white',
    fontSize: 13,
    fontWeight: '600',
    marginLeft: 6,
  },
  syncIndicator: {
    marginLeft: 8,
  },
});

export default OfflineIndicator;
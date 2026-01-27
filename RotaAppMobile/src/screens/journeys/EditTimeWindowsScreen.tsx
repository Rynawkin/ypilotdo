import React, { useState } from 'react';
import {
  View,
  ScrollView,
  Text,
  TouchableOpacity,
  StyleSheet,
} from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';

const EditTimeWindowsScreen = ({ route, navigation }) => {
  const { journeyId, excludedStops } = route.params;
  const [timeWindows, setTimeWindows] = useState({});

  const handleSave = async () => {
    try {
      // Time window'ları güncelle
      for (const stopId in timeWindows) {
        await routeService.updateStopTimeWindow(stopId, timeWindows[stopId]);
      }
      
      // Tekrar optimize et
      await journeyService.optimizeForDeviation(journeyId, format(new Date(), 'HH:mm:ss'));
      
      Alert.alert('Başarılı', 'Time window\'lar güncellendi ve rota yeniden optimize edildi');
      navigation.goBack();
    } catch (error) {
      Alert.alert('Hata', 'Time window güncellenemedi');
    }
  };

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Time Window Düzenle</Text>
      
      {excludedStops.map((stop) => (
        <View key={stop.id} style={styles.stopCard}>
          <Text style={styles.stopName}>{stop.name}</Text>
          
          <View style={styles.timeRow}>
            <Text>Başlangıç:</Text>
            <TouchableOpacity 
              style={styles.timeButton}
              onPress={() => {/* Time picker aç */}}
            >
              <Text>{timeWindows[stop.id]?.start || stop.timeWindowStart || '09:00'}</Text>
            </TouchableOpacity>
          </View>
          
          <View style={styles.timeRow}>
            <Text>Bitiş:</Text>
            <TouchableOpacity 
              style={styles.timeButton}
              onPress={() => {/* Time picker aç */}}
            >
              <Text>{timeWindows[stop.id]?.end || stop.timeWindowEnd || '18:00'}</Text>
            </TouchableOpacity>
          </View>
        </View>
      ))}
      
      <TouchableOpacity style={styles.saveButton} onPress={handleSave}>
        <Text style={styles.saveButtonText}>Kaydet ve Yeniden Optimize Et</Text>
      </TouchableOpacity>
    </ScrollView>
  );
};
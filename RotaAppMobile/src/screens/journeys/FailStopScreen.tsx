// C:\Projects\RotaAppMobile\src\screens\journeys\FailStopScreen.tsx

import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Alert,
  TouchableOpacity,
} from 'react-native';
import { Button, TextInput, Card, RadioButton } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useRoute, useNavigation } from '@react-navigation/native';
import journeyService from '../../services/journeyService';

interface RouteParams {
  journeyId: number;
  stopId: number;
  stopName: string;
}

// Başarısızlık sebepleri
const FAILURE_REASONS = [
  { value: 'customer_not_available', label: 'Müşteri bulunamadı', icon: 'account-off' },
  { value: 'wrong_address', label: 'Yanlış adres', icon: 'map-marker-off' },
  { value: 'refused_delivery', label: 'Teslimat reddedildi', icon: 'cancel' },
  { value: 'damaged_goods', label: 'Ürün hasarlı', icon: 'package-variant-closed-remove' },
  { value: 'payment_issue', label: 'Ödeme problemi', icon: 'credit-card-off' },
  { value: 'time_constraint', label: 'Zaman yetersizliği', icon: 'clock-alert' },
  { value: 'vehicle_problem', label: 'Araç problemi', icon: 'truck-alert' },
  { value: 'weather_conditions', label: 'Hava koşulları', icon: 'weather-lightning-rainy' },
  { value: 'other', label: 'Diğer', icon: 'dots-horizontal' },
];

const FailStopScreen: React.FC = () => {
  const route = useRoute();
  const navigation = useNavigation<any>();
  const { journeyId, stopId, stopName } = route.params as RouteParams;

  const [selectedReason, setSelectedReason] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);

  // Başarısız işaretle
  const handleFailStop = async () => {
    if (!selectedReason) {
      Alert.alert('Uyarı', 'Lütfen bir başarısızlık sebebi seçin.');
      return;
    }

    Alert.alert(
      'Teslimat Başarısız',
      'Teslimatı başarısız olarak işaretlemek istediğinize emin misiniz?\n\nBu durağı daha sonra tekrar deneyebilirsiniz.',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Başarısız İşaretle',
          style: 'destructive',
          onPress: async () => {
            try {
              setLoading(true);
              
              // Seçilen sebep label'ını bul
              const reasonLabel = FAILURE_REASONS.find(r => r.value === selectedReason)?.label || selectedReason;
              
              await journeyService.failStop(journeyId, stopId, {
                FailureReason: reasonLabel,
                Notes: notes,
              });
              
              Alert.alert(
                'Kayıt Edildi',
                'Teslimat başarısız olarak kaydedildi. Bu durağı daha sonra tekrar deneyebilirsiniz.',
                [
                  {
                    text: 'Tamam',
                    onPress: () => {
                      // Önceki sayfaya dön (JourneyDetail)
                      navigation.goBack();
                    },
                  },
                ]
              );
            } catch (error: any) {
              console.error('Fail stop error:', error);
              Alert.alert(
                'Hata',
                error.response?.data?.message || 'İşlem sırasında bir hata oluştu.'
              );
            } finally {
              setLoading(false);
            }
          },
        },
      ]
    );
  };

  return (
    <ScrollView style={styles.container}>
      {/* Stop bilgileri */}
      <Card style={styles.infoCard}>
        <Card.Content>
          <Text style={styles.stopName}>{stopName}</Text>
          <Text style={styles.label}>Teslimat Durağı</Text>
        </Card.Content>
      </Card>

      {/* Başarısızlık sebepleri */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>
            Başarısızlık Sebebi <Text style={styles.required}>*</Text>
          </Text>
          
          <RadioButton.Group 
            onValueChange={value => setSelectedReason(value)} 
            value={selectedReason}
          >
            {FAILURE_REASONS.map((reason) => (
              <TouchableOpacity
                key={reason.value}
                style={[
                  styles.reasonItem,
                  selectedReason === reason.value && styles.reasonItemSelected
                ]}
                onPress={() => setSelectedReason(reason.value)}
              >
                <View style={styles.reasonContent}>
                  <Icon 
                    name={reason.icon} 
                    size={24} 
                    color={selectedReason === reason.value ? '#3B82F6' : '#666'} 
                  />
                  <Text style={[
                    styles.reasonLabel,
                    selectedReason === reason.value && styles.reasonLabelSelected
                  ]}>
                    {reason.label}
                  </Text>
                </View>
                <RadioButton 
                  value={reason.value}
                  color="#3B82F6"
                />
              </TouchableOpacity>
            ))}
          </RadioButton.Group>
        </Card.Content>
      </Card>

      {/* Ek notlar */}
      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Ek Açıklama</Text>
          <TextInput
            mode="outlined"
            label="Detaylı açıklama ekleyin (opsiyonel)"
            value={notes}
            onChangeText={setNotes}
            multiline
            numberOfLines={4}
            style={styles.input}
            placeholder="Örn: Müşteri 3 kez arandı ancak ulaşılamadı..."
          />
        </Card.Content>
      </Card>

      {/* Aksiyon butonları */}
      <View style={styles.actions}>
        <Button
          mode="outlined"
          onPress={() => navigation.goBack()}
          style={styles.cancelButton}
          disabled={loading}
        >
          İptal
        </Button>
        <Button
          mode="contained"
          onPress={handleFailStop}
          style={styles.failButton}
          buttonColor="#EF4444"
          loading={loading}
          disabled={loading || !selectedReason}
        >
          Başarısız İşaretle
        </Button>
      </View>

      {/* Bilgi notu - Güncellenmiş */}
      <Card style={styles.infoNote}>
        <Card.Content>
          <View style={styles.infoNoteContent}>
            <Icon name="information" size={20} color="#F59E0B" />
            <Text style={styles.infoNoteText}>
              Başarısız işaretlenen duraklar, sefer detay sayfasından "Tekrar Dene" butonuyla yeniden denenebilir. Müşteri ile iletişime geçilerek sorun çözüldükten sonra teslimat tamamlanabilir.
            </Text>
          </View>
        </Card.Content>
      </Card>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  infoCard: {
    margin: 16,
    marginBottom: 8,
    elevation: 2,
  },
  card: {
    marginHorizontal: 16,
    marginVertical: 8,
    elevation: 2,
  },
  stopName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 4,
  },
  label: {
    fontSize: 14,
    color: '#666',
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#333',
    marginBottom: 16,
  },
  required: {
    color: '#EF4444',
  },
  reasonItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 12,
    paddingHorizontal: 12,
    marginBottom: 8,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#e5e5e5',
    backgroundColor: 'white',
  },
  reasonItemSelected: {
    borderColor: '#3B82F6',
    backgroundColor: '#EFF6FF',
  },
  reasonContent: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  reasonLabel: {
    fontSize: 15,
    color: '#333',
    marginLeft: 12,
    flex: 1,
  },
  reasonLabelSelected: {
    color: '#3B82F6',
    fontWeight: '600',
  },
  input: {
    backgroundColor: 'white',
  },
  actions: {
    flexDirection: 'row',
    padding: 16,
    gap: 12,
  },
  cancelButton: {
    flex: 1,
  },
  failButton: {
    flex: 1,
  },
  infoNote: {
    marginHorizontal: 16,
    marginBottom: 24,
    backgroundColor: '#FEF3C7',
    elevation: 1,
  },
  infoNoteContent: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  infoNoteText: {
    fontSize: 13,
    color: '#92400E',
    marginLeft: 8,
    flex: 1,
    lineHeight: 18,
  },
});

export default FailStopScreen;
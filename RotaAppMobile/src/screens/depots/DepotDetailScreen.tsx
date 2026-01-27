import React, { useEffect, useState } from 'react';
import { View, StyleSheet, ScrollView, Alert, Linking } from 'react-native';
import { Card, Text, Button, Divider, ActivityIndicator } from 'react-native-paper';
import { useNavigation, useRoute } from '@react-navigation/native';
import depotService, { Depot } from '../../services/depotService';

const dayLabels: Record<string, string> = {
  monday: 'Pazartesi',
  tuesday: 'Salı',
  wednesday: 'Çarşamba',
  thursday: 'Perşembe',
  friday: 'Cuma',
  saturday: 'Cumartesi',
  sunday: 'Pazar',
};

const DepotDetailScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const route = useRoute<any>();
  const depotId = route.params?.depotId as number;

  const [depot, setDepot] = useState<Depot | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadDepot();
  }, [depotId]);

  const loadDepot = async () => {
    try {
      setLoading(true);
      const data = await depotService.getById(depotId);
      setDepot(data);
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Depo yüklenemedi.';
      Alert.alert('Hata', message);
      navigation.goBack();
    } finally {
      setLoading(false);
    }
  };

  const openInMaps = () => {
    if (!depot) return;
    const url = `https://www.google.com/maps?q=${depot.latitude},${depot.longitude}`;
    Linking.openURL(url).catch(() => Alert.alert('Hata', 'Harita açılamadı'));
  };

  const handleSetDefault = async () => {
    if (!depot) return;
    try {
      await depotService.setDefault(depot.id);
      Alert.alert('Başarılı', 'Varsayılan depo güncellendi.');
      loadDepot();
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Varsayılan ayarlanamadı.';
      Alert.alert('Hata', message);
    }
  };

  const handleDelete = () => {
    if (!depot) return;
    Alert.alert(
      'Depoyu Sil',
      'Bu depoyu silmek istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        {
          text: 'Sil',
          style: 'destructive',
          onPress: async () => {
            try {
              await depotService.delete(depot.id);
              Alert.alert('Başarılı', 'Depo silindi.');
              navigation.goBack();
            } catch (error: any) {
              const message = error.userFriendlyMessage || error.message || 'Depo silinemedi.';
              Alert.alert('Hata', message);
            }
          },
        },
      ]
    );
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
          <Text variant="titleLarge">{depot.name}</Text>
          <Text style={styles.addressText}>{depot.address}</Text>
          <Text style={styles.coordText}>
            {depot.latitude.toFixed(6)}, {depot.longitude.toFixed(6)}
          </Text>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <Text style={styles.sectionTitle}>Çalışma Saatleri</Text>
          <Divider style={styles.divider} />
          {depot.workingHours ? (
            Object.entries(depot.workingHours).map(([day, hours]) => (
              <View key={day} style={styles.hourRow}>
                <Text style={styles.hourLabel}>{dayLabels[day] || day}</Text>
                <Text style={styles.hourValue}>
                  {hours.open === 'closed' ? 'Kapalı' : `${hours.open} - ${hours.close}`}
                </Text>
              </View>
            ))
          ) : (
            <Text style={styles.bodyText}>Çalışma saatleri belirtilmemiş.</Text>
          )}
        </Card.Content>
      </Card>

      <View style={styles.actions}>
        <Button mode="outlined" onPress={openInMaps} icon="map">
          Haritada Göster
        </Button>
        <Button
          mode="contained"
          onPress={() => navigation.navigate('EditDepot', { depotId: depot.id })}
          buttonColor="#3B82F6"
          icon="pencil"
        >
          Düzenle
        </Button>
        {!depot.isDefault && (
          <Button mode="contained" onPress={handleSetDefault} buttonColor="#10B981" icon="check-circle">
            Varsayılan Yap
          </Button>
        )}
        <Button mode="outlined" onPress={handleDelete} textColor="#EF4444" icon="delete">
          Sil
        </Button>
      </View>
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
  },
  card: {
    marginBottom: 16,
    elevation: 2,
  },
  addressText: {
    marginTop: 6,
    color: '#374151',
  },
  coordText: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 4,
  },
  sectionTitle: {
    fontSize: 14,
    fontWeight: '600',
  },
  divider: {
    marginVertical: 8,
  },
  hourRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: 4,
  },
  hourLabel: {
    fontSize: 12,
    color: '#374151',
  },
  hourValue: {
    fontSize: 12,
    color: '#111827',
  },
  bodyText: {
    fontSize: 12,
    color: '#6B7280',
  },
  actions: {
    gap: 10,
    marginBottom: 24,
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

export default DepotDetailScreen;

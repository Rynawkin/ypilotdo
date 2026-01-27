import React, { useCallback, useEffect, useState } from 'react';
import {
  View,
  StyleSheet,
  FlatList,
  RefreshControl,
  TouchableOpacity,
  Alert,
} from 'react-native';
import { Card, Text, Searchbar, FAB, ActivityIndicator, Chip } from 'react-native-paper';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import depotService, { Depot } from '../../services/depotService';

const DepotsListScreen: React.FC = () => {
  const navigation = useNavigation<any>();
  const [depots, setDepots] = useState<Depot[]>([]);
  const [filteredDepots, setFilteredDepots] = useState<Depot[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  const loadDepots = useCallback(async (showLoader = true) => {
    try {
      if (showLoader) setLoading(true);
      const data = await depotService.getAll();
      setDepots(data);
      filterDepots(data, searchQuery);
    } catch (error: any) {
      const message = error.userFriendlyMessage || error.message || 'Depolar yüklenemedi.';
      Alert.alert('Hata', message);
      setDepots([]);
      setFilteredDepots([]);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, [searchQuery]);

  useEffect(() => {
    loadDepots(true);
  }, [loadDepots]);

  useFocusEffect(
    useCallback(() => {
      loadDepots(false);
    }, [loadDepots])
  );

  const onRefresh = () => {
    setRefreshing(true);
    loadDepots(false);
  };

  const filterDepots = (list: Depot[], query: string) => {
    if (!query.trim()) {
      setFilteredDepots(list);
      return;
    }
    const normalizedQuery = query.toLowerCase().trim();
    const filtered = list.filter(depot =>
      depot.name?.toLowerCase().includes(normalizedQuery) ||
      depot.address?.toLowerCase().includes(normalizedQuery)
    );
    setFilteredDepots(filtered);
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    filterDepots(depots, query);
  };

  const renderDepot = ({ item }: { item: Depot }) => (
    <TouchableOpacity
      onPress={() => navigation.navigate('DepotDetail', { depotId: item.id })}
      activeOpacity={0.8}
    >
      <Card style={styles.card}>
        <Card.Content>
          <View style={styles.cardHeader}>
            <Text variant="titleMedium">{item.name}</Text>
            {item.isDefault && (
              <Chip style={styles.defaultChip} textStyle={styles.defaultChipText}>
                Varsayılan
              </Chip>
            )}
          </View>
          <Text style={styles.addressText}>{item.address}</Text>
          <Text style={styles.coordText}>
            {item.latitude.toFixed(6)}, {item.longitude.toFixed(6)}
          </Text>
        </Card.Content>
      </Card>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <Searchbar
        placeholder="Depo ara"
        value={searchQuery}
        onChangeText={handleSearch}
        style={styles.searchbar}
      />

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#3B82F6" />
          <Text style={styles.loadingText}>Depolar yükleniyor...</Text>
        </View>
      ) : (
        <FlatList
          data={filteredDepots}
          keyExtractor={(item) => item.id.toString()}
          renderItem={renderDepot}
          refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
          contentContainerStyle={filteredDepots.length === 0 ? styles.emptyList : styles.list}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>Depo bulunamadı.</Text>
            </View>
          }
        />
      )}

      <FAB
        icon="plus"
        style={styles.fab}
        onPress={() => navigation.navigate('CreateDepot')}
        label="Yeni Depo"
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  searchbar: {
    margin: 16,
  },
  list: {
    paddingHorizontal: 16,
    paddingBottom: 100,
  },
  emptyList: {
    flexGrow: 1,
    justifyContent: 'center',
    paddingHorizontal: 16,
  },
  card: {
    marginBottom: 12,
    elevation: 2,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 6,
  },
  defaultChip: {
    backgroundColor: '#DBEAFE',
  },
  defaultChipText: {
    color: '#1D4ED8',
    fontSize: 11,
  },
  addressText: {
    color: '#374151',
    marginBottom: 4,
  },
  coordText: {
    fontSize: 11,
    color: '#6B7280',
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
    alignItems: 'center',
  },
  emptyText: {
    color: '#6B7280',
  },
  fab: {
    position: 'absolute',
    right: 16,
    bottom: 24,
    backgroundColor: '#3B82F6',
  },
});

export default DepotsListScreen;

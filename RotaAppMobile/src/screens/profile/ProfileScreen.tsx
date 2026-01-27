// C:\Projects\RotaAppMobile\src\screens\profile\ProfileScreen.tsx

import React, { useState } from 'react';
import { View, StyleSheet, ScrollView, Alert } from 'react-native';
import { 
  Button, 
  Text, 
  Card, 
  Avatar, 
  List, 
  Divider,
  ActivityIndicator,
  Chip,
  Surface
} from 'react-native-paper';
import { useAuth } from '../../contexts/AuthContext';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import AsyncStorage from '@react-native-async-storage/async-storage';

const ProfileScreen: React.FC = () => {
  const { user, logout, refreshUser } = useAuth();
  const [loggingOut, setLoggingOut] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const handleLogout = async () => {
    Alert.alert(
      'Çıkış Yap',
      'Çıkış yapmak istediğinizden emin misiniz?',
      [
        {
          text: 'İptal',
          style: 'cancel',
        },
        {
          text: 'Çıkış Yap',
          style: 'destructive',
          onPress: async () => {
            setLoggingOut(true);
            try {
              await logout();
            } catch (error) {
              Alert.alert('Hata', 'Çıkış yapılırken bir hata oluştu');
            } finally {
              setLoggingOut(false);
            }
          },
        },
      ],
    );
  };

  const handleRefresh = async () => {
    setRefreshing(true);
    try {
      await refreshUser();
      Alert.alert('Başarılı', 'Profil bilgileri güncellendi');
    } catch (error) {
      Alert.alert('Hata', 'Profil güncellenirken bir hata oluştu');
    } finally {
      setRefreshing(false);
    }
  };

  const clearAllData = async () => {
    Alert.alert(
      'Tüm Verileri Temizle',
      'Bu işlem tüm uygulama verilerini silecek. Emin misiniz?',
      [
        {
          text: 'İptal',
          style: 'cancel',
        },
        {
          text: 'Temizle',
          style: 'destructive',
          onPress: async () => {
            try {
              await AsyncStorage.clear();
              Alert.alert('Başarılı', 'Tüm veriler temizlendi. Uygulama yeniden başlatılıyor...');
              // Uygulamayı yeniden başlatmak için logout çağır
              await logout();
            } catch (error) {
              Alert.alert('Hata', 'Veriler temizlenirken bir hata oluştu');
            }
          },
        },
      ],
    );
  };

  const getRoleDisplay = () => {
    const roles = [];
    if (user?.isSuperAdmin) roles.push('Super Admin');
    if (user?.isAdmin) roles.push('Admin');
    if (user?.isDispatcher) roles.push('Dispatcher');
    if (user?.isDriver) roles.push('Driver');
    return roles.length > 0 ? roles.join(', ') : 'Kullanıcı';
  };

  const getRoleColor = () => {
    if (user?.isSuperAdmin) return '#d32f2f';
    if (user?.isAdmin) return '#f57c00';
    if (user?.isDispatcher) return '#388e3c';
    if (user?.isDriver) return '#1976d2';
    return '#757575';
  };

  if (!user) {
    return (
      <View style={styles.container}>
        <Text>Kullanıcı bilgisi yüklenemedi</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container}>
      <Surface style={styles.header} elevation={2}>
        <Avatar.Text 
          size={80} 
          label={user.fullName ? user.fullName.substring(0, 2).toUpperCase() : 'U'} 
          style={styles.avatar}
        />
        <Text variant="headlineSmall" style={styles.name}>
          {user.fullName || 'İsimsiz Kullanıcı'}
        </Text>
        <Text variant="bodyMedium" style={styles.email}>
          {user.email}
        </Text>
        <View style={styles.roleContainer}>
          <Chip 
            icon="shield-account" 
            style={[styles.roleChip, { backgroundColor: getRoleColor() }]}
            textStyle={{ color: 'white' }}
          >
            {getRoleDisplay()}
          </Chip>
        </View>
      </Surface>

      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Hesap Bilgileri</List.Subheader>
            
            <List.Item
              title="Kullanıcı ID"
              description={user.id}
              left={props => <List.Icon {...props} icon="identifier" />}
            />
            
            <List.Item
              title="Workspace"
              description={user.workspaceName || `Workspace #${user.workspaceId}`}
              left={props => <List.Icon {...props} icon="domain" />}
            />
            
            {user.phoneNumber && (
              <List.Item
                title="Telefon"
                description={user.phoneNumber}
                left={props => <List.Icon {...props} icon="phone" />}
              />
            )}
            
            {user.depotId && (
              <List.Item
                title="Depo ID"
                description={`#${user.depotId}`}
                left={props => <List.Icon {...props} icon="warehouse" />}
              />
            )}
            
            <List.Item
              title="Onboarding Durumu"
              description={user.isOnboarded ? 'Tamamlandı' : 'Bekliyor'}
              left={props => <List.Icon {...props} icon="check-circle" />}
              right={() => (
                <Icon 
                  name={user.isOnboarded ? 'check-circle' : 'clock-outline'} 
                  size={24} 
                  color={user.isOnboarded ? '#4caf50' : '#ff9800'}
                />
              )}
            />
          </List.Section>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <List.Section>
            <List.Subheader>Roller ve Yetkiler</List.Subheader>
            
            <List.Item
              title="Super Admin"
              description="Sistem yöneticisi"
              left={props => <List.Icon {...props} icon="shield-crown" />}
              right={() => (
                <Icon 
                  name={user.isSuperAdmin ? 'check' : 'close'} 
                  size={24} 
                  color={user.isSuperAdmin ? '#4caf50' : '#bdbdbd'}
                />
              )}
            />
            
            <List.Item
              title="Admin"
              description="Workspace yöneticisi"
              left={props => <List.Icon {...props} icon="shield-account" />}
              right={() => (
                <Icon 
                  name={user.isAdmin ? 'check' : 'close'} 
                  size={24} 
                  color={user.isAdmin ? '#4caf50' : '#bdbdbd'}
                />
              )}
            />
            
            <List.Item
              title="Dispatcher"
              description="Operasyon yöneticisi"
              left={props => <List.Icon {...props} icon="account-cog" />}
              right={() => (
                <Icon 
                  name={user.isDispatcher ? 'check' : 'close'} 
                  size={24} 
                  color={user.isDispatcher ? '#4caf50' : '#bdbdbd'}
                />
              )}
            />
            
            <List.Item
              title="Driver"
              description="Sürücü"
              left={props => <List.Icon {...props} icon="truck" />}
              right={() => (
                <Icon 
                  name={user.isDriver ? 'check' : 'close'} 
                  size={24} 
                  color={user.isDriver ? '#4caf50' : '#bdbdbd'}
                />
              )}
            />
          </List.Section>
        </Card.Content>
      </Card>

      <View style={styles.buttonContainer}>
        <Button
          mode="outlined"
          onPress={handleRefresh}
          loading={refreshing}
          disabled={refreshing}
          style={styles.button}
          icon="refresh"
        >
          Bilgileri Güncelle
        </Button>

        <Button
          mode="outlined"
          onPress={clearAllData}
          style={[styles.button, styles.clearButton]}
          icon="delete-sweep"
          textColor="#ff5252"
        >
          Tüm Verileri Temizle
        </Button>

        <Button
          mode="contained"
          onPress={handleLogout}
          loading={loggingOut}
          disabled={loggingOut}
          style={[styles.button, styles.logoutButton]}
          icon="logout"
        >
          Çıkış Yap
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
  header: {
    alignItems: 'center',
    paddingVertical: 30,
    backgroundColor: 'white',
    marginBottom: 10,
  },
  avatar: {
    backgroundColor: '#6200ee',
    marginBottom: 10,
  },
  name: {
    fontWeight: 'bold',
    marginBottom: 5,
  },
  email: {
    color: '#666',
    marginBottom: 10,
  },
  roleContainer: {
    marginTop: 10,
  },
  roleChip: {
    backgroundColor: '#6200ee',
  },
  card: {
    margin: 10,
    marginBottom: 5,
  },
  buttonContainer: {
    padding: 20,
    paddingBottom: 30,
  },
  button: {
    marginVertical: 5,
  },
  clearButton: {
    borderColor: '#ff5252',
  },
  logoutButton: {
    backgroundColor: '#d32f2f',
    marginTop: 10,
  },
});

export default ProfileScreen;
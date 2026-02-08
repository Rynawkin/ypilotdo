// C:\Projects\RotaAppMobile\src\navigation\MainNavigator.tsx

import React, { useState, useEffect } from 'react';
import { View, TouchableOpacity, StyleSheet, Alert, Modal, Text, Pressable, Dimensions, Platform, StatusBar } from 'react-native';
import { BottomTabBar, createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Badge } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialCommunityIcons';
import { useAuth } from '../contexts/AuthContext';
import { useNavigation } from '@react-navigation/native';
import OfflineIndicator from '../components/OfflineIndicator';
import notificationService from '../services/notificationService';
import { SafeAreaView, useSafeAreaInsets } from 'react-native-safe-area-context';

// Screens
import DashboardScreen from '../screens/dashboard/DashboardScreen';
import JourneysScreen from '../screens/journeys/JourneysScreen';
import JourneyDetailScreen from '../screens/journeys/JourneyDetailScreen';
import CompleteStopScreen from '../screens/journeys/CompleteStopScreen';
import CompleteJourneyScreen from '../screens/journeys/CompleteJourneyScreen';
import FailStopScreen from '../screens/journeys/FailStopScreen';
import RequestLocationUpdateScreen from '../screens/journeys/RequestLocationUpdateScreen';
import LocationUpdateRequestsScreen from '../screens/journeys/LocationUpdateRequestsScreen';
import PerformanceScreen from '../screens/performance/PerformanceScreen';
import ProfileScreen from '../screens/profile/ProfileScreen';
import SettingsScreen from '../screens/settings/SettingsScreen';
import PrivacyPolicyScreen from '../screens/settings/PrivacyPolicyScreen';
import TermsOfServiceScreen from '../screens/settings/TermsOfServiceScreen';
import ReportIssueScreen from '../screens/settings/ReportIssueScreen';
import HelpScreen from '../screens/settings/HelpScreen';
import NotificationsScreen from '../screens/notifications/NotificationsScreen';

// Routes Screens (YENİ)
import RoutesListScreen from '../screens/routes/RoutesListScreen';
import CreateRouteScreen from '../screens/routes/CreateRouteScreen';
import AddStopsToRouteScreen from '../screens/routes/AddStopsToRouteScreen';
import RouteDetailScreen from '../screens/routes/RouteDetailScreen';

// Customers Screens
import CustomersListScreen from '../screens/customers/CustomersListScreen';
import CustomerDetailScreen from '../screens/customers/CustomerDetailScreen';
import CreateCustomerScreen from '../screens/customers/CreateCustomerScreen';
import EditCustomerScreen from '../screens/customers/EditCustomerScreen';

// Vehicles Screens
import VehiclesListScreen from '../screens/vehicles/VehiclesListScreen';
import VehicleDetailScreen from '../screens/vehicles/VehicleDetailScreen';
import CreateVehicleScreen from '../screens/vehicles/CreateVehicleScreen';
import EditVehicleScreen from '../screens/vehicles/EditVehicleScreen';

// Drivers Screens
import DriversListScreen from '../screens/drivers/DriversListScreen';
import DriverDetailScreen from '../screens/drivers/DriverDetailScreen';
import CreateDriverScreen from '../screens/drivers/CreateDriverScreen';
import EditDriverScreen from '../screens/drivers/EditDriverScreen';

// Depots Screens
import DepotsListScreen from '../screens/depots/DepotsListScreen';
import DepotDetailScreen from '../screens/depots/DepotDetailScreen';
import CreateDepotScreen from '../screens/depots/CreateDepotScreen';
import EditDepotScreen from '../screens/depots/EditDepotScreen';

const Tab = createBottomTabNavigator();
const Stack = createNativeStackNavigator();

const AppTabBar = (props: any) => (
  <SafeAreaView edges={['bottom']} style={styles.tabBarSafeArea}>
    <BottomTabBar {...props} />
  </SafeAreaView>
);

// Header Component - Modal ile dropdown
const HeaderRight = () => {
  const navigation = useNavigation<any>();
  const { user, logout } = useAuth();
  const [modalVisible, setModalVisible] = useState(false);
  const [badgeCount, setBadgeCount] = useState(0);

  useEffect(() => {
    const loadBadgeCount = async () => {
      const count = await notificationService.getUnreadCount();
      setBadgeCount(count);
    };
    
    // İlk yükleme
    loadBadgeCount();
    
    // Her 30 saniyede bir kontrol et
    const interval = setInterval(loadBadgeCount, 30000);
    
    // Navigation listener - Bildirimler sayfasından dönünce güncelle
    const unsubscribe = navigation.addListener('focus', () => {
      loadBadgeCount();
    });
    
    return () => {
      clearInterval(interval);
      unsubscribe();
    };
  }, [navigation]);

  const handleNotifications = () => {
    navigation.navigate('NotificationsStack');
  };

  const handleProfileNavigation = () => {
    setModalVisible(false);
    navigation.navigate('ProfileStack');
  };

  const handleSettings = () => {
    setModalVisible(false);
    navigation.navigate('SettingsStack');
  };

  const handleHelp = () => {
    setModalVisible(false);
    navigation.navigate('SettingsStack', { screen: 'Help' });
  };

  const handleLogout = async () => {
    setModalVisible(false);
    Alert.alert(
      'Çıkış Yap',
      'Çıkış yapmak istediğinizden emin misiniz?',
      [
        { text: 'İptal', style: 'cancel' },
        { 
          text: 'Çıkış Yap', 
          style: 'destructive',
          onPress: async () => {
            try {
              await logout();
            } catch (error) {
              Alert.alert('Hata', 'Çıkış yapılırken bir hata oluştu');
            }
          }
        }
      ]
    );
  };

  return (
    <View style={styles.headerRight}>
      {/* Bildirimler */}
      <TouchableOpacity onPress={handleNotifications} style={styles.notificationButton}>
        <Icon name="bell-outline" size={24} color="#fff" />
        {badgeCount > 0 && (
          <Badge style={styles.badge}>{badgeCount > 99 ? '99+' : badgeCount}</Badge>
        )}
      </TouchableOpacity>

      {/* Profil İkonu */}
      <TouchableOpacity
        onPress={() => setModalVisible(true)}
        style={styles.profileButton}
      >
        <Icon name="account-circle" size={28} color="#fff" />
      </TouchableOpacity>

      {/* Dropdown Modal */}
      <Modal
        animationType="fade"
        transparent={true}
        visible={modalVisible}
        onRequestClose={() => setModalVisible(false)}
      >
        <Pressable 
          style={styles.modalOverlay}
          onPress={() => setModalVisible(false)}
        >
          <View style={styles.modalContent}>
            <TouchableOpacity 
              style={styles.menuItem}
              onPress={handleProfileNavigation}
            >
              <Icon name="account" size={20} color="#333" />
              <Text style={styles.menuText}>Profil</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={styles.menuItem}
              onPress={handleSettings}
            >
              <Icon name="cog" size={20} color="#333" />
              <Text style={styles.menuText}>Ayarlar</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={styles.menuItem}
              onPress={handleHelp}
            >
              <Icon name="help-circle" size={20} color="#333" />
              <Text style={styles.menuText}>Yardım</Text>
            </TouchableOpacity>

            <View style={styles.divider} />

            <TouchableOpacity 
              style={styles.menuItem}
              onPress={handleLogout}
            >
              <Icon name="logout" size={20} color="#EF4444" />
              <Text style={[styles.menuText, { color: '#EF4444' }]}>Çıkış Yap</Text>
            </TouchableOpacity>
          </View>
        </Pressable>
      </Modal>
    </View>
  );
};

// Journey Stack Navigator
const JourneyStackNavigator = () => {
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="JourneysList" 
        component={JourneysScreen}
        options={{ 
          title: 'Seferlerim',
          headerShown: false
        }}
      />
      <Stack.Screen 
        name="JourneyDetail" 
        component={JourneyDetailScreen}
        options={{ 
          title: 'Sefer Detayı',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="CompleteStop" 
        component={CompleteStopScreen}
        options={{ 
          title: 'Durağı Tamamla',
          presentation: 'modal'
        }}
      />
      <Stack.Screen
        name="FailStop"
        component={FailStopScreen}
        options={{
          title: 'Teslimat Başarısız',
          presentation: 'modal'
        }}
      />
      <Stack.Screen
        name="RequestLocationUpdate"
        component={RequestLocationUpdateScreen}
        options={{
          title: 'Konum Güncelleme Talebi',
          presentation: 'modal'
        }}
      />
      <Stack.Screen
        name="CompleteJourney"
        component={CompleteJourneyScreen}
        options={{
          title: 'Seferi Tamamla',
          presentation: 'modal'
        }}
      />
    </Stack.Navigator>
  );
};

// Routes Stack Navigator (YENİ)
const RoutesStackNavigator = () => {
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="RoutesList" 
        component={RoutesListScreen}
        options={{ 
          title: 'Rotalar',
          headerShown: false
        }}
      />
      <Stack.Screen 
        name="CreateRoute" 
        component={CreateRouteScreen}
        options={{ 
          title: 'Yeni Rota',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="AddStopsToRoute" 
        component={AddStopsToRouteScreen}
        options={{ 
          title: 'Müşteri Ekle',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="RouteDetail" 
        component={RouteDetailScreen}
        options={{ 
          title: 'Rota Detayı',
          headerBackTitle: 'Geri'
        }}
      />
    </Stack.Navigator>
  );
};

// Customers Stack Navigator
const CustomersStackNavigator = () => {
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="CustomersList" 
        component={CustomersListScreen}
        options={{ 
          title: 'Müşteriler',
          headerShown: false
        }}
      />
      <Stack.Screen 
        name="CustomerDetail" 
        component={CustomerDetailScreen}
        options={{ 
          title: 'Müşteri Detayı',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="CreateCustomer" 
        component={CreateCustomerScreen}
        options={{ 
          title: 'Yeni Müşteri',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="EditCustomer" 
        component={EditCustomerScreen}
        options={{ 
          title: 'Müşteri Düzenle',
          headerBackTitle: 'Geri'
        }}
      />
    </Stack.Navigator>
  );
};

// Vehicles Stack Navigator
const VehiclesStackNavigator = () => {
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="VehiclesList" 
        component={VehiclesListScreen}
        options={{ 
          title: 'Araçlar',
          headerShown: false
        }}
      />
      <Stack.Screen 
        name="VehicleDetail" 
        component={VehicleDetailScreen}
        options={{ 
          title: 'Araç Detayı',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="CreateVehicle" 
        component={CreateVehicleScreen}
        options={{ 
          title: 'Yeni Araç',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="EditVehicle" 
        component={EditVehicleScreen}
        options={{ 
          title: 'Araç Düzenle',
          headerBackTitle: 'Geri'
        }}
      />
    </Stack.Navigator>
  );
};

// Drivers Stack Navigator
const DriversStackNavigator = () => {
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="DriversList" 
        component={DriversListScreen}
        options={{ 
          title: 'Sürücüler',
          headerShown: false
        }}
      />
      <Stack.Screen 
        name="DriverDetail" 
        component={DriverDetailScreen}
        options={{ 
          title: 'Sürücü Detayı',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="CreateDriver" 
        component={CreateDriverScreen}
        options={{ 
          title: 'Yeni Sürücü',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="EditDriver" 
        component={EditDriverScreen}
        options={{ 
          title: 'Sürücü Düzenle',
          headerBackTitle: 'Geri'
        }}
      />
    </Stack.Navigator>
  );
};

// Profile Stack Navigator
const ProfileStackNavigator = () => {
  const navigation = useNavigation<any>();
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="ProfileMain" 
        component={ProfileScreen}
        options={{ 
          title: 'Profil',
          headerLeft: () => (
            <TouchableOpacity
              onPress={() => navigation.goBack()}
              style={{ marginLeft: 10 }}
            >
              <Icon name="arrow-left" size={24} color="#fff" />
            </TouchableOpacity>
          )
        }}
      />
    </Stack.Navigator>
  );
};

// Notifications Stack Navigator
const NotificationsStackNavigator = () => {
  const navigation = useNavigation<any>();
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="NotificationsMain" 
        component={NotificationsScreen}
        options={{ 
          title: 'Bildirimler',
          headerLeft: () => (
            <TouchableOpacity
              onPress={() => navigation.goBack()}
              style={{ marginLeft: 10 }}
            >
              <Icon name="arrow-left" size={24} color="#fff" />
            </TouchableOpacity>
          )
        }}
      />
    </Stack.Navigator>
  );
};

// Settings Stack Navigator
const SettingsStackNavigator = () => {
  const navigation = useNavigation<any>();
  return (
    <Stack.Navigator
      screenOptions={{
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
      }}
    >
      <Stack.Screen 
        name="SettingsMain" 
        component={SettingsScreen}
        options={{ 
          title: 'Ayarlar',
          headerLeft: () => (
            <TouchableOpacity
              onPress={() => navigation.goBack()}
              style={{ marginLeft: 10 }}
            >
              <Icon name="arrow-left" size={24} color="#fff" />
            </TouchableOpacity>
          )
        }}
      />
      <Stack.Screen 
        name="PrivacyPolicy" 
        component={PrivacyPolicyScreen}
        options={{ 
          title: 'Gizlilik Politikası',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="Help" 
        component={HelpScreen}
        options={{ 
          title: 'Yardım ve Destek',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="TermsOfService" 
        component={TermsOfServiceScreen}
        options={{ 
          title: 'Kullanım Koşulları',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen 
        name="ReportIssue" 
        component={ReportIssueScreen}
        options={{ 
          title: 'Sorun Bildir',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen
        name="DepotsList"
        component={DepotsListScreen}
        options={{
          title: 'Depolar',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen
        name="DepotDetail"
        component={DepotDetailScreen}
        options={{
          title: 'Depo Detayı',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen
        name="CreateDepot"
        component={CreateDepotScreen}
        options={{
          title: 'Yeni Depo',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen
        name="EditDepot"
        component={EditDepotScreen}
        options={{
          title: 'Depo Düzenle',
          headerBackTitle: 'Geri'
        }}
      />
      <Stack.Screen
        name="LocationUpdateRequests"
        component={LocationUpdateRequestsScreen}
        options={{
          title: 'Konum Güncelleme Talepleri',
          headerBackTitle: 'Geri'
        }}
      />
    </Stack.Navigator>
  );
};

// Ana Stack Navigator
const RootStack = createNativeStackNavigator();

const MainNavigator: React.FC = () => {
  return (
    <>
      {/* Offline Indicator - Tüm ekranlarda görünür */}
      <OfflineIndicator />
      
      <RootStack.Navigator screenOptions={{ headerShown: false }}>
        <RootStack.Screen name="MainTabs" component={TabNavigator} />
        <RootStack.Screen name="ProfileStack" component={ProfileStackNavigator} />
        <RootStack.Screen name="SettingsStack" component={SettingsStackNavigator} />
        <RootStack.Screen name="NotificationsStack" component={NotificationsStackNavigator} />
      </RootStack.Navigator>
    </>
  );
};

// Tab Navigator Component (GÜNCELLEME)
const TabNavigator = () => {
  const { isDispatcher } = useAuth(); // Auth context'ten rol kontrolü
  const insets = useSafeAreaInsets(); // YENİ
  const screenHeight = Dimensions.get('screen').height;
  const windowHeight = Dimensions.get('window').height;
  const statusBarHeight = Platform.OS === 'android' ? (StatusBar.currentHeight || 0) : 0;
  const androidNavBarHeight = Platform.OS === 'android'
    ? Math.max(0, screenHeight - windowHeight - statusBarHeight)
    : 0;
  const androidFallbackInset = Platform.OS === 'android' ? 48 : 0;
  const rawBottomInset = Math.max(insets.bottom, androidNavBarHeight, androidFallbackInset);
  const extraBottomInset = Math.max(0, rawBottomInset - insets.bottom);

  return (
    <Tab.Navigator
      tabBar={(props) => <AppTabBar {...props} />}
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName;
          switch (route.name) {
            case 'Dashboard':
              iconName = focused ? 'home' : 'home-outline';
              break;
            case 'Routes':
              iconName = 'routes';
              break;
            case 'Customers':
              iconName = focused ? 'account-group' : 'account-group-outline';
              break;
            case 'Vehicles':
              iconName = focused ? 'car-multiple' : 'car-outline';
              break;
            case 'Drivers':
              iconName = focused ? 'account-multiple' : 'account-multiple-outline';
              break;
            case 'Journeys':
              iconName = focused ? 'truck-delivery' : 'truck-delivery-outline';
              break;
            case 'Performance':
              iconName = focused ? 'chart-line' : 'chart-line-variant';
              break;
            default:
              iconName = 'circle';
          }
          return <Icon name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: '#3B82F6',
        tabBarInactiveTintColor: 'gray',
        tabBarSafeAreaInsets: { bottom: 0 },
        tabBarStyle: {
          paddingVertical: 5,
          height: 60 + rawBottomInset,
          paddingBottom: extraBottomInset, // YENİ
        },
        tabBarLabelStyle: {
          fontSize: 12,
          marginBottom: 3,
        },
        headerStyle: {
          backgroundColor: '#3B82F6',
        },
        headerTintColor: '#fff',
        headerTitleStyle: {
          fontWeight: 'bold',
        },
        headerRight: () => <HeaderRight />,
      })}
    >
      <Tab.Screen 
        name="Dashboard" 
        component={DashboardScreen}
        options={{ 
          title: 'Ana Sayfa',
          tabBarLabel: 'Ana',
        }}
      />
      
      {/* Admin/Dispatcher için Rotalar sekmesi */}
      {isDispatcher && (
        <Tab.Screen 
          name="Routes" 
          component={RoutesStackNavigator}
          options={{ 
            title: 'Rotalar',
            tabBarLabel: 'Rotalar',
            headerShown: true,
          }}
        />
      )}

      {/* Admin/Dispatcher için Müşteriler sekmesi */}
      {isDispatcher && (
        <Tab.Screen 
          name="Customers" 
          component={CustomersStackNavigator}
          options={{ 
            title: 'Müşteriler',
            tabBarLabel: 'Müşteriler',
            headerShown: true,
          }}
        />
      )}

      {/* Admin/Dispatcher için Araçlar sekmesi */}
      {isDispatcher && (
        <Tab.Screen 
          name="Vehicles" 
          component={VehiclesStackNavigator}
          options={{ 
            title: 'Araçlar',
            tabBarLabel: 'Araçlar',
            headerShown: true,
          }}
        />
      )}

      {/* Admin/Dispatcher için Sürücüler sekmesi */}
      {isDispatcher && (
        <Tab.Screen 
          name="Drivers" 
          component={DriversStackNavigator}
          options={{ 
            title: 'Sürücüler',
            tabBarLabel: 'Sürücüler',
            headerShown: true,
          }}
        />
      )}
      
      <Tab.Screen 
        name="Journeys" 
        component={JourneyStackNavigator}
        options={{ 
          title: 'Seferler',
          tabBarLabel: 'Seferler',
          headerShown: true,
        }}
      />
      
      <Tab.Screen 
        name="Performance" 
        component={PerformanceScreen}
        options={{ 
          title: 'Performans',
          tabBarLabel: 'Performans',
        }}
      />
    </Tab.Navigator>
  );
};

const styles = StyleSheet.create({
  headerRight: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: 12,
  },
  tabBarSafeArea: {
    backgroundColor: '#fff',
  },
  notificationButton: {
    padding: 8,
    position: 'relative',
  },
  profileButton: {
    padding: 8,
    marginLeft: 4,
  },
  badge: {
    position: 'absolute',
    top: 4,
    right: 4,
    backgroundColor: '#EF4444',
    fontSize: 10,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'flex-start',
    alignItems: 'flex-end',
  },
  modalContent: {
    backgroundColor: 'white',
    marginTop: 60,
    marginRight: 10,
    borderRadius: 8,
    padding: 8,
    minWidth: 180,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 4,
    elevation: 5,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
  menuText: {
    marginLeft: 12,
    fontSize: 16,
    color: '#333',
  },
  divider: {
    height: 1,
    backgroundColor: '#E5E7EB',
    marginVertical: 4,
  },
});

export default MainNavigator;

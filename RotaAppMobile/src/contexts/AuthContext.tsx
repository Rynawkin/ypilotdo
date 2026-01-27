// C:\Projects\RotaAppMobile\src\contexts\AuthContext.tsx

import React, { createContext, useState, useContext, useEffect, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import authService from '../services/authService';
import { User, LoginRequest, RegisterRequest } from '../types/auth.types';
import api from '../services/api';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isDispatcher: boolean;
  isDriver: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    checkAuthStatus();
  }, []);

  const checkAuthStatus = async () => {
    try {
      console.log('Checking auth status...');
      
      // ESKİ TOKEN'LARI TEMİZLE (Migration)
      const oldAuthToken = await AsyncStorage.getItem('@auth_token');
      const oldRefreshToken = await AsyncStorage.getItem('@refresh_token');
      
      if (oldAuthToken || oldRefreshToken) {
        console.log('Migrating from old token system...');
        await AsyncStorage.multiRemove(['@auth_token', '@refresh_token']);
      }
      
      // SADECE bearerToken'ı kontrol et
      const token = await AsyncStorage.getItem('bearerToken');
      console.log('Token found:', !!token);
      
      if (token) {
        const currentUser = await authService.getCurrentUser();
        console.log('Current user from storage:', currentUser);
        
        if (currentUser) {
          try {
            // Backend /auth/me endpoint'i kullan
            const response = await api.get('/auth/me');
            console.log('Backend /auth/me response:', response.data);
            setUser(response.data);
            
            await checkFirstLaunchForUser(response.data.id);
          } catch (error: any) {
            console.log('Backend check failed, using local user');
            setUser(currentUser);
            
            if (currentUser.id) {
              await checkFirstLaunchForUser(currentUser.id);
            }
          }
        } else {
          console.log('No user data, clearing token');
          await authService.logout();
        }
      }
    } catch (error: any) {
      console.error('Auth status check error:', error);
      await authService.logout();
    } finally {
      setLoading(false);
    }
  };

  const checkFirstLaunchForUser = async (userId: string) => {
    try {
      const key = `hasLaunched_${userId}`;
      const hasLaunched = await AsyncStorage.getItem(key);
      
      if (!hasLaunched) {
        console.log(`İlk açılış tespit edildi kullanıcı için: ${userId}`);
        
        await AsyncStorage.setItem(key, 'true');
        
        try {
          const response = await api.get('/workspace/journeys');
          const knownJourneys: any = {};
          
          if (response.data && Array.isArray(response.data)) {
            response.data.forEach((journey: any) => {
              knownJourneys[journey.id] = {
                id: journey.id,
                status: journey.status
              };
            });
            
            await AsyncStorage.setItem('knownJourneys', JSON.stringify(knownJourneys));
            console.log(`İlk açılışta ${Object.keys(knownJourneys).length} journey kaydedildi`);
          }
        } catch (error: any) {
          console.log('İlk journey yükleme hatası:', error);
        }
      } else {
        console.log(`Kullanıcı ${userId} daha önce giriş yapmış`);
      }
    } catch (error: any) {
      console.error('First launch check error:', error);
    }
  };

  const login = async (data: LoginRequest) => {
    console.log('Login attempt with:', data.email);
    
    // LOGIN'DE ESKİ TOKEN'LARI TEMİZLE!
    console.log('Cleaning up old tokens before login...');
    await AsyncStorage.multiRemove([
      '@auth_token', 
      '@refresh_token',
      '@user',
      'user'
    ]);
    
    const response = await authService.login(data);
    console.log('Login successful, response:', response);
    
    // authService'ten dönen response'ta me field'ı var
    setUser(response.me);
    
    if (response.me && response.me.id) {
      await checkFirstLaunchForUser(response.me.id);
    }
  };

  const register = async (data: RegisterRequest) => {
    // REGISTER'DA DA ESKİ TOKEN'LARI TEMİZLE!
    console.log('Cleaning up old tokens before register...');
    await AsyncStorage.multiRemove([
      '@auth_token', 
      '@refresh_token',
      '@user',
      'user'
    ]);
    
    const response = await authService.register(data);
    console.log('Register successful, response:', response);
    
    setUser(response.me);
    
    if (response.me && response.me.id) {
      await checkFirstLaunchForUser(response.me.id);
    }
  };

  const logout = async () => {
    console.log('Logging out...');
    // Logout'ta TÜM token'ları temizle
    await AsyncStorage.multiRemove([
      'bearerToken',
      '@auth_token', 
      '@refresh_token',
      '@user',
      'user'
    ]);
    await authService.logout();
    setUser(null);
    console.log('Logout complete');
  };

  const refreshUser = async () => {
    const updatedUser = await authService.refreshUserData();
    if (updatedUser) {
      setUser(updatedUser);
    }
  };

  // Backend'den gelen boolean field'ları direkt kullan
  const isAdmin = user?.isAdmin || false;
  const isDispatcher = user?.isDispatcher || user?.isAdmin || false; // Admin aynı zamanda dispatcher yetkilerine sahip
  const isDriver = user?.isDriver || false;

  // Debug için
  useEffect(() => {
    if (user) {
      console.log('Current user state:', {
        id: user.id,
        email: user.email,
        fullName: user.fullName,
        isAdmin: user.isAdmin,
        isDispatcher: user.isDispatcher,
        isDriver: user.isDriver,
        calculatedRoles: {
          isAdmin,
          isDispatcher,
          isDriver
        }
      });
    }
  }, [user, isAdmin, isDispatcher, isDriver]);

  return (
    <AuthContext.Provider 
      value={{
        user,
        loading,
        login,
        register,
        logout,
        refreshUser,
        isAuthenticated: !!user,
        isAdmin,
        isDispatcher,
        isDriver
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
// C:\Projects\RotaAppMobile\src\services\authService.ts

import AsyncStorage from '@react-native-async-storage/async-storage';
import api from './api';
import { LoginRequest, LoginResponse, RegisterRequest, User } from '../types/auth.types';

class AuthService {
  private readonly TOKEN_KEY = 'bearerToken';
  private readonly USER_KEY = 'user';

  async login(data: LoginRequest): Promise<LoginResponse> {
    try {
      // Backend'deki AuthController /auth/login endpoint'ini kullan
      const response = await api.post('/auth/login', data);
      
      // Backend'den gelen response formatı:
      // { success: true, token: "...", user: {...} }
      
      // Token ve user bilgilerini kaydet
      await AsyncStorage.setItem(this.TOKEN_KEY, response.data.token);
      await AsyncStorage.setItem(this.USER_KEY, JSON.stringify(response.data.user));
      
      // API interceptor'ını güncelle
      api.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
      
      // Frontend'in beklediği formata dönüştür
      return {
        success: response.data.success,
        bearerToken: response.data.token,
        me: response.data.user
      };
    } catch (error: any) {
      const message = error.response?.data?.message || 
                     error.response?.data?.errors?.[0] || 
                     'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.';
      throw new Error(message);
    }
  }

  async register(data: RegisterRequest): Promise<LoginResponse> {
    try {
      // Backend'deki AuthController /auth/register endpoint'ini kullan
      const response = await api.post('/auth/register', data);
      
      // Token ve user bilgilerini kaydet
      await AsyncStorage.setItem(this.TOKEN_KEY, response.data.token);
      await AsyncStorage.setItem(this.USER_KEY, JSON.stringify(response.data.user));
      
      // API interceptor'ını güncelle
      api.defaults.headers.common['Authorization'] = `Bearer ${response.data.token}`;
      
      // Frontend'in beklediği formata dönüştür
      return {
        success: response.data.success,
        bearerToken: response.data.token,
        me: response.data.user
      };
    } catch (error: any) {
      const message = error.response?.data?.message || 
                     error.response?.data?.errors?.[0] || 
                     'Kayıt başarısız. Lütfen tekrar deneyin.';
      throw new Error(message);
    }
  }

  async logout(): Promise<void> {
    try {
      // Backend'e logout isteği gönder
      await api.post('/auth/logout');
    } catch (error) {
      // Logout hatası olsa bile local temizlik yapılmalı
      console.log('Logout API error:', error);
    }
    
    // Local storage'ı temizle
    await AsyncStorage.multiRemove([this.TOKEN_KEY, this.USER_KEY]);
    
    // API header'ını temizle
    delete api.defaults.headers.common['Authorization'];
  }

  async getCurrentUser(): Promise<User | null> {
    try {
      const userStr = await AsyncStorage.getItem(this.USER_KEY);
      return userStr ? JSON.parse(userStr) : null;
    } catch (error) {
      console.error('Get current user error:', error);
      return null;
    }
  }

  async getToken(): Promise<string | null> {
    return await AsyncStorage.getItem(this.TOKEN_KEY);
  }

  async setToken(token: string): Promise<void> {
    await AsyncStorage.setItem(this.TOKEN_KEY, token);
    api.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  async setUser(user: User): Promise<void> {
    await AsyncStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  async isLoggedIn(): Promise<boolean> {
    const token = await AsyncStorage.getItem(this.TOKEN_KEY);
    return !!token;
  }

  async refreshUserData(): Promise<User | null> {
    try {
      // Backend'deki /auth/me endpoint'ini kullan
      const response = await api.get<User>('/auth/me');
      await AsyncStorage.setItem(this.USER_KEY, JSON.stringify(response.data));
      return response.data;
    } catch (error) {
      console.error('Refresh user data error:', error);
      return null;
    }
  }

  // Rol kontrol metodları - User type'ındaki boolean field'ları kullan
  isDriver(user: User): boolean {
    return user.isDriver || false;
  }

  isDispatcher(user: User): boolean {
    return user.isDispatcher || user.isAdmin || false;
  }

  isAdmin(user: User): boolean {
    return user.isAdmin || false;
  }
}

export default new AuthService();
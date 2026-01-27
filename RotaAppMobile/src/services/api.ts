// src/services/api.ts

import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

const BASE_URL = 'https://api.yolpilot.com/api';

const api = axios.create({
  baseURL: BASE_URL,
  timeout: 60000,
  headers: { 'Content-Type': 'application/json' },
});

// Basit JWT parse (RN/Hermes'te çalışır)
function parseJwt(token: string) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

// === REQUEST INTERCEPTOR ===
// Sadece 'bearerToken' kullan. Legacy anahtarları ASLA okuma.
api.interceptors.request.use(
  async (config) => {
    const token = await AsyncStorage.getItem('bearerToken');

    if (token) {
      config.headers = config.headers ?? {};
      (config.headers as any).Authorization = `Bearer ${token}`;

      // (Geçici guardrail) WorkspaceId'yi header'a da bas
      const payload = parseJwt(token);
      const wsid = payload?.WorkspaceId ?? payload?.workspaceId;
      if (wsid) {
        (config.headers as any)['X-Workspace-Id'] = String(wsid);
      }

      // Debug: hangi token gidiyor?
      // console.log('[AUTH] sending token prefix:', token.slice(0, 20));
      // console.log('[AUTH] iss:', payload?.iss, 'WorkspaceId:', wsid);
    }

    // console.log('API Request:', config.method?.toUpperCase(), config.url);
    return config;
  },
  (error) => Promise.reject(error)
);

// === RESPONSE INTERCEPTOR ===
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest: any = error.config || {};

    // 401 yakala → refresh dene (yalnızca yeni anahtarlarla)
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = await AsyncStorage.getItem('refreshToken');
        if (!refreshToken) throw new Error('No refreshToken');

        // Refresh çağrısı 'api' yerine çıplak axios ile; expired auth header taşımayalım
        const refreshResp = await axios.post(
          `${BASE_URL}/auth/refresh`,
          { refreshToken },
          { headers: { 'Content-Type': 'application/json' }, timeout: 10000 }
        );

        const newToken: string | undefined = refreshResp.data?.token;
        const newRefresh: string | undefined = refreshResp.data?.refreshToken;

        if (!newToken) throw new Error('Missing token in refresh response');

        // Sadece yeni anahtarları güncelle
        await AsyncStorage.setItem('bearerToken', newToken);
        if (newRefresh) await AsyncStorage.setItem('refreshToken', newRefresh);

        // Varsayılan header’ı güncelle ve isteği yeniden gönder
        api.defaults.headers.common.Authorization = `Bearer ${newToken}`;
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${newToken}`;

        return api(originalRequest);
      } catch (e) {
        // Refresh de başarısızsa her şeyi temizle (legacy anahtarlar dahil)
        await AsyncStorage.multiRemove([
          'bearerToken',
          'refreshToken',
          'user',
          '@auth_token',
          '@refresh_token',
          '@user',
        ]);
      }
    }

    // Kullanıcı dostu hata mesajlarını ekle
    let userFriendlyMessage = '';
    
    if (error.response?.data?.message) {
      // Backend'den mesaj geliyorsa kullan
      userFriendlyMessage = error.response.data.message;
    } else if (typeof error.response?.data === 'string') {
      userFriendlyMessage = error.response.data;
    } else {
      // Status code'a göre fallback mesajlar
      switch (error.response?.status) {
        case 400:
          userFriendlyMessage = 'Geçersiz istek. Lütfen bilgileri kontrol edin.';
          break;
        case 401:
          userFriendlyMessage = 'Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.';
          break;
        case 403:
          userFriendlyMessage = 'Bu işlem için yetkiniz yok.';
          break;
        case 404:
          userFriendlyMessage = 'Aradığınız kayıt bulunamadı.';
          break;
        case 408:
          userFriendlyMessage = 'İstek zaman aşımına uğradı. Lütfen tekrar deneyin.';
          break;
        case 500:
          userFriendlyMessage = 'Bir hata oluştu. Lütfen daha sonra tekrar deneyin.';
          break;
        default:
          userFriendlyMessage = 'Bir hata oluştu. Lütfen tekrar deneyin.';
      }
    }
    
    // Error nesnesine kullanıcı dostu mesajı ekle
    error.userFriendlyMessage = userFriendlyMessage;
    
    console.log('[API]', error.response?.status, 'Error - Backend message:', error.response?.data?.message);
    console.log('[API]', error.response?.status, 'Error - User friendly message:', userFriendlyMessage);

    // Network bağlantı hataları için ayrı mesaj
    if (!error.response && error.code === 'NETWORK_ERROR') {
      error.userFriendlyMessage = 'İnternet bağlantınızı kontrol edin ve tekrar deneyin.';
    } else if (!error.response && (error.code === 'ECONNABORTED' || error.message?.includes('timeout'))) {
      error.userFriendlyMessage = 'İstek zaman aşımına uğradı. Lütfen tekrar deneyin.';
    }

    return Promise.reject(error);
  }
);

export default api;

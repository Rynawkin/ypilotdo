// C:\Projects\RotaAppMobile\src\services\journeyService.ts

import api from './api';
import offlineQueueService from './offlineQueueService';
import networkService from './networkService';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  JourneyResponse,
  JourneySummaryResponse,
  JourneyListParams,
  FailStopRequest,
  BulkOperationResult,
  JourneyStatusResponse,
  CheckInResponse,
  DelayReasonCategory
} from '../types/journey.types';
import { format, startOfDay, endOfDay, addHours } from 'date-fns';

class JourneyService {
  private readonly JOURNEY_CACHE_PREFIX = '@journey_cache_';
  private readonly JOURNEYS_LIST_CACHE = '@journeys_list_cache';
  private readonly DEFAULT_TIMEOUT = 30000; // 30 saniye timeout
  private readonly MAX_RETRIES = 2; // 3'ten 2'ye düşürüldü
  
  // API config helper - TIMEOUT EKLENDI
  private getApiConfig(customTimeout?: number) {
    return {
      timeout: customTimeout || this.DEFAULT_TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
      }
    };
  }

  // Retry logic for failed requests - DÜZELTİLDİ
  private async retryRequest<T>(
    operation: () => Promise<T>,
    retries: number = this.MAX_RETRIES
  ): Promise<T> {
    try {
      return await operation();
    } catch (error: any) {
      if (retries > 0 && (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK')) {
        console.log(`[JourneyService] Retrying request... (${this.MAX_RETRIES - retries + 1}/${this.MAX_RETRIES})`);
        await new Promise(resolve => setTimeout(resolve, 1000)); // 1 saniye bekle
        return this.retryRequest(operation, retries - 1);
      }
      throw error;
    }
  }
  
  // Journey'yi cache'e kaydet
  private async cacheJourney(journey: JourneyResponse): Promise<void> {
    try {
      await AsyncStorage.setItem(
        `${this.JOURNEY_CACHE_PREFIX}${journey.id}`,
        JSON.stringify(journey)
      );
    } catch (error) {
      console.error('[JourneyService] Cache journey error:', error);
    }
  }

  // Cache'den journey getir
  private async getCachedJourney(journeyId: number): Promise<JourneyResponse | null> {
    try {
      const cached = await AsyncStorage.getItem(`${this.JOURNEY_CACHE_PREFIX}${journeyId}`);
      return cached ? JSON.parse(cached) : null;
    } catch (error) {
      console.error('[JourneyService] Get cached journey error:', error);
      return null;
    }
  }

  // Journey listesini cache'e kaydet - BOYUT KONTROLÜ DÜZELTİLDİ
  private async cacheJourneysList(journeys: JourneyResponse[]): Promise<void> {
    try {
      // Büyük veri setleri için sadece son 100 journey'yi cache'le (50'den artırıldı)
      const limitedJourneys = journeys.slice(0, 100);
      
      await AsyncStorage.setItem(this.JOURNEYS_LIST_CACHE, JSON.stringify(limitedJourneys));
      
      // Her journey'yi ayrı ayrı da cache'le (ilk 30 tane - 20'den artırıldı)
      const journeysToCache = limitedJourneys.slice(0, 30);
      for (const journey of journeysToCache) {
        await this.cacheJourney(journey);
      }
    } catch (error) {
      console.error('[JourneyService] Cache journeys list error:', error);
      // Cache hatası uygulamayı durdurmamalı
    }
  }

  // Cache'den journey listesi getir
  private async getCachedJourneysList(): Promise<JourneyResponse[]> {
    try {
      const cached = await AsyncStorage.getItem(this.JOURNEYS_LIST_CACHE);
      return cached ? JSON.parse(cached) : [];
    } catch (error) {
      console.error('[JourneyService] Get cached journeys list error:', error);
      return [];
    }
  }

  // Offline mode kontrolü - LOCATION_UPDATE_REQUEST TİPİ EKLENDİ
  private async executeWithOfflineSupport<T>(
    operation: () => Promise<T>,
    offlineData?: {
      type: 'CHECK_IN' | 'COMPLETE' | 'FAIL' | 'RESET' | 'START_JOURNEY' | 'FINISH_JOURNEY' | 'LOCATION_UPDATE_REQUEST';
      journeyId: number;
      stopId?: number;
      data?: any;
    }
  ): Promise<T> {
    const isOnline = networkService.getIsConnected();
    
    console.log('[JourneyService] Network status:', isOnline);
    console.log('[JourneyService] Attempting operation:', offlineData?.type);
    
    if (isOnline) {
      try {
        console.log('[JourneyService] Trying online operation...');
        const result = await this.retryRequest(operation);
        console.log('[JourneyService] Online operation successful');
        return result;
      } catch (error: any) {
        console.log('[JourneyService] Online operation failed:', error.message);
        
        const isNetworkError = 
          error.code === 'ECONNABORTED' || 
          error.code === 'ERR_NETWORK' || 
          error.code === 'NETWORK_ERROR' ||
          error.message?.includes('Network Error') ||
          error.message?.includes('timeout') ||
          !error.response;
        
        if (isNetworkError && offlineData) {
          console.log('[JourneyService] Network error detected, adding to offline queue');
          await offlineQueueService.addToQueue(offlineData);
          return { success: true, offline: true } as any;
        }
        
        // Check for user-friendly error message from API interceptor
        if (error.userFriendlyMessage) {
          const enhancedError = new Error(error.userFriendlyMessage);
          enhancedError.originalError = error;
          throw enhancedError;
        }
        
        throw error;
      }
    } else {
      console.log('[JourneyService] Offline mode, adding to queue directly');
      
      if (offlineData) {
        await offlineQueueService.addToQueue(offlineData);
        console.log('[JourneyService] Added to offline queue successfully');
        return { success: true, offline: true } as any;
      }
      
      throw new Error('Çevrimdışı moddasınız. Bu işlem internet bağlantısı gerektiriyor.');
    }
  }

  // Journey listesi getir - BÜYÜK EKRAN SORUNU İÇİN DÜZELTİLDİ
  async getJourneys(params?: JourneyListParams & { page?: number; limit?: number }, options?: any): Promise<JourneyResponse[]> {
    // Debug log
    console.log('[JourneyService] getJourneys called with params:', params);
    
    // Offline durumda cache'den dön
    if (!networkService.getIsConnected()) {
      console.log('[JourneyService] Offline - returning cached journeys');
      const cached = await this.getCachedJourneysList();
      return cached;
    }
    
    try {
      const queryParams = new URLSearchParams();
      
      // Sayfalama parametreleri - DÜZELTİLDİ
      const page = params?.page || 1;
      const limit = params?.limit || 20; // Varsayılan 20 kayıt
      
      // Sayfalama parametrelerini string olarak ekle
      queryParams.append('page', String(page));
      queryParams.append('limit', String(limit));
      
      // Tarih parametreleri
      if (params?.from || params?.to) {
        const now = new Date();
        const turkeyTime = addHours(now, 3);
        
        if (params.from) {
          const fromDate = new Date(params.from);
          const fromStart = startOfDay(fromDate);
          const fromUTC = addHours(fromStart, -3);
          queryParams.append('from', fromUTC.toISOString());
        }
        
        if (params.to) {
          const toDate = new Date(params.to);
          const toEnd = endOfDay(toDate);
          const toUTC = addHours(toEnd, -3);
          queryParams.append('to', toUTC.toISOString());
        }
      }
      
      // Diğer parametreler
      if (params?.status) queryParams.append('status', params.status);
      if (params?.driverId) queryParams.append('driverId', String(params.driverId));
      if (params?.vehicleId) queryParams.append('vehicleId', String(params.vehicleId));
      
      const queryString = queryParams.toString();
      const url = `/workspace/journeys${queryString ? `?${queryString}` : ''}`;
      
      console.log('[JourneyService] Request URL:', url);
      
      // OPTIONS parametresini kullan (signal için)
      const config = {
        ...this.getApiConfig(),
        ...options
      };
      
      // API çağrısı - DÜZELTİLDİ
      const response = await api.get<JourneyResponse[]>(url, config);
      
      // Data kontrolü
      const data = Array.isArray(response.data) ? response.data : [];
      
      console.log(`[JourneyService] Loaded ${data.length} journeys`);
      
      // Cache'e kaydet (boyut kontrolü ile)
      if (data.length <= 100) {
        await this.cacheJourneysList(data);
      }
      
      return data;
    } catch (error: any) {
      console.error('[JourneyService] Get journeys error:', error?.message || error);
      console.error('[JourneyService] Error details:', {
        code: error?.code,
        response: error?.response?.status,
        data: error?.response?.data
      });
      
      // Timeout veya network hatası durumunda cache'den dön
      if (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK') {
        console.log('[JourneyService] Network error, returning cached data');
        const cached = await this.getCachedJourneysList();
        if (cached.length > 0) {
          return cached;
        }
      }
      
      // API 500 hatası veya diğer hatalar için boş array dön
      console.log('[JourneyService] Returning empty array due to error');
      return [];
    }
  }

  // Tek journey detayı getir
  async getJourney(journeyId: number): Promise<JourneyResponse> {
    // Önce cache'den kontrol et
    const cached = await this.getCachedJourney(journeyId);
    
    // Offline durumda cache varsa dön
    if (!networkService.getIsConnected()) {
      if (cached) {
        console.log('[JourneyService] Offline - returning cached journey');
        return cached;
      }
      throw new Error('İnternet bağlantısı yok ve bu sefer için önbellek verisi bulunamadı.');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.get<JourneyResponse>(`/workspace/journeys/${journeyId}`, this.getApiConfig())
      );
      
      // Cache'e kaydet
      await this.cacheJourney(response.data);
      
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Get journey error:', error);
      
      // Hata durumunda cache varsa dön
      if (cached && (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK')) {
        console.log('[JourneyService] Network error, returning cached journey');
        return cached;
      }
      
      throw error;
    }
  }

  // Route'dan journey oluştur (AssignRoute) - NAME PARAMETRESİ EKLENDİ
  async createFromRoute(routeId: number, driverId: number, name?: string): Promise<any> {
    try {
      console.log('Creating journey from route:', { routeId, driverId, name });
      
      const response = await this.retryRequest(() =>
        api.post('/workspace/journeys/assignment', {
          routeId,
          driverId,
          name // Sefer adı
        }, this.getApiConfig())
      );
      
      console.log('Journey created:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('Error creating journey from route:', error);
      
      // Daha anlaşılır hata mesajı
      if (error.code === 'ECONNABORTED') {
        throw new Error('İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  // YENİ: Konum güncelleme talebi oluştur
  async createLocationUpdateRequest(data: {
    journeyId: number;
    stopId: number;
    customerId: number;
    currentLatitude: number;
    currentLongitude: number;
    currentAddress: string;
    requestedLatitude: number;
    requestedLongitude: number;
    reason: string;
  }): Promise<any> {
    // Offline desteği ile çalışır
    return this.executeWithOfflineSupport(
      async () => {
        const response = await api.post('/workspace/location-update-requests', data, this.getApiConfig());
        return response.data;
      },
      {
        type: 'LOCATION_UPDATE_REQUEST',
        journeyId: data.journeyId,
        stopId: data.stopId,
        data
      }
    );
  }

  // YENİ: Bekleyen konum güncelleme taleplerini getir
  async getPendingLocationUpdateRequests(): Promise<any[]> {
    if (!networkService.getIsConnected()) {
      return [];
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.get('/workspace/location-update-requests/pending', this.getApiConfig())
      );
      return response.data;
    } catch (error) {
      console.error('[JourneyService] Get pending location update requests error:', error);
      return [];
    }
  }

  // YENİ: Konum güncelleme talebini onayla
  async approveLocationUpdateRequest(requestId: number, updateFutureStops: boolean = true): Promise<boolean> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post(`/workspace/location-update-requests/${requestId}/approve`, 
          { updateFutureStops }, 
          this.getApiConfig()
        )
      );
      return response.data.success;
    } catch (error) {
      console.error('[JourneyService] Approve location update request error:', error);
      throw error;
    }
  }

  // YENİ: Konum güncelleme talebini reddet
  async rejectLocationUpdateRequest(requestId: number, reason: string): Promise<boolean> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post(`/workspace/location-update-requests/${requestId}/reject`, 
          { reason }, 
          this.getApiConfig()
        )
      );
      return response.data.success;
    } catch (error) {
      console.error('[JourneyService] Reject location update request error:', error);
      throw error;
    }
  }
  
  // ✅ YENİ AD: Launch delay için ETA güncelle (sadece zaman delta, reorder yok)
  async optimizeForLaunchDelay(journeyId: number, actualStartTime: string): Promise<JourneyResponse> {
    if (!networkService.getIsConnected()) {
      throw new Error('ETA güncellemesi için internet bağlantısı gerekli');
    }

    try {
      console.log(`[JourneyService] Updating ETAs for journey ${journeyId} based on launch delay`);
      console.log(`[JourneyService] Actual start time: ${actualStartTime}`);

      const response = await this.retryRequest(() =>
        api.post<JourneyResponse>(
          `/workspace/journeys/${journeyId}/optimize-for-deviation`,
          { actualStartTime },
          this.getApiConfig(30000) // ETA güncellemesi için 30 saniye timeout (Google API yok)
        )
      );

      // Cache'i güncelle
      if (response.data) {
        await this.cacheJourney(response.data);
      }

      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Optimize for launch delay error:', error);

      if (error.code === 'ECONNABORTED') {
        throw new Error('ETA güncellemesi zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }

      throw error;
    }
  }

  // ✅ Backward compatibility - eski ad
  async optimizeForDeviation(journeyId: number, actualStartTime: string): Promise<JourneyResponse> {
    return this.optimizeForLaunchDelay(journeyId, actualStartTime);
  }
  
  async getStaticMapUrl(journeyId: number): Promise<string> {
    try {
      const response = await this.retryRequest(() =>
        api.get(`/workspace/journeys/${journeyId}/static-map`, this.getApiConfig())
      );
      return response.data.staticMapUrl;
    } catch (error) {
      console.error('Static map URL error:', error);
      // Hata durumunda boş string dön
      return '';
    }
  }

  // Stop'u tamamla - ÇOKLU FOTOĞRAF DESTEĞİ EKLENDİ
  async completeStopWithFiles(
    journeyId: number, 
    stopId: number, 
    formData: FormData
  ): Promise<boolean> {
    // FormData'yı parse et
    const formDataParts = (formData as any)._parts || [];
    let notes = '';
    let signatureUri = '';
    const photoUris: string[] = [];
    
    for (const part of formDataParts) {
      if (part[0] === 'notes') {
        notes = part[1];
      } else if (part[0] === 'signature') {
        signatureUri = part[1].uri;
      } else if (part[0] === 'photo' || part[0] === 'photos') {
        // Çoklu fotoğraf desteği
        photoUris.push(part[1].uri);
      }
    }
    
    // Offline durumda
    if (!networkService.getIsConnected()) {
      // Queue'ya ekle
      const offlineData: any = { notes };
      
      // Çoklu fotoğraf desteği
      if (photoUris.length > 0) {
        offlineData.photoUris = photoUris;
      }
      
      if (signatureUri) {
        offlineData.signatureUri = signatureUri;
      }
      
      await offlineQueueService.addToQueue({
        type: 'COMPLETE',
        journeyId,
        stopId,
        data: offlineData
      });
      
      // Local cache'i güncelle
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        const stopIndex = journey.stops.findIndex(s => s.id === stopId);
        if (stopIndex >= 0) {
          journey.stops[stopIndex].status = 'completed';
          journey.stops[stopIndex].checkOutTime = new Date().toISOString();
          await this.cacheJourney(journey);
        }
      }
      
      return true;
    }
    
    // Online durumda normal işlem
    return this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<boolean>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/complete`,
          formData,
          {
            headers: {
              'Content-Type': 'multipart/form-data',
            },
            timeout: 60000 // Çoklu dosya yükleme için 60 saniye
          }
        );
        return response.data;
      },
      {
        type: 'COMPLETE',
        journeyId,
        stopId,
        data: { notes, photoUris, signatureUri }
      }
    );
  }

  // Stop'a check-in yap
  async checkInStop(journeyId: number, stopId: number): Promise<CheckInResponse> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<CheckInResponse>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/checkin`,
          {},
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'CHECK_IN',
        journeyId,
        stopId
      }
    );

    // Local cache'i güncelle
    if (!networkService.getIsConnected()) {
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        const stopIndex = journey.stops.findIndex(s => s.id === stopId);
        if (stopIndex >= 0) {
          journey.stops[stopIndex].status = 'in_progress';
          journey.stops[stopIndex].checkInTime = new Date().toISOString();
          await this.cacheJourney(journey);
        }
      }

      // Offline modda default response dön
      return {
        success: true,
        requiresDelayReason: false,
        newDelay: 0,
        cumulativeDelay: 0,
        message: 'Çevrimdışı check-in kaydedildi'
      };
    }

    return result;
  }

  // Gecikme sebebi gönder
  async submitDelayReason(
    journeyId: number,
    stopId: number,
    category: DelayReasonCategory,
    reason: string
  ): Promise<boolean> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<boolean>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/delay-reason`,
          {
            delayReasonCategory: category,
            delayReason: reason
          },
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'SUBMIT_DELAY_REASON' as any,
        journeyId,
        stopId,
        data: {
          delayReasonCategory: category,
          delayReason: reason
        }
      }
    );

    return result;
  }

  // Journey başlat
  async startJourney(journeyId: number): Promise<JourneyResponse> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<JourneyResponse>(
          `/workspace/journeys/${journeyId}/start`,
          {},
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'START_JOURNEY',
        journeyId
      }
    );
    
    // Local cache'i güncelle
    if (!networkService.getIsConnected()) {
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        journey.status = 'in_progress';
        await this.cacheJourney(journey);
        return journey;
      }
    }
    
    return result;
  }

  // Journey bitir
  async finishJourney(journeyId: number): Promise<JourneyResponse> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<JourneyResponse>(
          `/workspace/journeys/${journeyId}/finish`,
          {},
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'FINISH_JOURNEY',
        journeyId
      }
    );
    
    // Local cache'i güncelle
    if (!networkService.getIsConnected()) {
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        journey.status = 'completed';
        await this.cacheJourney(journey);
        return journey;
      }
    }
    
    return result;
  }

  // Stop'u başarısız olarak işaretle
  async failStop(
    journeyId: number, 
    stopId: number, 
    data: FailStopRequest
  ): Promise<boolean> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<boolean>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/fail`,
          data,
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'FAIL',
        journeyId,
        stopId,
        data
      }
    );
    
    // Local cache'i güncelle
    if (!networkService.getIsConnected()) {
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        const stopIndex = journey.stops.findIndex(s => s.id === stopId);
        if (stopIndex >= 0) {
          journey.stops[stopIndex].status = 'failed';
          journey.stops[stopIndex].checkOutTime = new Date().toISOString();
          await this.cacheJourney(journey);
        }
      }
    }
    
    return result;
  }

  // Stop'u reset et
  async resetStop(journeyId: number, stopId: number): Promise<boolean> {
    const result = await this.executeWithOfflineSupport(
      async () => {
        const response = await api.post<boolean>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/reset`,
          {},
          this.getApiConfig()
        );
        return response.data;
      },
      {
        type: 'RESET',
        journeyId,
        stopId
      }
    );
    
    // Local cache'i güncelle
    if (!networkService.getIsConnected()) {
      const journey = await this.getCachedJourney(journeyId);
      if (journey) {
        const stopIndex = journey.stops.findIndex(s => s.id === stopId);
        if (stopIndex >= 0) {
          journey.stops[stopIndex].status = 'pending';
          journey.stops[stopIndex].checkInTime = undefined;
          journey.stops[stopIndex].checkOutTime = undefined;
          await this.cacheJourney(journey);
        }
      }
    }
    
    return result;
  }

  // Journey status geçmişi getir
  async getJourneyStatuses(journeyId: number): Promise<JourneyStatusResponse[]> {
    if (!networkService.getIsConnected()) {
      return [];
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.get<JourneyStatusResponse[]>(
          `/workspace/journeys/${journeyId}/statuses`,
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error) {
      console.error('[JourneyService] Get journey statuses error:', error);
      return []; // Hata durumunda boş array dön
    }
  }

  // YENİ: Stop fotoğraflarını getir
  async getStopPhotos(journeyId: number, stopId: number): Promise<any[]> {
    if (!networkService.getIsConnected()) {
      return [];
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.get<any[]>(
          `/workspace/journeys/${journeyId}/stops/${stopId}/photos`,
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error) {
      console.error('[JourneyService] Get stop photos error:', error);
      return [];
    }
  }

  // YENİ: Journey'nin tüm fotoğraflarını getir
  async getAllJourneyPhotos(journeyId: number): Promise<any[]> {
    if (!networkService.getIsConnected()) {
      return [];
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.get<any[]>(
          `/workspace/journeys/${journeyId}/photos`,
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error) {
      console.error('[JourneyService] Get all journey photos error:', error);
      return [];
    }
  }

  // Journey özet bilgileri getir
  async getJourneysSummary(params?: JourneyListParams): Promise<JourneySummaryResponse> {
    const defaultSummary: JourneySummaryResponse = {
      totalJourneys: 0,
      completedJourneys: 0,
      inProgressJourneys: 0,
      cancelledJourneys: 0,
      plannedJourneys: 0,
      totalDistance: 0,
      totalDuration: 0,
      completionRate: 0,
      isDriverView: false
    };
    
    if (!networkService.getIsConnected()) {
      console.log('[JourneyService] Offline - returning default summary');
      return defaultSummary;
    }
    
    try {
      const queryParams = new URLSearchParams();
      
      if (params?.from || params?.to) {
        const now = new Date();
        const turkeyTime = addHours(now, 3);
        
        if (params.from) {
          const fromDate = new Date(params.from);
          const fromStart = startOfDay(fromDate);
          const fromUTC = addHours(fromStart, -3);
          queryParams.append('from', fromUTC.toISOString());
        }
        
        if (params.to) {
          const toDate = new Date(params.to);
          const toEnd = endOfDay(toDate);
          const toUTC = addHours(toEnd, -3);
          queryParams.append('to', toUTC.toISOString());
        }
      }
      
      if (params?.status) queryParams.append('status', params.status);
      if (params?.driverId) queryParams.append('driverId', params.driverId.toString());
      
      const queryString = queryParams.toString();
      const url = `/workspace/journeys/summary${queryString ? `?${queryString}` : ''}`;
      
      const response = await this.retryRequest(() =>
        api.get<JourneySummaryResponse>(url, this.getApiConfig())
      );
      
      return response.data;
    } catch (error) {
      console.error('[JourneyService] Get journeys summary error:', error);
      return defaultSummary;
    }
  }

  // Diğer metodlar
  
  async cancelJourney(journeyId: number): Promise<JourneyResponse> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post<JourneyResponse>(
          `/workspace/journeys/${journeyId}/cancel`,
          {},
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Cancel journey error:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async optimizeJourney(journeyId: number): Promise<JourneyResponse> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post<JourneyResponse>(
          `/workspace/journeys/${journeyId}/optimize`,
          {},
          this.getApiConfig(60000) // Optimizasyon için 60 saniye timeout
        )
      );
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Optimize journey error:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('Optimizasyon işlemi zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async bulkCancelJourneys(journeyIds: number[]): Promise<BulkOperationResult> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post<BulkOperationResult>(
          '/workspace/journeys/bulk/cancel',
          { journeyIds },
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Bulk cancel journeys error:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async bulkArchiveJourneys(journeyIds: number[]): Promise<BulkOperationResult> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.post<BulkOperationResult>(
          '/workspace/journeys/bulk/archive',
          { journeyIds },
          this.getApiConfig()
        )
      );
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Bulk archive journeys error:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async bulkDeleteJourneys(journeyIds: number[]): Promise<BulkOperationResult> {
    if (!networkService.getIsConnected()) {
      throw new Error('Bu işlem internet bağlantısı gerektiriyor');
    }
    
    try {
      const response = await this.retryRequest(() =>
        api.delete<BulkOperationResult>(
          '/workspace/journeys/bulk/delete',
          { 
            data: { journeyIds },
            ...this.getApiConfig()
          }
        )
      );
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Bulk delete journeys error:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İşlem zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async getTodaysJourneys(): Promise<JourneyResponse[]> {
    const today = new Date();
    const todayStr = format(today, 'yyyy-MM-dd');
    
    return this.getJourneys({
      from: todayStr,
      to: todayStr,
      limit: 50 // Günlük maksimum 50 sefer göster
    });
  }

  async getActiveJourneys(): Promise<JourneyResponse[]> {
    try {
      const journeys = await this.getJourneys({
        status: 'InProgress',
        limit: 20 // Aktif seferler için maksimum 20
      });
      return journeys;
    } catch (error) {
      console.error('[JourneyService] Get active journeys error:', error);
      return [];
    }
  }

  // ✅ YENİ: Aktif seferi yeniden optimize et
  async reoptimizeJourney(
    journeyId: number,
    currentLatitude: number,
    currentLongitude: number
  ): Promise<JourneyResponse> {
    try {
      console.log(`[JourneyService] Reoptimizing journey ${journeyId}`);

      const response = await api.post<JourneyResponse>(
        `/workspace/journeys/${journeyId}/reoptimize`,
        {
          currentLatitude,
          currentLongitude
        }
      );

      // Başarılı ise cache'i güncelle
      if (response.data) {
        await this.cacheJourney(response.data);
      }

      console.log('[JourneyService] Journey reoptimized successfully');
      return response.data;
    } catch (error: any) {
      console.error('[JourneyService] Reoptimize journey error:', error);
      const message = error.response?.data?.message || error.message || 'Optimizasyon başarısız';
      throw new Error(message);
    }
  }

  // Cache temizleme (gerekirse)
  async clearCache(): Promise<void> {
    try {
      const keys = await AsyncStorage.getAllKeys();
      const journeyKeys = keys.filter(key => 
        key.startsWith(this.JOURNEY_CACHE_PREFIX) || 
        key === this.JOURNEYS_LIST_CACHE
      );
      
      if (journeyKeys.length > 0) {
        await AsyncStorage.multiRemove(journeyKeys);
        console.log(`[JourneyService] Cleared ${journeyKeys.length} cached items`);
      }
    } catch (error) {
      console.error('[JourneyService] Clear cache error:', error);
    }
  }
}

export default new JourneyService();
import AsyncStorage from '@react-native-async-storage/async-storage';
import networkService from './networkService';
import { Alert } from 'react-native';
import api from './api';
import RNFS from 'react-native-fs';
import NetInfo from '@react-native-community/netinfo'; // BUGFIX S4.6: Dynamic sync interval based on connection type
import journeyService from './journeyService'; // YENİ EKLEME
import customerService from './customerService';
import vehicleService from './vehicleService';
import driverService from './driverService';

export interface QueueItem {
  id: string;
  type: 'CHECK_IN' | 'COMPLETE' | 'FAIL' | 'RESET' | 'START_JOURNEY' | 'FINISH_JOURNEY' | 'LOCATION_UPDATE_REQUEST' | 'CUSTOMER_CREATE' | 'VEHICLE_CREATE' | 'VEHICLE_UPDATE' | 'VEHICLE_DELETE' | 'DRIVER_CREATE' | 'DRIVER_UPDATE' | 'DRIVER_DELETE';
  data: any;
  timestamp: number;
  retryCount: number;
  maxRetries: number;
  journeyId: number;
  stopId?: number;
}

interface QueueResult {
  success: boolean;
  error?: string;
  conflictResolution?: 'retry' | 'skip' | 'override';
}

class OfflineQueueService {
  private readonly QUEUE_KEY = '@offline_queue';
  private readonly MAX_RETRIES = 3;
  // BUGFIX S4.6: Dynamic sync intervals based on connection type
  private readonly SYNC_INTERVAL_WIFI = 15000; // 15 seconds on WiFi (faster, no data cost)
  private readonly SYNC_INTERVAL_CELLULAR = 60000; // 60 seconds on cellular (save data)
  private readonly FILE_CACHE_DIR = `${RNFS.DocumentDirectoryPath}/offline_files`;
  private syncTimer: NodeJS.Timeout | null = null;
  private isSyncing: boolean = false;
  private networkUnsubscribe: (() => void) | null = null; // BUGFIX S4.7: Memory leak - store unsubscribe function

  constructor() {
    // Initialize'ı setTimeout ile erteleyerek döngüyü kırıyoruz
    setTimeout(() => this.initialize(), 0);
  }

  private initialize = async () => {
    // File cache dizinini oluştur
    try {
      const dirExists = await RNFS.exists(this.FILE_CACHE_DIR);
      if (!dirExists) {
        await RNFS.mkdir(this.FILE_CACHE_DIR);
      }
    } catch (error) {
      console.error('Error creating file cache dir:', error);
    }

    // BUGFIX S4.7: Properly track network listener for cleanup
    const reconnectedHandler = () => {
      console.log('Network reconnected, starting sync...');
      this.syncQueue();
    };

    networkService.on('reconnected', reconnectedHandler);

    // Store cleanup function
    this.networkUnsubscribe = () => {
      networkService.off('reconnected', reconnectedHandler);
    };

    // BUGFIX S4.6: Listen to connection type changes for dynamic sync interval
    NetInfo.addEventListener(state => {
      console.log('Connection type changed:', state.type);
      this.startPeriodicSync(); // Restart with new interval based on connection type
    });

    // Periyodik sync
    this.startPeriodicSync();
  };

  // File'ı cache'e kaydet
  private async cacheFile(uri: string, type: 'photo' | 'signature', index?: number): Promise<string | null> {
    try {
      const timestamp = Date.now();
      const extension = type === 'photo' ? 'jpg' : 'png';
      const suffix = index !== undefined ? `_${index}` : '';
      const filename = `${type}${suffix}_${timestamp}.${extension}`;
      const cachePath = `${this.FILE_CACHE_DIR}/${filename}`;
      
      // Base64 string ise direkt kaydet
      if (uri.startsWith('data:')) {
        const base64Data = uri.split(',')[1];
        await RNFS.writeFile(cachePath, base64Data, 'base64');
        return cachePath;
      }
      
      // File URI ise kopyala
      await RNFS.copyFile(uri, cachePath);
      return cachePath;
    } catch (error) {
      console.error('Error caching file:', error);
      return null;
    }
  }

  // Cache'den file'ı oku ve FormData'ya ekle
  private async loadCachedFile(cachePath: string): Promise<string | null> {
    try {
      const exists = await RNFS.exists(cachePath);
      if (!exists) return null;
      
      const base64 = await RNFS.readFile(cachePath, 'base64');
      return `data:image/${cachePath.endsWith('.png') ? 'png' : 'jpeg'};base64,${base64}`;
    } catch (error) {
      console.error('Error loading cached file:', error);
      return null;
    }
  }

  // Queue'ya item ekle - ÇOKLU FOTOĞRAF DESTEĞİ EKLENDİ
  public addToQueue = async (item: Omit<QueueItem, 'id' | 'timestamp' | 'retryCount' | 'maxRetries'>): Promise<void> => {
    try {
      const queue = await this.getQueue();

      // BUGFIX S4.1: Queue size limit to prevent phone storage overflow
      const MAX_QUEUE_SIZE = 100; // Maximum 100 pending items
      if (queue.length >= MAX_QUEUE_SIZE) {
        console.error(`BUGFIX S4.1: Queue size limit reached (${queue.length}/${MAX_QUEUE_SIZE}). Cannot add more items.`);
        Alert.alert(
          'Depolama Dolu',
          `Çevrimdışı kuyruk limiti doldu (${MAX_QUEUE_SIZE} öğe). Lütfen internete bağlanıp senkronize edin.`,
          [{ text: 'Tamam' }]
        );
        throw new Error(`Queue size limit reached: ${queue.length}/${MAX_QUEUE_SIZE}`);
      }

      const newItem: QueueItem = {
        ...item,
        id: `${Date.now()}_${Math.random()}`,
        timestamp: Date.now(),
        retryCount: 0,
        maxRetries: this.MAX_RETRIES,
      };

      // COMPLETE tipinde zorunluluk kontrolü ve photo/signature cache
      if (item.type === 'COMPLETE' && item.data) {
        // Zorunluluk kontrolü
        const { signatureRequired, photoRequired } = item.data.requirements || {};
        
        // İmza zorunluluk kontrolü
        if (signatureRequired && !item.data.signatureUri && !item.data.signatureCachePath) {
          throw new Error('İmza zorunludur. Teslimat tamamlanamaz.');
        }
        
        // Fotoğraf zorunluluk kontrolü - çoklu fotoğraf desteği
        const hasPhoto = (item.data.photoUri || item.data.photoUris?.length > 0 || 
                         item.data.photoCachePath || item.data.photoCachePaths?.length > 0);
        
        if (photoRequired && !hasPhoto) {
          throw new Error('En az bir fotoğraf zorunludur. Teslimat tamamlanamaz.');
        }
        
        // ÇOKLU FOTOĞRAF CACHE İŞLEMİ
        if (item.data.photoUris && Array.isArray(item.data.photoUris)) {
          const cachedPaths: string[] = [];
          for (let i = 0; i < item.data.photoUris.length; i++) {
            const cachedPath = await this.cacheFile(item.data.photoUris[i], 'photo', i);
            if (cachedPath) {
              cachedPaths.push(cachedPath);
            }
          }
          if (cachedPaths.length > 0) {
            newItem.data.photoCachePaths = cachedPaths;
            delete newItem.data.photoUris;
          }
        }
        // Tek fotoğraf (backward compatibility)
        else if (item.data.photoUri) {
          const cachedPath = await this.cacheFile(item.data.photoUri, 'photo');
          if (cachedPath) {
            newItem.data.photoCachePath = cachedPath;
            delete newItem.data.photoUri;
          }
        }
        
        // Cache signature if exists
        if (item.data.signatureUri) {
          const cachedPath = await this.cacheFile(item.data.signatureUri, 'signature');
          if (cachedPath) {
            newItem.data.signatureCachePath = cachedPath;
            delete newItem.data.signatureUri;
          }
        }
        
        // Zorunlulukları da sakla (sync sırasında tekrar kontrol için)
        newItem.data.requirements = {
          signatureRequired,
          photoRequired
        };
      }

      // Aynı tip ve aynı stop için bekleyen item varsa üzerine yaz
      const existingIndex = queue.findIndex(
        q => q.type === newItem.type && 
             q.journeyId === newItem.journeyId && 
             q.stopId === newItem.stopId
      );

      if (existingIndex >= 0) {
        // Eski cached file'ları temizle
        const oldItem = queue[existingIndex];
        
        // Tek fotoğraf
        if (oldItem.data?.photoCachePath) {
          try {
            await RNFS.unlink(oldItem.data.photoCachePath);
          } catch (e) {}
        }
        
        // Çoklu fotoğraf
        if (oldItem.data?.photoCachePaths && Array.isArray(oldItem.data.photoCachePaths)) {
          for (const path of oldItem.data.photoCachePaths) {
            try {
              await RNFS.unlink(path);
            } catch (e) {}
          }
        }
        
        // İmza
        if (oldItem.data?.signatureCachePath) {
          try {
            await RNFS.unlink(oldItem.data.signatureCachePath);
          } catch (e) {}
        }
        
        queue[existingIndex] = newItem;
      } else {
        queue.push(newItem);
      }

      await this.saveQueue(queue);
      
      // Hemen sync dene
      if (networkService.getIsConnected()) {
        this.syncQueue();
      }
    } catch (error) {
      console.error('Error adding to queue:', error);
      throw error; // Hata fırlatarak üst katmanda yakalanmasını sağla
    }
  };

  // Convenient method to add operations to the queue
  public addOperation = async (type: QueueItem['type'], data: any, journeyId: number = 0, stopId?: number): Promise<void> => {
    await this.addToQueue({
      type,
      data,
      journeyId,
      stopId,
    });
  };

  // Queue'yu getir
  private getQueue = async (): Promise<QueueItem[]> => {
    try {
      const queueString = await AsyncStorage.getItem(this.QUEUE_KEY);
      if (queueString) {
        return JSON.parse(queueString);
      }
      return [];
    } catch (error) {
      console.error('Error getting queue:', error);
      return [];
    }
  };

  // Queue'yu kaydet
  private saveQueue = async (queue: QueueItem[]): Promise<void> => {
    try {
      await AsyncStorage.setItem(this.QUEUE_KEY, JSON.stringify(queue));
    } catch (error) {
      console.error('Error saving queue:', error);
    }
  };

  // Queue'yu temizle
  public clearQueue = async (): Promise<void> => {
    try {
      // Önce cached file'ları temizle
      const queue = await this.getQueue();
      for (const item of queue) {
        // Tek fotoğraf
        if (item.data?.photoCachePath) {
          try {
            await RNFS.unlink(item.data.photoCachePath);
          } catch (e) {}
        }
        
        // Çoklu fotoğraf
        if (item.data?.photoCachePaths && Array.isArray(item.data.photoCachePaths)) {
          for (const path of item.data.photoCachePaths) {
            try {
              await RNFS.unlink(path);
            } catch (e) {}
          }
        }
        
        // İmza
        if (item.data?.signatureCachePath) {
          try {
            await RNFS.unlink(item.data.signatureCachePath);
          } catch (e) {}
        }
      }
      
      await AsyncStorage.removeItem(this.QUEUE_KEY);
    } catch (error) {
      console.error('Error clearing queue:', error);
    }
  };

  // Queue'daki item sayısı
  public getQueueCount = async (): Promise<number> => {
    const queue = await this.getQueue();
    return queue.length;
  };

  // Sync işlemi
  public syncQueue = async (): Promise<void> => {
    if (this.isSyncing) {
      console.log('Already syncing, skipping...');
      return;
    }

    if (!networkService.getIsConnected()) {
      console.log('No network connection, skipping sync...');
      return;
    }

    this.isSyncing = true;

    try {
      const queue = await this.getQueue();
      
      if (queue.length === 0) {
        this.isSyncing = false;
        return;
      }

      console.log(`Syncing ${queue.length} items...`);
      
      const processedIds: string[] = [];
      const failedItems: QueueItem[] = [];

      // Sırayla işle (paralel değil, sıra önemli)
      for (const item of queue) {
        const result = await this.processQueueItem(item);
        
        if (result.success) {
          processedIds.push(item.id);
          
          // Başarılı olduktan sonra cached file'ları temizle
          // Tek fotoğraf
          if (item.data?.photoCachePath) {
            try {
              await RNFS.unlink(item.data.photoCachePath);
            } catch (e) {}
          }
          
          // Çoklu fotoğraf
          if (item.data?.photoCachePaths && Array.isArray(item.data.photoCachePaths)) {
            for (const path of item.data.photoCachePaths) {
              try {
                await RNFS.unlink(path);
              } catch (e) {}
            }
          }
          
          // İmza
          if (item.data?.signatureCachePath) {
            try {
              await RNFS.unlink(item.data.signatureCachePath);
            } catch (e) {}
          }
        } else {
          item.retryCount++;
          
          if (item.retryCount < item.maxRetries) {
            failedItems.push(item);
          } else {
            // Max retry'a ulaştı, kullanıcıyı bilgilendir
            this.notifyMaxRetryReached(item);
            processedIds.push(item.id); // Queue'dan çıkar
            
            // Cached file'ları temizle
            // Tek fotoğraf
            if (item.data?.photoCachePath) {
              try {
                await RNFS.unlink(item.data.photoCachePath);
              } catch (e) {}
            }
            
            // Çoklu fotoğraf
            if (item.data?.photoCachePaths && Array.isArray(item.data.photoCachePaths)) {
              for (const path of item.data.photoCachePaths) {
                try {
                  await RNFS.unlink(path);
                } catch (e) {}
              }
            }
            
            // İmza
            if (item.data?.signatureCachePath) {
              try {
                await RNFS.unlink(item.data.signatureCachePath);
              } catch (e) {}
            }
          }
        }
      }

      // Queue'yu güncelle
      const remainingQueue = queue.filter(
        item => !processedIds.includes(item.id)
      );
      
      // Failed items'ları ekle
      remainingQueue.push(...failedItems);
      
      await this.saveQueue(remainingQueue);

      if (processedIds.length > 0) {
        console.log(`Successfully synced ${processedIds.length} items`);
      }

      if (failedItems.length > 0) {
        console.log(`${failedItems.length} items failed, will retry`);
      }

    } catch (error) {
      console.error('Sync queue error:', error);
    } finally {
      this.isSyncing = false;
    }
  };

  // Queue item'ı işle - ÇOKLU FOTOĞRAF DESTEĞİ EKLENDİ VE LOCATION_UPDATE_REQUEST EKLENDİ
  private processQueueItem = async (item: QueueItem): Promise<QueueResult> => {
    try {
      switch (item.type) {
        case 'CHECK_IN':
          await api.post<boolean>(
            `/workspace/journeys/${item.journeyId}/stops/${item.stopId}/checkin`
          );
          return { success: true };

        case 'COMPLETE':
          // FormData oluştur
          const formData = new FormData();
          
          if (item.data?.notes) {
            formData.append('notes', item.data.notes);
          }

          if (item.data?.receiverName) {
            formData.append('receiverName', item.data.receiverName);
          }
          
          // ÇOKLU FOTOĞRAF YÜKLEME
          if (item.data?.photoCachePaths && Array.isArray(item.data.photoCachePaths)) {
            for (let i = 0; i < item.data.photoCachePaths.length; i++) {
              const photoData = await this.loadCachedFile(item.data.photoCachePaths[i]);
              if (photoData) {
                // İlk fotoğraf için 'photo', diğerleri için 'photos' field'ı kullan
                const fieldName = i === 0 ? 'photo' : 'photos';
                formData.append(fieldName, {
                  uri: photoData,
                  type: 'image/jpeg',
                  name: `photo_${i}.jpg`
                } as any);
              }
            }
          }
          // Tek fotoğraf (backward compatibility)
          else if (item.data?.photoCachePath) {
            const photoData = await this.loadCachedFile(item.data.photoCachePath);
            if (photoData) {
              formData.append('photo', {
                uri: photoData,
                type: 'image/jpeg',
                name: 'photo.jpg'
              } as any);
            }
          }
          
          // Cached signature'ı yükle
          if (item.data?.signatureCachePath) {
            const signatureData = await this.loadCachedFile(item.data.signatureCachePath);
            if (signatureData) {
              formData.append('signature', {
                uri: signatureData,
                type: 'image/png',
                name: 'signature.png'
              } as any);
            }
          }
          
          await api.post<boolean>(
            `/workspace/journeys/${item.journeyId}/stops/${item.stopId}/complete`,
            formData,
            {
              headers: {
                'Content-Type': 'multipart/form-data',
              },
            }
          );
          return { success: true };

        case 'FAIL':
          await api.post<boolean>(
            `/workspace/journeys/${item.journeyId}/stops/${item.stopId}/fail`,
            item.data
          );
          return { success: true };

        case 'RESET':
          await api.post<boolean>(
            `/workspace/journeys/${item.journeyId}/stops/${item.stopId}/reset`
          );
          return { success: true };

        case 'START_JOURNEY':
          await api.post(`/workspace/journeys/${item.journeyId}/start`);
          return { success: true };

        case 'FINISH_JOURNEY':
          await api.post(`/workspace/journeys/${item.journeyId}/finish`);
          return { success: true };

        // YENİ: LOCATION_UPDATE_REQUEST case'i eklendi
        case 'LOCATION_UPDATE_REQUEST':
          await journeyService.createLocationUpdateRequest({
            journeyId: item.journeyId!,
            stopId: item.stopId!,
            customerId: item.data.customerId,
            currentLatitude: item.data.currentLatitude,
            currentLongitude: item.data.currentLongitude,
            currentAddress: item.data.currentAddress,
            requestedLatitude: item.data.requestedLatitude,
            requestedLongitude: item.data.requestedLongitude,
            reason: item.data.reason
          });
          return { success: true };

        case 'CUSTOMER_CREATE':
          await customerService.create(item.data);
          return { success: true };

        case 'VEHICLE_CREATE':
          await vehicleService.create(item.data);
          return { success: true };

        case 'VEHICLE_UPDATE':
          await vehicleService.update(item.data.id, item.data);
          return { success: true };

        case 'VEHICLE_DELETE':
          await vehicleService.delete(item.data.id);
          return { success: true };

        case 'DRIVER_CREATE':
          await driverService.create(item.data);
          return { success: true };

        case 'DRIVER_UPDATE':
          await driverService.update(item.data.id, item.data);
          return { success: true };

        case 'DRIVER_DELETE':
          await driverService.delete(item.data.id);
          return { success: true };

        default:
          return { success: false, error: 'Unknown queue item type' };
      }
    } catch (error: any) {
      console.error('Process queue item error:', error);

      // BUGFIX S4.8: Improved conflict resolution for offline mode
      // 409 Conflict durumunda - server'daki veri daha güncel
      if (error.response?.status === 409) {
        console.warn(`Conflict detected for ${item.type} on journey ${item.journeyId}, stop ${item.stopId}`);

        // Try to fetch current state from server and merge
        try {
          if (item.stopId) {
            // Fetch current stop state from server
            const response = await api.get(`/workspace/journeys/${item.journeyId}/stops/${item.stopId}`);
            console.log('Server state:', response.data);

            // If stop is already completed/failed on server, skip this operation
            if (response.data?.status === 'Completed' || response.data?.status === 'Failed') {
              console.log('Stop already processed on server, skipping queue item');
              return {
                success: true, // Mark as success to remove from queue
                error: 'Already processed on server'
              };
            }
          }
        } catch (fetchError) {
          console.error('Failed to fetch server state:', fetchError);
        }

        return {
          success: false,
          error: 'Conflict detected - will retry',
          conflictResolution: 'retry' // Retry instead of skip
        };
      }

      // 404 Not Found durumunda
      if (error.response?.status === 404) {
        console.warn(`Resource not found for ${item.type}`);
        return {
          success: true, // Mark as success to remove from queue
          error: 'Resource not found - removed from queue',
          conflictResolution: 'skip'
        };
      }

      return { success: false, error: error.message };
    }
  };

  // Max retry bildirimi
  private notifyMaxRetryReached = (item: QueueItem) => {
    let message = '';
    
    switch (item.type) {
      case 'CHECK_IN':
        message = 'Varış bildirimi gönderilemedi';
        break;
      case 'COMPLETE':
        message = 'Teslimat tamamlama gönderilemedi';
        break;
      case 'FAIL':
        message = 'Başarısız teslimat bildirimi gönderilemedi';
        break;
      case 'LOCATION_UPDATE_REQUEST': // YENİ
        message = 'Konum güncelleme talebi gönderilemedi';
        break;
      case 'CUSTOMER_CREATE':
        message = 'Müşteri oluşturma talebi gönderilemedi';
        break;
      case 'VEHICLE_CREATE':
        message = 'Araç oluşturma talebi gönderilemedi';
        break;
      case 'VEHICLE_UPDATE':
        message = 'Araç güncelleme talebi gönderilemedi';
        break;
      case 'VEHICLE_DELETE':
        message = 'Araç silme talebi gönderilemedi';
        break;
      case 'DRIVER_CREATE':
        message = 'Sürücü oluşturma talebi gönderilemedi';
        break;
      case 'DRIVER_UPDATE':
        message = 'Sürücü güncelleme talebi gönderilemedi';
        break;
      case 'DRIVER_DELETE':
        message = 'Sürücü silme talebi gönderilemedi';
        break;
      default:
        message = 'İşlem gönderilemedi';
    }
    
    Alert.alert(
      'Senkronizasyon Hatası',
      `${message}. Lütfen internet bağlantınızı kontrol edin ve tekrar deneyin.`,
      [{ text: 'Tamam' }]
    );
  };

  // BUGFIX S4.6: Dynamic sync interval based on connection type
  private startPeriodicSync = async () => {
    if (this.syncTimer) {
      clearInterval(this.syncTimer);
      this.syncTimer = null;
    }

    // Get current connection type
    const state = await NetInfo.fetch();
    let interval = this.SYNC_INTERVAL_CELLULAR; // Default to cellular (slower)

    if (state.type === 'wifi') {
      interval = this.SYNC_INTERVAL_WIFI;
      console.log(`Starting periodic sync with WiFi interval: ${interval}ms`);
    } else if (state.type === 'cellular') {
      interval = this.SYNC_INTERVAL_CELLULAR;
      console.log(`Starting periodic sync with Cellular interval: ${interval}ms (data saving)`);
    } else {
      console.log(`Starting periodic sync with default interval: ${interval}ms`);
    }

    this.syncTimer = setInterval(() => {
      this.syncQueue();
    }, interval);
  };

  // BUGFIX S4.7: Proper cleanup to prevent memory leaks
  public cleanup = () => {
    // Clear sync timer
    if (this.syncTimer) {
      clearInterval(this.syncTimer);
      this.syncTimer = null;
    }

    // Unsubscribe from network events
    if (this.networkUnsubscribe) {
      this.networkUnsubscribe();
      this.networkUnsubscribe = null;
    }

    console.log('OfflineQueueService cleaned up');
  };

  // Queue durumu
  public getQueueStatus = async (): Promise<{
    count: number;
    oldestItem: number | null;
    isOnline: boolean;
    isSyncing: boolean;
  }> => {
    const queue = await this.getQueue();
    const oldestItem = queue.length > 0 
      ? Math.min(...queue.map(q => q.timestamp))
      : null;
    
    return {
      count: queue.length,
      oldestItem,
      isOnline: networkService.getIsConnected(),
      isSyncing: this.isSyncing,
    };
  };
}

export default new OfflineQueueService();
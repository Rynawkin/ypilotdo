// C:\Projects\RotaAppMobile\src\services\routeService.ts

import api from './api';

export interface RouteStartDetails {
  startTime: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
}

export interface RouteEndDetails {
  name: string;
  address: string;
  latitude: number;
  longitude: number;
}

export interface RouteStop {
  id?: number;
  customerId?: number;
  customer?: any;
  order: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  contactFullName?: string;
  contactPhone?: string;
  contactEmail?: string;
  type: number;
  orderType: number;
  proofOfDeliveryRequired: boolean;
  serviceTime: string;
  estimatedArrivalTime?: string;
  actualArrivalTime?: string;
  actualDepartureTime?: string;
  status?: string;
  notes?: string;
  arriveBetweenStart?: string;
  arriveBetweenEnd?: string;
  priority?: number;
}

export interface Route {
  id: number;
  name: string;
  date: string;
  status: string;
  driverId?: number;
  driver?: any;
  vehicleId?: number;
  vehicle?: any;
  depotId: number;
  depot?: any;
  totalDistance: number;
  totalDuration: number;
  totalDeliveries: number;
  completedDeliveries: number;
  optimized: boolean;
  polyline?: string;
  startedAt?: string;
  completedAt?: string;
  notes?: string;
  workspaceId: number;
  startDetails?: RouteStartDetails;
  endDetails?: RouteEndDetails;
  stops: RouteStop[];
  createdAt: string;
  updatedAt: string;
}

// API config with timeout
const getApiConfig = () => ({
  timeout: 30000, // 30 saniye timeout
  headers: {
    'Content-Type': 'application/json',
  }
});

// Retry helper
const retryRequest = async (fn: () => Promise<any>, retries = 2): Promise<any> => {
  try {
    return await fn();
  } catch (error: any) {
    if (retries > 0 && (error.code === 'ECONNABORTED' || !error.response)) {
      console.log(`Retrying request... ${retries} attempts left`);
      await new Promise(resolve => setTimeout(resolve, 1000));
      return retryRequest(fn, retries - 1);
    }
    throw error;
  }
};

class RouteService {
  async create(data: Partial<Route>): Promise<Route> {
    try {
      const payload = {
        name: data.name,
        date: data.date,
        depotId: data.depotId,
        driverId: data.driverId || null,
        vehicleId: data.vehicleId || null,
        notes: data.notes || '',
        optimized: data.optimized || false,
        totalDistance: data.totalDistance || 0,
        totalDuration: data.totalDuration || 0,
        stops: data.stops || [],
        startDetails: data.startDetails || null,
        endDetails: data.endDetails || null
      };
      
      console.log('Creating route with payload:', JSON.stringify(payload, null, 2));
      
      const response = await retryRequest(() => 
        api.post('/workspace/routes', payload, getApiConfig())
      );
      
      console.log('Route created successfully:', response.data);
      return response.data;
    } catch (error: any) {
      console.error('Route creation error:', {
        status: error.response?.status,
        data: error.response?.data,
        message: error.message,
        url: error.config?.url
      });
      
      if (error.response?.data?.errors) {
        const validationErrors = Object.entries(error.response.data.errors)
          .map(([key, value]) => `${key}: ${value}`)
          .join('\n');
        throw new Error(validationErrors);
      }
      
      if (error.response?.data?.message) {
        throw new Error(error.response.data.message);
      }
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      if (!error.response) {
        throw new Error('Bağlantı hatası. İnternet bağlantınızı kontrol edin.');
      }
      
      throw error;
    }
  }

  async getAll(cacheBuster?: string): Promise<Route[]> {
    try {
      // Cache buster kullanma, direkt çağır
      const url = '/workspace/routes';
      
      const response = await api.get(url); // Direkt api kullan
      
      // Veri yoksa boş array dön
      if (!response.data) {
        return [];
      }
      
      // Array değilse veya hata varsa boş array dön
      if (!Array.isArray(response.data)) {
        console.warn('Unexpected response format:', response.data);
        return [];
      }
      
      return response.data;
    } catch (error: any) {
      console.error('Error fetching routes:', error);
      
      // 404 veya diğer hatalar için boş array dön
      return [];
    }
  }

  async getById(id: number): Promise<Route> {
    try {
      const response = await retryRequest(() => 
        api.get(`/workspace/routes/${id}`, getApiConfig())
      );
      return response.data;
    } catch (error: any) {
      console.error('Error fetching route:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async update(id: number, data: Partial<Route>): Promise<Route> {
    try {
      const response = await retryRequest(() => 
        api.put(`/workspace/routes/${id}`, data, getApiConfig())
      );
      return response.data;
    } catch (error: any) {
      console.error('Error updating route:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async delete(id: number): Promise<void> {
    try {
      await retryRequest(() => 
        api.delete(`/workspace/routes/${id}`, getApiConfig())
      );
    } catch (error: any) {
      console.error('Error deleting route:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async addStops(routeId: number, stops: RouteStop[]): Promise<RouteStop[]> {
    try {
      // Time window formatını düzelt
      const formattedStops = stops.map(stop => ({
        ...stop,
        arriveBetweenStart: stop.arriveBetweenStart ? this.formatTimeWindow(stop.arriveBetweenStart) : null,
        arriveBetweenEnd: stop.arriveBetweenEnd ? this.formatTimeWindow(stop.arriveBetweenEnd) : null,
      }));
      
      const response = await retryRequest(() => 
        api.post(`/workspace/routes/${routeId}/stops`, formattedStops, getApiConfig())
      );
      return response.data;
    } catch (error: any) {
      console.error('Error adding stops:', error);
      
      if (error.response?.status === 400) {
        throw new Error('Time window formatı hatalı. Lütfen saat formatını kontrol edin (ÖRN: 09:00)');
      }
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async updateStop(routeId: number, stopId: number, data: Partial<RouteStop>): Promise<boolean> {
    try {
      // Time window formatını düzelt
      if (data.arriveBetweenStart || data.arriveBetweenEnd) {
        data = {
          ...data,
          arriveBetweenStart: data.arriveBetweenStart ? this.formatTimeWindow(data.arriveBetweenStart) : undefined,
          arriveBetweenEnd: data.arriveBetweenEnd ? this.formatTimeWindow(data.arriveBetweenEnd) : undefined,
        };
      }
      
      const response = await retryRequest(() => 
        api.put(`/workspace/routes/${routeId}/stops/${stopId}`, data, getApiConfig())
      );
      return response.data;
    } catch (error: any) {
      console.error('Error updating stop:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  async deleteStop(routeId: number, stopId: number): Promise<void> {
    try {
      await retryRequest(() => 
        api.delete(`/workspace/routes/${routeId}/stops/${stopId}`, getApiConfig())
      );
    } catch (error: any) {
      console.error('Error deleting stop:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  private async waitForOptimizationJob(jobId: string, timeoutMs: number): Promise<any> {
    const startedAt = Date.now();
    const pollIntervalMs = 2000;

    while (Date.now() - startedAt < timeoutMs) {
      const response = await retryRequest(() =>
        api.get(`/workspace/routes/optimization-jobs/${jobId}`, getApiConfig())
      );

      const job = response.data;
      if (job?.status === 'completed' || job?.status === 'failed') {
        return job;
      }

      await new Promise(resolve => setTimeout(resolve, pollIntervalMs));
    }

    throw new Error('Optimization timed out.');
  }

  async optimize(routeId: number, mode: 'distance' | 'duration' = 'distance'): Promise<any> {
    try {
      const response = await retryRequest(() =>
        api.post(`/workspace/routes/${routeId}/optimize`, {
          optimizationMode: mode
        }, {
          ...getApiConfig(),
          timeout: 60000 // Optimize icin daha uzun timeout
        })
      );

      const responseData = response.data;
      if (responseData?.jobId) {
        const jobResult = await this.waitForOptimizationJob(responseData.jobId, 10 * 60 * 1000);
        if (jobResult?.status === 'failed') {
          return {
            success: false,
            message: jobResult.message || 'Optimization failed.'
          };
        }

        return jobResult.result || {
          success: false,
          message: jobResult.message || 'Optimization result not ready.'
        };
      }

      return responseData;
    } catch (error: any) {
      console.error('Error optimizing route:', error);

      if (error.response?.status === 400) {
        const message = error.response?.data?.message || 'Optimizasyon hatasi';
        throw new Error(message);
      }

      if (error.code === 'ECONNABORTED') {
        throw new Error('Optimizasyon zaman asimina ugradi. Lutfen tekrar deneyin.');
      }

      throw error;
    }
  }

  async startFromRoute(routeId: number, driverId: number): Promise<any> {
    try {
      const route = await this.getById(routeId);
      
      // Journey oluştur
      const response = await retryRequest(() => 
        api.post('/workspace/journeys', {
          routeId: routeId,
          driverId: driverId || route.driverId,
          vehicleId: route.vehicleId,
          date: new Date().toISOString(),
          status: 'assigned'
        }, getApiConfig())
      );
      
      return response.data;
    } catch (error: any) {
      console.error('Error starting journey from route:', error);
      
      if (error.code === 'ECONNABORTED') {
        throw new Error('İstek zaman aşımına uğradı. Lütfen tekrar deneyin.');
      }
      
      throw error;
    }
  }

  // Time window formatını düzelten yardımcı metod
  private formatTimeWindow(time: string): string {
    if (!time) return '';
    
    // Eğer zaten doğru formattaysa (HH:MM:SS) direkt dön
    if (/^\d{2}:\d{2}:\d{2}$/.test(time)) {
      return time;
    }
    
    // HH:MM formatındaysa :00 ekle
    if (/^\d{2}:\d{2}$/.test(time)) {
      return `${time}:00`;
    }
    
    // Sadece saat varsa (HH) :00:00 ekle
    if (/^\d{2}$/.test(time)) {
      return `${time}:00:00`;
    }
    
    return time;
  }
}

export default new RouteService();
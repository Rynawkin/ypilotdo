// C:\Projects\RotaAppMobile\src\services\reportService.ts

import api from './api';

export interface DriverPerformanceResponse {
  driverId: number;
  driverName: string;
  totalDeliveries: number;
  completedDeliveries: number;
  failedDeliveries: number;
  avgDeliveryTime: number;
  totalDistance: number;
  rating: number;
}

export interface SummaryStatsResponse {
  period: string;
  totalJourneys: number;
  completedJourneys: number;
  totalDistance: number;
  totalDuration: number;
  successRate: number;
  avgDeliveryTime: number;
}

export interface FailureReasonResponse {
  reason: string;
  count: number;
  percentage: number;
}

class ReportService {
  // Driver performans verileri
  async getDriverPerformance(from?: Date, to?: Date): Promise<DriverPerformanceResponse[]> {
    try {
      const params = new URLSearchParams();
      if (from) params.append('from', from.toISOString());
      if (to) params.append('to', to.toISOString());
      
      const url = `/reports/driver-performance${params.toString() ? `?${params}` : ''}`;
      const response = await api.get<DriverPerformanceResponse[]>(url);
      return response.data;
    } catch (error) {
      console.error('Get driver performance error:', error);
      return [];
    }
  }

  // Özet istatistikler
  async getSummaryStats(period: string = 'month'): Promise<SummaryStatsResponse | null> {
    try {
      const response = await api.get<SummaryStatsResponse>(`/reports/summary?period=${period}`);
      return response.data;
    } catch (error) {
      console.error('Get summary stats error:', error);
      return null;
    }
  }

  // Teslimat trendleri
  async getDeliveryTrends(days: number = 7): Promise<any[]> {
    try {
      const response = await api.get(`/reports/delivery-trends?days=${days}`);
      return response.data;
    } catch (error) {
      console.error('Get delivery trends error:', error);
      return [];
    }
  }

  // Başarısızlık nedenleri
  async getFailureReasons(period: string = 'month', from?: Date, to?: Date): Promise<FailureReasonResponse[]> {
    try {
      const params = new URLSearchParams();
      params.append('period', period);
      if (from) params.append('from', from.toISOString());
      if (to) params.append('to', to.toISOString());
      
      const url = `/reports/failure-reasons${params.toString() ? `?${params}` : ''}`;
      const response = await api.get<FailureReasonResponse[]>(url);
      return response.data;
    } catch (error) {
      console.error('Get failure reasons error:', error);
      return [];
    }
  }
}

export default new ReportService();
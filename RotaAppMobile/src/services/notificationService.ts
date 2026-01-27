// C:\Projects\RotaAppMobile\src\services\notificationService.ts

import api from './api';

export interface Notification {
  id: string; // Guid as string
  userId: string; // Guid as string
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  data?: string;
}

export enum NotificationType {
  JOURNEY_ASSIGNED = 'JOURNEY_ASSIGNED',
  JOURNEY_STATUS_CHANGED = 'JOURNEY_STATUS_CHANGED',
  SYSTEM_ANNOUNCEMENT = 'SYSTEM_ANNOUNCEMENT',
  ROUTE_CANCELLED = 'ROUTE_CANCELLED',
  DEADLINE_REMINDER = 'DEADLINE_REMINDER',
  PERFORMANCE_ALERT = 'PERFORMANCE_ALERT',
  MAINTENANCE_REMINDER = 'MAINTENANCE_REMINDER',
  CUSTOMER_FEEDBACK = 'CUSTOMER_FEEDBACK'
}

interface GetNotificationsParams {
  page?: number;
  pageSize?: number;
  isRead?: boolean;
  type?: NotificationType;
}

interface NotificationsResponse {
  notifications: Notification[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

interface UnreadCountResponse {
  count: number;
}

class NotificationService {
  async getNotifications(params: GetNotificationsParams = {}): Promise<NotificationsResponse> {
    try {
      const { page = 1, pageSize = 20, isRead, type } = params;
      const queryParams = new URLSearchParams();
      
      queryParams.append('page', page.toString());
      queryParams.append('pageSize', pageSize.toString());
      
      if (isRead !== undefined) {
        queryParams.append('isRead', isRead.toString());
      }
      
      if (type) {
        queryParams.append('type', type);
      }
      
      const response = await api.get(`/notifications?${queryParams.toString()}`);
      return response.data;
    } catch (error) {
      console.error('Bildirimler alınamadı:', error);
      throw error;
    }
  }
  
  async getUnreadCount(): Promise<number> {
    try {
      const response = await api.get<UnreadCountResponse>('/notifications/unread-count');
      return response.data.count;
    } catch (error) {
      console.error('Okunmamış bildirim sayısı alınamadı:', error);
      return 0;
    }
  }
  
  async markAsRead(notificationId: string): Promise<void> {
    try {
      await api.put(`/notifications/${notificationId}/mark-read`);
    } catch (error) {
      console.error('Bildirim okundu olarak işaretlenemedi:', error);
      throw error;
    }
  }
  
  async markAllAsRead(): Promise<void> {
    try {
      await api.put('/notifications/mark-all-read');
    } catch (error) {
      console.error('Tüm bildirimler okundu olarak işaretlenemedi:', error);
      throw error;
    }
  }
  
  async deleteNotification(notificationId: string): Promise<void> {
    try {
      await api.delete(`/notifications/${notificationId}`);
    } catch (error) {
      console.error('Bildirim silinemedi:', error);
      throw error;
    }
  }
  
  async clearReadNotifications(): Promise<void> {
    try {
      await api.delete('/notifications/clear-read');
    } catch (error) {
      console.error('Okunmuş bildirimler temizlenemedi:', error);
      throw error;
    }
  }
  
  async updateBadgeCount(): Promise<number> {
    const count = await this.getUnreadCount();
    console.log('Okunmamış bildirim sayısı:', count);
    return count;
  }
  
  // Legacy methods for backward compatibility - these now use the API
  async addNotification(type: 'route_assigned' | 'route_cancelled', routeData: any) {
    console.warn('addNotification is deprecated - notifications are now created automatically by the backend');
    return null;
  }
}

export default new NotificationService();
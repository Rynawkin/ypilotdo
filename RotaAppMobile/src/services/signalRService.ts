// C:\Projects\RotaAppMobile\src\services\signalRService.ts

import AsyncStorage from '@react-native-async-storage/async-storage';

interface SignalRNotification {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
  data?: string;
}

type NotificationHandler = (notification: SignalRNotification) => void;

class SignalRService {
  private connection: any = null;
  private notificationHandlers: NotificationHandler[] = [];
  private isConnecting = false;
  private reconnectTimer: any = null;
  private reconnectAttempts = 0;
  private readonly maxReconnectAttempts = 5;

  async connect(): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      return;
    }

    if (this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      // Polyfill for React Native
      if (typeof (global as any).WebSocket === 'undefined') {
        (global as any).WebSocket = require('react-native/Libraries/WebSocket/WebSocket');
      }

      // Note: This is a placeholder implementation. 
      // To use SignalR in React Native, you'll need to install @microsoft/signalr
      // and use it properly. This is a simplified version.
      
      const token = await AsyncStorage.getItem('bearerToken');
      if (!token) {
        console.warn('No bearer token found for SignalR connection');
        this.isConnecting = false;
        return;
      }

      // For now, we'll use a simplified WebSocket-like approach
      // In production, you should use @microsoft/signalr properly
      console.log('SignalR connection placeholder - would connect to /hubs/notifications');
      this.isConnecting = false;
      
    } catch (error) {
      console.error('SignalR connection failed:', error);
      this.isConnecting = false;
      this.scheduleReconnect();
    }
  }

  disconnect(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.connection) {
      this.connection = null;
    }
    
    this.reconnectAttempts = 0;
    console.log('SignalR disconnected');
  }

  private scheduleReconnect(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.warn('Max reconnect attempts reached');
      return;
    }

    const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
    this.reconnectAttempts++;

    console.log(`Scheduling SignalR reconnect attempt ${this.reconnectAttempts} in ${delay}ms`);
    
    this.reconnectTimer = setTimeout(() => {
      this.connect();
    }, delay);
  }

  onNotification(handler: NotificationHandler): () => void {
    this.notificationHandlers.push(handler);
    
    return () => {
      const index = this.notificationHandlers.indexOf(handler);
      if (index > -1) {
        this.notificationHandlers.splice(index, 1);
      }
    };
  }

  private handleNotification(notification: SignalRNotification): void {
    this.notificationHandlers.forEach(handler => {
      try {
        handler(notification);
      } catch (error) {
        console.error('Error in notification handler:', error);
      }
    });
  }

  getConnectionState(): string {
    return this.connection?.state || 'Disconnected';
  }
}

export default new SignalRService();
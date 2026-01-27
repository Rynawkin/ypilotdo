import NetInfo, { NetInfoState } from '@react-native-community/netinfo';

type NetworkListener = (isConnected: boolean) => void;
type EventListener = () => void;

class NetworkService {
  private isConnected: boolean = true;
  private connectionType: string = 'unknown';
  private unsubscribe: (() => void) | null = null;
  private listeners: {
    connectionChange: NetworkListener[];
    reconnected: EventListener[];
    disconnected: EventListener[];
  } = {
    connectionChange: [],
    reconnected: [],
    disconnected: []
  };

  constructor() {
    this.initialize();
  }

  private initialize = () => {
    // Network durumunu dinle
    this.unsubscribe = NetInfo.addEventListener((state: NetInfoState) => {
      const wasConnected = this.isConnected;
      this.isConnected = state.isConnected ?? false;
      this.connectionType = state.type;

      // Bağlantı durumu değişti
      if (wasConnected !== this.isConnected) {
        this.emit('connectionChange', this.isConnected);
        
        if (this.isConnected && !wasConnected) {
          // Yeniden bağlandı
          this.emit('reconnected');
        } else if (!this.isConnected && wasConnected) {
          // Bağlantı kesildi
          this.emit('disconnected');
        }
      }
    });

    // İlk durumu kontrol et
    NetInfo.fetch().then((state) => {
      this.isConnected = state.isConnected ?? false;
      this.connectionType = state.type;
    });
  };

  // Event listener ekle
  public on(event: 'connectionChange', listener: NetworkListener): void;
  public on(event: 'reconnected' | 'disconnected', listener: EventListener): void;
  public on(event: string, listener: any): void {
    if (event === 'connectionChange') {
      this.listeners.connectionChange.push(listener);
    } else if (event === 'reconnected') {
      this.listeners.reconnected.push(listener);
    } else if (event === 'disconnected') {
      this.listeners.disconnected.push(listener);
    }
  }

  // Event listener kaldır
  public removeListener(event: 'connectionChange', listener: NetworkListener): void;
  public removeListener(event: 'reconnected' | 'disconnected', listener: EventListener): void;
  public removeListener(event: string, listener: any): void {
    if (event === 'connectionChange') {
      const index = this.listeners.connectionChange.indexOf(listener);
      if (index > -1) {
        this.listeners.connectionChange.splice(index, 1);
      }
    } else if (event === 'reconnected') {
      const index = this.listeners.reconnected.indexOf(listener);
      if (index > -1) {
        this.listeners.reconnected.splice(index, 1);
      }
    } else if (event === 'disconnected') {
      const index = this.listeners.disconnected.indexOf(listener);
      if (index > -1) {
        this.listeners.disconnected.splice(index, 1);
      }
    }
  }

  // Event emit et
  private emit(event: 'connectionChange', isConnected: boolean): void;
  private emit(event: 'reconnected' | 'disconnected'): void;
  private emit(event: string, ...args: any[]): void {
    if (event === 'connectionChange') {
      this.listeners.connectionChange.forEach(listener => listener(args[0]));
    } else if (event === 'reconnected') {
      this.listeners.reconnected.forEach(listener => listener());
    } else if (event === 'disconnected') {
      this.listeners.disconnected.forEach(listener => listener());
    }
  }

  public getIsConnected = (): boolean => {
    return this.isConnected;
  };

  public getConnectionType = (): string => {
    return this.connectionType;
  };

  public checkConnection = async (): Promise<boolean> => {
    const state = await NetInfo.fetch();
    this.isConnected = state.isConnected ?? false;
    this.connectionType = state.type;
    return this.isConnected;
  };

  public cleanup = () => {
    if (this.unsubscribe) {
      this.unsubscribe();
      this.unsubscribe = null;
    }
    // Tüm listener'ları temizle
    this.listeners.connectionChange = [];
    this.listeners.reconnected = [];
    this.listeners.disconnected = [];
  };
}

export default new NetworkService();
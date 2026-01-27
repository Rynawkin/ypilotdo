import api from './api';
import { 
  Driver, 
  CreateDriverRequest, 
  UpdateDriverRequest,
  UpdateDriverStatusRequest,
  BulkDriverImportRequest,
  BulkDriverImportResponse,
  CreateUserResponse
} from '../types/driver.types';

class DriverService {
  private readonly basePath = '/workspace/drivers';

  // Get all drivers
  async getAll(): Promise<Driver[]> {
    try {
      const response = await api.get<Driver[]>(this.basePath);
      return response.data;
    } catch (error) {
      console.error('Error fetching drivers:', error);
      throw error;
    }
  }

  // Get driver by ID
  async getById(id: number): Promise<Driver | null> {
    try {
      const response = await api.get<Driver>(`${this.basePath}/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching driver ${id}:`, error);
      return null;
    }
  }

  // Create new driver
  async create(data: CreateDriverRequest): Promise<CreateUserResponse> {
    try {
      const response = await api.post<CreateUserResponse>(this.basePath, data);
      return response.data;
    } catch (error) {
      console.error('Error creating driver:', error);
      throw error;
    }
  }

  // Update driver
  async update(id: number, data: UpdateDriverRequest): Promise<Driver> {
    try {
      const response = await api.put<Driver>(`${this.basePath}/${id}`, data);
      return response.data;
    } catch (error) {
      console.error(`Error updating driver ${id}:`, error);
      throw error;
    }
  }

  // Delete driver
  async delete(id: number): Promise<void> {
    try {
      await api.delete(`${this.basePath}/${id}`);
    } catch (error) {
      console.error(`Error deleting driver ${id}:`, error);
      throw error;
    }
  }

  // Update driver status
  async updateStatus(id: number, status: 'active' | 'inactive' | 'on_leave'): Promise<Driver> {
    try {
      const response = await api.patch<Driver>(`${this.basePath}/${id}/status`, { status });
      return response.data;
    } catch (error) {
      console.error(`Error updating driver ${id} status:`, error);
      throw error;
    }
  }


  // Bulk import drivers
  async bulkImport(drivers: CreateDriverRequest[]): Promise<BulkDriverImportResponse> {
    try {
      const data: BulkDriverImportRequest = { drivers };
      const response = await api.post<BulkDriverImportResponse>(`${this.basePath}/bulk`, data);
      return response.data;
    } catch (error) {
      console.error('Error bulk importing drivers:', error);
      throw error;
    }
  }

  // Get available drivers (status = active)
  async getAvailable(): Promise<Driver[]> {
    try {
      const drivers = await this.getAll();
      return drivers.filter(d => d.status === 'active');
    } catch (error) {
      console.error('Error fetching available drivers:', error);
      throw error;
    }
  }

  // Get drivers by status
  async getByStatus(status: 'active' | 'inactive' | 'on_leave'): Promise<Driver[]> {
    try {
      const drivers = await this.getAll();
      return drivers.filter(d => d.status === status);
    } catch (error) {
      console.error('Error fetching drivers by status:', error);
      throw error;
    }
  }

  // Search drivers (client-side filtering)
  async search(query: string): Promise<Driver[]> {
    try {
      const drivers = await this.getAll();
      const lowerQuery = query.toLowerCase();
      return drivers.filter(d => 
        d.name.toLowerCase().includes(lowerQuery) ||
        d.phone.toLowerCase().includes(lowerQuery) ||
        d.licenseNumber.toLowerCase().includes(lowerQuery) ||
        (d.email && d.email.toLowerCase().includes(lowerQuery))
      );
    } catch (error) {
      console.error('Error searching drivers:', error);
      throw error;
    }
  }

  // Get status label for display
  getStatusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'Aktif';
      case 'inactive':
        return 'Pasif';
      case 'on_leave':
        return 'İzinli';
      default:
        return status;
    }
  }

  // Get status color for display
  getStatusColor(status: string): string {
    switch (status) {
      case 'active':
        return '#10B981'; // green
      case 'inactive':
        return '#6B7280'; // gray
      case 'on_leave':
        return '#F59E0B'; // orange
      default:
        return '#6B7280';
    }
  }

  // Validate phone number (10 digits)
  validatePhone(phone: string): boolean {
    const phoneRegex = /^[0-9]{10}$/;
    return phoneRegex.test(phone.replace(/\D/g, ''));
  }

  // Validate email format
  validateEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  // Format phone number for display
  formatPhone(phone: string): string {
    const cleaned = phone.replace(/\D/g, '');
    if (cleaned.length === 10) {
      return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
    }
    return phone;
  }

  // Get full driver name
  getFullName(driver: Driver): string {
    return `${driver.firstName} ${driver.lastName}`.trim();
  }

  // Check if driver has vehicle assigned
  hasVehicleAssigned(driver: Driver): boolean {
    return !!(driver.vehicleId && driver.vehicle);
  }

  // Check if driver license is expired
  isLicenseExpired(driver: Driver): boolean {
    if (!driver.licenseExpiry) return false;
    const today = new Date();
    const expiryDate = new Date(driver.licenseExpiry);
    return expiryDate < today;
  }

  // Check if license expires soon (within 30 days)
  isLicenseExpiringSoon(driver: Driver): boolean {
    if (!driver.licenseExpiry) return false;
    const today = new Date();
    const expiryDate = new Date(driver.licenseExpiry);
    const thirtyDaysFromNow = new Date(today.getTime() + (30 * 24 * 60 * 60 * 1000));
    return expiryDate <= thirtyDaysFromNow && expiryDate >= today;
  }

  // Format license expiry date
  formatLicenseExpiry(driver: Driver): string {
    if (!driver.licenseExpiry) return 'Belirtilmemiş';
    const date = new Date(driver.licenseExpiry);
    return date.toLocaleDateString('tr-TR');
  }

  // Get driver activity status
  getActivityStatus(driver: Driver): 'online' | 'offline' | 'idle' {
    if (!driver.lastActivityAt) return 'offline';
    
    const now = new Date();
    const lastActivity = new Date(driver.lastActivityAt);
    const diffMinutes = (now.getTime() - lastActivity.getTime()) / (1000 * 60);
    
    if (diffMinutes < 5) return 'online';
    if (diffMinutes < 30) return 'idle';
    return 'offline';
  }

  // Format last activity time
  formatLastActivity(driver: Driver): string {
    if (!driver.lastActivityAt) return 'Hiçbir zaman';
    
    const now = new Date();
    const lastActivity = new Date(driver.lastActivityAt);
    const diffMs = now.getTime() - lastActivity.getTime();
    const diffMinutes = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);
    
    if (diffMinutes < 1) return 'Şimdi';
    if (diffMinutes < 60) return `${diffMinutes} dakika önce`;
    if (diffHours < 24) return `${diffHours} saat önce`;
    if (diffDays < 7) return `${diffDays} gün önce`;
    
    return lastActivity.toLocaleDateString('tr-TR');
  }
}

export default new DriverService();
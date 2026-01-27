import api from './api';
import { 
  Vehicle, 
  CreateVehicleRequest, 
  UpdateVehicleRequest,
  UpdateVehicleStatusRequest 
} from '../types/vehicle.types';

class VehicleService {
  private readonly basePath = '/workspace/vehicles';

  // Get all vehicles
  async getAll(): Promise<Vehicle[]> {
    try {
      const response = await api.get<Vehicle[]>(this.basePath);
      return response.data;
    } catch (error) {
      console.error('Error fetching vehicles:', error);
      throw error;
    }
  }

  // Get vehicle by ID
  async getById(id: number): Promise<Vehicle | null> {
    try {
      const response = await api.get<Vehicle>(`${this.basePath}/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Error fetching vehicle ${id}:`, error);
      return null;
    }
  }

  // Create new vehicle
  async create(data: CreateVehicleRequest): Promise<Vehicle> {
    try {
      const response = await api.post<Vehicle>(this.basePath, data);
      return response.data;
    } catch (error) {
      console.error('Error creating vehicle:', error);
      throw error;
    }
  }

  // Update vehicle
  async update(id: number, data: UpdateVehicleRequest): Promise<Vehicle> {
    try {
      const response = await api.put<Vehicle>(`${this.basePath}/${id}`, data);
      return response.data;
    } catch (error) {
      console.error(`Error updating vehicle ${id}:`, error);
      throw error;
    }
  }

  // Delete vehicle
  async delete(id: number): Promise<void> {
    try {
      await api.delete(`${this.basePath}/${id}`);
    } catch (error) {
      console.error(`Error deleting vehicle ${id}:`, error);
      throw error;
    }
  }

  // Update vehicle status
  async updateStatus(id: number, status: 'active' | 'maintenance' | 'inactive'): Promise<Vehicle> {
    try {
      const response = await api.put<Vehicle>(`${this.basePath}/${id}/status`, { status });
      return response.data;
    } catch (error) {
      console.error(`Error updating vehicle ${id} status:`, error);
      throw error;
    }
  }

  // Get available vehicles (status = active)
  async getAvailable(): Promise<Vehicle[]> {
    try {
      const vehicles = await this.getAll();
      return vehicles.filter(v => v.status === 'active');
    } catch (error) {
      console.error('Error fetching available vehicles:', error);
      throw error;
    }
  }

  // Get vehicles by type
  async getByType(type: 'car' | 'van' | 'truck' | 'motorcycle'): Promise<Vehicle[]> {
    try {
      const vehicles = await this.getAll();
      return vehicles.filter(v => v.type === type);
    } catch (error) {
      console.error('Error fetching vehicles by type:', error);
      throw error;
    }
  }

  // Get vehicles in maintenance
  async getInMaintenance(): Promise<Vehicle[]> {
    try {
      const vehicles = await this.getAll();
      return vehicles.filter(v => v.status === 'maintenance');
    } catch (error) {
      console.error('Error fetching vehicles in maintenance:', error);
      throw error;
    }
  }

  // Search vehicles (client-side filtering)
  async search(query: string): Promise<Vehicle[]> {
    try {
      const vehicles = await this.getAll();
      const lowerQuery = query.toLowerCase();
      return vehicles.filter(v => 
        v.plateNumber.toLowerCase().includes(lowerQuery) ||
        v.brand.toLowerCase().includes(lowerQuery) ||
        v.model.toLowerCase().includes(lowerQuery)
      );
    } catch (error) {
      console.error('Error searching vehicles:', error);
      throw error;
    }
  }

  // Get vehicle type label for display
  getVehicleTypeLabel(type: string): string {
    switch (type) {
      case 'car':
        return 'Otomobil';
      case 'van':
        return 'Panelvan';
      case 'truck':
        return 'Kamyon';
      case 'motorcycle':
        return 'Motosiklet';
      default:
        return type;
    }
  }

  // Get fuel type label for display
  getFuelTypeLabel(fuelType: string): string {
    switch (fuelType) {
      case 'gasoline':
        return 'Benzin';
      case 'diesel':
        return 'Dizel';
      case 'electric':
        return 'Elektrik';
      case 'hybrid':
        return 'Hibrit';
      default:
        return fuelType;
    }
  }

  // Get status label for display
  getStatusLabel(status: string): string {
    switch (status) {
      case 'active':
        return 'Aktif';
      case 'maintenance':
        return 'Bakımda';
      case 'inactive':
        return 'Pasif';
      default:
        return status;
    }
  }

  // Validate Turkish plate number format (34 ABC 123)
  validatePlateNumber(plateNumber: string): boolean {
    const turkishPlateRegex = /^(0[1-9]|[1-7][0-9]|8[01])\s[A-Z]{2,3}\s\d{2,4}$/;
    return turkishPlateRegex.test(plateNumber.toUpperCase());
  }

  // Format plate number to Turkish standard
  formatPlateNumber(plateNumber: string): string {
    const cleanPlate = plateNumber.replace(/\s+/g, '').toUpperCase();
    
    // Extract city code, letters, and numbers using regex
    const match = cleanPlate.match(/^(\d{2})([A-Z]{2,3})(\d{2,4})$/);
    
    if (match) {
      const [, cityCode, letters, numbers] = match;
      return `${cityCode} ${letters} ${numbers}`;
    }
    
    return plateNumber; // Return original if format doesn't match
  }
}

export default new VehicleService();
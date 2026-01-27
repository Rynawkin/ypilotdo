// Vehicle interface matching the web frontend
export interface Vehicle {
  id: number;
  plateNumber: string;
  type: 'car' | 'van' | 'truck' | 'motorcycle';
  brand: string;
  model: string;
  year: number;
  capacity: number;
  status: 'active' | 'maintenance' | 'inactive';
  fuelType: 'gasoline' | 'diesel' | 'electric' | 'hybrid';
  createdAt?: Date;
  updatedAt?: Date;
}

// Request DTOs for creating and updating vehicles
export interface CreateVehicleRequest {
  plateNumber: string;
  type: 'car' | 'van' | 'truck' | 'motorcycle';
  brand: string;
  model: string;
  year: number;
  capacity: number;
  status?: 'active' | 'maintenance' | 'inactive';
  fuelType: 'gasoline' | 'diesel' | 'electric' | 'hybrid';
}

export interface UpdateVehicleRequest {
  plateNumber?: string;
  type?: 'car' | 'van' | 'truck' | 'motorcycle';
  brand?: string;
  model?: string;
  year?: number;
  capacity?: number;
  status?: 'active' | 'maintenance' | 'inactive';
  fuelType?: 'gasoline' | 'diesel' | 'electric' | 'hybrid';
}

// Type for vehicle status updates
export interface UpdateVehicleStatusRequest {
  status: 'active' | 'maintenance' | 'inactive';
}

// Vehicle type helper types for filtering
export type VehicleType = 'car' | 'van' | 'truck' | 'motorcycle' | 'all';
export type VehicleStatus = 'active' | 'maintenance' | 'inactive' | 'all';
export type FuelType = 'gasoline' | 'diesel' | 'electric' | 'hybrid';

// Vehicle filter interface for search
export interface VehicleFilter {
  search?: string;
  type?: VehicleType;
  status?: VehicleStatus;
  fuelType?: FuelType;
}

// Vehicle form validation schema
export interface VehicleFormData {
  plateNumber: string;
  type: 'car' | 'van' | 'truck' | 'motorcycle';
  brand: string;
  model: string;
  year: string;
  capacity: string;
  status: 'active' | 'maintenance' | 'inactive';
  fuelType: 'gasoline' | 'diesel' | 'electric' | 'hybrid';
}

// Vehicle validation errors
export interface VehicleFormErrors {
  plateNumber?: string;
  type?: string;
  brand?: string;
  model?: string;
  year?: string;
  capacity?: string;
  status?: string;
  fuelType?: string;
}
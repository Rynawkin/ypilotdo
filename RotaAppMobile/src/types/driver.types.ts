export interface ApplicationUser {
  id: string;
  email: string;
  name: string;
  role: 'admin' | 'dispatcher' | 'driver';
  workspaceId: string;
  isActive: boolean;
  lastLoginAt?: Date;
  createdAt: Date;
  updatedAt: Date;
}

export interface Driver {
  id: number;
  firstName: string;
  lastName: string;
  name: string; // computed from firstName + lastName
  phone: string;
  email?: string;
  licenseNumber: string;
  licenseExpiry?: Date;
  vehicleId?: number;
  vehicle?: {
    id: number;
    plateNumber: string;
    brand: string;
    model: string;
  };
  emergencyContact?: string;
  emergencyPhone?: string;
  address?: string;
  status: 'active' | 'inactive' | 'on_leave';
  currentLocation?: {
    latitude: number;
    longitude: number;
  };
  avatar?: string;
  rating?: number;
  totalDeliveries?: number;
  avgDeliveryTime?: number;
  lastActivityAt?: Date;
  applicationUserId?: string;
  applicationUser?: ApplicationUser;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateDriverRequest {
  firstName: string;
  lastName: string;
  phone: string;
  email?: string;
  licenseNumber: string;
  licenseExpiry?: Date;
  vehicleId?: number;
  emergencyContact?: string;
  emergencyPhone?: string;
  address?: string;
  status: 'active' | 'inactive' | 'on_leave';
}

export interface UpdateDriverRequest {
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  licenseExpiry?: Date;
  vehicleId?: number;
  emergencyContact?: string;
  emergencyPhone?: string;
  address?: string;
  status: 'active' | 'inactive' | 'on_leave';
}

export interface UpdateDriverStatusRequest {
  status: 'active' | 'inactive' | 'on_leave';
}

export interface DriverFormData {
  firstName: string;
  lastName: string;
  phone: string;
  email: string;
  licenseNumber: string;
  licenseExpiry: string;
  vehicleId: string;
  emergencyContact: string;
  emergencyPhone: string;
  address: string;
  status: 'active' | 'inactive' | 'on_leave';
  sendInviteEmail: boolean;
}

export interface DriverFormErrors {
  firstName?: string;
  lastName?: string;
  phone?: string;
  email?: string;
  licenseNumber?: string;
  licenseExpiry?: string;
  vehicleId?: string;
  emergencyContact?: string;
  emergencyPhone?: string;
  address?: string;
}

export interface DriverPerformanceMetrics {
  totalDeliveries: number;
  completedDeliveries: number;
  failedDeliveries: number;
  avgDeliveryTime: number; // in minutes
  rating: number;
  totalDistance: number; // in km
  totalHours: number;
  onTimeDeliveryRate: number; // percentage
  customerSatisfactionRate: number; // percentage
}

export interface BulkDriverImportRequest {
  drivers: CreateDriverRequest[];
}

export interface BulkDriverImportResponse {
  successCount: number;
  failureCount: number;
  errors: string[];
  createdDrivers: Driver[];
}

export interface SendInviteEmailRequest {
  driverId: number;
  email: string;
}

export interface CreateUserResponse {
  driver: Driver;
  isUserCreated: boolean;
  generatedPassword?: string;
  loginEmail?: string;
  tempPassword?: string;
}
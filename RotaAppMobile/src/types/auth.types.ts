// C:\Projects\RotaAppMobile\src\types\auth.types.ts

export interface User {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  workspaceId: number;
  workspaceName?: string;
  
  // Backend'den gelen boolean rol field'ları
  isAdmin?: boolean;
  isDispatcher?: boolean;
  isDriver?: boolean;
  
  // Diğer opsiyonel field'lar
  driverStatus?: string;
  assignedVehicleId?: number;
  assignedVehicle?: any;
  
  // Eski uyumluluk için (kullanılmıyor olabilir)
  roles?: string[];
  permissions?: string[];
  
  avatarUrl?: string;
  createdAt?: string;
  lastLoginAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  fullName: string;
  phoneNumber?: string;
  companyName: string;
  acceptTerms: boolean;
}

export interface LoginResponse {
  me: User;
  token: string;
  refreshToken?: string;
  expiresIn: number;
  message?: string;
}

export interface RegisterResponse {
  me: User;
  token: string;
  refreshToken?: string;
  message: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken?: string;
  expiresIn: number;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface UpdateProfileRequest {
  fullName?: string;
  phoneNumber?: string;
  avatarUrl?: string;
}

export interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

export interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isDispatcher: boolean;
  isDriver: boolean;
}

export enum UserRole {
  SuperAdmin = 'SuperAdmin',
  Admin = 'Admin',
  Dispatcher = 'Dispatcher',
  Driver = 'Driver',
  Customer = 'Customer'
}

export enum UserStatus {
  Active = 'Active',
  Inactive = 'Inactive',
  Suspended = 'Suspended',
  Pending = 'Pending'
}
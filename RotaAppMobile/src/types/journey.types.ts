// C:\Projects\RotaAppMobile\src\types\journey.types.ts

// Journey Status Enum (Entity seviyesi)
export enum JourneyStatusEnum {
  Planned = 100,
  InProgress = 200,
  Completed = 300,
  Cancelled = 400,
  OnHold = 500
}

// Journey Stop Status Enum
export enum JourneyStopStatus {
  Pending = 'pending',
  InProgress = 'in_progress',
  Completed = 'completed',
  Failed = 'failed',
  Skipped = 'skipped'
}

// Delay Reason Category Enum
export enum DelayReasonCategory {
  Traffic = 1,
  CustomerNotReady = 2,
  VehicleIssue = 3,
  Weather = 4,
  UnloadingDelay = 5,
  RouteChange = 6,
  AccidentArea = 7,
  BreakTime = 8,
  Other = 99
}

// Location Type
export enum LocationType {
  Delivery = 'Delivery',
  Pickup = 'Pickup',
  Both = 'Both'
}

// Order Type
export enum OrderType {
  Standard = 'Standard',
  Express = 'Express',
  Priority = 'Priority'
}

// Customer Response
export interface CustomerResponse {
  id: number;
  code: string;
  name: string;
  address: string;
  phone: string;
  email: string;
  latitude: number;
  longitude: number;
  createdAt: string;
  updatedAt: string;
}

// Driver Response
export interface JourneyDriverResponse {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
}

// Vehicle Response
export interface VehicleResponse {
  id: number;
  plateNumber: string;
  type: string;
  brand: string;
  model: string;
  year: number;
  capacity: number;
  status: string;
  fuelType: string;
  createdAt: string;
  updatedAt?: string;
}

// Depot Response
export interface DepotResponse {
  id: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}

// Start/End Details
export interface StartDetailsResponse {
  startTime: string; // "hh:mm:ss" format
  name: string;
  address: string;
  latitude: number;
  longitude: number;
}

export interface EndDetailsResponse {
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  estimatedArrivalTime?: string; // ✅ EKSIK FIELD EKLENDI - "hh:mm:ss" format
}

// Route Stop Response
export interface RouteStopResponse {
  id: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  notes: string;
  contactFullName: string;
  contactPhone: string;
  contactEmail: string;
  type: string;
  orderType: string;
  order: number;
  proofOfDeliveryRequired: boolean;
  serviceTime: string;
  status: string;
  customerId: number;
  customer?: CustomerResponse;
  isExcluded?: boolean;
  exclusionReason?: string;
}

// Journey Stop Response - ✅ DÜZELTİLDİ
export interface JourneyStopResponse {
  id: number;
  journeyId: number;
  stopId: number;
  routeStopId: number;
  order: number;
  status: string; // JourneyStopStatus string deÄŸeri
  distance: number;
  startAddress: string;
  startLatitude: number;
  startLongitude: number;
  endAddress: string;
  endLatitude: number;
  endLongitude: number;
  estimatedArrivalTime: string; // "hh:mm:ss" format
  estimatedDepartureTime?: string; // "hh:mm:ss" format

  // ✅ YENİ - Original ETA'lar (planlanan zamanlar - analiz için)
  originalEstimatedArrivalTime?: string; // "hh:mm:ss" format
  originalEstimatedDepartureTime?: string; // "hh:mm:ss" format

  arriveBetweenStart?: string;
  arriveBetweenEnd?: string;
  routeStop: RouteStopResponse;
  checkInTime?: string;  // ✅ EKLENDİ
  checkOutTime?: string; // ✅ EKLENDİ
  createdAt?: string;
  isExcluded?: boolean;

  // ✅ YENİ - Delay tracking fields
  delayReasonCategory?: string;
  delayReason?: string;
  newDelay?: number;
  cumulativeDelay?: number;
}

// Journey Status Response
export interface JourneyStatusResponse {
  id: number;
  status: string;
  notes: string;
  createdAt: string;
  failureReason?: string;
  signatureBase64?: string;
  photoBase64?: string;
  signatureUrl?: string;
  photoUrl?: string;
  latitude: number;
  longitude: number;
}

// Route Response
export interface RouteResponse {
  id: number;
  name: string;
  date: string;
  status: string;
  driverId: string;
  vehicleId: string;
  depotId: string;
  totalDistance: number;
  totalDuration: number;
  totalDeliveries: number;
  completedDeliveries: number;
  optimized: boolean;
  notes: string;
  createdAt: string;
  updatedAt: string;
  startedAt?: string;
  completedAt?: string;
  startDetails?: StartDetailsResponse;
  endDetails?: EndDetailsResponse;
  stops?: RouteStopResponse[];
  driver?: JourneyDriverResponse;
  vehicle?: VehicleResponse;
  depot?: DepotResponse;
}

// Live Location Response
export interface LiveLocationResponse {
  latitude: number;
  longitude: number;
  speed?: number;
  heading?: number;
  timestamp: string;
}

// Main Journey Response
export interface JourneyResponse {
  id: number;
  name?: string;
  routeId: number;
  polyline: string;
  date: string;
  assignedAt: string;
  startedAt?: string;
  finishedAt?: string;
  startDetails: StartDetailsResponse;
  endDetails: EndDetailsResponse;
  driver: JourneyDriverResponse;
  stops: JourneyStopResponse[];
  statuses: JourneyStatusResponse[];
  route: RouteResponse;
  totalDistance: number;
  totalDuration: number;
  status: string;
  currentStopIndex: number;
  liveLocation?: LiveLocationResponse;
  needsReoptimization?: boolean; // ✅ YENİ - Optimizasyon gerekiyor mu?
}

// Journey Summary Response (Dashboard iÃ§in)
export interface JourneySummaryResponse {
  totalJourneys: number;
  completedJourneys: number;
  inProgressJourneys: number;
  cancelledJourneys: number;
  plannedJourneys: number;
  totalDistance: number;
  totalDuration: number;
  completionRate: number;
  isDriverView: boolean;
}

// Journey List Query Parameters
export interface JourneyListParams {
  from?: string;
  to?: string;
  status?: string;
  driverId?: number;
  vehicleId?: number;
}

// Stop Action Request Types
export interface CheckInResponse {
  success: boolean;
  requiresDelayReason: boolean;
  newDelay: number;
  cumulativeDelay: number;
  message: string;
}

export interface FailStopRequest {
  failureReason: string;
  notes?: string;
}

export interface CompleteStopRequest {
  notes?: string;
  signatureFile?: any; // FormData ile gÃ¶nderilecek
  photoFile?: any; // FormData ile gÃ¶nderilecek
}

// Bulk Operation Result
export interface BulkOperationResult {
  totalCount: number;
  successCount: number;
  failedCount: number;
  failedIds: number[];
  message: string;
}

// Helper function to get status color
export const getJourneyStatusColor = (status: string): string => {
  switch (status.toLowerCase()) {
    case 'planned':
      return '#3B82F6'; // Blue
    case 'preparing':
      return '#F59E0B'; // Amber
    case 'in_progress':
      return '#10B981'; // Green
    case 'completed':
      return '#6B7280'; // Gray
    case 'cancelled':
      return '#EF4444'; // Red
    case 'on_hold':
      return '#8B5CF6'; // Purple
    default:
      return '#6B7280'; // Gray
  }
};

// Helper function to get stop status color
export const getStopStatusColor = (status: string): string => {
  switch (status.toLowerCase()) {
    case 'pending':
      return '#9CA3AF'; // Gray-400
    case 'in_progress':
      return '#3B82F6'; // Blue
    case 'completed':
      return '#10B981'; // Green
    case 'failed':
      return '#EF4444'; // Red
    case 'skipped':
      return '#F59E0B'; // Amber
    default:
      return '#9CA3AF'; // Gray-400
  }
};

// Helper function to format time
export const formatTime = (timeString: string): string => {
  if (!timeString) return '--:--';
  // "hh:mm:ss" formatÄ±ndan "HH:MM" formatÄ±na Ã§evir
  const parts = timeString.split(':');
  if (parts.length >= 2) {
    return `${parts[0]}:${parts[1]}`;
  }
  return timeString;
};

// Helper function to calculate progress
export const calculateJourneyProgress = (stops: JourneyStopResponse[]): number => {
  if (!stops || stops.length === 0) return 0;

  const completedStops = stops.filter(
    s => s.status === 'completed' || s.status === 'failed' || s.status === 'skipped'
  ).length;

  return Math.round((completedStops / stops.length) * 100);
};

// Helper function to get delay reason category label
export const getDelayReasonLabel = (category: DelayReasonCategory): string => {
  switch (category) {
    case DelayReasonCategory.Traffic:
      return 'Trafik Yoğunluğu';
    case DelayReasonCategory.CustomerNotReady:
      return 'Müşteri Hazır Değil';
    case DelayReasonCategory.VehicleIssue:
      return 'Araç Arızası';
    case DelayReasonCategory.Weather:
      return 'Hava Koşulları';
    case DelayReasonCategory.UnloadingDelay:
      return 'Yükleme/Boşaltma Gecikmesi';
    case DelayReasonCategory.RouteChange:
      return 'Rota Değişikliği';
    case DelayReasonCategory.AccidentArea:
      return 'Kaza Bölgesi';
    case DelayReasonCategory.BreakTime:
      return 'Yemek Molası';
    case DelayReasonCategory.Other:
      return 'Diğer';
    default:
      return 'Bilinmiyor';
  }
};

// Get all delay reason categories with labels
export const getDelayReasonCategories = () => [
  { value: DelayReasonCategory.Traffic, label: 'Trafik Yoğunluğu', icon: 'traffic-light' },
  { value: DelayReasonCategory.CustomerNotReady, label: 'Müşteri Hazır Değil', icon: 'account-clock' },
  { value: DelayReasonCategory.VehicleIssue, label: 'Araç Arızası', icon: 'car-wrench' },
  { value: DelayReasonCategory.Weather, label: 'Hava Koşulları', icon: 'weather-rainy' },
  { value: DelayReasonCategory.UnloadingDelay, label: 'Yükleme/Boşaltma Gecikmesi', icon: 'package-variant' },
  { value: DelayReasonCategory.RouteChange, label: 'Rota Değişikliği', icon: 'map-marker-path' },
  { value: DelayReasonCategory.AccidentArea, label: 'Kaza Bölgesi', icon: 'car-emergency' },
  { value: DelayReasonCategory.BreakTime, label: 'Yemek Molası', icon: 'food' },
  { value: DelayReasonCategory.Other, label: 'Diğer', icon: 'dots-horizontal' },
];

export interface Customer {
  id: number;
  code: string;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  whatsApp?: string;
  whatsAppOptIn?: boolean;
  whatsAppVerified?: boolean;
  whatsAppOptInDate?: string;
  latitude?: number;
  longitude?: number;
  priority?: 'low' | 'normal' | 'high';
  estimatedServiceTime?: number;
  notes?: string;
  tags?: string[];
  timeWindow?: { start: string; end: string };
  createdAt?: string;
}

export interface CreateCustomerRequest {
  code: string;
  name: string;
  address?: string;
  phone?: string;
  email?: string;
  whatsApp?: string;
  whatsAppOptIn?: boolean;
  whatsAppVerified?: boolean;
  whatsAppOptInDate?: string;
  latitude?: number;
  longitude?: number;
  priority?: 'low' | 'normal' | 'high';
  estimatedServiceTime?: number;
  notes?: string;
  tags?: string[];
  timeWindow?: { start: string; end: string };
}
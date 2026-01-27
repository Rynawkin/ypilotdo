// C:\Projects\RotaAppMobile\src\services\depotService.ts

import api from './api';

export interface WorkingHour {
  open: string;
  close: string;
}

export interface Depot {
  id: number;
  workspaceId: number;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  isDefault: boolean;
  workingHours?: Record<string, WorkingHour>;
  startWorkingHours?: string;
  endWorkingHours?: string;
  createdAt: string;
  updatedAt?: string;
}

class DepotService {
  async getAll(): Promise<Depot[]> {
    try {
      const response = await api.get('/workspace/depots');
      return response.data;
    } catch (error) {
      console.error('Error fetching depots:', error);
      throw error;
    }
  }

  async getDefault(): Promise<Depot | null> {
    try {
      const depots = await this.getAll();
      return depots.find(d => d.isDefault) || depots[0] || null;
    } catch (error) {
      console.error('Error fetching default depot:', error);
      return null;
    }
  }

  async getById(id: number): Promise<Depot> {
    const response = await api.get(`/workspace/depots/${id}`);
    return response.data;
  }

  async create(data: Partial<Depot>): Promise<Depot> {
    const payload = {
      name: data.name,
      address: data.address,
      latitude: data.latitude,
      longitude: data.longitude,
      isDefault: data.isDefault || false,
      workingHours: data.workingHours || buildWorkingHours(data.startWorkingHours, data.endWorkingHours)
    };
    const response = await api.post('/workspace/depots', payload);
    return response.data;
  }

  async update(id: number, data: Partial<Depot>): Promise<Depot> {
    const payload = {
      name: data.name,
      address: data.address,
      latitude: data.latitude,
      longitude: data.longitude,
      isDefault: data.isDefault || false,
      workingHours: data.workingHours || buildWorkingHours(data.startWorkingHours, data.endWorkingHours)
    };
    const response = await api.put(`/workspace/depots/${id}`, payload);
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await api.delete(`/workspace/depots/${id}`);
  }

  async setDefault(id: number): Promise<Depot> {
    const response = await api.post(`/workspace/depots/${id}/set-default`);
    return response.data;
  }
}

export default new DepotService();

const buildWorkingHours = (start?: string, end?: string) => {
  const open = start || '08:00';
  const close = end || '18:00';
  return {
    monday: { open, close },
    tuesday: { open, close },
    wednesday: { open, close },
    thursday: { open, close },
    friday: { open, close },
    saturday: { open: '09:00', close: '14:00' },
    sunday: { open: 'closed', close: 'closed' }
  };
};

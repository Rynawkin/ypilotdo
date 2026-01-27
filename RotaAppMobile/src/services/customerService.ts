// C:\Projects\RotaAppMobile\src\services\customerService.ts

import api from './api';
import { Customer, CreateCustomerRequest } from '../types/customer.types';

class CustomerService {
  async getAll(): Promise<Customer[]> {
    try {
      const response = await api.get('/workspace/customers');
      return response.data;
    } catch (error) {
      console.error('Error fetching customers:', error);
      throw error;
    }
  }

  async getById(id: number): Promise<Customer> {
    try {
      const response = await api.get(`/workspace/customers/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching customer:', error);
      throw error;
    }
  }

  async search(query: string): Promise<Customer[]> {
    try {
      const response = await api.get('/workspace/customers/search', {
        params: { q: query }
      });
      return response.data;
    } catch (error) {
      console.error('Error searching customers:', error);
      throw error;
    }
  }

  async create(data: CreateCustomerRequest): Promise<Customer> {
    try {
      const response = await api.post('/workspace/customers', data);
      return response.data;
    } catch (error) {
      console.error('Error creating customer:', error);
      throw error;
    }
  }

  async update(id: number, data: CreateCustomerRequest): Promise<Customer> {
    try {
      const response = await api.put(`/workspace/customers/${id}`, data);
      return response.data;
    } catch (error) {
      console.error('Error updating customer:', error);
      throw error;
    }
  }

  async delete(id: number): Promise<void> {
    try {
      await api.delete(`/workspace/customers/${id}`);
    } catch (error) {
      console.error('Error deleting customer:', error);
      throw error;
    }
  }
}

export default new CustomerService();
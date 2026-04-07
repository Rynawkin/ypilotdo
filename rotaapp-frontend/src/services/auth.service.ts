import { api } from './api';

interface MeUserModel {
  id: string;
  email: string;
  fullName?: string;
  phoneNumber?: string;
  workspaceId: number;
  workspaceName?: string;
  isAdmin: boolean;
  isDispatcher: boolean;
  isDriver: boolean;
  isSuperAdmin: boolean;
  isOnboarded: boolean;
  depotId?: number;
}

interface TokenResponse {
  bearerToken: string;
  refreshToken?: string;
  expiresIn?: string;
  me: MeUserModel;
}

export const authService = {
  setSession(response: TokenResponse) {
    if (!response?.bearerToken || !response.me) {
      throw new Error('Geçersiz sunucu yanıtı');
    }

    localStorage.setItem('token', response.bearerToken);
    localStorage.setItem('user', JSON.stringify(response.me));
    localStorage.setItem('isAuthenticated', 'true');

    if (response.refreshToken) {
      localStorage.setItem('refreshToken', response.refreshToken);
    }

    if (response.me.workspaceId) {
      localStorage.setItem('workspaceId', response.me.workspaceId.toString());
    }

    api.defaults.headers.common['Authorization'] = `Bearer ${response.bearerToken}`;
  },

  async login(email: string, password: string): Promise<TokenResponse> {
    try {
      const response = await api.post<TokenResponse>('/me/login', {
        Email: email,
        Password: password
      });

      if (!response.data?.bearerToken || !response.data.me) {
        throw new Error('Geçersiz sunucu yanıtı');
      }

      this.setSession(response.data);
      return response.data;
    } catch (error: any) {
      if (error.response) {
        throw new Error(error.response.data?.message || 'Giriş başarısız');
      }
      if (error.request) {
        throw new Error('Sunucuya bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin.');
      }
      throw new Error(error.message || 'Giriş yapılırken bir hata oluştu');
    }
  },

  async register(
    email: string,
    password: string,
    fullName: string,
    companyName?: string,
    companyEmail?: string,
    companyPhone?: string
  ): Promise<TokenResponse> {
    try {
      const response = await api.post<TokenResponse>('/me/register', {
        Email: email,
        Password: password,
        FullName: fullName,
        CompanyName: companyName,
        CompanyEmail: companyEmail,
        CompanyPhone: companyPhone
      });

      if (!response.data?.bearerToken || !response.data.me) {
        throw new Error('Geçersiz sunucu yanıtı');
      }

      this.setSession(response.data);
      return response.data;
    } catch (error: any) {
      if (error.response) {
        throw new Error(error.response.data?.message || 'Kayıt başarısız');
      }
      if (error.request) {
        throw new Error('Sunucuya bağlanılamıyor');
      }
      throw new Error(error.message || 'Kayıt olurken bir hata oluştu');
    }
  },

  async forgotPassword(email: string): Promise<void> {
    try {
      await api.post('/me/forgot-password', {
        Email: email
      });
    } catch (error: any) {
      if (error.response) {
        throw new Error(error.response.data?.message || 'Şifre sıfırlama bağlantısı gönderilemedi');
      }
      if (error.request) {
        throw new Error('Sunucuya bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin.');
      }
      throw new Error(error.message || 'Şifre sıfırlama işlemi sırasında bir hata oluştu');
    }
  },

  async resetPassword(email: string, token: string, newPassword: string): Promise<void> {
    try {
      await api.post('/me/reset-password', {
        Email: email,
        Token: token,
        NewPassword: newPassword
      });
    } catch (error: any) {
      if (error.response) {
        throw new Error(error.response.data?.message || 'Şifre sıfırlama başarısız');
      }
      if (error.request) {
        throw new Error('Sunucuya bağlanılamıyor. Lütfen internet bağlantınızı kontrol edin.');
      }
      throw new Error(error.message || 'Şifre sıfırlama işlemi sırasında bir hata oluştu');
    }
  },

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    localStorage.removeItem('isAuthenticated');
    localStorage.removeItem('workspaceId');
    delete api.defaults.headers.common['Authorization'];
    window.location.href = '/login';
  },

  getUser(): MeUserModel | null {
    try {
      const userStr = localStorage.getItem('user');
      return userStr ? JSON.parse(userStr) : null;
    } catch {
      return null;
    }
  },

  isAuthenticated(): boolean {
    const token = localStorage.getItem('token');
    const isAuth = localStorage.getItem('isAuthenticated');
    return !!(token && isAuth === 'true');
  },

  getToken(): string | null {
    return localStorage.getItem('token');
  },

  getWorkspaceId(): string | null {
    return localStorage.getItem('workspaceId');
  },

  getUserRole(): string {
    const user = this.getUser();
    if (!user) return 'guest';
    if (user.isSuperAdmin) return 'superadmin';
    if (user.isAdmin) return 'admin';
    if (user.isDispatcher) return 'dispatcher';
    if (user.isDriver) return 'driver';
    return 'user';
  },

  isSuperAdmin(): boolean {
    return this.getUser()?.isSuperAdmin === true;
  },

  isAdmin(): boolean {
    const user = this.getUser();
    return user?.isAdmin === true || user?.isSuperAdmin === true;
  },

  isDispatcher(): boolean {
    const user = this.getUser();
    return user?.isDispatcher === true || user?.isAdmin === true || user?.isSuperAdmin === true;
  },

  isDriver(): boolean {
    return this.getUser()?.isDriver === true;
  },

  async refreshToken(): Promise<boolean> {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        return false;
      }

      const response = await api.post<TokenResponse>('/me/refresh-token', {
        RefreshToken: refreshToken
      });

      if (!response.data?.bearerToken || !response.data.me) {
        throw new Error('Invalid refresh response');
      }

      this.setSession(response.data);
      return true;
    } catch {
      this.logout();
      return false;
    }
  },

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  },

  async me(): Promise<MeUserModel> {
    const response = await api.get<MeUserModel>('/me');
    return response.data;
  },

  debugAuth() {
    console.log('=== Auth Debug Info ===');
    console.log('Token:', this.getToken());
    console.log('User:', this.getUser());
    console.log('Role:', this.getUserRole());
    console.log('WorkspaceId:', this.getWorkspaceId());
    console.log('Is Authenticated:', this.isAuthenticated());
    console.log('Is SuperAdmin:', this.isSuperAdmin());
    console.log('Is Admin:', this.isAdmin());
    console.log('Is Dispatcher:', this.isDispatcher());
    console.log('Is Driver:', this.isDriver());
    console.log('=====================');
  }
};

export type { MeUserModel, TokenResponse };

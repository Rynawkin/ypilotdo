// C:\Projects\RotaAppMobile\src\services\subscriptionService.ts

import api from './api';

export interface UsageData {
  workspaceId: number;
  workspaceName: string;
  planType: string;
  includedMonthlyStops: number;
  currentMonthStops: number;
  remainingStops: number;
  includedWhatsAppMessages: number;
  currentMonthWhatsAppMessages: number;
  remainingWhatsAppMessages: number;
  currentMonthAdditionalCharges: number;
  estimatedMonthlyTotal: number;
  lastResetDate: string;
  nextResetDate: string;
}

class SubscriptionService {
  // Mevcut kullanım bilgilerini getir
  async getCurrentUsage(): Promise<UsageData> {
    try {
      const response = await api.get('/subscription/usage');
      return response.data;
    } catch (error) {
      console.error('[SubscriptionService] Get current usage error:', error);
      throw error;
    }
  }

  // Billing summary'yi getir (Settings sayfasının çalışan yöntemi)
  async getBillingSummary(): Promise<any> {
    try {
      const response = await api.get('/subscription/billing-summary');
      return response.data;
    } catch (error) {
      console.error('[SubscriptionService] Get billing summary error:', error);
      throw error;
    }
  }

  // Trial limitlerine yaklaşıp yaklaşmadığını kontrol et
  async checkTrialLimits(): Promise<{
    isTrialUser: boolean;
    stopsNearLimit: boolean;
    whatsAppNearLimit: boolean;
    stopsExceeded: boolean;
    whatsAppExceeded: boolean;
    usage?: UsageData;
  }> {
    try {
      // Frontend'in çalışan yöntemini kullan
      const billingData = await this.getBillingSummary();
      console.log('[SubscriptionService] Billing data received:', billingData);
      
      const isTrialUser = billingData.plan.name === 'Trial';
      
      if (!isTrialUser) {
        console.log('[SubscriptionService] Not a trial user:', billingData.plan.name);
        return {
          isTrialUser: false,
          stopsNearLimit: false,
          whatsAppNearLimit: false,
          stopsExceeded: false,
          whatsAppExceeded: false
        };
      }

      const stopsExceeded = billingData.usage.stops.used >= billingData.usage.stops.included;
      const whatsAppExceeded = billingData.usage.whatsApp.used >= billingData.usage.whatsApp.included;
      
      // %80'e yaklaştığında uyarı
      const stopsNearLimit = billingData.usage.stops.used >= (billingData.usage.stops.included * 0.8);
      const whatsAppNearLimit = billingData.usage.whatsApp.used >= (billingData.usage.whatsApp.included * 0.8);

      // billingData'dan UsageData formatına çevir
      const usage: UsageData = {
        workspaceId: 0,
        workspaceName: '',
        planType: billingData.plan.name,
        includedMonthlyStops: billingData.usage.stops.included,
        currentMonthStops: billingData.usage.stops.used,
        remainingStops: billingData.usage.stops.included - billingData.usage.stops.used,
        includedWhatsAppMessages: billingData.usage.whatsApp.included,
        currentMonthWhatsAppMessages: billingData.usage.whatsApp.used,
        remainingWhatsAppMessages: billingData.usage.whatsApp.included - billingData.usage.whatsApp.used,
        currentMonthAdditionalCharges: billingData.summary.additionalCharges,
        estimatedMonthlyTotal: billingData.summary.estimatedTotal,
        lastResetDate: billingData.summary.billingPeriod.start,
        nextResetDate: billingData.summary.billingPeriod.end
      };

      console.log('[SubscriptionService] Trial limit check result:', {
        isTrialUser: true,
        stopsNearLimit: stopsNearLimit && !stopsExceeded,
        whatsAppNearLimit: whatsAppNearLimit && !whatsAppExceeded,
        stopsExceeded,
        whatsAppExceeded,
        stops: `${billingData.usage.stops.used}/${billingData.usage.stops.included}`,
        whatsApp: `${billingData.usage.whatsApp.used}/${billingData.usage.whatsApp.included}`
      });

      return {
        isTrialUser: true,
        stopsNearLimit: stopsNearLimit && !stopsExceeded,
        whatsAppNearLimit: whatsAppNearLimit && !whatsAppExceeded,
        stopsExceeded,
        whatsAppExceeded,
        usage
      };
    } catch (error) {
      console.error('[SubscriptionService] Check trial limits error:', error);
      return {
        isTrialUser: false,
        stopsNearLimit: false,
        whatsAppNearLimit: false,
        stopsExceeded: false,
        whatsAppExceeded: false
      };
    }
  }
}

export default new SubscriptionService();
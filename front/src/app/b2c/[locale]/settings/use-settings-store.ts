import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export enum DistanceUnits {
  Kilometers = 'Kilometers',
  Miles = 'Miles',
}

export interface SettingsDto {
  distanceUnits: DistanceUnits;
  pushesEnabled: boolean;
  emailsEnabled: boolean;
  locale: string;
}

interface SettingsState {
  distanceUnits: DistanceUnits;
  pushesEnabled: boolean;
  emailsEnabled: boolean;
  locale: string;
  serverSettings: SettingsDto | null;
  isLoading: boolean;

  fetchSettings: () => Promise<void>;
  updateDistanceUnits: (units: DistanceUnits) => void;
  updateLocale: (locale: string) => void;
  togglePushes: (enabled: boolean) => void;
  toggleEmails: (enabled: boolean) => void;
  saveSettings: () => Promise<void>;
  reset: () => void;
}

export const useSettingsStore = create<SettingsState>((set, get) => ({
  distanceUnits: DistanceUnits.Kilometers,
  pushesEnabled: false,
  emailsEnabled: false,
  locale: '',
  serverSettings: null,
  isLoading: false,

  fetchSettings: async () => {
    set({ isLoading: true });
    try {
      const [feedSettingsRes, notifSettingsRes] = await Promise.all([
        apiClient.get<
          ApiResult<{ settings: { distanceUnits: DistanceUnits; locale: string } }>
        >(API_ENDPOINTS.FEED.SETTINGS),
        apiClient.get<
          ApiResult<{ pushesEnabled: boolean; emailsEnabled: boolean }>
        >(API_ENDPOINTS.NOTIFICATIONS.SETTINGS),
      ]);

      if (feedSettingsRes.data.success && notifSettingsRes.data.success) {
        const distanceUnits = feedSettingsRes.data.result.settings.distanceUnits;
        const locale = feedSettingsRes.data.result.settings.locale || 'en';
        const { pushesEnabled, emailsEnabled } = notifSettingsRes.data.result;

        set({
          distanceUnits,
          pushesEnabled,
          emailsEnabled,
          locale,
          serverSettings: {
            distanceUnits,
            pushesEnabled,
            emailsEnabled,
            locale,
          },
        });
      }
    } catch (error) {
      rethrowApiError(error, 'Failed to fetch settings');
    } finally {
      set({ isLoading: false });
    }
  },

  updateDistanceUnits: (units: DistanceUnits) => {
    set({ distanceUnits: units });
  },

  updateLocale: (locale: string) => {
    set({ locale });
  },

  togglePushes: (enabled: boolean) => {
    set({ pushesEnabled: enabled });
  },

  toggleEmails: (enabled: boolean) => {
    set({ emailsEnabled: enabled });
  },

  saveSettings: async () => {
    set({ isLoading: true });
    try {
      const { distanceUnits, pushesEnabled, emailsEnabled, locale } = get();

      const promises = [];

      promises.push(
        apiClient.put(API_ENDPOINTS.FEED.SETTINGS, { distanceUnits, locale }),
      );

      promises.push(
        apiClient.put(API_ENDPOINTS.NOTIFICATIONS.SETTINGS, {
          pushesEnabled,
          emailsEnabled,
        }),
      );

      await Promise.all(promises);

      set({
        serverSettings: {
          distanceUnits,
          pushesEnabled,
          emailsEnabled,
          locale,
        },
      });
    } catch (error) {
      rethrowApiError(error, 'Failed to save settings');
    } finally {
      set({ isLoading: false });
    }
  },

  reset: () =>
    set({
      distanceUnits: DistanceUnits.Kilometers,
      pushesEnabled: false,
      emailsEnabled: false,
      locale: '',
      serverSettings: null,
      isLoading: false,
    }),
}));

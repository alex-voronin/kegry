import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export enum DistanceUnits {
  Kilometers = 0,
  Miles = 1,
}

export interface SettingsDto {
  distanceUnits: DistanceUnits;
  pushesEnabled: boolean;
  emailsEnabled: boolean;
}

interface SettingsState {
  distanceUnits: DistanceUnits;
  pushesEnabled: boolean;
  emailsEnabled: boolean;
  serverSettings: SettingsDto | null;
  isLoading: boolean;

  fetchSettings: () => Promise<void>;
  updateDistanceUnits: (units: DistanceUnits) => void;
  togglePushes: (enabled: boolean) => void;
  toggleEmails: (enabled: boolean) => void;
  saveSettings: () => Promise<void>;
  reset: () => void;
}

export const useSettingsStore = create<SettingsState>((set, get) => ({
  distanceUnits: DistanceUnits.Kilometers,
  pushesEnabled: false,
  emailsEnabled: false,
  serverSettings: null,
  isLoading: false,

  fetchSettings: async () => {
    set({ isLoading: true });
    try {
      const [feedSettingsRes, notifSettingsRes] = await Promise.all([
        apiClient.get<
          ApiResult<{ settings: { distanceUnits: DistanceUnits } }>
        >(API_ENDPOINTS.FEED.SETTINGS),
        apiClient.get<
          ApiResult<{ pushesEnabled: boolean; emailsEnabled: boolean }>
        >(API_ENDPOINTS.NOTIFICATIONS.SETTINGS),
      ]);

      if (feedSettingsRes.data.success && notifSettingsRes.data.success) {
        const distanceUnits =
          feedSettingsRes.data.result.settings.distanceUnits;
        const { pushesEnabled, emailsEnabled } = notifSettingsRes.data.result;

        set({
          distanceUnits,
          pushesEnabled,
          emailsEnabled,
          serverSettings: {
            distanceUnits,
            pushesEnabled,
            emailsEnabled,
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

  togglePushes: (enabled: boolean) => {
    set({ pushesEnabled: enabled });
  },

  toggleEmails: (enabled: boolean) => {
    set({ emailsEnabled: enabled });
  },

  saveSettings: async () => {
    set({ isLoading: true });
    try {
      const { distanceUnits, pushesEnabled, emailsEnabled } = get();

      const promises = [];

      promises.push(
        apiClient.put(API_ENDPOINTS.FEED.SETTINGS, { distanceUnits }),
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
      serverSettings: null,
      isLoading: false,
    }),
}));

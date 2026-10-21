import { create } from '@/lib/zustand-reset';
import apiClient from '@/lib/api-client';
import { rethrowApiError } from '@/lib/api-error';
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

export enum SettingsFetchType {
  All = 'All',
  OnlyNotifications = 'OnlyNotifications',
}

interface SettingsState {
  distanceUnits: DistanceUnits;
  pushesEnabled: boolean;
  emailsEnabled: boolean;
  locale: string;
  serverSettings: SettingsDto | null;
  isLoading: boolean;

  fetchSettings: (fetchType?: SettingsFetchType) => Promise<void>;
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

  fetchSettings: async (
    fetchType: SettingsFetchType = SettingsFetchType.All,
  ) => {
    set({ isLoading: true });
    try {
      const fetchFeed = fetchType === SettingsFetchType.All;

      const [feedSettingsRes, notifSettingsRes] = await Promise.all([
        fetchFeed
          ? apiClient.get<{
              settings: { distanceUnits: DistanceUnits };
            }>(API_ENDPOINTS.FEED.SETTINGS)
          : null,
        apiClient.get<{
          pushesEnabled: boolean;
          emailsEnabled: boolean;
          locale: string;
        }>(API_ENDPOINTS.NOTIFICATIONS.SETTINGS),
      ]);

      const newState: Partial<SettingsState> = {};
      const newServerSettings: Partial<SettingsDto> = {
        ...get().serverSettings,
      };

      if (feedSettingsRes?.data) {
        const distanceUnits = feedSettingsRes.data.settings.distanceUnits;
        newState.distanceUnits = distanceUnits;
        newServerSettings.distanceUnits = distanceUnits;
      }

      if (notifSettingsRes?.data) {
        const {
          pushesEnabled,
          emailsEnabled,
          locale: serverLocale,
        } = notifSettingsRes.data;
        const locale = serverLocale || 'en';

        newState.pushesEnabled = pushesEnabled;
        newState.emailsEnabled = emailsEnabled;
        newState.locale = locale;

        newServerSettings.pushesEnabled = pushesEnabled;
        newServerSettings.emailsEnabled = emailsEnabled;
        newServerSettings.locale = locale;
      }

      set({
        ...newState,
        serverSettings: newServerSettings as SettingsDto,
      });
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
        apiClient.put<void>(API_ENDPOINTS.FEED.SETTINGS, { distanceUnits }),
      );

      promises.push(
        apiClient.put<void>(API_ENDPOINTS.NOTIFICATIONS.SETTINGS, {
          pushesEnabled,
          emailsEnabled,
          locale,
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

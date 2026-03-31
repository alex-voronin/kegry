import { create } from 'zustand';

interface NewsData {
  date: string;
  content: string;
}

interface NewsStore {
  data: NewsData[] | null;
  isLoading: boolean;
  error: string | null;
  loadedLocale: string | null;
  fetchNews: (locale: string) => Promise<void>;
}

export const useNewsStore = create<NewsStore>((set, get) => ({
  data: null,
  isLoading: false,
  error: null,
  loadedLocale: null,
  fetchNews: async (locale: string) => {
    // Prevent re-fetching if data is already loaded for THIS locale or loading
    if (get().isLoading || get().loadedLocale === locale) return;

    set({ isLoading: true, error: null });
    try {
      const res = await fetch(`/news/${locale}.json`);
      const data = await res.json();

      if (Array.isArray(data)) {
        set({
          data: data,
          loadedLocale: locale,
          isLoading: false,
        });
      } else {
        set({ error: 'Invalid news format', isLoading: false });
      }
    } catch (err) {
      set({ error: (err as Error).message, isLoading: false });
    }
  },
}));

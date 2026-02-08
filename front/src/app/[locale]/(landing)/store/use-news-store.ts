import { create } from 'zustand';

interface NewsData {
  date: string;
  content: string;
}

interface NewsStore {
  data: NewsData | null;
  isLoading: boolean;
  error: string | null;
  fetchNews: () => Promise<void>;
}

export const useNewsStore = create<NewsStore>((set, get) => ({
  data: null,
  isLoading: false,
  error: null,
  fetchNews: async () => {
    // Prevent re-fetching if data is already loaded or loading
    if (get().data || get().isLoading) return;

    set({ isLoading: true, error: null });
    try {
      const res = await fetch('/news.json');
      const data = await res.json();

      if (data && data.date && data.content) {
        set({
          data: { date: data.date, content: data.content },
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

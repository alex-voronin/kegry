'use client';

import { create } from 'zustand';
import apiClient from '@/lib/api-client';
import { handleApiError } from '@/lib/api-error';
import { ApiResult } from '@/lib/api-types';
import { API_ENDPOINTS } from '@/lib/routes';

export interface ChatParticipantDto {
  id: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
}

export interface ChatMessageDto {
  id: string;
  messageText: string;
  createdAt: string;
  isMine: boolean;
}

export interface ChatDto {
  id: string;
  createdAt: string;
  expiresAt: string;
  maxMessagesPerUser: number;
  participantsMet: boolean;
  participant: ChatParticipantDto;
  messages: ChatMessageDto[];
}

interface ChatState {
  chat: ChatDto | null;
  isLoading: boolean;
  error: string | null;
  showEndMeetingPrompt: boolean;

  fetchChat: () => Promise<void>;
  refreshChat: () => Promise<void>;
  sendMessage: (text: string) => Promise<void>;
  addMessage: (message: ChatMessageDto) => void;
  deleteChat: () => Promise<void>;
  setShowEndMeetingPrompt: (show: boolean) => void;
  reset: () => void;
}

const initialState = {
  chat: null as ChatDto | null,
  isLoading: false,
  error: null as string | null,
  showEndMeetingPrompt: false,
};

export const useChatStore = create<ChatState>()((set, get) => ({
  ...initialState,

  fetchChat: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await apiClient.get<ApiResult<ChatDto>>(
        API_ENDPOINTS.CHAT.GET,
      );

      if (!response.data.success) {
        throw new Error(
          response.data.errors[0]?.error || 'Failed to fetch chat',
        );
      }

      set({ chat: response.data.result, isLoading: false });
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to fetch chat';
      set({ error: errorMessage, isLoading: false });
    }
  },

  refreshChat: async () => {
    try {
      const response = await apiClient.get<ApiResult<ChatDto>>(
        API_ENDPOINTS.CHAT.GET,
      );

      if (!response.data.success) {
        return;
      }

      set({ chat: response.data.result });
    } catch (error) {
      console.error('Failed to refresh chat', error);
    }
  },

  sendMessage: async (text: string) => {
    try {
      await apiClient.post(API_ENDPOINTS.CHAT.SEND_MESSAGE, {
        messageText: text,
      });

      const chat = get().chat;
      if (chat) {
        const newMessage: ChatMessageDto = {
          id: crypto.randomUUID(),
          messageText: text,
          createdAt: new Date().toISOString(),
          isMine: true,
        };
        set({
          chat: {
            ...chat,
            messages: [...chat.messages, newMessage],
          },
        });
      }
    } catch (error) {
      handleApiError(error, 'Failed to send message');
    }
  },

  addMessage: (message: ChatMessageDto) => {
    const chat = get().chat;
    if (chat) {
      if (chat.messages.some((m) => m.id === message.id)) return;

      set({
        chat: {
          ...chat,
          messages: [...chat.messages, message],
        },
      });
    }
  },

  deleteChat: async () => {
    try {
      await apiClient.delete(API_ENDPOINTS.CHAT.DELETE);
      set({ chat: null });
    } catch (error) {
      handleApiError(error, 'Failed to end meeting');
    }
  },

  setShowEndMeetingPrompt: (show: boolean) =>
    set({ showEndMeetingPrompt: show }),

  reset: () => set(initialState),
}));

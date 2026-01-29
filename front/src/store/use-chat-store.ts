'use client';

import { create } from 'zustand';
import axios from 'axios';
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
  sendMessage: (text: string) => Promise<void>;
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
      const response = await axios.get<ApiResult<ChatDto>>(
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

  sendMessage: async (text: string) => {
    try {
      await axios.post(API_ENDPOINTS.CHAT.SEND_MESSAGE, { messageText: text });

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
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to send message';
      throw new Error(errorMessage);
    }
  },

  deleteChat: async () => {
    try {
      await axios.delete(API_ENDPOINTS.CHAT.DELETE);
      set({ chat: null });
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : 'Failed to end meeting';
      throw new Error(errorMessage);
    }
  },

  setShowEndMeetingPrompt: (show: boolean) =>
    set({ showEndMeetingPrompt: show }),

  reset: () => set(initialState),
}));

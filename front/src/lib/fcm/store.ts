'use client';

import { create } from 'zustand';
import { FirebaseApp } from 'firebase/app';
import { Messaging } from 'firebase/messaging';
import { FcmState, FcmMessageHandler, FcmApiCallbacks } from './types';

interface FcmStoreState {
  state: FcmState;
  app: FirebaseApp | null;
  messaging: Messaging | null;
  swRegistration: ServiceWorkerRegistration | null;
  unsubForeground: (() => void) | null;

  // SW message listener reference for cleanup
  swMessageListener: ((event: MessageEvent) => void) | null;

  // Dynamic handlers
  customForegroundHandlers: FcmMessageHandler[];
  backgroundMessageHandler: FcmMessageHandler | null;

  // Callbacks
  apiCallbacks: FcmApiCallbacks | null;

  setState: (state: FcmState) => void;
  setApp: (app: FirebaseApp) => void;
  setMessaging: (messaging: Messaging) => void;
  setSwRegistration: (reg: ServiceWorkerRegistration) => void;
  setUnsubForeground: (unsub: (() => void) | null) => void;
  setSwMessageListener: (
    listener: ((event: MessageEvent) => void) | null,
  ) => void;

  registerForegroundHandler: (handler: FcmMessageHandler) => void;
  unregisterForegroundHandler: (handler: FcmMessageHandler) => void;
  setBackgroundMessageHandler: (handler: FcmMessageHandler | null) => void;

  setApiCallbacks: (callbacks: FcmApiCallbacks | null) => void;

  reset: () => void;
}

const initialState = {
  state: FcmState.IDLE,
  app: null as FirebaseApp | null,
  messaging: null as Messaging | null,
  swRegistration: null as ServiceWorkerRegistration | null,
  unsubForeground: null as (() => void) | null,
  swMessageListener: null as ((event: MessageEvent) => void) | null,
  customForegroundHandlers: [] as FcmMessageHandler[],
  backgroundMessageHandler: null as FcmMessageHandler | null,
  apiCallbacks: null as FcmApiCallbacks | null,
};

export const useFcmStore = create<FcmStoreState>()((set) => ({
  ...initialState,

  setState: (state) => set({ state }),
  setApp: (app) => set({ app }),
  setMessaging: (messaging) => set({ messaging }),
  setSwRegistration: (swRegistration) => set({ swRegistration }),
  setUnsubForeground: (unsubForeground) => set({ unsubForeground }),
  setSwMessageListener: (swMessageListener) => set({ swMessageListener }),

  registerForegroundHandler: (handler) =>
    set((state) => ({
      customForegroundHandlers: [...state.customForegroundHandlers, handler],
    })),

  unregisterForegroundHandler: (handler) =>
    set((state) => ({
      customForegroundHandlers: state.customForegroundHandlers.filter(
        (h) => h !== handler,
      ),
    })),

  setBackgroundMessageHandler: (handler) =>
    set({ backgroundMessageHandler: handler }),

  setApiCallbacks: (apiCallbacks) => set({ apiCallbacks }),

  reset: () => set(initialState),
}));

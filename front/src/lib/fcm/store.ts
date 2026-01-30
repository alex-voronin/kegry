'use client';

import { create } from 'zustand';
import { FirebaseApp } from 'firebase/app';
import { Messaging } from 'firebase/messaging';
import { FcmState } from './types';

interface FcmStoreState {
  state: FcmState;
  app: FirebaseApp | null;
  messaging: Messaging | null;
  swRegistration: ServiceWorkerRegistration | null;
  unsubForeground: (() => void) | null;

  setState: (state: FcmState) => void;
  setApp: (app: FirebaseApp) => void;
  setMessaging: (messaging: Messaging) => void;
  setSwRegistration: (reg: ServiceWorkerRegistration) => void;
  setUnsubForeground: (unsub: (() => void) | null) => void;
  reset: () => void;
}

const initialState = {
  state: FcmState.IDLE,
  app: null as FirebaseApp | null,
  messaging: null as Messaging | null,
  swRegistration: null as ServiceWorkerRegistration | null,
  unsubForeground: null as (() => void) | null,
};

export const useFcmStore = create<FcmStoreState>()((set) => ({
  ...initialState,

  setState: (state) => set({ state }),
  setApp: (app) => set({ app }),
  setMessaging: (messaging) => set({ messaging }),
  setSwRegistration: (swRegistration) => set({ swRegistration }),
  setUnsubForeground: (unsubForeground) => set({ unsubForeground }),

  reset: () => set(initialState),
}));

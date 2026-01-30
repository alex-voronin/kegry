'use client';

import { MessagePayload } from 'firebase/messaging';

export type FcmPayload = MessagePayload;

export type FcmMessageHandler = (payload: FcmPayload) => void | Promise<void>;

/* ------------------------------------------------------------------ */
/* FCM State Types */
/* ------------------------------------------------------------------ */

export enum FcmState {
  IDLE = 'IDLE',
  PERMISSION = 'PERMISSION',
  SW = 'SW',
  TOKEN = 'TOKEN',
  READY = 'READY',
  ERROR = 'ERROR',
}

/* ------------------------------------------------------------------ */
/* Token Storage Types */
/* ------------------------------------------------------------------ */

export interface StoredToken {
  token: string;
  ts: number;
}

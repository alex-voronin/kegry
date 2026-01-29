'use client';

/* ------------------------------------------------------------------ */
/* FCM Payload Types */
/* ------------------------------------------------------------------ */

export interface FcmNotification {
  title?: string;
  body?: string;
  icon?: string;
}

export interface FcmData {
  action?: string;
  userAction?: string;
  messageId?: string;
  [key: string]: string | undefined;
}

export interface FcmPayload {
  notification?: FcmNotification;
  data?: FcmData;
  messageId?: string;
}

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

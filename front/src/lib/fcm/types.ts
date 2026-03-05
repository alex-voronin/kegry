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

export enum NotificationType {
  MEETING_ARRANGED = 'meetingArranged',
  USER_INVITED = 'userInvited',
  CHAT_MESSAGE_RECEIVED = 'chatMessageReceived',
  OPEN_RATE_USER_SCREEN = 'openRateUserScreen',
  SHOW_END_MEETING_SUGGESTION = 'showEndMeetingSuggestion',
  REFERRER_REWARD_RECEIVED = 'referrerRewardReceived',
}

export enum SwMessageType {
  FCM_MESSAGE = 'FCM_MESSAGE',
  NOTIFICATION_CLICK = 'NOTIFICATION_CLICK',
}

/* ------------------------------------------------------------------ */
/* Token Storage Types */
/* ------------------------------------------------------------------ */

export interface StoredToken {
  token: string;
  ts: number;
}

/* ------------------------------------------------------------------ */
/* API Callbacks */
/* ------------------------------------------------------------------ */

export interface FcmApiCallbacks {
  onTokenRegister: (deviceId: string, fcmToken: string) => Promise<void>;
  onTokenUnregister: (deviceId: string) => Promise<void>;
}

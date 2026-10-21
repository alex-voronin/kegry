export type EventType = 'Event' | 'Meetup';

export interface EventContactDto {
  id: string;
  type: string;
  value: string;
  isPublic: boolean;
}

export interface EventDto {
  id: string;
  coverImageUrl: string | null;
  title: string;
  description: string;
  startDateTime: string;
  type: EventType;
  venueId: string;
  creatorId: string;
  creatorAvatarUrl: string | null;
  isVisibleOnMap: boolean;
  maxParticipants: number | null;
  requiresApproval: boolean;
  participantsCount: number;
  pendingApprovalsCount: number;
  pendingParticipantIds: string[];
  approvedParticipantIds: string[];
  contacts: EventContactDto[];
  hobbyIds: string[];
  participationStatus?: string;
  isCreator: boolean;
}

export interface EventMapItem {
  id: string;
  coverImageUrl: string | null;
  title: string;
  type: 'Event' | 'Meetup';
  startDateTime: string;
  venueId: string;
}

export interface VenueEventsDto {
  venueId: string;
  totalCount: number;
  events: EventMapItem[];
}

export type EventCountMap = Record<string, number>;

export interface EventListItem {
  id: string;
  coverImageUrl: string | null;
  title: string;
  type: 'Event' | 'Meetup';
  startDateTime: string;
  venueId: string;
  participantsCount: number;
  isJoined: boolean;
}

export interface EventMapItem {
  id: string;
  coverImageUrl: string | null;
  title: string;
  type: 'Event' | 'Meetup';
  startDateTime: string;
  venueId: string;
  latitude: number;
  longitude: number;
}

export type EventCountMap = Record<string, number>;

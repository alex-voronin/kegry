import dayjs from 'dayjs';
import isToday from 'dayjs/plugin/isToday';
import isTomorrow from 'dayjs/plugin/isTomorrow';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(isToday);
dayjs.extend(isTomorrow);
dayjs.extend(relativeTime);

export function formatNotificationDate(
  dateString: string,
  t: (key: string, params?: any) => string,
): string {
  const date = dayjs(dateString);
  const hours = date.hour();

  if (date.isToday()) {
    return t('todayUntil', { h: hours });
  }

  if (date.isTomorrow()) {
    return t('tomorrowUntil', { h: hours });
  }

  // specified formats: 1 day ago, 3 days ago, 1 week ago, 1 month ago, 1 year ago
  return date.fromNow();
}

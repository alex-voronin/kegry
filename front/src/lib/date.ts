import dayjs from 'dayjs';
import isToday from 'dayjs/plugin/isToday';
import isTomorrow from 'dayjs/plugin/isTomorrow';
import relativeTime from 'dayjs/plugin/relativeTime';
import utc from 'dayjs/plugin/utc';

dayjs.extend(isToday);
dayjs.extend(isTomorrow);
dayjs.extend(relativeTime);
dayjs.extend(utc);

export function formatNotificationDate(
  dateString: string,
  t: (key: string, params?: any) => string,
): string {
  const date = dayjs.utc(dateString).local();
  const hours = date.hour();
  const minutes = date.minute();

  if (date.isToday()) {
    return t('todayUntil', { h: hours, m: minutes });
  }

  if (date.isTomorrow()) {
    return t('tomorrowUntil', { h: hours, m: minutes });
  }

  // specified formats: 1 day ago, 3 days ago, 1 week ago, 1 month ago, 1 year ago
  return date.fromNow();
}

/**
 * Calculate age from birthday string (ISO date format)
 */
export function calculateAge(birthday: string): number {
  const birthDate = new Date(birthday);
  const today = new Date();
  let age = today.getFullYear() - birthDate.getFullYear();
  const monthDiff = today.getMonth() - birthDate.getMonth();

  if (
    monthDiff < 0 ||
    (monthDiff === 0 && today.getDate() < birthDate.getDate())
  ) {
    age--;
  }

  return age;
}

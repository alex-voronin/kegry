export enum InviteExpiration {
  Now = 'now',
  Today = 'today',
  Tomorrow = 'tomorrow',
}

const FOUR_HOURS = 4 * 60 * 60 * 1000;

const endOfDay = (date: Date) =>
  new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate(),
    23,
    59,
    59,
    999,
  );

const startOfDay = (date: Date) =>
  new Date(date.getFullYear(), date.getMonth(), date.getDate());

export const computeExpiresAt = (when: InviteExpiration): string => {
  const now = new Date();

  switch (when) {
    case InviteExpiration.Today:
      return endOfDay(now).toISOString();

    case InviteExpiration.Tomorrow: {
      const tomorrow = new Date(now);
      tomorrow.setDate(now.getDate() + 1);
      return endOfDay(tomorrow).toISOString();
    }

    case InviteExpiration.Now:
    default:
      return new Date(now.getTime() + FOUR_HOURS).toISOString();
  }
};

export const expiresAtToWhen = (expiresAt: string): InviteExpiration => {
  const expires = new Date(expiresAt);

  if (isNaN(expires.getTime())) {
    return InviteExpiration.Now;
  }

  const now = new Date();

  const todayStart = startOfDay(now);
  const tomorrowStart = new Date(todayStart);
  tomorrowStart.setDate(todayStart.getDate() + 1);

  const dayAfterTomorrowStart = new Date(todayStart);
  dayAfterTomorrowStart.setDate(todayStart.getDate() + 2);

  if (expires >= todayStart && expires < tomorrowStart) {
    return InviteExpiration.Today;
  }

  if (expires >= tomorrowStart && expires < dayAfterTomorrowStart) {
    return InviteExpiration.Tomorrow;
  }

  return InviteExpiration.Now;
};

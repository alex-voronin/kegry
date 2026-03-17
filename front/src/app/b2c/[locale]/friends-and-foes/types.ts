export type SelectedUser = {
  id: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
  coverUrl?: string;
  type: 'friend' | 'blocked' | 'imaginary';
};

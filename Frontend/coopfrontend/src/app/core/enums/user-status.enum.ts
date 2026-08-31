import { BadgeTone } from './badge-tone';

export enum UserStatus {
  Active = 0,
  Suspended = 1,
  Deleted = 2
}

export const UserStatusLabels: Record<UserStatus, string> = {
  [UserStatus.Active]: 'نشط',
  [UserStatus.Suspended]: 'موقوف',
  [UserStatus.Deleted]: 'محذوف'
};

export const UserStatusTones: Record<UserStatus, BadgeTone> = {
  [UserStatus.Active]: 'success',
  [UserStatus.Suspended]: 'danger',
  [UserStatus.Deleted]: 'neutral'
};

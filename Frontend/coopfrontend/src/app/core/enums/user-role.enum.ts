import { BadgeTone } from './badge-tone';

export enum UserRole {
  Customer = 0,
  Merchant = 1,
  Driver = 2,
  Admin = 3
}

export const UserRoleLabels: Record<UserRole, string> = {
  [UserRole.Customer]: 'زبون',
  [UserRole.Merchant]: 'تاجر',
  [UserRole.Driver]: 'سائق',
  [UserRole.Admin]: 'مشرف'
};

export const UserRoleTones: Record<UserRole, BadgeTone> = {
  [UserRole.Customer]: 'neutral',
  [UserRole.Merchant]: 'info',
  [UserRole.Driver]: 'warning',
  [UserRole.Admin]: 'success'
};

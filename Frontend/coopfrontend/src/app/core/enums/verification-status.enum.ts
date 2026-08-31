import { BadgeTone } from './badge-tone';

export enum VerificationStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  NeedsInformation = 3
}

export const VerificationStatusLabels: Record<VerificationStatus, string> = {
  [VerificationStatus.Pending]: 'قيد المراجعة',
  [VerificationStatus.Approved]: 'مقبول',
  [VerificationStatus.Rejected]: 'مرفوض',
  [VerificationStatus.NeedsInformation]: 'بحاجة معلومات إضافية'
};

export const VerificationStatusTones: Record<VerificationStatus, BadgeTone> = {
  [VerificationStatus.Pending]: 'warning',
  [VerificationStatus.Approved]: 'success',
  [VerificationStatus.Rejected]: 'danger',
  [VerificationStatus.NeedsInformation]: 'info'
};

import { BadgeTone } from './badge-tone';

export enum ComplaintStatus {
  Open = 0,
  UnderReview = 1,
  Resolved = 2,
  Rejected = 3
}

export const ComplaintStatusLabels: Record<ComplaintStatus, string> = {
  [ComplaintStatus.Open]: 'مفتوحة',
  [ComplaintStatus.UnderReview]: 'قيد المراجعة',
  [ComplaintStatus.Resolved]: 'تم حلها',
  [ComplaintStatus.Rejected]: 'مرفوضة'
};

export const ComplaintStatusTones: Record<ComplaintStatus, BadgeTone> = {
  [ComplaintStatus.Open]: 'warning',
  [ComplaintStatus.UnderReview]: 'info',
  [ComplaintStatus.Resolved]: 'success',
  [ComplaintStatus.Rejected]: 'danger'
};

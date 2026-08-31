import { BadgeTone } from './badge-tone';

export enum OfferStatus {
  Draft = 0,
  PendingApproval = 1,
  Approved = 2,
  Rejected = 3,
  Scheduled = 4,
  Active = 5,
  Paused = 6,
  SoldOut = 7,
  Expired = 8,
  Cancelled = 9
}

export const OfferStatusLabels: Record<OfferStatus, string> = {
  [OfferStatus.Draft]: 'مسودة',
  [OfferStatus.PendingApproval]: 'بانتظار الموافقة',
  [OfferStatus.Approved]: 'تمت الموافقة',
  [OfferStatus.Rejected]: 'مرفوض',
  [OfferStatus.Scheduled]: 'مجدول',
  [OfferStatus.Active]: 'نشط',
  [OfferStatus.Paused]: 'متوقف مؤقتاً',
  [OfferStatus.SoldOut]: 'نفدت الكمية',
  [OfferStatus.Expired]: 'منتهي',
  [OfferStatus.Cancelled]: 'ملغي'
};

export const OfferStatusTones: Record<OfferStatus, BadgeTone> = {
  [OfferStatus.Draft]: 'neutral',
  [OfferStatus.PendingApproval]: 'warning',
  [OfferStatus.Approved]: 'info',
  [OfferStatus.Rejected]: 'danger',
  [OfferStatus.Scheduled]: 'info',
  [OfferStatus.Active]: 'success',
  [OfferStatus.Paused]: 'warning',
  [OfferStatus.SoldOut]: 'neutral',
  [OfferStatus.Expired]: 'neutral',
  [OfferStatus.Cancelled]: 'danger'
};

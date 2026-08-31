import { BadgeTone } from './badge-tone';

export enum PaymentStatus {
  Pending = 0,
  Paid = 1,
  Failed = 2,
  Refunded = 3
}

export const PaymentStatusLabels: Record<PaymentStatus, string> = {
  [PaymentStatus.Pending]: 'بانتظار الدفع',
  [PaymentStatus.Paid]: 'مدفوع',
  [PaymentStatus.Failed]: 'فشل الدفع',
  [PaymentStatus.Refunded]: 'تم الاسترجاع'
};

export const PaymentStatusTones: Record<PaymentStatus, BadgeTone> = {
  [PaymentStatus.Pending]: 'warning',
  [PaymentStatus.Paid]: 'success',
  [PaymentStatus.Failed]: 'danger',
  [PaymentStatus.Refunded]: 'neutral'
};

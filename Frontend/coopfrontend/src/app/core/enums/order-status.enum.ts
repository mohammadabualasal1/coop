import { BadgeTone } from './badge-tone';

export enum OrderStatus {
  PendingPayment = 0,
  PendingMerchantConfirmation = 1,
  Accepted = 2,
  Rejected = 3,
  Preparing = 4,
  ReadyForPickup = 5,
  DriverAssigned = 6,
  OutForDelivery = 7,
  Delivered = 8,
  Completed = 9,
  Cancelled = 10,
  DeliveryFailed = 11
}

export const OrderStatusLabels: Record<OrderStatus, string> = {
  [OrderStatus.PendingPayment]: 'بانتظار الدفع',
  [OrderStatus.PendingMerchantConfirmation]: 'بانتظار تأكيد التاجر',
  [OrderStatus.Accepted]: 'مقبول',
  [OrderStatus.Rejected]: 'مرفوض',
  [OrderStatus.Preparing]: 'قيد التحضير',
  [OrderStatus.ReadyForPickup]: 'جاهز للاستلام',
  [OrderStatus.DriverAssigned]: 'تم تعيين سائق',
  [OrderStatus.OutForDelivery]: 'في الطريق للزبون',
  [OrderStatus.Delivered]: 'تم التوصيل',
  [OrderStatus.Completed]: 'مكتمل',
  [OrderStatus.Cancelled]: 'ملغي',
  [OrderStatus.DeliveryFailed]: 'فشل التوصيل'
};

export const OrderStatusTones: Record<OrderStatus, BadgeTone> = {
  [OrderStatus.PendingPayment]: 'warning',
  [OrderStatus.PendingMerchantConfirmation]: 'warning',
  [OrderStatus.Accepted]: 'info',
  [OrderStatus.Rejected]: 'danger',
  [OrderStatus.Preparing]: 'info',
  [OrderStatus.ReadyForPickup]: 'info',
  [OrderStatus.DriverAssigned]: 'info',
  [OrderStatus.OutForDelivery]: 'info',
  [OrderStatus.Delivered]: 'success',
  [OrderStatus.Completed]: 'success',
  [OrderStatus.Cancelled]: 'danger',
  [OrderStatus.DeliveryFailed]: 'danger'
};

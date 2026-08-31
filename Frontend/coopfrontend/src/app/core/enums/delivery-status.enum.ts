import { BadgeTone } from './badge-tone';

export enum DeliveryStatus {
  SearchingDriver = 0,
  Offered = 1,
  Assigned = 2,
  GoingToMerchant = 3,
  ArrivedAtMerchant = 4,
  PickedUp = 5,
  GoingToCustomer = 6,
  ArrivedAtCustomer = 7,
  Delivered = 8,
  Failed = 9,
  Cancelled = 10
}

export const DeliveryStatusLabels: Record<DeliveryStatus, string> = {
  [DeliveryStatus.SearchingDriver]: 'جاري البحث عن سائق',
  [DeliveryStatus.Offered]: 'تم العرض على سائق',
  [DeliveryStatus.Assigned]: 'تم تعيين السائق',
  [DeliveryStatus.GoingToMerchant]: 'في الطريق للمتجر',
  [DeliveryStatus.ArrivedAtMerchant]: 'وصل المتجر',
  [DeliveryStatus.PickedUp]: 'تم استلام الطلب',
  [DeliveryStatus.GoingToCustomer]: 'في الطريق للزبون',
  [DeliveryStatus.ArrivedAtCustomer]: 'وصل للزبون',
  [DeliveryStatus.Delivered]: 'تم التوصيل',
  [DeliveryStatus.Failed]: 'فشل التوصيل',
  [DeliveryStatus.Cancelled]: 'ملغي'
};

export const DeliveryStatusTones: Record<DeliveryStatus, BadgeTone> = {
  [DeliveryStatus.SearchingDriver]: 'warning',
  [DeliveryStatus.Offered]: 'warning',
  [DeliveryStatus.Assigned]: 'info',
  [DeliveryStatus.GoingToMerchant]: 'info',
  [DeliveryStatus.ArrivedAtMerchant]: 'info',
  [DeliveryStatus.PickedUp]: 'info',
  [DeliveryStatus.GoingToCustomer]: 'info',
  [DeliveryStatus.ArrivedAtCustomer]: 'info',
  [DeliveryStatus.Delivered]: 'success',
  [DeliveryStatus.Failed]: 'danger',
  [DeliveryStatus.Cancelled]: 'danger'
};

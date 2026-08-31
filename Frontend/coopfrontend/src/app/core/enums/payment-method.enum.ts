export enum PaymentMethod {
  CashOnDelivery = 0,
  MockOnlinePayment = 1
}

export const PaymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.CashOnDelivery]: 'الدفع عند الاستلام',
  [PaymentMethod.MockOnlinePayment]: 'دفع إلكتروني تجريبي'
};

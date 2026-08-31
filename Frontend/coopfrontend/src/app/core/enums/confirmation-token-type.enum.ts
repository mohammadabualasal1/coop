export enum ConfirmationTokenType {
  MerchantPickup = 0,
  CustomerDelivery = 1
}

export const ConfirmationTokenTypeLabels: Record<ConfirmationTokenType, string> = {
  [ConfirmationTokenType.MerchantPickup]: 'رمز استلام السائق من التاجر',
  [ConfirmationTokenType.CustomerDelivery]: 'رمز تسليم الزبون'
};

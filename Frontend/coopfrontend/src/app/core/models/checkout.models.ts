export interface CheckoutCalculateRequest {
  customerAddressId: string;
}

export interface CheckoutCalculation {
  subtotal: number;
  totalDiscount: number;
  deliveryFee: number;
  totalAmount: number;
}

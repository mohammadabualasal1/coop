export interface MerchantBranch {
  id: string;
  merchantId: string;
  name: string;
  address: string;
  city: string;
  area: string;
  latitude: number;
  longitude: number;
  phoneNumber: string;
  openingTime: string; // "HH:mm:ss"
  closingTime: string; // "HH:mm:ss"
  deliveryRadiusKm: number;
  minimumOrderAmount: number;
  baseDeliveryFee: number;
  isMainBranch: boolean;
  isActive: boolean;
}

export interface BranchRequest {
  name: string;
  address: string;
  city: string;
  area: string;
  latitude: number;
  longitude: number;
  phoneNumber: string;
  openingTime: string; // "HH:mm:ss"
  closingTime: string; // "HH:mm:ss"
  deliveryRadiusKm: number;
  minimumOrderAmount: number;
  baseDeliveryFee: number;
}

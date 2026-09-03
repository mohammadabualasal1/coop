export interface OfferSummary {
  id: string;
  title: string;
  productName: string;
  merchantId: string;
  merchantName: string;
  originalPrice: number;
  discountedPrice: number;
  discountPercentage: number;
  mainImageUrl: string | null;
  endAt: string;
  availableStock?: number;
  distanceKm?: number; // only from /offers/nearby
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface MerchantSummary {
  id: string;
  name: string;
  description: string | null;
  logoUrl: string | null;
  coverImageUrl: string | null;
  averageRating: number | null;
}

export interface SearchOffersParams {
  search?: string | null;
  categoryId?: string | null;
  merchantId?: string | null;
  city?: string | null;
  minimumDiscount?: number | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  sortBy?: string | null;
  pageNumber?: number;
  pageSize?: number;
}

export interface SearchMerchantsParams {
  search?: string | null;
  city?: string | null;
  pageNumber?: number;
  pageSize?: number;
}

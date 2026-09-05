export interface OfferSummary {
  id: string;
  title: string;
  productName?: string; // not returned by every offer-summary endpoint
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

export interface OfferBranchAvailability {
  merchantBranchId: string;
  branchName: string;
  city: string;
  area: string;
  availableStock: number;
  latitude: number;
  longitude: number;
}

export interface OfferDetail extends OfferSummary {
  description: string | null;
  startAt: string;
  maximumQuantityPerCustomer: number | null;
  merchantLogoUrl?: string | null;
  branches: OfferBranchAvailability[];
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

// Shape of a row from GET /api/favorites — a favorite record wrapping an offer.
// Field names match OfferSummary so a favorite can be mapped onto one directly.
export interface FavoriteOffer {
  id: string;
  offerId: string;
  title: string;
  merchantId: string;
  merchantName: string;
  mainImageUrl: string | null;
  originalPrice: number;
  discountedPrice: number;
  discountPercentage: number;
  endAt: string;
  createdAt: string;
}

// Shape of a row from GET /api/follows — a follow record wrapping a merchant.
export interface FollowedMerchant {
  id: string;
  merchantId: string;
  name: string;
  description: string | null;
  logoUrl: string | null;
  averageRating: number | null;
  createdAt: string;
}

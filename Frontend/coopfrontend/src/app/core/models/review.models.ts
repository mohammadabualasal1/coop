export interface Review {
  id: string;
  customerName: string;
  merchantRating: number;
  driverRating: number | null;
  comment: string | null;
  createdAt: string;
}

export interface PagedReviews {
  items: Review[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateReviewRequest {
  orderId: string;
  merchantRating: number;
  driverRating?: number | null;
  comment?: string | null;
}

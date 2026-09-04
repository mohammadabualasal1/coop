export interface Review {
  id: string;
  customerName: string;
  merchantRating: number;
  driverRating: number | null;
  comment: string | null;
  createdAt: string;
}

export interface CreateReviewRequest {
  orderId: string;
  merchantRating: number;
  driverRating?: number | null;
  comment?: string | null;
}

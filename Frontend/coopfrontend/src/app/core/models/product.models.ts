export interface ProductImage {
  id: string;
  productId: string;
  fileUrl: string;
  displayOrder: number;
}

export interface Product {
  id: string;
  merchantId: string;
  categoryId: string;
  name: string;
  description: string;
  sku: string | null;
  brand: string | null;
  mainImageUrl: string | null;
  isActive: boolean;
  createdAt: string;
  images?: ProductImage[]; // only present on GET /products/{id}
}

export interface ProductRequest {
  categoryId: string;
  name: string;
  description: string;
  sku: string | null;
  brand: string | null;
  mainImageUrl: string | null;
}

export interface UpdateProductRequest extends ProductRequest {
  isActive: boolean;
}

export interface ProductImageRequest {
  fileUrl: string;
  displayOrder: number;
}

export interface Category {
  id: string;
  parentCategoryId: string | null;
  nameEn: string;
  nameAr: string;
  description: string | null;
  imageUrl: string | null;
  displayOrder: number;
  isActive: boolean;
  // Nested subcategories — only populated by GET /categories (the tree endpoint).
  children?: Category[];
}

export interface CategoryRequest {
  parentCategoryId: string | null;
  nameEn: string;
  nameAr: string;
  description: string | null;
  imageUrl: string | null;
  displayOrder: number;
}

export interface UpdateCategoryRequest extends CategoryRequest {
  isActive: boolean;
}

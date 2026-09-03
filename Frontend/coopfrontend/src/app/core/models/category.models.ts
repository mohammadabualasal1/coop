export interface Category {
  id: string;
  parentCategoryId: string | null;
  nameEn: string;
  nameAr: string;
  description: string | null;
  imageUrl: string | null;
  displayOrder: number;
  isActive: boolean;
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

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

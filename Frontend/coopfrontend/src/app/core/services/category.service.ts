import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Category } from '../models/category.models';

export interface CategoryOption {
  id: string;
  label: string;
}

function flattenCategories(
  categories: Category[],
  parentId: string | null = null,
  depth = 0
): CategoryOption[] {
  const prefix = '— '.repeat(depth);

  return categories
    .filter((category) => category.parentCategoryId === parentId)
    .sort((a, b) => a.displayOrder - b.displayOrder)
    .flatMap((category) => [
      { id: category.id, label: `${prefix}${category.nameAr}` },
      ...flattenCategories(categories, category.id, depth + 1)
    ]);
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/categories`;

  private readonly _categories = signal<Category[] | null>(null);
  readonly categories = this._categories.asReadonly();

  readonly options = computed<CategoryOption[]>(() => flattenCategories(this._categories() ?? []));

  private readonly categoryMap = computed(
    () => new Map((this._categories() ?? []).map((category) => [category.id, category]))
  );

  getAll(): Observable<Category[]> {
    const cached = this._categories();

    if (cached) {
      return of(cached);
    }

    return this.http
      .get<Category[]>(this.baseUrl)
      .pipe(tap((categories) => this._categories.set(categories)));
  }

  nameAr(categoryId: string): string {
    return this.categoryMap().get(categoryId)?.nameAr ?? '';
  }
}

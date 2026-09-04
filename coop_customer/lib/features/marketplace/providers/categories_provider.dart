import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/network/network_providers.dart';
import '../data/categories_api.dart';
import '../data/categories_repository.dart';
import '../models/category_model.dart';

final categoriesApiProvider = Provider<CategoriesApi>((ref) {
  return CategoriesApi(ref.watch(dioProvider));
});

final categoriesRepositoryProvider = Provider<CategoriesRepository>((ref) {
  return CategoriesRepository(ref.watch(categoriesApiProvider));
});

/// Public endpoint — works pre-login. Screens should refetch on focus
/// since categories can change server-side (API reference §26).
final categoriesProvider = FutureProvider.autoDispose<List<CategoryModel>>((ref) {
  return ref.watch(categoriesRepositoryProvider).getCategories();
});

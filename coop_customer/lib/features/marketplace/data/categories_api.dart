import 'package:dio/dio.dart';

import '../models/category_model.dart';

/// `GET /api/categories/{id}/offers` is deliberately not wired yet — the
/// offer response shape isn't confirmed, so nothing that parses an Offer
/// gets built until real JSON is provided.
class CategoriesApi {
  CategoriesApi(this._dio);

  final Dio _dio;

  Future<List<CategoryModel>> getCategories() async {
    final response = await _dio.get('/categories');
    final data = response.data as List<dynamic>;
    return data.map((json) => CategoryModel.fromJson(json as Map<String, dynamic>)).toList();
  }
}

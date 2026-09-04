import 'package:dio/dio.dart';

import '../../../core/network/api_exception.dart';
import '../models/category_model.dart';
import 'categories_api.dart';

class CategoriesRepository {
  CategoriesRepository(this._api);

  final CategoriesApi _api;

  Future<List<CategoryModel>> getCategories() async {
    try {
      return await _api.getCategories();
    } on DioException catch (e) {
      throw ApiException.fromDioException(e);
    }
  }
}

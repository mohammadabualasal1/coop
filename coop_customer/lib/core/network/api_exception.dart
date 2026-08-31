import 'package:dio/dio.dart';

/// Wraps a backend error into a message ready for direct display.
///
/// Per the API reference, error bodies are plain Arabic strings written
/// for end users — no client-side message mapping needed, just surfacing.
class ApiException implements Exception {
  ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  static const _genericMessage = 'حدث خطأ غير متوقع، يرجى المحاولة مرة أخرى';
  static const _networkMessage = 'تعذر الاتصال بالخادم، تحقق من اتصالك بالإنترنت';

  factory ApiException.fromDioException(DioException e) {
    if (e.type == DioExceptionType.connectionError ||
        e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout ||
        e.type == DioExceptionType.sendTimeout) {
      return ApiException(_networkMessage);
    }

    final statusCode = e.response?.statusCode;
    final data = e.response?.data;

    final message = _extractMessage(data) ?? _genericMessage;
    return ApiException(message, statusCode: statusCode);
  }

  static String? _extractMessage(dynamic data) {
    if (data == null) return null;
    if (data is String && data.trim().isNotEmpty) return data;
    if (data is Map) {
      final candidate = data['message'] ?? data['title'] ?? data['error'];
      if (candidate is String && candidate.trim().isNotEmpty) return candidate;
    }
    return null;
  }

  /// True when this is a 404 — per API convention, "not found or not yours".
  /// Not necessarily a bug: ownership is enforced inside the query.
  bool get isNotFound => statusCode == 404;

  @override
  String toString() => message;
}

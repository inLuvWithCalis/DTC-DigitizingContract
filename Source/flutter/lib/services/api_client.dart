import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import '../config/app_config.dart';

class ApiException implements Exception {
  final int statusCode;
  final String message;
  final dynamic data;

  ApiException({required this.statusCode, required this.message, this.data});

  @override
  String toString() => message;
}

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  factory ApiClient() => _instance;
  ApiClient._internal();

  String _baseUrl = AppConfig.apiBaseUrl;

  final Map<String, String> _cookies = {};

  String get baseUrl {
    final effectiveUrl =
        (!kIsWeb && Platform.isAndroid && _baseUrl.contains('localhost'))
        ? _baseUrl.replaceAll('localhost', '10.0.2.2')
        : _baseUrl;
    debugPrint('Base URL: $effectiveUrl');
    return effectiveUrl;
  }

  void setBaseUrl(String url) {
    _baseUrl = url.endsWith('/') ? url.substring(0, url.length - 1) : url;
    if (kDebugMode) {
      debugPrint('[ApiClient] BaseURL updated: $_baseUrl');
    }
  }

  bool get kIsWeb {
    return identical(0, 0.0);
  }

  void clearCookies() {
    _cookies.clear();
    if (kDebugMode) {
      debugPrint('[ApiClient] Cleared all session cookies.');
    }
  }

  void _updateCookies(http.Response response) {
    final rawCookie = response.headers['set-cookie'];
    if (rawCookie != null) {
      final cookies = rawCookie.split(',');
      for (var cookie in cookies) {
        final parts = cookie.split(';')[0].split('=');
        if (parts.length >= 2) {
          final key = parts[0].trim();
          final val = parts.sublist(1).join('=').trim();
          _cookies[key] = val;
        }
      }
      if (kDebugMode) {
        debugPrint('[ApiClient] Updated cookies: $_cookies');
      }
    }
  }

  Map<String, String> _buildHeaders(Map<String, String>? customHeaders) {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (_cookies.isNotEmpty) {
      final cookieHeader = _cookies.entries
          .map((e) => '${e.key}=${e.value}')
          .join('; ');
      headers['Cookie'] = cookieHeader;
    }

    if (customHeaders != null) {
      headers.addAll(customHeaders);
    }

    return headers;
  }

  dynamic _processResponse(http.Response response, Uri url, String method) {
    _updateCookies(response);

    dynamic body;
    if (response.body.isNotEmpty) {
      try {
        body = jsonDecode(response.body);
      } catch (_) {
        body = response.body;
      }
    }

    if (kDebugMode) {
      debugPrint('📥 [HTTP Response ${response.statusCode}] $method $url');
      debugPrint('   Payload: $body');
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (body is Map<String, dynamic> &&
          body.containsKey('success') &&
          body.containsKey('data')) {
        return body['data'];
      }
      return body;
    }

    String errorMessage = 'Yêu cầu thất bại (${response.statusCode})';
    if (body is Map<String, dynamic>) {
      if (body['message'] != null) {
        errorMessage = body['message'].toString();
      } else if (body['error'] != null) {
        errorMessage = body['error'].toString();
      }
    }

    if (kDebugMode) {
      debugPrint(
        '❌ [HTTP Error ${response.statusCode}] $method $url -> $errorMessage',
      );
    }

    throw ApiException(
      statusCode: response.statusCode,
      message: errorMessage,
      data: body,
    );
  }

  Future<dynamic> get(String endpoint, {Map<String, String>? headers}) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final builtHeaders = _buildHeaders(headers);

    if (kDebugMode) {
      debugPrint('🌐 [HTTP Request] GET $url');
      debugPrint('   Headers: $builtHeaders');
    }

    try {
      final response = await http.get(url, headers: builtHeaders);
      return _processResponse(response, url, 'GET');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] GET $url -> $e');
      }
      rethrow;
    }
  }

  Future<dynamic> post(
    String endpoint, {
    dynamic body,
    Map<String, String>? headers,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final builtHeaders = _buildHeaders(headers);
    final jsonBody = body != null ? jsonEncode(body) : null;

    if (kDebugMode) {
      debugPrint('🌐 [HTTP Request] POST $url');
      debugPrint('   Headers: $builtHeaders');
      if (jsonBody != null) {
        debugPrint('   Body: $jsonBody');
      }
    }

    try {
      final response = await http.post(
        url,
        headers: builtHeaders,
        body: jsonBody,
      );
      return _processResponse(response, url, 'POST');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] POST $url -> $e');
      }
      rethrow;
    }
  }
}

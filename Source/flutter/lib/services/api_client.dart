import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
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

  static const String _sessionCookieKey = 'app_session_cookies';

  /// Callback toàn cục khi nhận phản hồi 401 Unauthorized (hết hạn session)
  static VoidCallback? onUnauthorized;

  String _baseUrl = AppConfig.apiBaseUrl;
  final Map<String, String> _cookies = {};
  bool _isInitialized = false;

  /// Khởi tạo và nạp Session Cookie đã lưu từ SharedPreferences (disk)
  Future<void> init() async {
    if (_isInitialized) return;
    try {
      final prefs = await SharedPreferences.getInstance();
      final savedCookiesJson = prefs.getString(_sessionCookieKey);
      if (savedCookiesJson != null && savedCookiesJson.isNotEmpty) {
        final decoded = jsonDecode(savedCookiesJson) as Map<String, dynamic>;
        decoded.forEach((key, value) {
          _cookies[key] = value.toString();
        });
        if (kDebugMode) {
          debugPrint('[ApiClient] Loaded persistent cookies: $_cookies');
        }
      }
    } catch (e) {
      if (kDebugMode) {
        debugPrint('[ApiClient] Error loading persistent cookies: $e');
      }
    } finally {
      _isInitialized = true;
    }
  }

  String get baseUrl {
    final effectiveUrl =
        (!kIsWeb && Platform.isAndroid && _baseUrl.contains('localhost'))
        ? _baseUrl.replaceAll('localhost', '10.0.2.2')
        : _baseUrl;
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

  Future<void> clearCookies() async {
    _cookies.clear();
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_sessionCookieKey);
    } catch (_) {}
    if (kDebugMode) {
      debugPrint('[ApiClient] Cleared all session cookies.');
    }
  }

  Future<void> _saveCookies() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(_sessionCookieKey, jsonEncode(_cookies));
    } catch (_) {}
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
      _saveCookies();
      if (kDebugMode) {
        debugPrint('[ApiClient] Updated and saved cookies: $_cookies');
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

  Future<dynamic> _processResponse(
    http.Response response,
    Uri url,
    String method,
  ) async {
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

    if (response.statusCode == 401 || response.statusCode == 403) {
      await clearCookies();
      if (onUnauthorized != null) {
        onUnauthorized!();
      }
    }

    throw ApiException(
      statusCode: response.statusCode,
      message: errorMessage,
      data: body,
    );
  }

  Future<void> _handleException(Object e) async {
    if (e is SocketException || e is http.ClientException) {
      await clearCookies();
      if (onUnauthorized != null) {
        onUnauthorized!();
      }
    }
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
      return await _processResponse(response, url, 'GET');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] GET $url -> $e');
      }
      await _handleException(e);
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
      return await _processResponse(response, url, 'POST');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] POST $url -> $e');
      }
      await _handleException(e);
      rethrow;
    }
  }

  Future<dynamic> put(
    String endpoint, {
    dynamic body,
    Map<String, String>? headers,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final builtHeaders = _buildHeaders(headers);
    final jsonBody = body != null ? jsonEncode(body) : null;

    if (kDebugMode) {
      debugPrint('🌐 [HTTP Request] PUT $url');
      debugPrint('   Headers: $builtHeaders');
      if (jsonBody != null) {
        debugPrint('   Body: $jsonBody');
      }
    }

    try {
      final response = await http.put(
        url,
        headers: builtHeaders,
        body: jsonBody,
      );
      return await _processResponse(response, url, 'PUT');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] PUT $url -> $e');
      }
      await _handleException(e);
      rethrow;
    }
  }

  Future<dynamic> delete(
    String endpoint, {
    Map<String, String>? headers,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final builtHeaders = _buildHeaders(headers);

    if (kDebugMode) {
      debugPrint('🌐 [HTTP Request] DELETE $url');
      debugPrint('   Headers: $builtHeaders');
    }

    try {
      final response = await http.delete(url, headers: builtHeaders);
      return await _processResponse(response, url, 'DELETE');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] DELETE $url -> $e');
      }
      await _handleException(e);
      rethrow;
    }
  }

  Future<dynamic> patch(
    String endpoint, {
    dynamic body,
    Map<String, String>? headers,
  }) async {
    final url = Uri.parse('$baseUrl$endpoint');
    final builtHeaders = _buildHeaders(headers);
    final jsonBody = body != null ? jsonEncode(body) : null;

    if (kDebugMode) {
      debugPrint('🌐 [HTTP Request] PATCH $url');
      debugPrint('   Headers: $builtHeaders');
      if (jsonBody != null) {
        debugPrint('   Body: $jsonBody');
      }
    }

    try {
      final response = await http.patch(
        url,
        headers: builtHeaders,
        body: jsonBody,
      );
      return await _processResponse(response, url, 'PATCH');
    } catch (e) {
      if (kDebugMode) {
        debugPrint('⚠️ [HTTP Exception] PATCH $url -> $e');
      }
      await _handleException(e);
      rethrow;
    }
  }
}

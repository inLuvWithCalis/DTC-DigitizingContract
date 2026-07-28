import '../models/auth_dto.dart';
import 'api_client.dart';

class AuthApi {
  static final ApiClient _apiClient = ApiClient();

  /// Đăng nhập vào hệ thống
  /// Mirrors: authApi.login(payload: LoginRequestDto, tenantCode: string)
  static Future<LoginResponseDto> login(
    String tenantCode, {
    required String accountName,
    required String password,
  }) async {
    final payload = LoginRequestDto(
      accountName: accountName,
      password: password,
    );

    final response = await _apiClient.post(
      '/Auth/login',
      body: payload.toJson(),
      headers: {
        'X-Tenant-Code': tenantCode,
      },
    );

    return LoginResponseDto.fromJson(response as Map<String, dynamic>);
  }

  /// Lấy thông tin tài khoản đang đăng nhập
  /// Mirrors: authApi.getMe()
  static Future<UserProfileDto> getMe() async {
    final response = await _apiClient.get('/Auth/me');
    return UserProfileDto.fromJson(response as Map<String, dynamic>);
  }

  /// Đăng xuất khỏi hệ thống
  /// Mirrors: authApi.logout()
  static Future<String> logout() async {
    String message = "Đăng xuất thành công!";
    try {
      final response = await _apiClient.post('/Auth/logout');
      if (response is Map<String, dynamic> && response['message'] != null) {
        message = response['message'].toString();
      }
    } finally {
      _apiClient.clearCookies();
    }
    return message;
  }
}

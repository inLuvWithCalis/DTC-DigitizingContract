import 'package:flutter_dotenv/flutter_dotenv.dart';

abstract class AppConfig {
  /// Base API URL duy nhất được định nghĩa tại file .env
  static String get apiBaseUrl {
    final envUrl = dotenv.env['API_BASE_URL'];
    if (envUrl != null && envUrl.isNotEmpty) {
      return envUrl;
    }
    return const String.fromEnvironment('API_BASE_URL');
  }

  static const String appName = 'Digitizing Contract';

  static const int apiTimeoutSeconds = 30;
}

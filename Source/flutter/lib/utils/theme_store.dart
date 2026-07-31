import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Class quản lý việc lưu và đọc trạng thái Dark/Light Theme của ứng dụng vào SharedPreferences.
class ThemeStore {
  static const String _key = 'is_dark_theme';

  /// Khởi tạo themeModeNotifier từ giá trị đã lưu trong bộ nhớ
  static Future<void> initTheme(ValueNotifier<ThemeMode> notifier) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final isDark = prefs.getBool(_key) ?? false;
      notifier.value = isDark ? ThemeMode.dark : ThemeMode.light;
    } catch (_) {}
  }

  /// Lưu trạng thái Theme (Tối / Sáng) vào bộ nhớ
  static Future<void> saveThemeMode(bool isDark) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool(_key, isDark);
    } catch (_) {}
  }
}

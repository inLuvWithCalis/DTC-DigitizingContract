import 'package:shared_preferences/shared_preferences.dart';

/// Class lưu trữ và đọc danh sách Lối tắt (Quick Actions) đã tùy chỉnh của người dùng vào SharedPreferences
class QuickActionsStore {
  static const String _key = 'user_quick_action_ids';

  static const List<String> defaultIds = [
    'services',
    'service_types',
    'create_contract',
    'approve_contract',
    'price_list',
    'contract_templates',
    'renew_contract',
    'liquidate_contract',
  ];

  /// Đọc danh sách lối tắt từ bộ nhớ thiết bị
  static Future<List<String>> loadQuickActionIds() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final saved = prefs.getStringList(_key);
      if (saved != null && saved.isNotEmpty) {
        return saved;
      }
    } catch (_) {}
    return List.from(defaultIds);
  }

  /// Lưu danh sách lối tắt vào bộ nhớ thiết bị
  static Future<void> saveQuickActionIds(List<String> ids) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setStringList(_key, ids);
    } catch (_) {}
  }
}

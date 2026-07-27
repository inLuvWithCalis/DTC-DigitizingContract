import 'package:flutter/material.dart';

/// Class tiện ích quản lý và hiển thị Toast (SnackBar) dùng chung cho toàn ứng dụng Flutter.
abstract class AppToast {
  /// Hiển thị thông báo Toast chuẩn hóa
  static void show(
    BuildContext context,
    String message, {
    bool isError = false,
    Duration duration = const Duration(seconds: 3),
  }) {
    if (!context.mounted) return;

    // Ẩn SnackBar hiện tại nếu có để hiển thị ngay thông báo mới
    ScaffoldMessenger.of(context).hideCurrentSnackBar();

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Icon(
              isError
                  ? Icons.error_outline_rounded
                  : Icons.check_circle_outline_rounded,
              color: Colors.white,
              size: 20,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 14,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
        backgroundColor: isError ? Colors.red.shade600 : Colors.green.shade600,
        behavior: SnackBarBehavior.floating,
        margin: const EdgeInsets.all(16),
        duration: duration,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }

  /// Shortcut hiển thị thông báo thành công
  static void success(BuildContext context, String message) {
    show(context, message, isError: false);
  }

  /// Shortcut hiển thị thông báo lỗi
  static void error(BuildContext context, String message) {
    show(context, message, isError: true);
  }
}

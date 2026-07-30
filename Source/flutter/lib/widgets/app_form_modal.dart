import 'package:flutter/material.dart';

/// Hỗ trợ chuẩn hóa UI của các form bottom sheet
class AppFormModal extends StatelessWidget {
  final String title;
  final bool viewOnly;
  final bool isEditMode;
  final bool isSaving;
  final VoidCallback? onSubmit;
  final VoidCallback? onCancel;
  final List<Widget> children;

  /// Cung cấp nhãn nút tuỳ chỉnh nếu không dùng mặc định
  final String? submitText;

  const AppFormModal({
    super.key,
    required this.title,
    this.viewOnly = false,
    this.isEditMode = false,
    this.isSaving = false,
    this.onSubmit,
    this.onCancel,
    required this.children,
    this.submitText,
  });

  /// Hàm tiện ích để hiện bottom sheet với padding tự động cho bàn phím
  static Future<T?> show<T>({
    required BuildContext context,
    required Widget child,
  }) async {
    return await showModalBottomSheet<T>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom,
        ),
        child: child,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header title bar
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close_rounded),
                  onPressed: onCancel ?? () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 12),

            // Content body
            ...children,

            const SizedBox(height: 24),

            // Buttons Footer
            Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                TextButton(
                  onPressed: isSaving
                      ? null
                      : (onCancel ?? () => Navigator.of(context).pop()),
                  child: Text(viewOnly ? 'Đóng' : 'Hủy bỏ'),
                ),
                if (!viewOnly) ...[
                  const SizedBox(width: 12),
                  ElevatedButton(
                    onPressed: isSaving ? null : onSubmit,
                    child: isSaving
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: Colors.white,
                            ),
                          )
                        : Text(
                            submitText ??
                                (isEditMode ? 'Cập nhật' : 'Tạo mới'),
                          ),
                  ),
                ],
              ],
            ),
            const SizedBox(height: 12),
          ],
        ),
      ),
    );
  }
}

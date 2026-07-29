import 'package:flutter/material.dart';

/// Mô tả một action trong bulk action menu.
class AppBulkActionItem {
  final String title;
  final IconData icon;

  /// Nếu null, dùng màu lỗi (đỏ) mặc định từ theme
  final Color? color;
  final VoidCallback onTap;

  const AppBulkActionItem({
    required this.title,
    required this.icon,
    this.color,
    required this.onTap,
  });
}

/// Nút bulk action tái sử dụng: hiện 1 icon button,
/// khi bấm mở BottomSheet danh sách các action.
class AppBulkActionButton extends StatelessWidget {
  final int selectedCount;
  final List<AppBulkActionItem> actions;

  const AppBulkActionButton({
    super.key,
    required this.selectedCount,
    required this.actions,
  });

  void _showMenu(BuildContext context) {
    final theme = Theme.of(context);

    showModalBottomSheet(
      context: context,
      useSafeArea: true,
      showDragHandle: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 5,
                        ),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.primary.withValues(
                            alpha: 0.1,
                          ),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          'Đã chọn $selectedCount mục',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                      ),
                      const Spacer(),
                      Text(
                        'Thao tác hàng loạt',
                        style: TextStyle(
                          fontSize: 12,
                          color: theme.colorScheme.onSurface.withValues(
                            alpha: 0.5,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                // Divider
                Divider(
                  color: theme.colorScheme.outlineVariant.withValues(
                    alpha: 0.4,
                  ),
                  height: 1,
                ),
                const SizedBox(height: 8),

                // Action items
                ...actions.map((action) {
                  final actionColor =
                      action.color ?? theme.colorScheme.onSurface;
                  return InkWell(
                    onTap: () {
                      Navigator.of(ctx).pop();
                      action.onTap();
                    },
                    borderRadius: BorderRadius.circular(14),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 14,
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 40,
                            height: 40,
                            decoration: BoxDecoration(
                              color: actionColor.withValues(alpha: 0.1),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Icon(
                              action.icon,
                              size: 20,
                              color: actionColor,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Text(
                            action.title,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w500,
                              color: actionColor,
                            ),
                          ),
                          const Spacer(),
                          Icon(
                            Icons.chevron_right_rounded,
                            size: 18,
                            color: theme.colorScheme.onSurface.withValues(
                              alpha: 0.3,
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                }),
              ],
            ),
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return IconButton.filled(
      style: IconButton.styleFrom(
        backgroundColor: theme.colorScheme.primary,
        foregroundColor: theme.colorScheme.onPrimary,
        minimumSize: const Size(40, 40),
        maximumSize: const Size(40, 40),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
      icon: const Icon(Icons.more_vert_rounded, size: 20),
      tooltip: 'Thao tác với $selectedCount mục',
      onPressed: () => _showMenu(context),
    );
  }
}

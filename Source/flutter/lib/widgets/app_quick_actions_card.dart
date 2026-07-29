import 'package:flutter/material.dart';

class QuickActionData {
  final String title;
  final IconData icon;
  final Color color;
  final Color bgColor;
  final String? badgeText;
  final Color? badgeColor;
  final VoidCallback? onTap;

  const QuickActionData({
    required this.title,
    required this.icon,
    required this.color,
    required this.bgColor,
    this.badgeText,
    this.badgeColor,
    this.onTap,
  });
}

class AppQuickActionsCard extends StatefulWidget {
  final List<QuickActionData> items;
  final int initialVisibleCount;

  const AppQuickActionsCard({
    super.key,
    required this.items,
    this.initialVisibleCount = 4,
  });

  @override
  State<AppQuickActionsCard> createState() => _AppQuickActionsCardState();
}

class _AppQuickActionsCardState extends State<AppQuickActionsCard> {
  bool _isExpanded = false;
  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final collapsedItems = widget.items
        .take(widget.initialVisibleCount)
        .toList();
    final expandedItems = widget.items
        .skip(widget.initialVisibleCount)
        .toList();

    return Stack(
      clipBehavior: Clip.none,
      alignment: Alignment.bottomCenter,
      children: [
        Container(
          width: double.infinity,
          margin: const EdgeInsets.only(bottom: 18),
          padding: const EdgeInsets.only(
            left: 14,
            right: 14,
            top: 20,
            bottom: 24, // Padding trong của thẻ
          ),
          decoration: BoxDecoration(
            color: theme.colorScheme.surface,
            borderRadius: BorderRadius.circular(24),
            border: Border.all(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.04),
                blurRadius: 14,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            children: [
              _buildItemGrid(context, collapsedItems),
              AnimatedSize(
                duration: const Duration(milliseconds: 300),
                curve: Curves.easeInOut,
                child: _isExpanded && expandedItems.isNotEmpty
                    ? Padding(
                        padding: const EdgeInsets.only(top: 18.0),
                        child: _buildItemGrid(context, expandedItems),
                      )
                    : const SizedBox.shrink(),
              ),
            ],
          ),
        ),

        if (widget.items.length > widget.initialVisibleCount)
          Positioned(
            bottom:
                0, // 👈 Đã chỉnh về 0, toàn bộ diện tích nút nằm trong ranh giới Hit-Test của Stack
            child: Material(
              color: Colors.transparent,
              child: InkWell(
                onTap: () {
                  setState(() {
                    _isExpanded = !_isExpanded;
                  });
                },
                borderRadius: BorderRadius.circular(20),
                child: AnimatedRotation(
                  turns: _isExpanded ? 0.5 : 0.0,
                  duration: const Duration(milliseconds: 300),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 300),
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: _isExpanded
                          ? theme.colorScheme.onPrimary
                          : theme.colorScheme.primary,
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: _isExpanded
                              ? Colors.black.withValues(alpha: 0.08)
                              : theme.colorScheme.primary.withValues(
                                  alpha: 0.4,
                                ),
                          blurRadius: 8,
                          offset: const Offset(0, 3),
                        ),
                      ],
                    ),
                    child: Icon(
                      Icons.keyboard_arrow_down_rounded,
                      color: _isExpanded
                          ? theme.colorScheme.primary
                          : Colors.white,
                      size: 24,
                    ),
                  ),
                ),
              ),
            ),
          ),
      ],
    );
  }

  /// Hàm dựng Grid chia đều 4 cột (Giữ đúng tỉ lệ kể cả khi hàng cuối thiếu item)
  Widget _buildItemGrid(BuildContext context, List<QuickActionData> itemList) {
    final rows = <List<QuickActionData>>[];
    for (var i = 0; i < itemList.length; i += 4) {
      rows.add(
        itemList.sublist(i, i + 4 > itemList.length ? itemList.length : i + 4),
      );
    }

    return Column(
      children: [
        for (int r = 0; r < rows.length; r++) ...[
          if (r > 0) const SizedBox(height: 18),
          Row(
            children: [
              for (int c = 0; c < 4; c++)
                Expanded(
                  child: c < rows[r].length
                      ? _buildItemCard(context, rows[r][c])
                      : const SizedBox.shrink(), // Giữ khung trống để không vỡ layout
                ),
            ],
          ),
        ],
      ],
    );
  }

  Widget _buildItemCard(BuildContext context, QuickActionData item) {
    final theme = Theme.of(context);

    return InkWell(
      onTap: item.onTap,
      borderRadius: BorderRadius.circular(20),
      child: Column(
        children: [
          Stack(
            clipBehavior: Clip.none,
            children: [
              Container(
                width: 58,
                height: 58,
                decoration: BoxDecoration(
                  color: item.bgColor, // Dùng màu nền tùy chỉnh từ model
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: theme.colorScheme.outlineVariant.withValues(
                      alpha: 0.3,
                    ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 8,
                      offset: const Offset(0, 3),
                    ),
                  ],
                ),
                child: Icon(item.icon, color: item.color, size: 26),
              ),
              if (item.badgeText != null)
                Positioned(
                  top: -6,
                  right: -6,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 5,
                      vertical: 2,
                    ),
                    decoration: BoxDecoration(
                      color: item.badgeColor ?? const Color(0xFFE11D48),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      item.badgeText!,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 8,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            item.title,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: theme.colorScheme.onSurface,
              height: 1.2,
            ),
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

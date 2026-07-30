import 'package:flutter/material.dart';

class AppFloatingDockNavItemData {
  final String label;
  final IconData icon;
  final IconData activeIcon;

  const AppFloatingDockNavItemData({
    required this.label,
    required this.icon,
    required this.activeIcon,
  });
}

class AppFloatingDockNavBar extends StatelessWidget {
  final int currentIndex;
  final ValueChanged<int> onTap;
  final List<AppFloatingDockNavItemData>? items;

  static const List<AppFloatingDockNavItemData> defaultItems = [
    AppFloatingDockNavItemData(
      label: 'Trang chủ',
      icon: Icons.home_outlined,
      activeIcon: Icons.home_rounded,
    ),
    AppFloatingDockNavItemData(
      label: 'Hợp đồng',
      icon: Icons.description_outlined,
      activeIcon: Icons.description_rounded,
    ),
    AppFloatingDockNavItemData(
      label: 'Danh mục',
      icon: Icons.category_outlined,
      activeIcon: Icons.category_rounded,
    ),
    AppFloatingDockNavItemData(
      label: 'Báo cáo',
      icon: Icons.analytics_outlined,
      activeIcon: Icons.analytics_rounded,
    ),
    AppFloatingDockNavItemData(
      label: 'Khác',
      icon: Icons.grid_view_outlined,
      activeIcon: Icons.grid_view_rounded,
    ),
  ];

  const AppFloatingDockNavBar({
    super.key,
    required this.currentIndex,
    required this.onTap,
    this.items,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final navItems = items ?? defaultItems;
    const barRadius = 28.0;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.only(left: 16, right: 16, bottom: 12),
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(barRadius),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: isDark ? 1 : 0.08),
                blurRadius: isDark ? 24 : 20,
                spreadRadius: 1,
                offset: const Offset(0, 8),
              ),
            ],
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(barRadius),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 2, vertical: 2),
              decoration: BoxDecoration(
                color: isDark
                    ? theme.colorScheme.surfaceContainerHighest.withValues(
                        alpha: 0.65,
                      )
                    : theme.colorScheme.surface,
                border: Border.all(
                  color: theme.colorScheme.outlineVariant.withValues(
                    alpha: isDark ? 0 : 0.4,
                  ),
                  width: 1,
                ),
              ),
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final itemWidth = constraints.maxWidth / navItems.length;
                  return Stack(
                    clipBehavior: Clip.none,
                    children: [
                      AnimatedPositioned(
                        duration: const Duration(milliseconds: 380),
                        curve: Curves.easeOutCubic,
                        left: itemWidth * currentIndex,
                        top: 0,
                        bottom: 0,
                        width: itemWidth,
                        child: _GlassPill(key: ValueKey(currentIndex)),
                      ),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceAround,
                        children: [
                          for (int i = 0; i < navItems.length; i++)
                            Expanded(
                              child: _buildNavItem(context, i, navItems[i]),
                            ),
                        ],
                      ),
                    ],
                  );
                },
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildNavItem(
    BuildContext context,
    int index,
    AppFloatingDockNavItemData item,
  ) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final isSelected = currentIndex == index;

    final unselectedColor = theme.colorScheme.onSurface.withValues(
      alpha: isDark ? 0.6 : 0.55,
    );

    return InkWell(
      onTap: () => onTap(index),
      borderRadius: BorderRadius.circular(24),
      splashColor: theme.colorScheme.primary.withValues(
        alpha: isDark ? 0.0 : 0.12,
      ),
      highlightColor: theme.colorScheme.primary.withValues(
        alpha: isDark ? 0.0 : 0.05,
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 0, vertical: 0),
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 0, vertical: 10),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TweenAnimationBuilder<double>(
                key: ValueKey('icon-$isSelected'),
                tween: Tween(begin: isSelected ? 0.7 : 1, end: 1),
                duration: const Duration(milliseconds: 380),
                curve: Curves.easeOutBack,
                builder: (context, scale, child) {
                  return Transform.scale(scale: scale, child: child);
                },
                child: Icon(
                  isSelected ? item.activeIcon : item.icon,
                  color: isSelected
                      ? theme.colorScheme.primary
                      : unselectedColor,
                  size: 22,
                ),
              ),
              const SizedBox(height: 2),
              AnimatedDefaultTextStyle(
                duration: const Duration(milliseconds: 250),
                curve: Curves.easeInOut,
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                  color: isSelected
                      ? theme.colorScheme.primary
                      : unselectedColor,
                ),
                child: Text(
                  item.label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Viên kính highlight, trượt theo tab được chọn (_AppFloatingDockNavBar).
/// Mô phỏng phong cách Liquid Glass: nền mờ, viền sáng mảnh, bóng đổ
/// nhẹ và hiệu ứng "phồng" lên khi xuất hiện ở vị trí mới.
class _GlassPill extends StatelessWidget {
  const _GlassPill({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0.85, end: 1),
      duration: const Duration(milliseconds: 380),
      curve: Curves.easeOutBack,
      builder: (context, scale, child) {
        return Transform.scale(scale: scale, child: child);
      },
      child: Container(
        margin: const EdgeInsets.symmetric(horizontal: 2, vertical: 2),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(24),
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              theme.colorScheme.primary.withValues(alpha: isDark ? 0.32 : 0.20),
              theme.colorScheme.primary.withValues(alpha: isDark ? 0.16 : 0.10),
            ],
          ),
          border: Border.all(
            color: theme.colorScheme.primary.withValues(
              alpha: isDark ? 0.38 : 0.22,
            ),
            width: 1,
          ),
          boxShadow: [
            BoxShadow(
              color: theme.colorScheme.primary.withValues(
                alpha: isDark ? 0 : 0.20,
              ),
              blurRadius: isDark ? 16 : 14,
              spreadRadius: -3,
              offset: const Offset(0, 5),
            ),
            BoxShadow(
              color: isDark
                  ? Colors.white.withValues(alpha: 0.12)
                  : Colors.white.withValues(alpha: 0.5),
              blurRadius: 1,
              spreadRadius: -1,
              offset: const Offset(0, -1),
            ),
          ],
        ),
      ),
    );
  }
}

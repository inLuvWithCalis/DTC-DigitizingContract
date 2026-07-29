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
    final navItems = items ?? defaultItems;

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.only(left: 16, right: 16, bottom: 12),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
          decoration: BoxDecoration(
            color: theme.colorScheme.surface,
            borderRadius: BorderRadius.circular(32),
            border: Border.all(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
              width: 1,
            ),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.08),
                blurRadius: 20,
                spreadRadius: 1,
                offset: const Offset(0, 8),
              ),
            ],
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              for (int i = 0; i < navItems.length; i++)
                _buildNavItem(context, i, navItems[i]),
            ],
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
    final isSelected = currentIndex == index;

    return InkWell(
      onTap: () => onTap(index),
      borderRadius: BorderRadius.circular(24),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeInOut,
          padding: EdgeInsets.symmetric(
            horizontal: isSelected ? 14 : 10,
            vertical: 8,
          ),
          decoration: BoxDecoration(
            color: isSelected
                ? theme.colorScheme.primary.withValues(alpha: 0.12)
                : Colors.transparent,
            borderRadius: BorderRadius.circular(24),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                isSelected ? item.activeIcon : item.icon,
                color: isSelected
                    ? theme.colorScheme.primary
                    : theme.colorScheme.onSurface.withValues(alpha: 0.55),
                size: 22,
              ),
              if (isSelected) ...[
                const SizedBox(width: 6),
                Text(
                  item.label,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: theme.colorScheme.primary,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../utils/app_toast.dart';

class QuickActionData {
  final String id;
  final String title;
  final IconData icon;
  final Color color;
  final Color bgColor;
  final String? badgeText;
  final Color? badgeColor;
  final VoidCallback? onTap;

  const QuickActionData({
    required this.id,
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
  final ValueChanged<String>? onRemoveItem;
  final VoidCallback? onOpenAddBottomSheet;

  const AppQuickActionsCard({
    super.key,
    required this.items,
    this.initialVisibleCount = 4,
    this.onRemoveItem,
    this.onOpenAddBottomSheet,
  });

  @override
  State<AppQuickActionsCard> createState() => _AppQuickActionsCardState();
}

class _AppQuickActionsCardState extends State<AppQuickActionsCard> {
  bool _isExpanded = false;
  bool _isEditing = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Khi bật chỉnh sửa, hiển thị tất cả items + nút Thêm (nếu < 8)
    final displayItems = List<QuickActionData>.from(widget.items);
    if (_isEditing && displayItems.length < 8) {
      displayItems.add(
        QuickActionData(
          id: '__add__',
          title: 'Thêm',
          icon: Icons.add_rounded,
          color: theme.colorScheme.primary,
          bgColor: theme.colorScheme.primary.withValues(alpha: 0.1),
          onTap: widget.onOpenAddBottomSheet,
        ),
      );
    }

    final collapsedItems = displayItems
        .take(widget.initialVisibleCount)
        .toList();
    final expandedItems = displayItems
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
            top: 14,
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
              // Items Grid
              _buildItemGrid(context, collapsedItems),
              AnimatedSize(
                duration: const Duration(milliseconds: 300),
                curve: Curves.easeInOut,
                child: (_isExpanded || _isEditing) && expandedItems.isNotEmpty
                    ? Padding(
                        padding: const EdgeInsets.only(top: 18.0),
                        child: _buildItemGrid(context, expandedItems),
                      )
                    : const SizedBox.shrink(),
              ),

              if (_isExpanded || _isEditing) ...[
                const SizedBox(height: 10),
                Align(
                  alignment: Alignment.centerRight,
                  child: InkWell(
                    onTap: () {
                      setState(() {
                        _isEditing = !_isEditing;
                      });
                    },
                    borderRadius: BorderRadius.circular(12),
                    child: Padding(
                      padding: const EdgeInsets.only(
                        top: 16,
                        right: 8,
                        left: 8,
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const SizedBox(width: 4),
                          Text(
                            _isEditing ? 'Xong' : 'Tùy chỉnh',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: theme.colorScheme.primary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),

        if (widget.items.length > widget.initialVisibleCount || _isEditing)
          Positioned(
            bottom: 0,
            child: _ExpandToggleButton(
              isExpanded: _isExpanded || _isEditing,
              onTap: () {
                setState(() {
                  _isExpanded = !_isExpanded;
                  if (!_isExpanded) {
                    _isEditing = false;
                  }
                });
              },
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
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (int c = 0; c < 4; c++)
                Expanded(
                  child: c < rows[r].length
                      ? _buildItemCard(context, rows[r][c])
                      : const SizedBox.shrink(),
                ),
            ],
          ),
        ],
      ],
    );
  }

  Widget _buildItemCard(BuildContext context, QuickActionData item) {
    final theme = Theme.of(context);
    final isAddButton = item.id == '__add__';

    final cardContent = InkWell(
      onTap: () {
        if (isAddButton) {
          widget.onOpenAddBottomSheet?.call();
        } else {
          item.onTap?.call();
        }
      },
      borderRadius: BorderRadius.circular(20),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.start,
        children: [
          Stack(
            clipBehavior: Clip.none,
            children: [
              Container(
                width: 58,
                height: 58,
                decoration: BoxDecoration(
                  color: item.bgColor,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: isAddButton
                        ? theme.colorScheme.primary.withValues(alpha: 0.6)
                        : theme.colorScheme.outlineVariant.withValues(
                            alpha: 0.5,
                          ),
                    width: isAddButton ? 1.5 : 1.0,
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.08),
                      blurRadius: 8,
                      offset: const Offset(0, 3),
                    ),
                  ],
                ),
                child: Icon(item.icon, color: item.color, size: 26),
              ),

              // Normal Item Badge
              if (item.badgeText != null && !_isEditing)
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

              // Edit Mode Delete Button (-) with Pop-in animation
              Positioned(
                top: -4,
                right: -4,
                child: AnimatedScale(
                  scale: (_isEditing && !isAddButton) ? 1.0 : 0.0,
                  duration: const Duration(milliseconds: 280),
                  curve: Curves.easeOutBack,
                  child: GestureDetector(
                    onTap: () {
                      if (widget.items.length <= 1) {
                        AppToast.error(
                          context,
                          "Cần giữ lại tối thiểu 1 lối tắt",
                        );
                      } else {
                        widget.onRemoveItem?.call(item.id);
                      }
                    },
                    child: Container(
                      padding: const EdgeInsets.all(2),
                      decoration: const BoxDecoration(
                        color: Color(0xFFDC2626),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.remove_rounded,
                        color: Colors.white,
                        size: 14,
                      ),
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
              color: isAddButton
                  ? theme.colorScheme.primary
                  : theme.colorScheme.onSurface,
              height: 1.2,
            ),
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );

    if (isAddButton) {
      return TweenAnimationBuilder<double>(
        key: const ValueKey('__add_button__'),
        tween: Tween<double>(begin: 0.0, end: 1.0),
        duration: const Duration(milliseconds: 320),
        curve: Curves.easeOutBack,
        builder: (context, scale, child) {
          return Transform.scale(
            scale: scale,
            child: child,
          );
        },
        child: cardContent,
      );
    }

    return cardContent;
  }
}

class _ExpandToggleButton extends StatefulWidget {
  final bool isExpanded;
  final VoidCallback onTap;

  const _ExpandToggleButton({required this.isExpanded, required this.onTap});

  @override
  State<_ExpandToggleButton> createState() => _ExpandToggleButtonState();
}

class _ExpandToggleButtonState extends State<_ExpandToggleButton>
    with SingleTickerProviderStateMixin {
  late final AnimationController _bounceController;
  late final Animation<double> _bounceAnimation;

  @override
  void initState() {
    super.initState();
    _bounceController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _bounceAnimation = Tween<double>(begin: 0, end: -4).animate(
      CurvedAnimation(parent: _bounceController, curve: Curves.easeInOut),
    );
    _updateBounce();
  }

  @override
  void didUpdateWidget(covariant _ExpandToggleButton oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.isExpanded != widget.isExpanded) {
      _updateBounce();
    }
  }

  void _updateBounce() {
    if (widget.isExpanded) {
      _bounceController.stop();
      _bounceController.reverse();
    } else {
      _bounceController.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _bounceController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isExpanded = widget.isExpanded;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: widget.onTap,
        borderRadius: BorderRadius.circular(20),
        child: AnimatedBuilder(
          animation: _bounceAnimation,
          builder: (context, child) {
            return Transform.translate(
              offset: Offset(0, _bounceAnimation.value),
              child: child,
            );
          },
          child: TweenAnimationBuilder<double>(
            tween: Tween<double>(begin: 0, end: isExpanded ? math.pi : 0),
            duration: const Duration(milliseconds: 350),
            curve: Curves.easeInOutCubic,
            builder: (context, angle, child) {
              return Transform(
                transform: Matrix4.identity()
                  ..setEntry(3, 2, 0.0015)
                  ..rotateX(angle),
                alignment: Alignment.center,
                child: child,
              );
            },
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 350),
              width: 36,
              height: 36,
              decoration: BoxDecoration(
                color: isExpanded
                    ? theme.colorScheme.onPrimary
                    : theme.colorScheme.primary,
                shape: BoxShape.circle,
                boxShadow: [
                  BoxShadow(
                    color: isExpanded
                        ? theme.colorScheme.primary.withValues(alpha: 1)
                        : theme.colorScheme.primary.withValues(alpha: 0),
                    blurRadius: 0,
                    offset: const Offset(0, 0),
                  ),
                ],
              ),
              child: Icon(
                Icons.keyboard_arrow_down_rounded,
                color: isExpanded ? theme.colorScheme.primary : Colors.white,
                size: 24,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

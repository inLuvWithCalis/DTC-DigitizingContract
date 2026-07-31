import 'package:flutter/material.dart';
import '../../utils/app_toast.dart';

/// Modal Bottom Sheet hiển thị Hệ sinh thái tất cả dịch vụ doanh nghiệp (Grid 4 cột)
/// Hỗ trợ cả chế độ xem thông thường và chế độ tick chọn Chỉnh sửa Lối tắt (Min 1, Max 8).
class AppServicesBottomSheet {
  static void show(
    BuildContext context, {
    bool isEditMode = false,
    List<String>? selectedIds,
    ValueChanged<List<String>>? onSelectedIdsChanged,
  }) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) {
        return _AppServicesBottomSheetContent(
          isEditMode: isEditMode,
          initialSelectedIds: selectedIds ?? [],
          onSelectedIdsChanged: onSelectedIdsChanged,
        );
      },
    );
  }
}

class _AppServicesBottomSheetContent extends StatefulWidget {
  final bool isEditMode;
  final List<String> initialSelectedIds;
  final ValueChanged<List<String>>? onSelectedIdsChanged;

  const _AppServicesBottomSheetContent({
    required this.isEditMode,
    required this.initialSelectedIds,
    this.onSelectedIdsChanged,
  });

  @override
  State<_AppServicesBottomSheetContent> createState() =>
      __AppServicesBottomSheetContentState();
}

class __AppServicesBottomSheetContentState
    extends State<_AppServicesBottomSheetContent> {
  late List<String> _selectedIds;

  @override
  void initState() {
    super.initState();
    _selectedIds = List.from(widget.initialSelectedIds);
  }

  void _toggleItem(String id) {
    if (_selectedIds.contains(id)) {
      if (_selectedIds.length <= 1) {
        AppToast.error(context, "Cần giữ lại tối thiểu 1 lối tắt");
        return;
      }
      setState(() {
        _selectedIds.remove(id);
      });
    } else {
      if (_selectedIds.length >= 8) {
        AppToast.error(context, "Tối đa chọn 8 lối tắt");
        return;
      }
      setState(() {
        _selectedIds.add(id);
      });
    }
    widget.onSelectedIdsChanged?.call(_selectedIds);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final primaryColor = theme.colorScheme.primary;
    final onPrimaryColor = theme.colorScheme.onPrimary;

    return Container(
      height: MediaQuery.of(context).size.height * 0.82,
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(28)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.15),
            blurRadius: 20,
            offset: const Offset(0, -6),
          ),
        ],
      ),
      child: Column(
        children: [
          // Handle Bar
          const SizedBox(height: 12),
          Container(
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.6),
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 12),

          // Header
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: primaryColor.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Icon(
                        widget.isEditMode
                            ? Icons.edit_note_rounded
                            : Icons.grid_view_rounded,
                        color: primaryColor,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          widget.isEditMode
                              ? 'Chỉnh sửa lối tắt'
                              : 'Tất cả dịch vụ',
                          style: const TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Text(
                          widget.isEditMode
                              ? 'Đã chọn ${_selectedIds.length}/8 lối tắt'
                              : 'Hệ sinh thái ứng dụng doanh nghiệp',
                          style: TextStyle(
                            fontSize: 12,
                            color: theme.colorScheme.onSurface.withValues(
                              alpha: 0.6,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
                Row(
                  children: [
                    if (widget.isEditMode)
                      IconButton(
                        icon: const Icon(Icons.close_rounded),
                        onPressed: () => Navigator.of(context).pop(),
                      ),
                  ],
                ),
              ],
            ),
          ),

          const SizedBox(height: 12),
          Divider(
            height: 1,
            color: theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
          ),

          // Body Grid Sections
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Section 1: Quản lý danh mục
                  _buildSectionTitle(
                    'QUẢN LÝ DANH MỤC',
                    Icons.folder_copy_rounded,
                    primaryColor,
                  ),
                  const SizedBox(height: 12),
                  GridView.count(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisCount: 4,
                    mainAxisSpacing: 16,
                    crossAxisSpacing: 12,
                    childAspectRatio: 0.72,
                    children: [
                      _buildItem(
                        context,
                        id: 'services',
                        title: 'Dịch vụ',
                        icon: Icons.miscellaneous_services_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () {
                          Navigator.of(context).pop();
                          Navigator.of(context).pushNamed('/catalog/services');
                        },
                      ),
                      _buildItem(
                        context,
                        id: 'service_types',
                        title: 'Loại dịch vụ',
                        icon: Icons.category_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () {
                          Navigator.of(context).pop();
                          Navigator.of(
                            context,
                          ).pushNamed('/catalog/service-types');
                        },
                      ),
                      _buildItem(
                        context,
                        id: 'price_list',
                        title: 'Bảng giá',
                        icon: Icons.sell_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'contract_templates',
                        title: 'Mẫu HĐ',
                        icon: Icons.description_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                    ],
                  ),

                  const SizedBox(height: 24),

                  // Section 2: Nghiệp vụ Hợp đồng
                  _buildSectionTitle(
                    'NGHIỆP VỤ HỢP ĐỒNG',
                    Icons.article_rounded,
                    primaryColor,
                  ),
                  const SizedBox(height: 12),
                  GridView.count(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisCount: 4,
                    mainAxisSpacing: 16,
                    crossAxisSpacing: 12,
                    childAspectRatio: 0.72,
                    children: [
                      _buildItem(
                        context,
                        id: 'create_contract',
                        title: 'Tạo mới HĐ',
                        icon: Icons.post_add_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'approve_contract',
                        title: 'Phê duyệt',
                        icon: Icons.verified_user_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'renew_contract',
                        title: 'Gia hạn HĐ',
                        icon: Icons.update_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'liquidate_contract',
                        title: 'Thanh lý',
                        icon: Icons.task_alt_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                    ],
                  ),

                  const SizedBox(height: 24),

                  // Section 3: Báo cáo & Hệ thống
                  _buildSectionTitle(
                    'BÁO CÁO & HỆ THỐNG',
                    Icons.bar_chart_rounded,
                    primaryColor,
                  ),
                  const SizedBox(height: 12),
                  GridView.count(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisCount: 4,
                    mainAxisSpacing: 16,
                    crossAxisSpacing: 12,
                    childAspectRatio: 0.72,
                    children: [
                      _buildItem(
                        context,
                        id: 'revenue',
                        title: 'Doanh thu',
                        icon: Icons.pie_chart_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'traffic',
                        title: 'Lưu lượng',
                        icon: Icons.show_chart_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                      _buildItem(
                        context,
                        id: 'activity_logs',
                        title: 'Nhật ký',
                        icon: Icons.history_toggle_off_rounded,
                        color: primaryColor,
                        bgColor: onPrimaryColor,
                        onTapNormal: () => Navigator.of(context).pop(),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSectionTitle(String title, IconData icon, Color color) {
    return Row(
      children: [
        Icon(icon, size: 16, color: color),
        const SizedBox(width: 8),
        Text(
          title,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.bold,
            letterSpacing: 0.5,
            color: color,
          ),
        ),
      ],
    );
  }

  Widget _buildItem(
    BuildContext context, {
    required String id,
    required String title,
    required IconData icon,
    required Color color,
    required Color bgColor,
    required VoidCallback onTapNormal,
  }) {
    final theme = Theme.of(context);
    final isSelected = _selectedIds.contains(id);

    return InkWell(
      onTap: () {
        if (widget.isEditMode) {
          _toggleItem(id);
        } else {
          onTapNormal();
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
                  color: bgColor,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: isSelected && widget.isEditMode
                        ? theme.colorScheme.primary
                        : theme.colorScheme.outlineVariant.withValues(
                            alpha: 0.5,
                          ),
                    width: isSelected && widget.isEditMode ? 2 : 1,
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.08),
                      blurRadius: 8,
                      offset: const Offset(0, 3),
                    ),
                  ],
                ),
                child: Icon(icon, color: color, size: 26),
              ),

              // Edit mode Selection Badge
              if (widget.isEditMode)
                Positioned(
                  top: -4,
                  right: -4,
                  child: Container(
                    padding: const EdgeInsets.all(3),
                    decoration: BoxDecoration(
                      color: isSelected
                          ? theme.colorScheme.primary
                          : theme.colorScheme.outlineVariant,
                      shape: BoxShape.circle,
                      border: Border.all(color: Colors.white, width: 1.5),
                    ),
                    child: Icon(
                      isSelected ? Icons.check_rounded : Icons.add_rounded,
                      color: Colors.white,
                      size: 12,
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            title,
            style: TextStyle(
              fontSize: 12,
              fontWeight: isSelected && widget.isEditMode
                  ? FontWeight.bold
                  : FontWeight.w600,
              color: isSelected && widget.isEditMode
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
  }
}

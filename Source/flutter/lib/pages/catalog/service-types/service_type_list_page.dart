import 'package:flutter/material.dart';
import '../../../models/catalog/service_type_dto.dart';
import '../../../services/catalog/service_types_api.dart';
import '../../../utils/app_toast.dart';
import '../../../widgets/app_mobile_data_table.dart';
import '../../../widgets/app_skeleton.dart';
import 'service_type_form_dialog.dart';

class ServiceTypeListPage extends StatefulWidget {
  const ServiceTypeListPage({super.key});

  @override
  State<ServiceTypeListPage> createState() => _ServiceTypeListPageState();
}

class _ServiceTypeListPageState extends State<ServiceTypeListPage> {
  List<ServiceTypeResponse> _items = [];
  int _totalCount = 0;
  int _page = 1;
  final int _pageSize = 10;
  int _totalPages = 1;
  bool _isLoading = false;
  String _searchTerm = '';

  @override
  void initState() {
    super.initState();
    _fetchServiceTypes();
  }

  Future<void> _fetchServiceTypes() async {
    setState(() => _isLoading = true);
    try {
      final res = await ServiceTypeApi.getList(
        ServiceTypeFilterParams(
          page: _page,
          pageSize: _pageSize,
          keyword: _searchTerm.isNotEmpty ? _searchTerm : null,
        ),
      );

      setState(() {
        _items = res.items;
        _totalCount = res.totalCount;
        _page = res.page;
        _totalPages = res.totalPages;
      });
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Lỗi khi tải danh sách loại dịch vụ");
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _handleDeleteSingle(ServiceTypeResponse item) async {
    final confirm = await _showConfirmDeleteDialog(
      context,
      title: "Xóa loại dịch vụ?",
      description:
          "Bạn có chắc chắn muốn xóa loại dịch vụ \"${item.serviceTypeName}\"? Lưu ý: Không thể xóa nếu loại dịch vụ này đang có dịch vụ con sử dụng.",
    );

    if (confirm != true) return;

    try {
      await ServiceTypeApi.delete(item.serviceTypeId);
      if (mounted) {
        AppToast.success(context, "Đã xóa loại dịch vụ thành công");
      }
      _fetchServiceTypes();
    } catch (error) {
      if (mounted) {
        final msg = error.toString().replaceAll(RegExp(r'^Exception:\s*'), '');
        AppToast.error(
          context,
          msg.isNotEmpty ? msg : "Không thể xóa loại dịch vụ",
        );
      }
    }
  }

  Future<void> _handleDeleteBulk(
    List<ServiceTypeResponse> selectedItems,
    VoidCallback resetSelection,
  ) async {
    final confirm = await _showConfirmDeleteDialog(
      context,
      title: "Xóa hàng loạt?",
      description:
          "Bạn có chắc chắn muốn xóa ${selectedItems.length} loại dịch vụ đã chọn?",
    );

    if (confirm != true) return;

    try {
      final ids = selectedItems.map((e) => e.serviceTypeId).toList();
      await ServiceTypeApi.deleteBulk(ids);
      if (mounted) {
        AppToast.success(context, "Đã xóa ${ids.length} loại dịch vụ");
      }
      resetSelection();
      _fetchServiceTypes();
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Lỗi hoặc loại dịch vụ đang được sử dụng");
      }
    }
  }

  Future<bool?> _showConfirmDeleteDialog(
    BuildContext context, {
    required String title,
    required String description,
  }) {
    return showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
        content: Text(description),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Hủy bỏ'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red.shade600,
              foregroundColor: Colors.white,
            ),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Xác nhận xóa'),
          ),
        ],
      ),
    );
  }

  int get _totalServiceUsed =>
      _items.fold(0, (sum, item) => sum + item.serviceCount);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Quản lý Loại dịch vụ'),
        scrolledUnderElevation: 2,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: _fetchServiceTypes,
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: AppMobileDataTable<ServiceTypeResponse>(
            items: _items,
            totalCount: _totalCount,
            page: _page,
            pageSize: _pageSize,
            totalPages: _totalPages,
            isLoading: _isLoading,
            searchPlaceholder: "Tên dịch vụ...",
            searchValue: _searchTerm,
            getItemId: (item) => item.serviceTypeId,
            onItemClick: (item) {
              ServiceTypeFormDialog.show(
                context,
                item: item,
                viewOnly: true,
                onSuccess: () {},
              );
            },
            onSearchChange: (value) {
              setState(() {
                _searchTerm = value;
                _page = 1;
              });
              _fetchServiceTypes();
            },
            onRefresh: _fetchServiceTypes,
            onPageChange: (newPage) {
              setState(() {
                _page = newPage;
              });
              _fetchServiceTypes();
            },

            // --- SUMMARY CARDS HEADER ---
            summaryHeader: _isLoading
                ? const AppSkeletonHeader()
                : Row(
                    children: [
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.primary.withValues(
                              alpha: 0.08,
                            ),
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(
                              color: theme.colorScheme.primary.withValues(
                                alpha: 0.2,
                              ),
                            ),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Icon(
                                    Icons.folder_copy_rounded,
                                    size: 18,
                                    color: theme.colorScheme.primary,
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    'Tổng loại dịch vụ',
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w500,
                                      color: theme.colorScheme.onSurface
                                          .withValues(alpha: 0.7),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 6),
                              Text(
                                '$_totalCount',
                                style: TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: theme.colorScheme.primary,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: const Color(
                              0xFF059669,
                            ).withValues(alpha: 0.08),
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(
                              color: const Color(
                                0xFF059669,
                              ).withValues(alpha: 0.2),
                            ),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  const Icon(
                                    Icons.layers_rounded,
                                    size: 18,
                                    color: Color(0xFF059669),
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    'Dịch vụ con (Trang)',
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w500,
                                      color: theme.colorScheme.onSurface
                                          .withValues(alpha: 0.7),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 6),
                              Text(
                                '$_totalServiceUsed',
                                style: const TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF059669),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),

            // --- ADD NEW ACTION BUTTON ---
            actionButton: IconButton.filled(
              style: IconButton.styleFrom(
                minimumSize: const Size(44, 44),
                maximumSize: const Size(44, 44),
                padding: EdgeInsets.zero,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              icon: const Icon(Icons.add_rounded, size: 20),
              onPressed: () {
                ServiceTypeFormDialog.show(
                  context,
                  onSuccess: _fetchServiceTypes,
                );
              },
            ),

            // --- BULK ACTIONS ---
            bulkActions: (selectedItems, resetSelection) => ElevatedButton.icon(
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red.shade600,
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 8,
                ),
              ),
              icon: const Icon(Icons.delete_outline_rounded, size: 16),
              label: Text('Xóa (${selectedItems.length})'),
              onPressed: () => _handleDeleteBulk(selectedItems, resetSelection),
            ),

            // --- MOBILE ITEM CARD RENDERER ---
            itemBuilder: (context, item, isSelected, onSelectToggle) {
              return Container(
                decoration: BoxDecoration(
                  color: isSelected
                      ? theme.colorScheme.primary.withValues(alpha: 0.05)
                      : theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(
                    color: isSelected
                        ? theme.colorScheme.primary.withValues(alpha: 0.5)
                        : theme.colorScheme.outlineVariant.withValues(
                            alpha: 0.5,
                          ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.02),
                      blurRadius: 8,
                      offset: const Offset(0, 3),
                    ),
                  ],
                ),
                child: Padding(
                  padding: const EdgeInsets.all(14.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (isSelected)
                            Checkbox(
                              value: isSelected,
                              onChanged: (val) => onSelectToggle(),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(4),
                              ),
                            ),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  'ID #${item.serviceTypeId}',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: theme.colorScheme.primary,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  item.serviceTypeName ?? "Chưa có tên",
                                  style: const TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          PopupMenuButton<String>(
                            icon: const Icon(Icons.more_vert_rounded),
                            onSelected: (action) {
                              if (action == 'view') {
                                ServiceTypeFormDialog.show(
                                  context,
                                  item: item,
                                  viewOnly: true,
                                  onSuccess: () {},
                                );
                              } else if (action == 'edit') {
                                ServiceTypeFormDialog.show(
                                  context,
                                  item: item,
                                  onSuccess: _fetchServiceTypes,
                                );
                              } else if (action == 'delete') {
                                _handleDeleteSingle(item);
                              }
                            },
                            itemBuilder: (context) => [
                              const PopupMenuItem(
                                value: 'view',
                                child: Row(
                                  children: [
                                    Icon(Icons.visibility_outlined, size: 18),
                                    SizedBox(width: 8),
                                    Text('Chi tiết'),
                                  ],
                                ),
                              ),
                              const PopupMenuItem(
                                value: 'edit',
                                child: Row(
                                  children: [
                                    Icon(Icons.edit_outlined, size: 18),
                                    SizedBox(width: 8),
                                    Text('Chỉnh sửa'),
                                  ],
                                ),
                              ),
                              PopupMenuItem(
                                value: 'delete',
                                child: Row(
                                  children: [
                                    Icon(
                                      Icons.delete_outline_rounded,
                                      size: 18,
                                      color: Colors.red.shade600,
                                    ),
                                    const SizedBox(width: 8),
                                    Text(
                                      'Xóa loại dịch vụ',
                                      style: TextStyle(
                                        color: Colors.red.shade600,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 8,
                        ),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.surfaceContainerHighest
                              .withValues(alpha: 0.3),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              children: [
                                Icon(
                                  Icons.description_outlined,
                                  size: 14,
                                  color: theme.colorScheme.primary,
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  '${item.serviceCount} dịch vụ',
                                  style: TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: theme.colorScheme.primary,
                                  ),
                                ),
                              ],
                            ),
                            Text(
                              item.langId != null
                                  ? 'Lang ID #${item.langId}'
                                  : 'Mặc định',
                              style: TextStyle(
                                fontSize: 12,
                                color: theme.colorScheme.onSurface.withValues(
                                  alpha: 0.6,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }
}

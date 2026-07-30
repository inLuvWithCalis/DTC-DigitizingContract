import 'package:flutter/material.dart';
import '../../../models/catalog/service_dto.dart';
import '../../../models/catalog/service_type_dto.dart';
import '../../../services/catalog/service_types_api.dart';
import '../../../services/catalog/services_api.dart';
import '../../../utils/app_date_utils.dart';
import '../../../utils/app_toast.dart';
import '../../../widgets/app_bulk_action_button.dart';
import '../../../widgets/app_filter_bar.dart';
import '../../../widgets/app_mobile_data_table.dart';
import '../../../widgets/app_skeleton.dart';
import 'service_form_dialog.dart';

class ServiceListPage extends StatefulWidget {
  const ServiceListPage({super.key});

  @override
  State<ServiceListPage> createState() => _ServiceListPageState();
}

class _ServiceListPageState extends State<ServiceListPage> {
  List<ServiceResponse> _items = [];
  int _totalCount = 0;
  int _page = 1;
  static const int _pageSize = 20;
  bool _hasMore = false;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String _searchTerm = '';

  // --- FILTER STATES ---
  int? _selectedStatus;
  int? _selectedServiceTypeId;
  DateTime? _fromDate;
  DateTime? _toDate;

  List<ServiceTypeResponse> _serviceTypes = [];

  @override
  void initState() {
    super.initState();
    _fetchServiceTypes();
    _fetchServices();
  }

  Future<void> _fetchServiceTypes() async {
    try {
      final res = await ServiceTypeApi.getList(
        ServiceTypeFilterParams(page: 1, pageSize: 100),
      );
      setState(() {
        _serviceTypes = res.items;
      });
    } catch (_) {}
  }

  /// Reset về trang 1, xóa danh sách cũ → dùng khi search hoặc filter thay đổi
  Future<void> _fetchServices() async {
    setState(() {
      _isLoading = true;
      _page = 1;
      _items = [];
    });
    try {
      final res = await ServiceApi.getList(
        ServiceFilterParams(
          page: 1,
          pageSize: _pageSize,
          keyword: _searchTerm.isNotEmpty ? _searchTerm : null,
          status: _selectedStatus,
          serviceTypeId: _selectedServiceTypeId,
          fromDate: AppDateUtils.formatToYMD(_fromDate),
          toDate: AppDateUtils.formatToYMD(_toDate),
        ),
      );

      if (mounted) {
        setState(() {
          _items = res.items;
          _totalCount = res.totalCount;
          _page = res.page;
          _hasMore = _items.length < _totalCount;
        });
      }
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Lỗi khi tải danh sách dịch vụ");
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  /// Load thêm page tiếp theo, gắn vào cuối danh sách hiện tại
  Future<void> _loadMoreServices() async {
    if (_isLoadingMore || !_hasMore) return;
    setState(() => _isLoadingMore = true);
    try {
      final nextPage = _page + 1;
      final res = await ServiceApi.getList(
        ServiceFilterParams(
          page: nextPage,
          pageSize: _pageSize,
          keyword: _searchTerm.isNotEmpty ? _searchTerm : null,
          status: _selectedStatus,
          serviceTypeId: _selectedServiceTypeId,
          fromDate: AppDateUtils.formatToYMD(_fromDate),
          toDate: AppDateUtils.formatToYMD(_toDate),
        ),
      );

      if (mounted) {
        setState(() {
          _items = [..._items, ...res.items];
          _page = res.page;
          _hasMore = _items.length < _totalCount;
        });
      }
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Lỗi khi tải thêm dịch vụ");
      }
    } finally {
      if (mounted) {
        setState(() => _isLoadingMore = false);
      }
    }
  }

  int get _activeFilterCount {
    int count = 0;
    if (_selectedStatus != null) count++;
    if (_selectedServiceTypeId != null) count++;
    if (_fromDate != null) count++;
    if (_toDate != null) count++;
    return count;
  }

  void _resetFilters() {
    setState(() {
      _selectedStatus = null;
      _selectedServiceTypeId = null;
      _fromDate = null;
      _toDate = null;
    });
    _fetchServices();
  }

  Future<void> _handleToggleStatus(ServiceResponse item) async {
    final newStatus = item.status == 1 ? 0 : 1;
    try {
      await ServiceApi.setStatus(item.serviceId, newStatus);
      if (mounted) {
        AppToast.success(
          context,
          newStatus == 1
              ? "Đã kích hoạt dịch vụ thành công"
              : "Đã tạm dừng dịch vụ",
        );
      }
      _fetchServices();
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Không thể cập nhật trạng thái");
      }
    }
  }

  Future<void> _handleDeleteSingle(ServiceResponse item) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text(
          'Xóa dịch vụ?',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        content: Text(
          'Bạn có chắc chắn muốn xóa dịch vụ "${item.serviceName}"?',
        ),
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

    if (confirm != true) return;

    try {
      await ServiceApi.delete(item.serviceId);
      if (mounted) {
        AppToast.success(context, "Đã xóa dịch vụ thành công");
      }
      _fetchServices();
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Không thể xóa dịch vụ");
      }
    }
  }

  Future<void> _handleDeleteBulk(
    List<ServiceResponse> selectedItems,
    VoidCallback resetSelection,
  ) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text(
          'Xóa hàng loạt?',
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        content: Text(
          'Bạn có chắc chắn muốn xóa ${selectedItems.length} dịch vụ đã chọn?',
        ),
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

    if (confirm != true) return;

    try {
      final ids = selectedItems.map((e) => e.serviceId).toList();
      await ServiceApi.deleteBulk(ids);
      if (mounted) {
        AppToast.success(context, "Đã xóa ${ids.length} dịch vụ");
      }
      resetSelection();
      _fetchServices();
    } catch (error) {
      if (mounted) {
        AppToast.error(context, "Không thể xóa dịch vụ đã chọn");
      }
    }
  }

  Future<void> _handleBulkSetStatus(
    List<ServiceResponse> selectedItems,
    VoidCallback resetSelection,
    int status,
  ) async {
    final label = status == 1 ? 'kích hoạt' : 'tạm dừng';
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(
          status == 1 ? 'Kích hoạt hàng loạt?' : 'Tạm dừng hàng loạt?',
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        content: Text(
          'Bạn có chắc chẫn muốn $label ${selectedItems.length} dịch vụ đã chọn?',
        ),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Hủy bỏ'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text('Đồng ý $label'),
          ),
        ],
      ),
    );

    if (confirm != true) return;

    try {
      final ids = selectedItems.map((e) => e.serviceId).toList();
      await ServiceApi.setBulkStatus(ids, status);
      if (mounted) {
        AppToast.success(
          context,
          status == 1
              ? 'Đã kích hoạt ${ids.length} dịch vụ'
              : 'Đã tạm dừng ${ids.length} dịch vụ',
        );
      }
      resetSelection();
      _fetchServices();
    } catch (error) {
      if (mounted) {
        AppToast.error(context, 'Không thể cập nhật trạng thái dịch vụ');
      }
    }
  }

  int get _activeCount => _items.where((i) => i.status == 1).length;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      extendBody: true,
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Quản lý Dịch vụ'),
        scrolledUnderElevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: _fetchServices,
          ),
        ],
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16.0),
          child: AppMobileDataTable<ServiceResponse>(
            items: _items,
            totalCount: _totalCount,
            isLoading: _isLoading,
            isLoadingMore: _isLoadingMore,
            hasMore: _hasMore,
            searchPlaceholder: "Tên dịch vụ...",
            searchValue: _searchTerm,
            getItemId: (item) => item.serviceId,
            activeFilterCount: _activeFilterCount,
            onApplyFilter: () {
              setState(() => _page = 1);
              _fetchServices();
            },

            onItemClick: (item) {
              ServiceFormDialog.show(
                context,
                item: item,
                viewOnly: true,
                onSuccess: () {},
              );
            },
            onSearchChange: (value) {
              setState(() {
                _searchTerm = value;
              });
              _fetchServices();
            },
            onRefresh: _fetchServices,
            onLoadMore: _loadMoreServices,

            // --- FILTER BAR EXPANDABLE SLOT ---
            filterBar: StatefulBuilder(
              builder: (context, setModalState) {
                return AppFilterBar(
                  setModalState: setModalState,
                  activeFilterCount: _activeFilterCount,
                  onResetFilters: () {
                    setModalState(() {
                      _selectedStatus = null;
                      _selectedServiceTypeId = null;
                      _fromDate = null;
                      _toDate = null;
                    });
                    _resetFilters();
                  },
                  statusValue: _selectedStatus,
                  onStatusChanged: (val) {
                    _selectedStatus = val;
                    setState(() {});
                  },
                  fromDate: _fromDate,
                  onFromDateChanged: (val) {
                    _fromDate = val;
                    setState(() {});
                  },
                  toDate: _toDate,
                  onToDateChanged: (val) {
                    _toDate = val;
                    setState(() {});
                  },
                  customFilters: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.surface,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color: theme.colorScheme.outlineVariant.withValues(
                            alpha: 0.5,
                          ),
                        ),
                      ),
                      child: DropdownButtonHideUnderline(
                        child: DropdownButton<int?>(
                          value: _selectedServiceTypeId,
                          isExpanded: true,
                          hint: const Text(
                            'Loại dịch vụ',
                            style: TextStyle(fontSize: 12),
                          ),
                          style: TextStyle(
                            fontSize: 13,
                            color: theme.colorScheme.onSurface,
                          ),
                          items: [
                            const DropdownMenuItem<int?>(
                              value: null,
                              child: Text('Tất cả loại DV'),
                            ),
                            ..._serviceTypes.map((st) {
                              return DropdownMenuItem<int?>(
                                value: st.serviceTypeId,
                                child: Text(
                                  st.serviceTypeName ??
                                      'Loại ${st.serviceTypeId}',
                                ),
                              );
                            }),
                          ],
                          onChanged: (val) {
                            setModalState(() {
                              _selectedServiceTypeId = val;
                            });
                            setState(() {});
                          },
                        ),
                      ),
                    ),
                  ],
                );
              },
            ),

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
                                    Icons.miscellaneous_services_rounded,
                                    size: 18,
                                    color: theme.colorScheme.primary,
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    'Tổng dịch vụ',
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
                                    Icons.check_circle_outline_rounded,
                                    size: 18,
                                    color: Color(0xFF059669),
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    'Hoạt động (Trang)',
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
                                '$_activeCount',
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
                ServiceFormDialog.show(context, onSuccess: _fetchServices);
              },
            ),

            // --- BULK ACTIONS ---
            bulkActions: (selectedItems, resetSelection) => AppBulkActionButton(
              selectedCount: selectedItems.length,
              actions: [
                AppBulkActionItem(
                  title: 'Kích hoạt ${selectedItems.length} dịch vụ',
                  icon: Icons.check_circle_outline_rounded,
                  color: const Color(0xFF059669),
                  onTap: () =>
                      _handleBulkSetStatus(selectedItems, resetSelection, 1),
                ),
                AppBulkActionItem(
                  title: 'Tạm dừng ${selectedItems.length} dịch vụ',
                  icon: Icons.pause_circle_outline_rounded,
                  color: theme.colorScheme.primary,
                  onTap: () =>
                      _handleBulkSetStatus(selectedItems, resetSelection, 0),
                ),
                AppBulkActionItem(
                  title: 'Xóa ${selectedItems.length} dịch vụ',
                  icon: Icons.delete_outline_rounded,
                  color: theme.colorScheme.error,
                  onTap: () => _handleDeleteBulk(selectedItems, resetSelection),
                ),
              ],
            ),

            itemBuilder: (context, item, isSelected, onSelectToggle) {
              final isActive = item.status == 1;

              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Text(
                                  'ID #${item.serviceId}',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: theme.colorScheme.primary,
                                  ),
                                ),
                                const SizedBox(width: 8),
                                Container(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 8,
                                    vertical: 2,
                                  ),
                                  decoration: BoxDecoration(
                                    color: isActive
                                        ? const Color(0xFF059669).withValues(alpha: 0.1)
                                        : Colors.grey.withValues(alpha: 0.1),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: Text(
                                    getServiceStatusLabel(item.status),
                                    style: TextStyle(
                                      fontSize: 10,
                                      fontWeight: FontWeight.w600,
                                      color: isActive
                                          ? const Color(0xFF059669)
                                          : Colors.grey.shade700,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              item.serviceName ?? "Chưa có tên",
                              style: const TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            if (item.serviceTypeName != null) ...[
                              const SizedBox(height: 2),
                              Text(
                                item.serviceTypeName!,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: theme.colorScheme.onSurface
                                      .withValues(alpha: 0.6),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                      PopupMenuButton<String>(
                        icon: const Icon(Icons.more_vert_rounded),
                        onSelected: (action) {
                          if (action == 'view') {
                            ServiceFormDialog.show(
                              context,
                              item: item,
                              viewOnly: true,
                              onSuccess: () {},
                            );
                          } else if (action == 'edit') {
                            ServiceFormDialog.show(
                              context,
                              item: item,
                              onSuccess: _fetchServices,
                            );
                          } else if (action == 'toggle_status') {
                            _handleToggleStatus(item);
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
                            value: 'toggle_status',
                            child: Row(
                              children: [
                                Icon(
                                  isActive
                                      ? Icons.pause_circle_outline_rounded
                                      : Icons.play_circle_outline_rounded,
                                  size: 18,
                                ),
                                const SizedBox(width: 8),
                                Text(isActive ? 'Tạm dừng' : 'Kích hoạt'),
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
                                  'Xóa dịch vụ',
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
                        Text(
                          item.servicePrice != null && item.servicePrice! > 0
                              ? 'Giá: ${item.servicePrice!.toStringAsFixed(0)} đ'
                              : 'Giá: Chưa cập nhật',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        Text(
                          item.dateCreated != null
                              ? 'Tạo: ${item.dateCreated!.substring(0, 10)}'
                              : 'Tạo: -',
                          style: TextStyle(
                            fontSize: 11,
                            color: theme.colorScheme.onSurface.withValues(
                              alpha: 0.6,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

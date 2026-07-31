import 'package:flutter/material.dart';

/// Item tùy chọn cho Filter Dropdown Trạng Thái
class FilterStatusOption {
  final int? value;
  final String label;

  const FilterStatusOption({required this.value, required this.label});
}

/// Component Bộ lọc dùng chung (Reusable Filter Bar)
/// Hỗ trợ mặc định:
/// - Filter Trạng thái (Status)
/// - Filter Khoảng thời gian (From Date -> To Date)
/// - Custom Filter Widgets (Truyền thêm bất kỳ dropdown/input tùy chỉnh nào)
class AppFilterBar extends StatelessWidget {
  final String title;
  final int activeFilterCount;
  final VoidCallback? onResetFilters;

  // Filter Trạng thái
  final bool showStatusFilter;
  final int? statusValue;
  final ValueChanged<int?>? onStatusChanged;
  final List<FilterStatusOption>? statusOptions;

  // Filter Ngày
  final bool showDateFilter;
  final DateTime? fromDate;
  final ValueChanged<DateTime?>? onFromDateChanged;
  final DateTime? toDate;
  final ValueChanged<DateTime?>? onToDateChanged;

  // Custom Filters (Tùy chọn bổ sung: loại dịch vụ, chi nhánh, v.v.)
  final List<Widget>? customFilters;

  /// StateSetter nếu được dùng bên trong StatefulBuilder của Modal
  final StateSetter? setModalState;

  const AppFilterBar({
    super.key,
    this.title = '',
    this.activeFilterCount = 0,
    this.onResetFilters,
    this.showStatusFilter = true,
    this.statusValue,
    this.onStatusChanged,
    this.statusOptions,
    this.showDateFilter = true,
    this.fromDate,
    this.onFromDateChanged,
    this.toDate,
    this.onToDateChanged,
    this.customFilters,
    this.setModalState,
  });

  static const List<FilterStatusOption> defaultStatusOptions = [
    FilterStatusOption(value: null, label: 'Tất cả trạng thái'),
    FilterStatusOption(value: 1, label: 'Đang hoạt động'),
    FilterStatusOption(value: 0, label: 'Ngừng hoạt động'),
  ];

  void _updateState(VoidCallback fn) {
    if (setModalState != null) {
      setModalState!(fn);
    } else {
      fn();
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final options = statusOptions ?? defaultStatusOptions;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Header Bộ lọc & Nút xóa
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              title,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            ),
            if (activeFilterCount > 0 && onResetFilters != null)
              TextButton.icon(
                style: TextButton.styleFrom(
                  padding: EdgeInsets.zero,
                  minimumSize: const Size(50, 30),
                ),
                icon: const Icon(Icons.clear_all_rounded, size: 16),
                label: const Text('Xóa bộ lọc', style: TextStyle(fontSize: 12)),
                onPressed: () {
                  _updateState(() {
                    onResetFilters?.call();
                  });
                },
              ),
          ],
        ),
        // 1. Filter Trạng thái (Chiếm 1 dòng / Full width)
        if (showStatusFilter) ...[
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
              ),
            ),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<int?>(
                value: statusValue,
                isExpanded: true,
                hint: const Text('Trạng thái', style: TextStyle(fontSize: 12)),
                style: TextStyle(
                  fontSize: 13,
                  color: theme.colorScheme.onSurface,
                ),
                items: options.map((opt) {
                  return DropdownMenuItem<int?>(
                    value: opt.value,
                    child: Text(opt.label),
                  );
                }).toList(),
                onChanged: (val) {
                  _updateState(() {
                    onStatusChanged?.call(val);
                  });
                },
              ),
            ),
          ),
          const SizedBox(height: 8),
        ],

        // 2. Filter Khoảng ngày (Từ ngày -> Đến ngày)
        if (showDateFilter) ...[
          Row(
            children: [
              Expanded(
                child: InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: fromDate ?? DateTime.now(),
                      firstDate: DateTime(2000),
                      lastDate: DateTime(2100),
                    );
                    if (picked != null) {
                      _updateState(() {
                        onFromDateChanged?.call(picked);
                      });
                    }
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: theme.colorScheme.outlineVariant.withValues(
                          alpha: 0.5,
                        ),
                      ),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_today_rounded, size: 14),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            fromDate != null
                                ? "${fromDate!.day}/${fromDate!.month}/${fromDate!.year}"
                                : "Từ ngày",
                            style: const TextStyle(fontSize: 13),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: InkWell(
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: toDate ?? DateTime.now(),
                      firstDate: DateTime(2000),
                      lastDate: DateTime(2100),
                    );
                    if (picked != null) {
                      _updateState(() {
                        onToDateChanged?.call(picked);
                      });
                    }
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 10,
                    ),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: theme.colorScheme.outlineVariant.withValues(
                          alpha: 0.5,
                        ),
                      ),
                    ),
                    child: Row(
                      children: [
                        const Icon(Icons.calendar_today_rounded, size: 14),
                        const SizedBox(width: 6),
                        Expanded(
                          child: Text(
                            toDate != null
                                ? "${toDate!.day}/${toDate!.month}/${toDate!.year}"
                                : "Đến ngày",
                            style: const TextStyle(fontSize: 13),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ],
          ),
          if (customFilters != null && customFilters!.isNotEmpty)
            const SizedBox(height: 8),
        ],

        // 3. Custom Filters ở dưới cùng
        if (customFilters != null && customFilters!.isNotEmpty)
          ...customFilters!.map(
            (w) =>
                Padding(padding: const EdgeInsets.only(bottom: 8.0), child: w),
          ),
      ],
    );
  }
}

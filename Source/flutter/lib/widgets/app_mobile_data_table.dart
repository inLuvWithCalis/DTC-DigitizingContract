import 'package:flutter/material.dart';
import '../widgets/app_text_field.dart';

typedef ItemWidgetBuilder<T> = Widget Function(
  BuildContext context,
  T item,
  bool isSelected,
  VoidCallback onSelectToggle,
);

typedef BulkActionsBuilder<T> = Widget Function(
  List<T> selectedItems,
  VoidCallback resetSelection,
);

/// Generic Reusable Mobile Data Table / Card View Component cho Flutter.
/// Phản chiếu tính năng của DataTable (React Table) trên Web nhưng tối ưu hóa cho UX Mobile.
class AppMobileDataTable<T> extends StatefulWidget {
  final List<T> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;
  final bool isLoading;
  final String searchPlaceholder;
  final String? searchValue;
  final ValueChanged<String>? onSearchChange;
  final Future<void> Function()? onRefresh;
  final ValueChanged<int>? onPageChange;
  final ItemWidgetBuilder<T> itemBuilder;
  final BulkActionsBuilder<T>? bulkActions;
  final Widget? summaryHeader;
  final Widget? actionButton;
  final Object Function(T item) getItemId;
  final String emptyMessage;

  /// Callback khi nhấn vào một dòng / thẻ item trong danh sách
  final ValueChanged<T>? onItemClick;
  final ValueChanged<T>? onRowClick;

  /// Props bổ sung cho Bộ lọc (Filter): status, category, date range...
  final Widget? filterBar;
  final int activeFilterCount;
  final VoidCallback? onFilterToggle;

  const AppMobileDataTable({
    super.key,
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
    required this.isLoading,
    this.searchPlaceholder = "Tìm kiếm...",
    this.searchValue,
    this.onSearchChange,
    this.onRefresh,
    this.onPageChange,
    required this.itemBuilder,
    this.bulkActions,
    this.summaryHeader,
    this.actionButton,
    required this.getItemId,
    this.emptyMessage = "Không tìm thấy dữ liệu",
    this.onItemClick,
    this.onRowClick,
    this.filterBar,
    this.activeFilterCount = 0,
    this.onFilterToggle,
  });

  @override
  State<AppMobileDataTable<T>> createState() => _AppMobileDataTableState<T>();
}

class _AppMobileDataTableState<T> extends State<AppMobileDataTable<T>> {
  final Set<Object> _selectedIds = {};
  late TextEditingController _searchController;
  bool _isFilterExpanded = false;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController(text: widget.searchValue ?? "");
  }

  @override
  void didUpdateWidget(covariant AppMobileDataTable<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.searchValue != oldWidget.searchValue &&
        widget.searchValue != _searchController.text) {
      _searchController.text = widget.searchValue ?? "";
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _toggleSelectAll() {
    setState(() {
      if (_selectedIds.length == widget.items.length) {
        _selectedIds.clear();
      } else {
        _selectedIds.clear();
        for (var item in widget.items) {
          _selectedIds.add(widget.getItemId(item));
        }
      }
    });
  }

  void _resetSelection() {
    setState(() {
      _selectedIds.clear();
    });
  }

  List<T> get _selectedItems {
    return widget.items
        .where((item) => _selectedIds.contains(widget.getItemId(item)))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isAllSelected = widget.items.isNotEmpty &&
        _selectedIds.length == widget.items.length;

    final clickHandler = widget.onItemClick ?? widget.onRowClick;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // --- SUMMARY HEADER (Nếu có) ---
        if (widget.summaryHeader != null) ...[
          widget.summaryHeader!,
          const SizedBox(height: 16),
        ],

        // --- SEARCH BAR, FILTER BUTTON & ACTION BUTTON ---
        Row(
          children: [
            Expanded(
              child: AppTextField(
                controller: _searchController,
                placeholder: widget.searchPlaceholder,
                onChanged: (val) {
                  if (widget.onSearchChange != null) {
                    widget.onSearchChange!(val);
                  }
                },
                suffixIcon: _searchController.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear_rounded, size: 20),
                        onPressed: () {
                          _searchController.clear();
                          if (widget.onSearchChange != null) {
                            widget.onSearchChange!("");
                          }
                        },
                      )
                    : const Icon(Icons.search_rounded, size: 20),
              ),
            ),

            // Filter Toggle Button
            if (widget.filterBar != null || widget.onFilterToggle != null) ...[
              const SizedBox(width: 8),
              Stack(
                clipBehavior: Clip.none,
                children: [
                  IconButton.filledTonal(
                    style: IconButton.styleFrom(
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                      ),
                      padding: const EdgeInsets.all(14),
                      backgroundColor: (_isFilterExpanded || widget.activeFilterCount > 0)
                          ? theme.colorScheme.primary.withValues(alpha: 0.15)
                          : theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.5),
                    ),
                    icon: Icon(
                      Icons.filter_list_rounded,
                      color: (_isFilterExpanded || widget.activeFilterCount > 0)
                          ? theme.colorScheme.primary
                          : theme.colorScheme.onSurface,
                    ),
                    onPressed: () {
                      if (widget.onFilterToggle != null) {
                        widget.onFilterToggle!();
                      } else {
                        setState(() {
                          _isFilterExpanded = !_isFilterExpanded;
                        });
                      }
                    },
                  ),
                  if (widget.activeFilterCount > 0)
                    Positioned(
                      top: -4,
                      right: -4,
                      child: Container(
                        padding: const EdgeInsets.all(5),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.primary,
                          shape: BoxShape.circle,
                        ),
                        child: Text(
                          '${widget.activeFilterCount}',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ],

            if (widget.actionButton != null) ...[
              const SizedBox(width: 8),
              widget.actionButton!,
            ],
          ],
        ),

        // --- FILTER BAR EXPANDABLE SLOT ---
        if (widget.filterBar != null && _isFilterExpanded) ...[
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
              ),
            ),
            child: widget.filterBar!,
          ),
        ],

        const SizedBox(height: 12),

        // --- BULK ACTION & SELECT ALL BAR ---
        if (widget.items.isNotEmpty)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: _selectedIds.isNotEmpty
                  ? theme.colorScheme.primary.withValues(alpha: 0.08)
                  : theme.colorScheme.surfaceContainerHighest
                      .withValues(alpha: 0.3),
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: _selectedIds.isNotEmpty
                    ? theme.colorScheme.primary.withValues(alpha: 0.3)
                    : theme.colorScheme.outlineVariant.withValues(alpha: 0.4),
              ),
            ),
            child: Row(
              children: [
                Checkbox(
                  value: isAllSelected,
                  onChanged: (val) => _toggleSelectAll(),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(4),
                  ),
                ),
                Text(
                  _selectedIds.isNotEmpty
                      ? 'Đã chọn ${_selectedIds.length}/${widget.items.length}'
                      : 'Chọn tất cả (${widget.totalCount})',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: _selectedIds.isNotEmpty
                        ? theme.colorScheme.primary
                        : theme.colorScheme.onSurface,
                  ),
                ),
                const Spacer(),
                if (_selectedIds.isNotEmpty && widget.bulkActions != null)
                  widget.bulkActions!(_selectedItems, _resetSelection),
              ],
            ),
          ),

        const SizedBox(height: 12),

        // --- DATA LIST CONTENT ---
        Expanded(
          child: widget.isLoading
              ? const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32.0),
                    child: CircularProgressIndicator(),
                  ),
                )
              : widget.items.isEmpty
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.folder_open_rounded,
                            size: 64,
                            color: theme.colorScheme.onSurface
                                .withValues(alpha: 0.3),
                          ),
                          const SizedBox(height: 12),
                          Text(
                            widget.emptyMessage,
                            style: TextStyle(
                              fontSize: 15,
                              color: theme.colorScheme.onSurface
                                  .withValues(alpha: 0.6),
                            ),
                          ),
                        ],
                      ),
                    )
                  : RefreshIndicator(
                      onRefresh: widget.onRefresh ?? () async {},
                      child: ListView.separated(
                        physics: const AlwaysScrollableScrollPhysics(
                          parent: BouncingScrollPhysics(),
                        ),
                        itemCount: widget.items.length,
                        separatorBuilder: (context, index) =>
                            const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final item = widget.items[index];
                          final id = widget.getItemId(item);
                          final isSelected = _selectedIds.contains(id);

                          final cardChild = widget.itemBuilder(
                            context,
                            item,
                            isSelected,
                            () {
                              setState(() {
                                if (isSelected) {
                                  _selectedIds.remove(id);
                                } else {
                                  _selectedIds.add(id);
                                }
                              });
                            },
                          );

                          if (clickHandler != null) {
                            return InkWell(
                              onTap: () => clickHandler(item),
                              borderRadius: BorderRadius.circular(16),
                              child: cardChild,
                            );
                          }

                          return cardChild;
                        },
                      ),
                    ),
        ),

        // --- PAGINATION FOOTER BAR ---
        if (widget.totalPages > 1)
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
              ),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Trang ${widget.page} / ${widget.totalPages} (${widget.totalCount} mục)',
                  style: TextStyle(
                    fontSize: 13,
                    color: theme.colorScheme.onSurface.withValues(alpha: 0.7),
                  ),
                ),
                Row(
                  children: [
                    IconButton(
                      icon: const Icon(Icons.chevron_left_rounded),
                      onPressed: widget.page > 1 && !widget.isLoading
                          ? () {
                              if (widget.onPageChange != null) {
                                widget.onPageChange!(widget.page - 1);
                              }
                            }
                          : null,
                    ),
                    IconButton(
                      icon: const Icon(Icons.chevron_right_rounded),
                      onPressed: widget.page < widget.totalPages &&
                              !widget.isLoading
                          ? () {
                              if (widget.onPageChange != null) {
                                widget.onPageChange!(widget.page + 1);
                              }
                            }
                          : null,
                    ),
                  ],
                ),
              ],
            ),
          ),
      ],
    );
  }
}

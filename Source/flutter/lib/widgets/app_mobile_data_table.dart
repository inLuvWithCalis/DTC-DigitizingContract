import 'dart:ui';
import 'package:flutter/material.dart';
import '../widgets/app_skeleton.dart';
import '../widgets/app_text_field.dart';

typedef ItemWidgetBuilder<T> =
    Widget Function(
      BuildContext context,
      T item,
      bool isSelected,
      VoidCallback onSelectToggle,
    );

typedef BulkActionsBuilder<T> =
    Widget Function(List<T> selectedItems, VoidCallback resetSelection);

/// Generic Reusable Mobile Data Table / Card View Component cho Flutter.
/// Hỗ trợ Lazy Loading (Infinite Scroll) + Glassmorphism Floating Header
/// (Header kính mờ nổi lên trên, danh sách lướt xuyên qua hiệu ứng blur).
class AppMobileDataTable<T> extends StatefulWidget {
  final List<T> items;
  final int totalCount;
  final bool isLoading;

  /// Đang tải thêm ở cuối danh sách (load more spinner)
  final bool isLoadingMore;

  /// Còn item để load thêm không
  final bool hasMore;

  final String searchPlaceholder;
  final String? searchValue;
  final ValueChanged<String>? onSearchChange;
  final Future<void> Function()? onRefresh;

  /// Gọi khi người dùng lướt đến gần cuối danh sách
  final Future<void> Function()? onLoadMore;

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
    required this.isLoading,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.searchPlaceholder = "Tìm kiếm...",
    this.searchValue,
    this.onSearchChange,
    this.onRefresh,
    this.onLoadMore,
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
  late String _lastSubmittedSearch;
  bool _isFilterExpanded = false;
  late ScrollController _scrollController;
  bool _isFetchingMore = false;

  final GlobalKey _headerKey = GlobalKey();
  double _headerHeight = 0;

  @override
  void initState() {
    super.initState();
    _searchController = TextEditingController(text: widget.searchValue ?? "");
    _lastSubmittedSearch = widget.searchValue ?? "";

    _scrollController = ScrollController();
    _scrollController.addListener(_onScroll);
  }

  @override
  void didUpdateWidget(covariant AppMobileDataTable<T> oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.searchValue != oldWidget.searchValue) {
      _lastSubmittedSearch = widget.searchValue ?? "";
      if (widget.searchValue != _searchController.text) {
        _searchController.text = widget.searchValue ?? "";
      }
    }
    // Khi danh sách thay đổi (reset), clear selection
    if (widget.items != oldWidget.items && oldWidget.items.isEmpty) {
      _selectedIds.clear();
    }
    // Khi loading xong, reset cờ fetching
    if (!widget.isLoadingMore && oldWidget.isLoadingMore) {
      _isFetchingMore = false;
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    super.dispose();
  }

  void _updateHeaderHeight() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final renderBox =
          _headerKey.currentContext?.findRenderObject() as RenderBox?;
      if (renderBox != null && renderBox.hasSize) {
        final height = renderBox.size.height;
        if ((_headerHeight - height).abs() > 1.0) {
          setState(() {
            _headerHeight = height;
          });
        }
      }
    });
  }

  void _onScroll() {
    if (!_scrollController.hasClients) return;
    final maxScroll = _scrollController.position.maxScrollExtent;
    final currentScroll = _scrollController.position.pixels;
    // Khi còn 200px từ đáy → load thêm
    if (currentScroll >= maxScroll - 200 &&
        widget.hasMore &&
        !widget.isLoadingMore &&
        !_isFetchingMore &&
        widget.onLoadMore != null) {
      _isFetchingMore = true;
      widget.onLoadMore!();
    }
  }

  void _triggerSearch([String? customValue, bool forceSubmit = false]) {
    final query = customValue ?? _searchController.text;
    // Bỏ qua nếu không thay đổi, trừ khi bấm nút tìm kiếm (forceSubmit = true)
    if (!forceSubmit && query == _lastSubmittedSearch) return;
    _lastSubmittedSearch = query;
    if (widget.onSearchChange != null) {
      widget.onSearchChange!(query);
    }
  }

  void _toggleSelectAll() {
    setState(() {
      // "Chọn tất cả" = chọn tất cả items đã được load
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
    _updateHeaderHeight();

    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final isAllLoadedSelected =
        widget.items.isNotEmpty && _selectedIds.length == widget.items.length;
    final hasMoreThanLoaded = widget.totalCount > widget.items.length;

    final clickHandler = widget.onItemClick ?? widget.onRowClick;
    final effectiveHeaderPadding = _headerHeight > 0
        ? _headerHeight + 12.0
        : 160.0;

    return Stack(
      clipBehavior: Clip.none,
      children: [
        // --- 1. DATA LIST CONTENT (Lướt xuyên qua kính mờ bên dưới) ---
        Positioned.fill(
          child: widget.isLoading
              ? Padding(
                  padding: EdgeInsets.only(top: effectiveHeaderPadding),
                  child: const AppSkeletonList(),
                )
              : widget.items.isEmpty
              ? Center(
                  child: Padding(
                    padding: EdgeInsets.only(top: effectiveHeaderPadding),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.folder_open_rounded,
                          size: 64,
                          color: theme.colorScheme.onSurface.withValues(
                            alpha: 0.3,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          widget.emptyMessage,
                          style: TextStyle(
                            fontSize: 15,
                            color: theme.colorScheme.onSurface.withValues(
                              alpha: 0.6,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: widget.onRefresh ?? () async {},
                  edgeOffset: effectiveHeaderPadding,
                  child: ListView.separated(
                    controller: _scrollController,
                    padding: EdgeInsets.only(
                      top: effectiveHeaderPadding,
                      bottom: 16,
                    ),
                    physics: const AlwaysScrollableScrollPhysics(
                      parent: BouncingScrollPhysics(),
                    ),
                    itemCount:
                        widget.items.length +
                        (widget.isLoadingMore ? 1 : 0) +
                        (!widget.hasMore &&
                                widget.items.isNotEmpty &&
                                widget.totalCount > 0
                            ? 1
                            : 0),
                    separatorBuilder: (context, index) =>
                        const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      if (widget.isLoadingMore &&
                          index == widget.items.length) {
                        return Padding(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          child: Center(
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Theme.of(
                                      context,
                                    ).colorScheme.primary,
                                  ),
                                ),
                                const SizedBox(width: 10),
                                Text(
                                  'Đang tải thêm...',
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: Theme.of(context)
                                        .colorScheme
                                        .onSurface
                                        .withValues(alpha: 0.6),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        );
                      }

                      // Indicator "Đã hiển thị tất cả" — cuối list khi không còn hasMore
                      if (!widget.hasMore &&
                          widget.items.isNotEmpty &&
                          widget.totalCount > 0 &&
                          index == widget.items.length) {
                        return Padding(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          child: Row(
                            children: [
                              Expanded(
                                child: Divider(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .outlineVariant
                                      .withValues(alpha: 0.4),
                                ),
                              ),
                              Padding(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 12,
                                ),
                                child: Text(
                                  'Đã hiển thị tất cả ${widget.totalCount} mục',
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: Theme.of(context)
                                        .colorScheme
                                        .onSurface
                                        .withValues(alpha: 0.45),
                                  ),
                                ),
                              ),
                              Expanded(
                                child: Divider(
                                  color: Theme.of(context)
                                      .colorScheme
                                      .outlineVariant
                                      .withValues(alpha: 0.4),
                                ),
                              ),
                            ],
                          ),
                        );
                      }

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

                      return AnimatedScale(
                        scale: isSelected ? 0.985 : 1.0,
                        duration: const Duration(milliseconds: 200),
                        curve: Curves.easeInOut,
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 220),
                          curve: Curves.easeInOut,
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(16),
                            boxShadow: isSelected
                                ? [
                                    BoxShadow(
                                      color: Theme.of(context)
                                          .colorScheme
                                          .primary
                                          .withValues(alpha: 0.15),
                                      blurRadius: 10,
                                      spreadRadius: 1,
                                      offset: const Offset(0, 4),
                                    ),
                                  ]
                                : [],
                          ),
                          child: InkWell(
                            onTap: () {
                              if (_selectedIds.isNotEmpty) {
                                setState(() {
                                  if (isSelected) {
                                    _selectedIds.remove(id);
                                  } else {
                                    _selectedIds.add(id);
                                  }
                                });
                              } else if (clickHandler != null) {
                                clickHandler(item);
                              }
                            },
                            onLongPress: () {
                              setState(() {
                                if (isSelected) {
                                  _selectedIds.remove(id);
                                } else {
                                  _selectedIds.add(id);
                                }
                              });
                            },
                            borderRadius: BorderRadius.circular(16),
                            child: cardChild,
                          ),
                        ),
                      );
                    },
                  ),
                ),
        ),

        // --- 2. FLOATING GLASS HEADER PANEL (Glassmorphism Backdrop Filter) ---
        Positioned(
          top: 0,
          left: 0,
          right: 0,
          child: Builder(
            builder: (context) {
              final headerContainer = Container(
                key: _headerKey,
                padding: const EdgeInsets.all(0),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface.withValues(
                    alpha: isDark ? 1.0 : 0.82,
                  ),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // --- SUMMARY HEADER (Nếu có) ---
                    if (widget.summaryHeader != null) ...[
                      widget.summaryHeader!,
                      const SizedBox(height: 12),
                    ],

                    // --- SEARCH BAR, FILTER BUTTON & ACTION BUTTON ---
                    if (widget.isLoading)
                      AppSkeletonSearchBar(
                        hasFilter:
                            widget.filterBar != null ||
                            widget.onFilterToggle != null,
                        hasAction: widget.actionButton != null,
                      )
                    else
                      Row(
                        children: [
                          Expanded(
                            child: AppTextField(
                              controller: _searchController,
                              placeholder: widget.searchPlaceholder,
                              textInputAction: TextInputAction.search,
                              contentPadding: const EdgeInsets.symmetric(
                                horizontal: 14,
                                vertical: 10,
                              ),
                              isDense: true,
                              onChanged: (val) {
                                setState(() {});
                              },
                              onSubmitted: (_) => _triggerSearch(),
                              suffixIcon: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  if (_searchController.text.isNotEmpty)
                                    IconButton(
                                      icon: const Icon(
                                        Icons.clear_rounded,
                                        size: 20,
                                      ),
                                      tooltip: 'Xóa tìm kiếm',
                                      onPressed: () {
                                        _searchController.clear();
                                        setState(() {});
                                        _triggerSearch("", true);
                                      },
                                    ),
                                  IconButton(
                                    icon: const Icon(
                                      Icons.search_rounded,
                                      size: 20,
                                    ),
                                    tooltip: 'Tìm kiếm',
                                    onPressed: () => _triggerSearch(null, true),
                                  ),
                                ],
                              ),
                            ),
                          ),

                          // Filter Toggle Button
                          if (widget.filterBar != null ||
                              widget.onFilterToggle != null) ...[
                            const SizedBox(width: 8),
                            Stack(
                              clipBehavior: Clip.none,
                              children: [
                                IconButton.filledTonal(
                                  style: IconButton.styleFrom(
                                    minimumSize: const Size(44, 44),
                                    maximumSize: const Size(44, 44),
                                    padding: EdgeInsets.zero,
                                    shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(14),
                                    ),
                                    backgroundColor:
                                        (_isFilterExpanded ||
                                            widget.activeFilterCount > 0)
                                        ? theme.colorScheme.primary.withValues(
                                            alpha: 0.15,
                                          )
                                        : theme
                                              .colorScheme
                                              .surfaceContainerHighest
                                              .withValues(alpha: 0.5),
                                  ),
                                  icon: Icon(
                                    Icons.filter_list_rounded,
                                    color:
                                        (_isFilterExpanded ||
                                            widget.activeFilterCount > 0)
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
                    if (widget.filterBar != null &&
                        _isFilterExpanded &&
                        !widget.isLoading) ...[
                      const SizedBox(height: 12),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: theme.colorScheme.surfaceContainerHighest
                              .withValues(alpha: 0.3),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                            color: theme.colorScheme.outlineVariant.withValues(
                              alpha: 0.4,
                            ),
                          ),
                        ),
                        child: widget.filterBar!,
                      ),
                    ],

                    // --- BULK ACTION & SELECT ALL BAR ---
                    if (widget.isLoading) ...[
                      const SizedBox(height: 10),
                      const AppSkeletonSelectAllBar(),
                    ] else if (widget.items.isNotEmpty) ...[
                      const SizedBox(height: 10),
                      AnimatedContainer(
                        duration: const Duration(milliseconds: 250),
                        curve: Curves.easeInOut,
                        decoration: BoxDecoration(
                          color: _selectedIds.isNotEmpty
                              ? theme.colorScheme.primary.withValues(
                                  alpha: 0.08,
                                )
                              : theme.colorScheme.surfaceContainerHighest
                                    .withValues(alpha: 0.3),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(
                            color: _selectedIds.isNotEmpty
                                ? theme.colorScheme.primary.withValues(
                                    alpha: 0.3,
                                  )
                                : theme.colorScheme.outlineVariant.withValues(
                                    alpha: 0.4,
                                  ),
                          ),
                        ),
                        child: Row(
                          children: [
                            Checkbox(
                              value: isAllLoadedSelected,
                              tristate: false,
                              onChanged: (val) => _toggleSelectAll(),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(4),
                              ),
                            ),
                            Expanded(
                              child: Text(
                                _selectedIds.isNotEmpty
                                    ? 'Đã chọn ${_selectedIds.length}${hasMoreThanLoaded ? " (trong ${widget.items.length} đã tải)" : "/${widget.totalCount}"}'
                                    : 'Chọn tất cả (${widget.items.length})',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w600,
                                  color: _selectedIds.isNotEmpty
                                      ? theme.colorScheme.primary
                                      : theme.colorScheme.onSurface,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            if (_selectedIds.isNotEmpty) ...[
                              const SizedBox(width: 4),
                              IconButton(
                                tooltip: 'Hủy chọn',
                                icon: const Icon(Icons.close_rounded, size: 18),
                                onPressed: _resetSelection,
                              ),
                            ],
                            if (_selectedIds.isNotEmpty &&
                                widget.bulkActions != null)
                              widget.bulkActions!(
                                _selectedItems,
                                _resetSelection,
                              ),
                          ],
                        ),
                      ),
                    ],
                  ],
                ),
              );

              if (isDark) return headerContainer;

              return ClipRect(
                child: BackdropFilter(
                  filter: ImageFilter.blur(sigmaX: 16, sigmaY: 16),
                  child: headerContainer,
                ),
              );
            },
          ),
        ),
      ],
    );
  }
}

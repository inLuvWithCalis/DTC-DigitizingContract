import 'package:flutter/material.dart';
import '../utils/auth_store.dart';
import '../services/auth_api.dart';
import '../utils/app_toast.dart';
import '../utils/quick_actions_store.dart';
import '../widgets/app_quick_actions_card.dart';
import '../widgets/app_floating_dock_nav_bar.dart';
import '../widgets/dashboard/app_dashboard_analytics_card.dart';
import '../widgets/dashboard/app_dashboard_recent_activities.dart';
import '../widgets/dashboard/app_dashboard_stats_grid.dart';
import '../widgets/dashboard/app_profile_sidebar.dart';
import '../widgets/dashboard/app_services_bottom_sheet.dart';
import 'catalog/service-types/service_type_list_page.dart';
import 'catalog/services/service_list_page.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  final AuthStore _authStore = AuthStore();
  int _currentNavIndex = 0;

  List<String> _quickActionIds = QuickActionsStore.defaultIds;

  @override
  void initState() {
    super.initState();
    _loadSavedQuickActions();
  }

  Future<void> _loadSavedQuickActions() async {
    final saved = await QuickActionsStore.loadQuickActionIds();
    if (mounted) {
      setState(() {
        _quickActionIds = saved;
      });
    }
  }

  void _updateQuickActionIds(List<String> newIds) {
    setState(() {
      _quickActionIds = List.from(newIds);
    });
    QuickActionsStore.saveQuickActionIds(newIds);
  }

  Future<void> _handleLogout() async {
    String message = "Đăng xuất thành công!";
    try {
      message = await AuthApi.logout();
    } catch (_) {}
    _authStore.clear();
    if (mounted) {
      AppToast.success(context, message);
      Navigator.of(context).pushReplacementNamed('/');
    }
  }

  Map<String, QuickActionData> _getQuickActionsCatalog(BuildContext context) {
    final theme = Theme.of(context);
    final primaryColor = theme.colorScheme.primary;
    final onPrimaryColor = theme.colorScheme.onPrimary;

    return {
      'services': QuickActionData(
        id: 'services',
        title: "Dịch vụ",
        icon: Icons.miscellaneous_services_outlined,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () => Navigator.of(context).pushNamed('/catalog/services'),
      ),
      'service_types': QuickActionData(
        id: 'service_types',
        title: "Loại dịch vụ",
        icon: Icons.category_outlined,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () => Navigator.of(context).pushNamed('/catalog/service-types'),
      ),
      'create_contract': QuickActionData(
        id: 'create_contract',
        title: "Tạo HĐ mới",
        icon: Icons.post_add_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'approve_contract': QuickActionData(
        id: 'approve_contract',
        title: "Phê duyệt HĐ",
        icon: Icons.verified_user_outlined,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'price_list': QuickActionData(
        id: 'price_list',
        title: "Bảng giá",
        icon: Icons.sell_outlined,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'contract_templates': QuickActionData(
        id: 'contract_templates',
        title: "Mẫu HĐ",
        icon: Icons.description_outlined,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'renew_contract': QuickActionData(
        id: 'renew_contract',
        title: "Gia hạn HĐ",
        icon: Icons.update_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'liquidate_contract': QuickActionData(
        id: 'liquidate_contract',
        title: "Thanh lý",
        icon: Icons.task_alt_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'revenue': QuickActionData(
        id: 'revenue',
        title: "Doanh thu",
        icon: Icons.pie_chart_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'traffic': QuickActionData(
        id: 'traffic',
        title: "Lưu lượng",
        icon: Icons.show_chart_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
      'activity_logs': QuickActionData(
        id: 'activity_logs',
        title: "Nhật ký",
        icon: Icons.history_toggle_off_rounded,
        color: primaryColor,
        bgColor: onPrimaryColor,
        onTap: () {},
      ),
    };
  }

  void _openEditQuickActionsBottomSheet(BuildContext context) {
    AppServicesBottomSheet.show(
      context,
      isEditMode: true,
      selectedIds: _quickActionIds,
      onSelectedIdsChanged: (newIds) {
        _updateQuickActionIds(newIds);
      },
    );
  }

  Widget _buildDashboardOverview(
    BuildContext context,
    ThemeData theme,
    String displayName,
  ) {
    const stats = [
      StatItem(
        label: "Tổng Hợp đồng",
        value: "1,248",
        trend: "+12.5%",
        isPositive: true,
        icon: Icons.description_outlined,
        color: Color(0xFF4F46E5),
        bgColor: Color(0xFFEEF2FF),
      ),
      StatItem(
        label: "Chờ Phê duyệt",
        value: "24",
        trend: "-2.4%",
        isPositive: true,
        icon: Icons.access_time_rounded,
        color: Color(0xFFD97706),
        bgColor: Color(0xFFFFFBEB),
      ),
      StatItem(
        label: "Sắp Hết hạn",
        value: "12",
        trend: "+4.1%",
        isPositive: false,
        icon: Icons.error_outline_rounded,
        color: Color(0xFFE11D48),
        bgColor: Color(0xFFFFE4E6),
      ),
      StatItem(
        label: "Đã Thanh lý",
        value: "856",
        trend: "+8.2%",
        isPositive: true,
        icon: Icons.task_alt_rounded,
        color: Color(0xFF059669),
        bgColor: Color(0xFFECFDF5),
      ),
    ];

    const recentActivities = [
      ActivityItem(
        action: "Tạo mới Hợp đồng Bán",
        details: "HĐ #HB-2024-001 - Công ty TNHH ABC",
        time: "2h trước",
        icon: Icons.post_add_rounded,
        color: Color(0xFF4F46E5),
        bgColor: Color(0xFFEEF2FF),
      ),
      ActivityItem(
        action: "Phê duyệt Phụ lục",
        details: "Phụ lục #PL-01 cho HĐ #HB-2023-102",
        time: "4h trước",
        icon: Icons.check_circle_outline_rounded,
        color: Color(0xFF059669),
        bgColor: Color(0xFFECFDF5),
      ),
      ActivityItem(
        action: "Cảnh báo Hết hạn",
        details: "Hợp đồng Mua #HM-092 sắp hết hiệu lực trong 5 ngày",
        time: "6h trước",
        icon: Icons.warning_amber_rounded,
        color: Color(0xFFE11D48),
        bgColor: Color(0xFFFFE4E6),
      ),
    ];

    final catalog = _getQuickActionsCatalog(context);
    final activeQuickActions = _quickActionIds
        .where((id) => catalog.containsKey(id))
        .map((id) => catalog[id]!)
        .toList();

    return SingleChildScrollView(
      physics: const BouncingScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // --- WELCOME HEADER ---
          _buildWelcomeHeader(theme, displayName),

          const SizedBox(height: 24),

          // --- QUICK ACTIONS REUSABLE COMPONENT ---
          AppQuickActionsCard(
            items: activeQuickActions,
            onRemoveItem: (id) {
              final updated = List<String>.from(_quickActionIds)..remove(id);
              _updateQuickActionIds(updated);
            },
            onOpenAddBottomSheet: () {
              _openEditQuickActionsBottomSheet(context);
            },
          ),

          const SizedBox(height: 32),

          // --- STATS GRID ---
          const AppDashboardStatsGrid(stats: stats),

          const SizedBox(height: 28),

          // --- CHARTS / VISUAL ANALYTICS ---
          const AppDashboardAnalyticsCard(),

          const SizedBox(height: 28),

          // --- RECENT ACTIVITIES ---
          const AppDashboardRecentActivities(activities: recentActivities),

          const SizedBox(height: 24),
        ],
      ),
    );
  }

  Widget _buildWelcomeHeader(ThemeData theme, String displayName) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            theme.colorScheme.primary,
            theme.colorScheme.primary.withValues(alpha: 0.8),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: theme.colorScheme.primary.withValues(alpha: 0.25),
            blurRadius: 16,
            offset: const Offset(0, 6),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.2),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: const Text(
                  'eContract Mobile',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              const Icon(
                Icons.verified_user_rounded,
                color: Colors.white70,
                size: 20,
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            'Xin chào, $displayName',
            style: const TextStyle(
              color: Colors.white,
              fontSize: 22,
              fontWeight: FontWeight.bold,
              letterSpacing: -0.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            'Dưới đây là tình hình số hóa hợp đồng hôm nay.',
            style: TextStyle(
              color: Colors.white.withValues(alpha: 0.85),
              fontSize: 14,
            ),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final user = _authStore.user;
    final displayName =
        user?.employeeFullName ?? user?.employeeAccount ?? "Quản trị viên";

    final pages = [
      _buildDashboardOverview(context, theme, displayName),
      const ServiceListPage(),
      const ServiceTypeListPage(),
    ];

    return Scaffold(
      extendBody: true,
      backgroundColor: theme.colorScheme.surface,
      endDrawer: AppProfileSidebar(user: user, onLogout: _handleLogout),
      appBar: AppBar(
        backgroundColor: theme.colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: theme.colorScheme.primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(
                Icons.description_rounded,
                color: theme.colorScheme.primary,
                size: 20,
              ),
            ),
            const SizedBox(width: 10),
            Text(
              'eContract Mobile',
              style: theme.textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_none_rounded),
            tooltip: 'Thông báo',
            onPressed: () {},
          ),
          Builder(
            builder: (scaffoldContext) {
              return IconButton(
                icon: const Icon(Icons.menu_rounded),
                tooltip: 'Menu cá nhân & Cài đặt',
                onPressed: () {
                  Scaffold.of(scaffoldContext).openEndDrawer();
                },
              );
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: IndexedStack(
        index: _currentNavIndex < pages.length ? _currentNavIndex : 0,
        children: pages,
      ),
      bottomNavigationBar: AppFloatingDockNavBar(
        currentIndex: _currentNavIndex,
        onTap: (index) {
          if (index == 4) {
            AppServicesBottomSheet.show(
              context,
              isEditMode: false,
              selectedIds: _quickActionIds,
              onSelectedIdsChanged: (newIds) {
                _updateQuickActionIds(newIds);
              },
            );
          } else {
            setState(() {
              _currentNavIndex = index;
            });
          }
        },
      ),
    );
  }
}

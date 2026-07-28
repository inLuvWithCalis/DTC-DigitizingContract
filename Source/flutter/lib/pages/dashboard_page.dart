import 'package:flutter/material.dart';
import '../main.dart';
import '../services/auth_api.dart';
import '../utils/app_toast.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  final AuthStore _authStore = AuthStore();

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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final user = _authStore.user;
    final displayName =
        user?.employeeFullName ?? user?.employeeAccount ?? "Quản trị viên";

    final stats = [
      _StatItem(
        label: "Tổng Hợp đồng",
        value: "1,248",
        trend: "+12.5%",
        isPositive: true,
        icon: Icons.description_outlined,
        color: const Color(0xFF4F46E5), // Indigo
        bgColor: const Color(0xFFEEF2FF),
      ),
      _StatItem(
        label: "Chờ Phê duyệt",
        value: "24",
        trend: "-2.4%",
        isPositive: true,
        icon: Icons.access_time_rounded,
        color: const Color(0xFFD97706), // Amber
        bgColor: const Color(0xFFFFFBEB),
      ),
      _StatItem(
        label: "Sắp Hết hạn",
        value: "12",
        trend: "+4.1%",
        isPositive: false,
        icon: Icons.error_outline_rounded,
        color: const Color(0xFFE11D48), // Rose
        bgColor: const Color(0xFFFFE4E6),
      ),
      _StatItem(
        label: "Đã Thanh lý",
        value: "856",
        trend: "+8.2%",
        isPositive: true,
        icon: Icons.task_alt_rounded,
        color: const Color(0xFF059669), // Emerald
        bgColor: const Color(0xFFECFDF5),
      ),
    ];

    final quickActions = [
      _QuickActionItem(
        title: "Dịch vụ",
        icon: Icons.miscellaneous_services_outlined,
        color: const Color(0xFF4F46E5),
        bgColor: const Color(0xFFEEF2FF),
        onTap: () => Navigator.of(context).pushNamed('/catalog/services'),
      ),
      _QuickActionItem(
        title: "Loại dịch vụ",
        icon: Icons.category_outlined,
        color: const Color(0xFF0284C7),
        bgColor: const Color(0xFFE0F2FE),
        onTap: () => Navigator.of(context).pushNamed('/catalog/service-types'),
      ),
      _QuickActionItem(
        title: "Duyệt HĐ",
        icon: Icons.verified_outlined,
        color: const Color(0xFFD97706),
        bgColor: const Color(0xFFFFFBEB),
        onTap: () {},
      ),
      _QuickActionItem(
        title: "Tra cứu",
        icon: Icons.search_rounded,
        color: const Color(0xFF7C3AED),
        bgColor: const Color(0xFFF3E8FF),
        onTap: () {},
      ),
    ];

    final recentActivities = [
      _ActivityItem(
        action: "Tạo mới Hợp đồng Bán",
        details: "HĐ #HB-2024-001 - Công ty TNHH ABC",
        time: "2h trước",
        icon: Icons.post_add_rounded,
        color: const Color(0xFF4F46E5),
        bgColor: const Color(0xFFEEF2FF),
      ),
      _ActivityItem(
        action: "Phê duyệt Phụ lục",
        details: "Phụ lục #PL-01 cho HĐ #HB-2023-102",
        time: "4h trước",
        icon: Icons.check_circle_outline_rounded,
        color: const Color(0xFF059669),
        bgColor: const Color(0xFFECFDF5),
      ),
      _ActivityItem(
        action: "Cảnh báo Hết hạn",
        details: "Hợp đồng Mua #HM-092 sắp hết hiệu lực trong 5 ngày",
        time: "6h trước",
        icon: Icons.warning_amber_rounded,
        color: const Color(0xFFE11D48),
        bgColor: const Color(0xFFFFE4E6),
      ),
    ];

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        backgroundColor: theme.colorScheme.surface,
        elevation: 0,
        scrolledUnderElevation: 2,
        title: Row(
          children: [
            Container(
              width: 38,
              height: 38,
              decoration: BoxDecoration(
                color: theme.colorScheme.primary.withValues(alpha: 0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(
                Icons.person_rounded,
                color: theme.colorScheme.primary,
                size: 22,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    displayName,
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  Text(
                    user?.userRoles ?? "Số hóa Hợp đồng",
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
                    ),
                  ),
                ],
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
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            tooltip: 'Đăng xuất',
            onPressed: _handleLogout,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          physics: const BouncingScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // --- WELCOME HEADER ---
              Container(
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
              ),

              const SizedBox(height: 24),

              // --- QUICK ACTIONS ---
              const Text(
                'Thao tác nhanh',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  letterSpacing: -0.3,
                ),
              ),
              const SizedBox(height: 12),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: quickActions.map((qa) {
                  return InkWell(
                    onTap: qa.onTap,
                    borderRadius: BorderRadius.circular(16),
                    child: Column(
                      children: [
                        Container(
                          width: 56,
                          height: 56,
                          decoration: BoxDecoration(
                            color: qa.bgColor,
                            borderRadius: BorderRadius.circular(16),
                          ),
                          child: Icon(qa.icon, color: qa.color, size: 26),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          qa.title,
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: theme.colorScheme.onSurface,
                          ),
                        ),
                      ],
                    ),
                  );
                }).toList(),
              ),

              const SizedBox(height: 28),

              // --- STATS GRID (2x2) ---
              const Text(
                'Thống kê hợp đồng',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  letterSpacing: -0.3,
                ),
              ),
              const SizedBox(height: 12),
              GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                itemCount: stats.length,
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 14,
                  mainAxisSpacing: 14,
                  childAspectRatio: 1.35,
                ),
                itemBuilder: (context, index) {
                  final stat = stats[index];
                  return Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: theme.colorScheme.surface,
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(
                        color: theme.colorScheme.outlineVariant.withValues(
                          alpha: 0.5,
                        ),
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.03),
                          blurRadius: 10,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Expanded(
                              child: Text(
                                stat.label,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w500,
                                  color: theme.colorScheme.onSurface.withValues(
                                    alpha: 0.6,
                                  ),
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            Container(
                              width: 34,
                              height: 34,
                              decoration: BoxDecoration(
                                color: stat.bgColor,
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: Icon(
                                stat.icon,
                                color: stat.color,
                                size: 18,
                              ),
                            ),
                          ],
                        ),
                        Text(
                          stat.value,
                          style: const TextStyle(
                            fontSize: 22,
                            fontWeight: FontWeight.bold,
                            letterSpacing: -0.5,
                          ),
                        ),
                        Row(
                          children: [
                            Icon(
                              stat.isPositive
                                  ? Icons.trending_up_rounded
                                  : Icons.trending_down_rounded,
                              size: 14,
                              color: stat.isPositive
                                  ? Colors.green.shade600
                                  : Colors.red.shade600,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              stat.trend,
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                color: stat.isPositive
                                    ? Colors.green.shade700
                                    : Colors.red.shade700,
                              ),
                            ),
                            const SizedBox(width: 4),
                            Text(
                              'so với T11',
                              style: TextStyle(
                                fontSize: 10,
                                color: theme.colorScheme.onSurface.withValues(
                                  alpha: 0.5,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  );
                },
              ),

              const SizedBox(height: 28),

              // --- CHARTS / VISUAL ANALYTICS ---
              const Text(
                'Phân tích & Lưu lượng',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  letterSpacing: -0.3,
                ),
              ),
              const SizedBox(height: 12),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: theme.colorScheme.outlineVariant.withValues(
                      alpha: 0.5,
                    ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Lưu lượng Hợp đồng theo tuần',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: theme.colorScheme.surfaceContainerHighest
                                .withValues(alpha: 0.5),
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Text(
                            'Tháng này',
                            style: TextStyle(fontSize: 11),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 20),
                    // Visual bar chart for mobile
                    SizedBox(
                      height: 120,
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceAround,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          _buildBarItem(context, 'T2', 0.45, '45'),
                          _buildBarItem(context, 'T3', 0.70, '70'),
                          _buildBarItem(
                            context,
                            'T4',
                            0.90,
                            '90',
                            isPeak: true,
                          ),
                          _buildBarItem(context, 'T5', 0.60, '60'),
                          _buildBarItem(context, 'T6', 0.80, '80'),
                          _buildBarItem(context, 'T7', 0.35, '35'),
                          _buildBarItem(context, 'CN', 0.20, '20'),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 16),

              // Status Breakdown Progress Card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: theme.colorScheme.outlineVariant.withValues(
                      alpha: 0.5,
                    ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Tỷ lệ Trạng thái Hợp đồng',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 16),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Row(
                        children: [
                          Expanded(
                            flex: 68,
                            child: Container(
                              height: 12,
                              color: const Color(0xFF059669),
                            ),
                          ),
                          Expanded(
                            flex: 18,
                            child: Container(
                              height: 12,
                              color: const Color(0xFF4F46E5),
                            ),
                          ),
                          Expanded(
                            flex: 10,
                            child: Container(
                              height: 12,
                              color: const Color(0xFFD97706),
                            ),
                          ),
                          Expanded(
                            flex: 4,
                            child: Container(
                              height: 12,
                              color: const Color(0xFFE11D48),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    Wrap(
                      spacing: 16,
                      runSpacing: 8,
                      children: [
                        _buildLegendItem(
                          'Đã thanh lý (68%)',
                          const Color(0xFF059669),
                        ),
                        _buildLegendItem(
                          'Đang hiệu lực (18%)',
                          const Color(0xFF4F46E5),
                        ),
                        _buildLegendItem(
                          'Chờ duyệt (10%)',
                          const Color(0xFFD97706),
                        ),
                        _buildLegendItem(
                          'Sắp hết hạn (4%)',
                          const Color(0xFFE11D48),
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 28),

              // --- RECENT ACTIVITIES ---
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'Hoạt động gần đây',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      letterSpacing: -0.3,
                    ),
                  ),
                  TextButton(onPressed: () {}, child: const Text('Xem tất cả')),
                ],
              ),
              const SizedBox(height: 8),
              Container(
                decoration: BoxDecoration(
                  color: theme.colorScheme.surface,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: theme.colorScheme.outlineVariant.withValues(
                      alpha: 0.5,
                    ),
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: recentActivities.length,
                  separatorBuilder: (context, index) => Divider(
                    height: 1,
                    color: theme.colorScheme.outlineVariant.withValues(
                      alpha: 0.4,
                    ),
                  ),
                  itemBuilder: (context, index) {
                    final item = recentActivities[index];
                    return Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Container(
                            width: 40,
                            height: 40,
                            decoration: BoxDecoration(
                              color: item.bgColor,
                              shape: BoxShape.circle,
                            ),
                            child: Icon(item.icon, color: item.color, size: 20),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  item.action,
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  item.details,
                                  style: TextStyle(
                                    fontSize: 13,
                                    color: theme.colorScheme.onSurface
                                        .withValues(alpha: 0.65),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: theme.colorScheme.surfaceContainerHighest
                                  .withValues(alpha: 0.5),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Text(
                              item.time,
                              style: TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w500,
                                color: theme.colorScheme.onSurface.withValues(
                                  alpha: 0.6,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  },
                ),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBarItem(
    BuildContext context,
    String label,
    double ratio,
    String count, {
    bool isPeak = false,
  }) {
    final theme = Theme.of(context);
    final color = isPeak ? theme.colorScheme.primary : const Color(0xFF818CF8);

    return Column(
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        Text(
          count,
          style: TextStyle(
            fontSize: 10,
            fontWeight: FontWeight.bold,
            color: isPeak ? theme.colorScheme.primary : Colors.grey.shade600,
          ),
        ),
        const SizedBox(height: 4),
        AnimatedContainer(
          duration: const Duration(milliseconds: 300),
          width: 18,
          height: 80 * ratio,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(6),
          ),
        ),
        const SizedBox(height: 6),
        Text(
          label,
          style: TextStyle(
            fontSize: 11,
            fontWeight: isPeak ? FontWeight.bold : FontWeight.normal,
            color: theme.colorScheme.onSurface.withValues(alpha: 0.7),
          ),
        ),
      ],
    );
  }

  Widget _buildLegendItem(String label, Color color) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 8,
          height: 8,
          decoration: BoxDecoration(color: color, shape: BoxShape.circle),
        ),
        const SizedBox(width: 6),
        Text(label, style: const TextStyle(fontSize: 11)),
      ],
    );
  }
}

class _StatItem {
  final String label;
  final String value;
  final String trend;
  final bool isPositive;
  final IconData icon;
  final Color color;
  final Color bgColor;

  _StatItem({
    required this.label,
    required this.value,
    required this.trend,
    required this.isPositive,
    required this.icon,
    required this.color,
    required this.bgColor,
  });
}

class _QuickActionItem {
  final String title;
  final IconData icon;
  final Color color;
  final Color bgColor;
  final VoidCallback? onTap;

  _QuickActionItem({
    required this.title,
    required this.icon,
    required this.color,
    required this.bgColor,
    this.onTap,
  });
}

class _ActivityItem {
  final String action;
  final String details;
  final String time;
  final IconData icon;
  final Color color;
  final Color bgColor;

  _ActivityItem({
    required this.action,
    required this.details,
    required this.time,
    required this.icon,
    required this.color,
    required this.bgColor,
  });
}

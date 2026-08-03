import 'package:flutter/material.dart';

class AppDashboardAnalyticsCard extends StatelessWidget {
  const AppDashboardAnalyticsCard({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Phân tích & Lưu lượng',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            letterSpacing: -0.3,
          ),
        ),
        const SizedBox(height: 12),
        // Bar Chart Card
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: theme.colorScheme.surface,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
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
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
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
              SizedBox(
                height: 120,
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    _buildBarItem(context, 'T2', 0.45, '45'),
                    _buildBarItem(context, 'T3', 0.70, '70'),
                    _buildBarItem(context, 'T4', 0.90, '90', isPeak: true),
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
              color: theme.colorScheme.outlineVariant.withValues(alpha: 0.5),
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
                style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
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
                  _buildLegendItem('Đã thanh lý (68%)', const Color(0xFF059669)),
                  _buildLegendItem(
                    'Đang hiệu lực (18%)',
                    const Color(0xFF4F46E5),
                  ),
                  _buildLegendItem('Chờ duyệt (10%)', const Color(0xFFD97706)),
                  _buildLegendItem('Sắp hết hạn (4%)', const Color(0xFFE11D48)),
                ],
              ),
            ],
          ),
        ),
      ],
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

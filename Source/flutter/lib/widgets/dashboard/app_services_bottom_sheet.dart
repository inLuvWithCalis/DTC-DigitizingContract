import 'package:flutter/material.dart';

/// Modal Bottom Sheet hiển thị Hệ sinh thái tất cả dịch vụ doanh nghiệp (Grid 4 cột)
class AppServicesBottomSheet {
  static void show(BuildContext context) {
    final theme = Theme.of(context);
    final primaryColor = theme.colorScheme.primary;
    final onPrimaryColor = theme.colorScheme.onPrimary;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) {
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
                  color: theme.colorScheme.outlineVariant.withValues(
                    alpha: 0.6,
                  ),
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
                            Icons.grid_view_rounded,
                            color: primaryColor,
                            size: 22,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text(
                              'Tùy chỉnh & Tất cả dịch vụ',
                              style: TextStyle(
                                fontSize: 18,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            Text(
                              'Hệ sinh thái ứng dụng doanh nghiệp',
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
                    IconButton(
                      icon: const Icon(Icons.close_rounded),
                      onPressed: () => Navigator.of(context).pop(),
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
                        childAspectRatio: 0.82,
                        children: [
                          _buildGridItem(
                            context,
                            title: 'Dịch vụ',
                            icon: Icons.miscellaneous_services_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () {
                              Navigator.of(context).pop();
                              Navigator.of(
                                context,
                              ).pushNamed('/catalog/services');
                            },
                          ),
                          _buildGridItem(
                            context,
                            title: 'Loại dịch vụ',
                            icon: Icons.category_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () {
                              Navigator.of(context).pop();
                              Navigator.of(
                                context,
                              ).pushNamed('/catalog/service-types');
                            },
                          ),
                          _buildGridItem(
                            context,
                            title: 'Bảng giá',
                            icon: Icons.sell_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Mẫu HĐ',
                            icon: Icons.description_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
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
                        childAspectRatio: 0.82,
                        children: [
                          _buildGridItem(
                            context,
                            title: 'Tạo mới HĐ',
                            icon: Icons.post_add_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Phê duyệt',
                            icon: Icons.verified_user_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Gia hạn HĐ',
                            icon: Icons.update_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Thanh lý',
                            icon: Icons.task_alt_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
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
                        childAspectRatio: 0.82,
                        children: [
                          _buildGridItem(
                            context,
                            title: 'Doanh thu',
                            icon: Icons.pie_chart_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Lưu lượng',
                            icon: Icons.show_chart_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
                          ),
                          _buildGridItem(
                            context,
                            title: 'Nhật ký',
                            icon: Icons.history_toggle_off_rounded,
                            color: primaryColor,
                            bgColor: onPrimaryColor,
                            onTap: () => Navigator.of(context).pop(),
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
      },
    );
  }

  static Widget _buildSectionTitle(String title, IconData icon, Color color) {
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

  static Widget _buildGridItem(
    BuildContext context, {
    required String title,
    required IconData icon,
    required Color color,
    required Color bgColor,
    required VoidCallback onTap,
  }) {
    final theme = Theme.of(context);

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 54,
            height: 54,
            decoration: BoxDecoration(
              color: bgColor,
              borderRadius: BorderRadius.circular(18),
              border: Border.all(
                color: theme.colorScheme.outlineVariant.withValues(alpha: 0.3),
              ),
              boxShadow: [
                BoxShadow(
                  color: color.withValues(alpha: 0.12),
                  blurRadius: 10,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Icon(icon, color: color, size: 26),
          ),
          const SizedBox(height: 8),
          Text(
            title,
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color: theme.colorScheme.onSurface,
            ),
            textAlign: TextAlign.center,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Text } from "@/components/ui/text";
import { Header } from "@/components/ui/custom/header";
import {
  AlertCircle,
  CheckCircle2,
  Clock,
  FileCheck2,
  FilePlus,
  FileText,
  TrendingDown,
  TrendingUp,
} from "lucide-react-native";
import { ScrollView, View } from "react-native";

export default function DashboardPage() {
  const stats = [
    {
      label: "Tổng Hợp đồng",
      value: "1,248",
      trend: "+12.5%",
      isPositive: true,
      icon: FileText,
      color: "text-indigo-600 dark:text-indigo-400",
      bg: "bg-indigo-100 dark:bg-indigo-500/15",
    },
    {
      label: "Chờ Phê duyệt",
      value: "24",
      trend: "-2.4%",
      isPositive: true,
      icon: Clock,
      color: "text-amber-600 dark:text-amber-400",
      bg: "bg-amber-100 dark:bg-amber-500/15",
    },
    {
      label: "Sắp Hết hạn",
      value: "12",
      trend: "+4.1%",
      isPositive: false,
      icon: AlertCircle,
      color: "text-rose-600 dark:text-rose-400",
      bg: "bg-rose-100 dark:bg-rose-500/15",
    },
    {
      label: "Đã Thanh lý",
      value: "856",
      trend: "+8.2%",
      isPositive: true,
      icon: FileCheck2,
      color: "text-emerald-600 dark:text-emerald-400",
      bg: "bg-emerald-100 dark:bg-emerald-500/15",
    },
  ];

  const recentActivities = [
    {
      action: "Tạo mới Hợp đồng Bán",
      details: "HĐ #HB-2024-001 - Công ty TNHH ABC",
      time: "2h trước",
      icon: FilePlus,
      iconColor: "text-indigo-500 dark:text-indigo-400",
      iconBg: "bg-indigo-50 dark:bg-indigo-500/15",
    },
    {
      action: "Phê duyệt Phụ lục",
      details: "Phụ lục #PL-01 cho HĐ #HB-2023-102",
      time: "4h trước",
      icon: CheckCircle2,
      iconColor: "text-emerald-500 dark:text-emerald-400",
      iconBg: "bg-emerald-50 dark:bg-emerald-500/15",
    },
    {
      action: "Cảnh báo Hết hạn",
      details: "Hợp đồng Mua #HM-092 sắp hết hiệu lực",
      time: "6h trước",
      icon: AlertCircle,
      iconColor: "text-rose-500 dark:text-rose-400",
      iconBg: "bg-rose-50 dark:bg-rose-500/15",
    },
  ];

  return (
    <View className="flex-1 bg-background">
      <Header title="Bảng điều khiển" />
      <ScrollView
        className="flex-1"
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ paddingBottom: 40 }}
      >
        <View className="px-5 py-6">
          {/* Header Section */}
          <View className="mb-6">
            <Text className="text-3xl font-bold text-foreground mb-1.5 tracking-tight">
              Xin chào, Quản trị viên
            </Text>
            <Text className="text-muted-foreground text-sm">
              Dưới đây là tình hình số hóa hợp đồng hôm nay.
            </Text>
          </View>

          {/* Stats Grid - Dùng flex-wrap chia 2 cột */}
          <View className="flex-row flex-wrap justify-between mb-8 gap-y-4">
            {stats.map((stat, i) => {
              const Icon = stat.icon;
              return (
                <Card key={i} className="w-[48%] border-border shadow-sm">
                  <CardHeader className="flex-row items-start justify-between pb-2 pt-4 px-4 space-y-0">
                    <View className="flex-1 pr-2">
                      <Text className="text-muted-foreground text-xs font-medium">
                        {stat.label}
                      </Text>
                    </View>
                    <View
                      className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 ${stat.bg}`}
                    >
                      <Icon size={20} className={stat.color} />
                    </View>
                  </CardHeader>
                  <CardContent className="px-4 pb-4">
                    <Text className="text-2xl font-bold text-foreground tracking-tight">
                      {stat.value}
                    </Text>
                    <View className="flex-row items-center gap-1 mt-1 flex-wrap">
                      {stat.isPositive ? (
                        <TrendingUp size={14} className="text-emerald-500" />
                      ) : (
                        <TrendingDown size={14} className="text-rose-500" />
                      )}
                      <Text
                        className={`text-xs font-semibold ${
                          stat.isPositive ? "text-emerald-600" : "text-rose-600"
                        }`}
                      >
                        {stat.trend}
                      </Text>
                    </View>
                    <Text className="text-muted-foreground text-[10px] mt-0.5">
                      so với tháng trước
                    </Text>
                  </CardContent>
                </Card>
              );
            })}
          </View>

          {/* Charts Section */}
          <View className="flex-col gap-6 mb-8">
            <Card className="shadow-sm">
              <CardHeader>
                <CardTitle className="text-lg">Lưu lượng Hợp đồng</CardTitle>
              </CardHeader>
              <CardContent>
                <View className="h-48 bg-muted/30 rounded-xl flex items-center justify-center border border-dashed border-border">
                  <View className="flex-row items-center gap-2">
                    <TrendingUp size={16} className="text-muted-foreground" />
                    <Text className="text-muted-foreground text-sm">
                      Khu vực nhúng Biểu đồ
                    </Text>
                  </View>
                </View>
              </CardContent>
            </Card>

            <Card className="shadow-sm">
              <CardHeader>
                <CardTitle className="text-lg">Tỷ lệ Trạng thái</CardTitle>
              </CardHeader>
              <CardContent>
                <View className="h-48 bg-muted/30 rounded-xl flex items-center justify-center border border-dashed border-border">
                  <View className="flex-row items-center gap-2">
                    <FileCheck2 size={16} className="text-muted-foreground" />
                    <Text className="text-muted-foreground text-sm">
                      Khu vực nhúng Biểu đồ Tròn
                    </Text>
                  </View>
                </View>
              </CardContent>
            </Card>
          </View>

          {/* Recent Activities */}
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle className="text-lg">Hoạt động gần đây</CardTitle>
            </CardHeader>
            <CardContent className="flex-col gap-5">
              {recentActivities.map((activity, i) => {
                const ActivityIcon = activity.icon;
                return (
                  <View
                    key={i}
                    className="flex-row items-start justify-between"
                  >
                    <View className="flex-row gap-3 items-start flex-1">
                      <View
                        className={`w-10 h-10 rounded-full flex items-center justify-center shrink-0 ${activity.iconBg}`}
                      >
                        <ActivityIcon
                          size={20}
                          className={activity.iconColor}
                        />
                      </View>
                      <View className="flex-1 pr-2">
                        <Text className="text-foreground font-semibold text-sm mb-0.5">
                          {activity.action}
                        </Text>
                        <Text
                          className="text-muted-foreground text-xs"
                          numberOfLines={2}
                        >
                          {activity.details}
                        </Text>
                      </View>
                    </View>
                    <View className="bg-muted px-2 py-1 rounded-full">
                      <Text className="text-[10px] font-medium text-muted-foreground">
                        {activity.time}
                      </Text>
                    </View>
                  </View>
                );
              })}
            </CardContent>
          </Card>
        </View>
      </ScrollView>
    </View>
  );
}

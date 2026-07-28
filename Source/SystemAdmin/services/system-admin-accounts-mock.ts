export type SystemAdminRole =
  | "Super Admin"
  | "Operations Admin"
  | "Security Auditor"
  | "Support Admin";

export type SystemAdminStatus = "Active" | "Locked" | "Pending";
export type LoginResult = "Success" | "Failed";

export interface LoginHistoryItem {
  id: string;
  loggedAt: string;
  ipAddress: string;
  device: string;
  location: string;
  result: LoginResult;
}

export interface SystemAdminAccount {
  systemAdminId: number;
  username: string;
  fullName: string;
  email: string;
  role: SystemAdminRole;
  status: SystemAdminStatus;
  permissions: string[];
  lastLoginAt: string;
  lastLoginIp: string;
  loginCount: number;
  createdAt: string;
  createdBy: string;
  loginHistory: LoginHistoryItem[];
}

export const ROLE_PERMISSIONS: Record<SystemAdminRole, string[]> = {
  "Super Admin": [
    "Toàn quyền quản lý tenant",
    "Quản lý tài khoản System Admin",
    "Xem và xuất nhật ký hệ thống",
    "Cấu hình hạ tầng và bảo mật",
  ],
  "Operations Admin": [
    "Tạo, khóa và kích hoạt tenant",
    "Theo dõi database và dung lượng",
    "Xử lý lỗi provisioning",
    "Xem nhật ký vận hành",
  ],
  "Security Auditor": [
    "Xem nhật ký hoạt động",
    "Xem lịch sử đăng nhập",
    "Theo dõi cảnh báo bảo mật",
    "Xuất báo cáo kiểm toán",
  ],
  "Support Admin": [
    "Xem thông tin tenant",
    "Xem trạng thái hệ thống",
    "Hỗ trợ tài khoản tenant",
    "Tạo yêu cầu xử lý sự cố",
  ],
};

export const MOCK_SYSTEM_ADMINS: SystemAdminAccount[] = [
  {
    systemAdminId: 1,
    username: "sysadmin",
    fullName: "System Administrator",
    email: "sysadmin@econtract.local",
    role: "Super Admin",
    status: "Active",
    permissions: ROLE_PERMISSIONS["Super Admin"],
    lastLoginAt: "28/07/2026, 09:42",
    lastLoginIp: "10.10.1.24",
    loginCount: 486,
    createdAt: "01/01/2025",
    createdBy: "System",
    loginHistory: [
      {
        id: "login-1-1",
        loggedAt: "28/07/2026, 09:42",
        ipAddress: "10.10.1.24",
        device: "Chrome 138 · Windows 11",
        location: "Hà Nội, Việt Nam",
        result: "Success",
      },
      {
        id: "login-1-2",
        loggedAt: "27/07/2026, 16:18",
        ipAddress: "10.10.1.24",
        device: "Chrome 138 · Windows 11",
        location: "Hà Nội, Việt Nam",
        result: "Success",
      },
      {
        id: "login-1-3",
        loggedAt: "27/07/2026, 08:03",
        ipAddress: "103.28.36.91",
        device: "Unknown device",
        location: "Hồ Chí Minh, Việt Nam",
        result: "Failed",
      },
    ],
  },
  {
    systemAdminId: 2,
    username: "operations.anh",
    fullName: "Nguyễn Minh Anh",
    email: "anh.nguyen@econtract.local",
    role: "Operations Admin",
    status: "Active",
    permissions: ROLE_PERMISSIONS["Operations Admin"],
    lastLoginAt: "28/07/2026, 08:15",
    lastLoginIp: "10.10.1.37",
    loginCount: 312,
    createdAt: "12/03/2025",
    createdBy: "System Administrator",
    loginHistory: [
      {
        id: "login-2-1",
        loggedAt: "28/07/2026, 08:15",
        ipAddress: "10.10.1.37",
        device: "Edge 138 · Windows 11",
        location: "Hà Nội, Việt Nam",
        result: "Success",
      },
      {
        id: "login-2-2",
        loggedAt: "25/07/2026, 17:21",
        ipAddress: "10.10.1.37",
        device: "Edge 138 · Windows 11",
        location: "Hà Nội, Việt Nam",
        result: "Success",
      },
    ],
  },
  {
    systemAdminId: 3,
    username: "audit.linh",
    fullName: "Vũ Ngọc Linh",
    email: "linh.vu@econtract.local",
    role: "Security Auditor",
    status: "Active",
    permissions: ROLE_PERMISSIONS["Security Auditor"],
    lastLoginAt: "27/07/2026, 14:09",
    lastLoginIp: "10.20.5.18",
    loginCount: 179,
    createdAt: "20/06/2025",
    createdBy: "System Administrator",
    loginHistory: [
      {
        id: "login-3-1",
        loggedAt: "27/07/2026, 14:09",
        ipAddress: "10.20.5.18",
        device: "Safari 18 · macOS",
        location: "Đà Nẵng, Việt Nam",
        result: "Success",
      },
      {
        id: "login-3-2",
        loggedAt: "26/07/2026, 09:30",
        ipAddress: "10.20.5.18",
        device: "Safari 18 · macOS",
        location: "Đà Nẵng, Việt Nam",
        result: "Success",
      },
    ],
  },
  {
    systemAdminId: 4,
    username: "support.nam",
    fullName: "Trần Hoàng Nam",
    email: "nam.tran@econtract.local",
    role: "Support Admin",
    status: "Locked",
    permissions: ROLE_PERMISSIONS["Support Admin"],
    lastLoginAt: "22/07/2026, 11:36",
    lastLoginIp: "10.10.2.51",
    loginCount: 94,
    createdAt: "08/10/2025",
    createdBy: "Nguyễn Minh Anh",
    loginHistory: [
      {
        id: "login-4-1",
        loggedAt: "22/07/2026, 11:36",
        ipAddress: "10.10.2.51",
        device: "Chrome 138 · Windows 10",
        location: "Hà Nội, Việt Nam",
        result: "Success",
      },
      {
        id: "login-4-2",
        loggedAt: "22/07/2026, 11:41",
        ipAddress: "45.117.80.12",
        device: "Firefox 140 · Linux",
        location: "Không xác định",
        result: "Failed",
      },
    ],
  },
  {
    systemAdminId: 5,
    username: "support.ha",
    fullName: "Lê Thu Hà",
    email: "ha.le@econtract.local",
    role: "Support Admin",
    status: "Pending",
    permissions: ROLE_PERMISSIONS["Support Admin"],
    lastLoginAt: "Chưa đăng nhập",
    lastLoginIp: "—",
    loginCount: 0,
    createdAt: "28/07/2026",
    createdBy: "System Administrator",
    loginHistory: [],
  },
];

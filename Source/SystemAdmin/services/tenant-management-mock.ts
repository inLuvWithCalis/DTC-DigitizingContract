export type TenantStatus = "Active" | "Locked" | "Provisioning";
export type DatabaseHealth = "Healthy" | "Warning" | "Provisioning";

export interface TenantManagementItem {
  tenantId: string;
  tenantCode: string;
  tenantName: string;
  status: TenantStatus;
  plan: "Enterprise" | "Business" | "Starter";
  databaseName: string;
  databaseMode: "Dedicated" | "Shared";
  databaseHealth: DatabaseHealth;
  databaseServer: string;
  databaseVersion: string;
  region: string;
  storageUsedMb: number;
  storageLimitMb: number;
  totalUsers: number;
  activeUsers: number;
  contractCount: number;
  ownerName: string;
  ownerEmail: string;
  domain: string;
  createdAt: string;
  lastActivityAt: string;
  lastBackupAt: string;
}

export const MOCK_TENANTS: TenantManagementItem[] = [
  {
    tenantId: "tenant-dtc-001",
    tenantCode: "dtc",
    tenantName: "Công ty TNHH DTC",
    status: "Active",
    plan: "Enterprise",
    databaseName: "ContractManagement_Tenant_dtc",
    databaseMode: "Dedicated",
    databaseHealth: "Healthy",
    databaseServer: "sql-prod-01.internal",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 7340,
    storageLimitMb: 20480,
    totalUsers: 86,
    activeUsers: 71,
    contractCount: 1248,
    ownerName: "Nguyễn Minh Anh",
    ownerEmail: "admin@dtc.com.vn",
    domain: "dtc.econtract.local",
    createdAt: "12/01/2025",
    lastActivityAt: "2 phút trước",
    lastBackupAt: "28/07/2026, 02:00",
  },
  {
    tenantId: "tenant-horizon-002",
    tenantCode: "horizon-tech",
    tenantName: "Công ty Cổ phần Horizon Tech",
    status: "Active",
    plan: "Business",
    databaseName: "ContractManagement_Tenant_horizon-tech",
    databaseMode: "Dedicated",
    databaseHealth: "Healthy",
    databaseServer: "sql-prod-02.internal",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 4290,
    storageLimitMb: 10240,
    totalUsers: 42,
    activeUsers: 34,
    contractCount: 683,
    ownerName: "Trần Hoàng Nam",
    ownerEmail: "nam.tran@horizon.vn",
    domain: "horizon-tech.econtract.local",
    createdAt: "03/03/2025",
    lastActivityAt: "18 phút trước",
    lastBackupAt: "28/07/2026, 02:15",
  },
  {
    tenantId: "tenant-nova-003",
    tenantCode: "nova-retail",
    tenantName: "Nova Retail Việt Nam",
    status: "Active",
    plan: "Business",
    databaseName: "ContractManagement_Tenant_nova-retail",
    databaseMode: "Shared",
    databaseHealth: "Warning",
    databaseServer: "sql-shared-01.internal",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 9210,
    storageLimitMb: 10240,
    totalUsers: 39,
    activeUsers: 29,
    contractCount: 954,
    ownerName: "Lê Thu Hà",
    ownerEmail: "ha.le@novaretail.vn",
    domain: "nova-retail.econtract.local",
    createdAt: "21/04/2025",
    lastActivityAt: "1 giờ trước",
    lastBackupAt: "28/07/2026, 02:30",
  },
  {
    tenantId: "tenant-zenith-004",
    tenantCode: "zenith-logistics",
    tenantName: "Zenith Logistics",
    status: "Locked",
    plan: "Starter",
    databaseName: "ContractManagement_Tenant_zenith-logistics",
    databaseMode: "Shared",
    databaseHealth: "Healthy",
    databaseServer: "sql-shared-01.internal",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 1780,
    storageLimitMb: 5120,
    totalUsers: 18,
    activeUsers: 0,
    contractCount: 226,
    ownerName: "Phạm Quốc Bảo",
    ownerEmail: "bao.pham@zenith.vn",
    domain: "zenith-logistics.econtract.local",
    createdAt: "09/06/2025",
    lastActivityAt: "5 ngày trước",
    lastBackupAt: "28/07/2026, 02:45",
  },
  {
    tenantId: "tenant-green-005",
    tenantCode: "green-energy",
    tenantName: "Green Energy Solutions",
    status: "Active",
    plan: "Enterprise",
    databaseName: "ContractManagement_Tenant_green-energy",
    databaseMode: "Dedicated",
    databaseHealth: "Healthy",
    databaseServer: "sql-prod-03.internal",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 12860,
    storageLimitMb: 20480,
    totalUsers: 103,
    activeUsers: 87,
    contractCount: 1786,
    ownerName: "Vũ Ngọc Linh",
    ownerEmail: "linh.vu@greenenergy.vn",
    domain: "green-energy.econtract.local",
    createdAt: "17/08/2025",
    lastActivityAt: "7 phút trước",
    lastBackupAt: "28/07/2026, 03:00",
  },
  {
    tenantId: "tenant-atlas-006",
    tenantCode: "atlas-holdings",
    tenantName: "Atlas Holdings",
    status: "Provisioning",
    plan: "Business",
    databaseName: "ContractManagement_Tenant_atlas-holdings",
    databaseMode: "Dedicated",
    databaseHealth: "Provisioning",
    databaseServer: "Đang cấp phát",
    databaseVersion: "SQL Server 2022",
    region: "Southeast Asia",
    storageUsedMb: 0,
    storageLimitMb: 10240,
    totalUsers: 1,
    activeUsers: 0,
    contractCount: 0,
    ownerName: "Đặng Hải Long",
    ownerEmail: "long.dang@atlas.vn",
    domain: "atlas-holdings.econtract.local",
    createdAt: "28/07/2026",
    lastActivityAt: "Đang khởi tạo",
    lastBackupAt: "Chưa có",
  },
];

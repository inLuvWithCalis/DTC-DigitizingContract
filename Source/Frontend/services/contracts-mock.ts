export type ContractStatus =
  | "Draft"
  | "Negotiating"
  | "Approved"
  | "Signed"
  | "Closing"
  | "Closed";

export type ContractType =
  | "Software"
  | "Maintenance"
  | "Liquidation"
  | "Appendix";

export interface ContractDocument {
  id: number;
  name: string;
  type: string;
  owner: string;
  uploadedAt: string;
  status: "Completed" | "Missing" | "Pending";
}

export interface ContractComment {
  id: number;
  author: string;
  role: string;
  content: string;
  createdAt: string;
}

export interface ContractTimelineItem {
  title: string;
  description: string;
  date: string;
  completed: boolean;
}

export interface ContractMock {
  id: number;
  contractNo: string;
  title: string;
  customerName: string;
  customerCompany: string;
  ownerName: string;
  type: ContractType;
  status: ContractStatus;
  value: number;
  createdAt: string;
  effectiveDate: string;
  expiredDate: string;
  paymentProgress: number;
  hardCopyStatus: "Chưa gửi" | "Đã gửi" | "Đã nhận";
  quotationNo?: string;
  publicLink: string;
  summary: string;
  documents: ContractDocument[];
  comments: ContractComment[];
  timeline: ContractTimelineItem[];
}

export const CONTRACT_STATUS_LABELS: Record<ContractStatus, string> = {
  Draft: "Bản nháp",
  Negotiating: "Đàm phán",
  Approved: "Đã chốt điều khoản",
  Signed: "Đã ký điện tử",
  Closing: "Hoàn thiện hồ sơ",
  Closed: "Đã đóng",
};

export const CONTRACT_TYPE_LABELS: Record<ContractType, string> = {
  Software: "Hợp đồng phần mềm",
  Maintenance: "Hợp đồng bảo trì",
  Liquidation: "Biên bản thanh lý",
  Appendix: "Phụ lục hợp đồng",
};

export const CONTRACT_STATUS_OPTIONS = [
  { label: "Tất cả trạng thái", value: "All" },
  { label: CONTRACT_STATUS_LABELS.Draft, value: "Draft" },
  { label: CONTRACT_STATUS_LABELS.Negotiating, value: "Negotiating" },
  { label: CONTRACT_STATUS_LABELS.Approved, value: "Approved" },
  { label: CONTRACT_STATUS_LABELS.Signed, value: "Signed" },
  { label: CONTRACT_STATUS_LABELS.Closing, value: "Closing" },
  { label: CONTRACT_STATUS_LABELS.Closed, value: "Closed" },
];

export const mockContracts: ContractMock[] = [
  {
    id: 1,
    contractNo: "HD-2026-001",
    title: "Triển khai hệ thống quản lý hợp đồng điện tử",
    customerName: "Nguyễn Văn A",
    customerCompany: "Công ty TNHH ABC",
    ownerName: "Trần Minh Quân",
    type: "Software",
    status: "Negotiating",
    value: 450000000,
    createdAt: "2026-07-02T09:15:00",
    effectiveDate: "2026-07-20T00:00:00",
    expiredDate: "2027-07-20T00:00:00",
    paymentProgress: 40,
    hardCopyStatus: "Chưa gửi",
    quotationNo: "BG-2026-018",
    publicLink: "https://contract.example.com/view/hd-2026-001",
    summary:
      "Hợp đồng triển khai phần mềm quản lý hợp đồng, bao gồm cấu hình hệ thống, đào tạo người dùng và bảo hành 12 tháng.",
    documents: [
      {
        id: 1,
        name: "Dự thảo hợp đồng v1.pdf",
        type: "Draft Contract",
        owner: "Sales",
        uploadedAt: "2026-07-02T10:20:00",
        status: "Completed",
      },
      {
        id: 2,
        name: "Biên bản thống nhất điều khoản.docx",
        type: "Negotiation",
        owner: "Sales",
        uploadedAt: "2026-07-06T15:30:00",
        status: "Pending",
      },
    ],
    comments: [
      {
        id: 1,
        author: "Khách hàng",
        role: "Người phụ trách đàm phán",
        content: "Đề xuất bổ sung điều khoản hỗ trợ kỹ thuật ngoài giờ.",
        createdAt: "2026-07-05T09:10:00",
      },
      {
        id: 2,
        author: "Trần Minh Quân",
        role: "Sales",
        content: "Đã ghi nhận và cập nhật vào điều khoản dịch vụ hỗ trợ.",
        createdAt: "2026-07-05T14:45:00",
      },
    ],
    timeline: [
      {
        title: "Tạo bản nháp",
        description: "Sales tạo hợp đồng từ báo giá đã chốt.",
        date: "02/07/2026",
        completed: true,
      },
      {
        title: "Gửi khách xem link",
        description: "Khách hàng xem và góp ý điều khoản mềm.",
        date: "03/07/2026",
        completed: true,
      },
      {
        title: "Chốt điều khoản",
        description: "Hai bên thống nhất nội dung trước khi ký.",
        date: "Đang xử lý",
        completed: false,
      },
      {
        title: "Ký điện tử OTP",
        description: "Đại diện hai bên xác nhận ký bằng OTP.",
        date: "Chưa thực hiện",
        completed: false,
      },
    ],
  },
  {
    id: 2,
    contractNo: "HD-2026-002",
    title: "Gói bảo trì phần mềm CRM năm 2026",
    customerName: "Phạm Văn D",
    customerCompany: "MNO Company",
    ownerName: "Lê Hoàng Nam",
    type: "Maintenance",
    status: "Closing",
    value: 85000000,
    createdAt: "2026-06-18T11:30:00",
    effectiveDate: "2026-07-01T00:00:00",
    expiredDate: "2027-07-01T00:00:00",
    paymentProgress: 100,
    hardCopyStatus: "Đã nhận",
    quotationNo: "BG-2026-014",
    publicLink: "https://contract.example.com/view/hd-2026-002",
    summary:
      "Gia hạn bảo trì phần mềm CRM, bao gồm hỗ trợ vận hành, cập nhật phiên bản và xử lý lỗi trong 12 tháng.",
    documents: [
      {
        id: 1,
        name: "Hợp đồng đã ký điện tử.pdf",
        type: "Signed Contract",
        owner: "Giám đốc",
        uploadedAt: "2026-06-25T08:00:00",
        status: "Completed",
      },
      {
        id: 2,
        name: "Hóa đơn VAT.pdf",
        type: "Invoice",
        owner: "Admin Officer",
        uploadedAt: "2026-06-27T13:20:00",
        status: "Completed",
      },
      {
        id: 3,
        name: "Biên bản nghiệm thu.pdf",
        type: "Acceptance",
        owner: "Technical Staff",
        uploadedAt: "2026-06-29T16:10:00",
        status: "Pending",
      },
    ],
    comments: [
      {
        id: 1,
        author: "Admin Officer",
        role: "Hành chính",
        content: "Đã nhận bản cứng từ khách hàng, chờ biên bản nghiệm thu.",
        createdAt: "2026-06-30T09:00:00",
      },
    ],
    timeline: [
      {
        title: "Ký điện tử",
        description: "Hai bên đã hoàn tất ký OTP.",
        date: "25/06/2026",
        completed: true,
      },
      {
        title: "Upload hóa đơn",
        description: "Admin đã đính kèm hóa đơn VAT.",
        date: "27/06/2026",
        completed: true,
      },
      {
        title: "Bổ sung biên bản nghiệm thu",
        description: "Kỹ thuật cần upload biên bản nghiệm thu cuối cùng.",
        date: "Đang xử lý",
        completed: false,
      },
    ],
  },
  {
    id: 3,
    contractNo: "HD-2026-003",
    title: "Cung cấp module quản lý kho tích hợp",
    customerName: "Hoàng Văn E",
    customerCompany: "PQR Company",
    ownerName: "Nguyễn Thu Hà",
    type: "Software",
    status: "Signed",
    value: 230000000,
    createdAt: "2026-07-10T08:45:00",
    effectiveDate: "2026-07-25T00:00:00",
    expiredDate: "2027-07-25T00:00:00",
    paymentProgress: 70,
    hardCopyStatus: "Đã gửi",
    quotationNo: "BG-2026-021",
    publicLink: "https://contract.example.com/view/hd-2026-003",
    summary:
      "Cung cấp module quản lý kho, tích hợp dữ liệu tồn kho với hệ thống hợp đồng và báo giá hiện có.",
    documents: [
      {
        id: 1,
        name: "Hợp đồng đã ký điện tử.pdf",
        type: "Signed Contract",
        owner: "System",
        uploadedAt: "2026-07-12T09:30:00",
        status: "Completed",
      },
      {
        id: 2,
        name: "Biên bản bàn giao.pdf",
        type: "Handover",
        owner: "Technical Staff",
        uploadedAt: "2026-07-15T14:00:00",
        status: "Missing",
      },
    ],
    comments: [
      {
        id: 1,
        author: "Nguyễn Thu Hà",
        role: "Sales",
        content: "Khách đã ký điện tử, cần theo dõi thanh toán đợt cuối.",
        createdAt: "2026-07-12T10:00:00",
      },
    ],
    timeline: [
      {
        title: "Chốt điều khoản",
        description: "Khách hàng xác nhận điều khoản triển khai.",
        date: "11/07/2026",
        completed: true,
      },
      {
        title: "Ký điện tử",
        description: "Đại diện hai bên đã ký qua OTP.",
        date: "12/07/2026",
        completed: true,
      },
      {
        title: "Theo dõi thanh toán",
        description: "Chờ thanh toán 30% còn lại.",
        date: "Đang xử lý",
        completed: false,
      },
    ],
  },
  {
    id: 4,
    contractNo: "HD-2026-004",
    title: "Phụ lục bổ sung tính năng ký OTP",
    customerName: "Trần Văn B",
    customerCompany: "XYZ Company",
    ownerName: "Trần Minh Quân",
    type: "Appendix",
    status: "Draft",
    value: 65000000,
    createdAt: "2026-07-16T10:00:00",
    effectiveDate: "2026-08-01T00:00:00",
    expiredDate: "2027-08-01T00:00:00",
    paymentProgress: 0,
    hardCopyStatus: "Chưa gửi",
    quotationNo: "BG-2026-024",
    publicLink: "https://contract.example.com/view/hd-2026-004",
    summary:
      "Phụ lục bổ sung tính năng ký OTP qua SMS cho luồng hợp đồng điện tử hiện tại.",
    documents: [],
    comments: [],
    timeline: [
      {
        title: "Tạo bản nháp",
        description: "Đang chuẩn bị nội dung phụ lục.",
        date: "16/07/2026",
        completed: true,
      },
      {
        title: "Gửi khách duyệt",
        description: "Chưa gửi link xem cho khách hàng.",
        date: "Chưa thực hiện",
        completed: false,
      },
    ],
  },
];

export const contractMockApi = {
  getAll: async () => mockContracts,
  getById: async (id: number) =>
    mockContracts.find((contract) => contract.id === id) || null,
};

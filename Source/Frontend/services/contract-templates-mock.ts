import { ContractType } from "@/services/contract-api";

export interface MockContractTerm {
  id: string;
  termCode: string;
  termTitle: string;
  termContent: string;
  isNegotiable: boolean;
  displayOrder: number;
}

export interface MockContractTemplate {
  versionId: number;
  templateCode: string;
  name: string;
  version: string;
  description: string;
  contractType: ContractType;
  updatedAt: string;
  terms: MockContractTerm[];
}

const softwareSupplyTerms: MockContractTerm[] = [
  {
    id: "supply-scope",
    termCode: "SCOPE",
    termTitle: "Điều 1. Phạm vi cung cấp",
    termContent:
      "Bên B cung cấp các sản phẩm, dịch vụ và phạm vi triển khai theo danh mục được ghi nhận trong hợp đồng.",
    isNegotiable: false,
    displayOrder: 1,
  },
  {
    id: "supply-payment",
    termCode: "PAYMENT",
    termTitle: "Điều 2. Giá trị và thanh toán",
    termContent:
      "Giá trị hợp đồng được thanh toán theo từng đợt, căn cứ vào tiến độ và biên bản xác nhận của hai bên.",
    isNegotiable: true,
    displayOrder: 2,
  },
  {
    id: "supply-delivery",
    termCode: "DELIVERY",
    termTitle: "Điều 3. Bàn giao và triển khai",
    termContent:
      "Việc bàn giao và triển khai được thực hiện theo kế hoạch do hai bên thống nhất.",
    isNegotiable: true,
    displayOrder: 3,
  },
  {
    id: "supply-warranty",
    termCode: "WARRANTY",
    termTitle: "Điều 4. Bảo hành và hỗ trợ",
    termContent:
      "Bên B thực hiện bảo hành và hỗ trợ kỹ thuật theo phạm vi đã thỏa thuận.",
    isNegotiable: true,
    displayOrder: 4,
  },
  {
    id: "supply-confidentiality",
    termCode: "CONFIDENTIALITY",
    termTitle: "Điều 5. Bảo mật thông tin",
    termContent:
      "Hai bên có trách nhiệm bảo mật các thông tin nhận được trong quá trình thực hiện hợp đồng.",
    isNegotiable: false,
    displayOrder: 5,
  },
];

const maintenanceTerms: MockContractTerm[] = [
  {
    id: "maintenance-scope",
    termCode: "MAINTENANCE_SCOPE",
    termTitle: "Điều 1. Phạm vi bảo trì",
    termContent:
      "Bên B thực hiện kiểm tra, khắc phục lỗi và cập nhật kỹ thuật cho các hạng mục phần mềm thuộc phạm vi hợp đồng.",
    isNegotiable: false,
    displayOrder: 1,
  },
  {
    id: "maintenance-sla",
    termCode: "SERVICE_LEVEL",
    termTitle: "Điều 2. Mức độ dịch vụ",
    termContent:
      "Thời gian tiếp nhận và xử lý sự cố được áp dụng theo mức độ ưu tiên đã thống nhất giữa hai bên.",
    isNegotiable: true,
    displayOrder: 2,
  },
  {
    id: "maintenance-fee",
    termCode: "PAYMENT",
    termTitle: "Điều 3. Phí dịch vụ và thanh toán",
    termContent:
      "Phí bảo trì được thanh toán định kỳ theo kế hoạch và hồ sơ nghiệm thu dịch vụ.",
    isNegotiable: true,
    displayOrder: 3,
  },
  {
    id: "maintenance-responsibility",
    termCode: "RESPONSIBILITY",
    termTitle: "Điều 4. Trách nhiệm phối hợp",
    termContent:
      "Mỗi bên chỉ định đầu mối và cung cấp đầy đủ thông tin cần thiết để xử lý yêu cầu bảo trì.",
    isNegotiable: true,
    displayOrder: 4,
  },
  {
    id: "maintenance-confidentiality",
    termCode: "CONFIDENTIALITY",
    termTitle: "Điều 5. Bảo mật thông tin",
    termContent:
      "Dữ liệu và thông tin kỹ thuật phát sinh trong quá trình bảo trì phải được bảo mật.",
    isNegotiable: false,
    displayOrder: 5,
  },
];

const upkeepTerms: MockContractTerm[] = [
  {
    id: "upkeep-scope",
    termCode: "UPKEEP_SCOPE",
    termTitle: "Điều 1. Phạm vi duy trì",
    termContent:
      "Bên B duy trì hoạt động ổn định của hệ thống và thực hiện các công việc định kỳ theo kế hoạch.",
    isNegotiable: false,
    displayOrder: 1,
  },
  {
    id: "upkeep-monitoring",
    termCode: "MONITORING",
    termTitle: "Điều 2. Giám sát hệ thống",
    termContent:
      "Các chỉ số vận hành được theo dõi và cảnh báo theo ngưỡng do hai bên thống nhất.",
    isNegotiable: true,
    displayOrder: 2,
  },
  {
    id: "upkeep-report",
    termCode: "REPORTING",
    termTitle: "Điều 3. Báo cáo định kỳ",
    termContent:
      "Bên B cung cấp báo cáo tình trạng vận hành và khuyến nghị cải thiện theo định kỳ.",
    isNegotiable: true,
    displayOrder: 3,
  },
  {
    id: "upkeep-payment",
    termCode: "PAYMENT",
    termTitle: "Điều 4. Phí duy trì",
    termContent:
      "Phí duy trì được thanh toán theo kỳ và căn cứ trên biên bản xác nhận dịch vụ.",
    isNegotiable: true,
    displayOrder: 4,
  },
  {
    id: "upkeep-confidentiality",
    termCode: "CONFIDENTIALITY",
    termTitle: "Điều 5. Bảo mật thông tin",
    termContent:
      "Hai bên cam kết bảo mật dữ liệu vận hành và thông tin truy cập hệ thống.",
    isNegotiable: false,
    displayOrder: 5,
  },
];

export const mockContractTemplates: MockContractTemplate[] = [
  {
    versionId: 1,
    templateCode: "TPL-SW-SUPPLY",
    name: "Hợp đồng cung cấp phần mềm tiêu chuẩn",
    version: "1.0",
    description:
      "Mẫu cơ bản cho hợp đồng cung cấp, triển khai và bàn giao phần mềm.",
    contractType: ContractType.SoftwareSupply,
    updatedAt: "15/07/2026",
    terms: softwareSupplyTerms,
  },
  {
    versionId: 2,
    templateCode: "TPL-SW-MAINTENANCE",
    name: "Hợp đồng bảo trì phần mềm",
    version: "2.1",
    description:
      "Mẫu dành cho dịch vụ bảo trì, hỗ trợ kỹ thuật và xử lý sự cố.",
    contractType: ContractType.SoftwareMaintenance,
    updatedAt: "18/07/2026",
    terms: maintenanceTerms,
  },
  {
    versionId: 3,
    templateCode: "TPL-SW-UPKEEP",
    name: "Hợp đồng duy trì phần mềm",
    version: "1.2",
    description:
      "Mẫu dành cho giám sát, vận hành và duy trì hệ thống theo định kỳ.",
    contractType: ContractType.SoftwareUpkeep,
    updatedAt: "20/07/2026",
    terms: upkeepTerms,
  },
];

export function cloneMockTerms(terms: MockContractTerm[]) {
  return terms.map((term) => ({ ...term }));
}

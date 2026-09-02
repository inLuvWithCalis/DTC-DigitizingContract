# API handoff — ContractManagement MVP

## Cách gọi chung

- Base URL: `https://api.example.com`.
- Employee API dùng session cookie `ContractManagement.Session` và tenant được resolve từ session; `X-Tenant-Code` chỉ là fallback được cấu hình, không phải quyền truy cập.
- System Admin API dùng System Admin session và không nhận tenant business session.
- Public customer API dùng cookie HttpOnly do OTP verify cấp.
- JSON response nghiệp vụ thường có `{ success, message, data, errors }`; frontend interceptor unwrap `data`.
- Mutation có cạnh tranh dùng `rowVersion`. Khi nhận `409 StaleRowVersion`, refetch resource trước khi cho sửa lại.

## Vai trò chính

| Nhóm | Quyền chính |
|---|---|
| Employee active | Directory/lookup/catalog read; quotation; contract phụ trách |
| Manager | Đọc contract toàn tenant, audit tenant, quản trị employee/department; không tự sửa contract người khác |
| Admin Officer | Quản lý catalog và contract template |
| Sale/Marketing/Manager | Full CRM |
| System Admin | Provision tenant, đổi/thu hồi Manager, central security audit |
| Customer public | Xem shared contract và comment sau OTP; không có employee permission |

## Endpoint MVP cần smoke test

- `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/logout`
- `POST /api/system-auth/login`, `GET /api/system-auth/me`, `POST /api/system-auth/logout`
- `GET|POST /api/admin/tenants`
- `PUT /api/admin/tenants/{tenantCode}/employees/{employeeId}/role`
- `GET /api/employees/directory`, `GET /api/customers/lookup`, `GET /api/contract-templates/available`
- `GET|POST|PUT /api/contracts/...` và các route negotiation/comment/version/approval
- `GET /api/contract-approvals`, `POST /api/contract-approvals/{id}/approve|request-changes|reject`
- signing evidence, acceptance, payment schedules, completion/appendix theo contract
- `GET /api/security-audits`, `GET /api/admin/security-audits`, `GET /api/contract-audits`
- `GET /api/contracts/{contractId}/attachments/{attachmentId}/download`

Swagger UI được bật trong Development. XML comments là nguồn mô tả request/response; collection Postman đi kèm dùng biến `baseUrl`, `tenantCode`, `contractId`, `rowVersion` và không chứa credential.

## Mã lỗi frontend phải xử lý

| HTTP/code | Xử lý |
|---|---|
| `401 AuthenticationRequired`, `EmployeeInactive` | Xóa state và về đúng login employee/public/System Admin |
| `403 PermissionDenied` | Giữ trang an toàn, ẩn mutation theo permission và báo không đủ quyền |
| `404 ResourceNotFound` | Hiển thị not-found/resource đã bị xóa; không retry mutation mù |
| `409 StaleRowVersion` | Refetch, hiển thị dữ liệu mới, yêu cầu người dùng xác nhận lại |
| `409 LastActiveManager` | Không thu hồi/khóa Manager active cuối cùng |

Mỗi lỗi production cần ghi HTTP status, error code, `correlationId`, actor, tenant, endpoint và timestamp; không log password, OTP, token, cookie, connection string hoặc public link token.


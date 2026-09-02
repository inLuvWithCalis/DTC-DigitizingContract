# Phase 11 — Production runbook

## Phạm vi triển khai

MVP chạy đúng **một instance ContractManagement.API**, một tenant frontend và một System Admin frontend. Không bật auto-migration khi API khởi động. Dashboard/report, notification, e-signing và OTP signing nằm ngoài MVP.

## Cấu hình bắt buộc

Toàn bộ giá trị nhạy cảm được cấp bằng environment variable hoặc secret store; không ghi vào `appsettings*.json` hay `.env*` được commit.

| Biến | Ý nghĩa |
|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | Bật production validation, HSTS và Secure cookie |
| `ASPNETCORE_URLS=https://0.0.0.0:5005` | HTTPS listener; có thể thay bằng TLS termination đã kiểm soát |
| `AllowedHosts=api.example.com` | Host cố định, không dùng `*`/`localhost` |
| `ConnectionStrings__CentralDatabase` | Central DB |
| `ConnectionStrings__TenantDatabaseTemplate` | SQL template dùng provisioning; database trong chuỗi sẽ được thay |
| `Cors__AllowedOrigins__0=https://app.example.com` | Tenant frontend origin |
| `Cors__AllowedOrigins__1=https://admin.example.com` | System Admin origin |
| `PrivateFileStorage__RootPath` | Đường dẫn tuyệt đối, nằm ngoài source/publish/wwwroot |
| `PrivateFileStorage__MinimumFreeSpaceBytes` | Ngưỡng disk fail-fast; mặc định 1 GiB |
| `TemplatePdfRendering__ExecutablePath` | Đường dẫn tuyệt đối tới `soffice` |
| `CustomerOtp__HashKey`, `CustomerOtp__EncryptionKey` | Base64, mỗi khóa đúng 32 bytes |
| `CustomerOtp__Provider` | Provider thật, không dùng `Fake` |
| `CustomerOtp__ProviderEndpoint`, `CustomerOtp__ProviderApiKey` | Kênh gửi OTP production |

Bootstrap System Admin chỉ dùng một lần bằng `SystemAdminBootstrap__Enabled=true`, kèm `Username`, `Password` (tối thiểu 12 ký tự), `FullName`, `Email`. Sau khi tài khoản được tạo, đặt lại `Enabled=false` và rotate/xóa secret bootstrap.

## Private storage

1. Tạo volume riêng ngoài thư mục publish, ví dụ `D:\ContractManagement\private-files` hoặc `/srv/contract-management/private-files`.
2. Chỉ cấp read/write/delete cho identity chạy API và tài khoản backup; không publish volume qua Nginx/IIS/static files.
3. API fail-fast nếu đường dẫn tương đối trong production, nằm trong app/wwwroot, không ghi được hoặc dưới ngưỡng dung lượng.
4. Backup **database và private storage trong cùng maintenance window**. Metadata DB (`StorageKey`, `TenantCode`, `Sha256`) và file vật lý phải được restore cùng một mốc.
5. Hàng ngày theo dõi dung lượng; cảnh báo trước ngưỡng cấu hình. Hàng tuần lấy mẫu hash SHA-256 và so với DB; trước/sau restore phải kiểm tra toàn bộ artifact hợp đồng quan trọng.
6. File legacy trong `wwwroot/uploads` vẫn đọc được để tương thích. Upload mới phải có `StorageKey` và nằm trong private storage; lập đợt migration legacy riêng, không xóa file cũ trước khi đối soát.

## Migration production thủ công

### Chuẩn bị

1. Chốt phiên bản code/migration, build Release và backup Central DB, từng tenant DB, private storage.
2. Kiểm tra dung lượng SQL/storage, quyền account migration, `soffice`, certificate và secret.
3. Bật maintenance/read-only ở load balancer; dừng API để không có transaction mới.

### Central DB

Trong `Source/Backend`, cấp `ConnectionStrings__CentralDatabase` tới Central DB đích rồi chạy:

```powershell
dotnet ef database update --context CentralDbContext --project .\ContractManagement.Infrastructure\ContractManagement.Infrastructure.csproj --startup-project .\ContractManagement.API\ContractManagement.API.csproj
```

### Từng tenant DB

Lặp tuần tự theo danh sách tenant Active trong Central DB. Với mỗi tenant, đặt `ConnectionStrings__TenantDatabaseTemplate` thành connection string **trỏ đúng database tenant đó**, rồi chạy:

```powershell
dotnet ef database update --context DbDtctechContext --project .\ContractManagement.Infrastructure\ContractManagement.Infrastructure.csproj --startup-project .\ContractManagement.API\ContractManagement.API.csproj
```

Không chạy lệnh tenant khi connection string còn trỏ `master`. Ghi log tenant code, database, migration trước/sau và kết quả của từng lần chạy; dừng rollout khi một tenant lỗi.

### Xác minh và mở lại

1. Chạy `dotnet test -c Release --no-build` trên artifact đã build.
2. Khởi động đúng một API instance; xác nhận không có startup migration, bootstrap ngoài ý muốn hoặc lỗi storage/config.
3. Smoke test: employee login, System Admin login, tenant list/create (chỉ môi trường test), contract create → negotiate → submit → approve → signing evidence → complete, download artifact và contract attachment.
4. Xác nhận 401/403/404/409 đúng contract, kiểm tra audit/correlation ID và cookie `Secure; HttpOnly`.
5. Mở traffic, theo dõi error rate, disk, SQL và OTP outbox. Có lỗi thì đóng traffic, restore DB + storage cùng checkpoint.

## Build frontend

```powershell
cd Source\Frontend
npm ci
npm run typecheck
npm run build

cd ..\SystemAdmin
npm ci
npm run typecheck
npm run lint
npm run build
```

`NEXT_PUBLIC_API_URL` chỉ chứa origin API (ví dụ `https://api.example.com`), không thêm `/api`; mỗi service tự khai báo route `/api/...`.


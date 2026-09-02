# Phase 12 — QA release gate report

Ngày chạy: **2026-09-02**  
Môi trường: Windows, .NET 10, SQL Server local, LibreOffice 26.2.5.2, Next.js 16.3.4.

## 1. Kết luận

**Automated release gate: PASS**

- Backend: **440/440 pass, 0 fail, 0 skipped**.
- SQL Server release gate: pass trên hai tenant database tạm được migrate từ database trống.
- LibreOffice release gate: DOCX thật được chuyển thành PDF có header `%PDF-` hợp lệ.
- Frontend: typecheck pass, production build pass, lint 0 error.
- SystemAdmin: typecheck pass, production build pass, lint 0 error.

**Production go-live gate: CONDITIONAL**

Code và automated QA đã pass. Trước khi mở traffic production vẫn phải chạy checklist thủ công ở mục 8 trên đúng hạ tầng production/staging: cookie HTTPS, CORS, OTP provider thật, backup/restore DB + private storage và browser E2E bằng tài khoản thật.

## 2. Lỗi được Phase 12 phát hiện và đã sửa

### Completed vẫn được đánh giá là ready

Trước khi sửa, `ContractCompletionPolicy` cho phép cả `Signed` và `Completed` thỏa điều kiện lifecycle. Hậu quả:

- `GET readiness` sau Completed vẫn trả `Ready = true`.
- Manager có thể gọi Complete lần thứ hai.

Đã sửa:

- Chỉ `Signed` được đánh giá đủ điều kiện chuyển sang `Completed`.
- `Completed` trả readiness false.
- Upload acceptance, add payment, void payment và complete lần hai đều bị chặn sau Completed.
- Bổ sung regression test ở policy và service.

File:

- `Backend/ContractManagement.API/Domains/Policies/Contract/ContractCompletionPolicy.cs`.
- `Backend/ContractManagement.Tests/Domains/Policies/Contract/ContractCompletionPolicyTests.cs`.
- `Backend/ContractManagement.Tests/Domains/Services/Contract/ContractCompletionServicePhase10Tests.cs`.

## 3. Kết quả Backend

Lệnh cuối cùng:

```powershell
$env:PHASE12_SQLSERVER_CONNECTION = "<SQL Server QA connection; database sẽ bị thay bằng tên ContractManagement_Phase12_QA_*; không dùng production DB>"
$env:PHASE12_LIBREOFFICE_PATH = "C:\Program Files\LibreOffice\program\soffice.exe"
dotnet test .\ContractManagement.Tests\ContractManagement.Tests.csproj `
  --configuration Release `
  --no-restore `
  --logger "console;verbosity=minimal"
```

Kết quả:

```text
Passed: 440
Failed: 0
Skipped: 0
Duration: 15 s
```

Ghi chú:

- Restore/build vẫn cảnh báo `NU1900` vì máy không truy cập được vulnerability feed `https://api.nuget.org/v3/index.json`.
- Đây không phải test failure, nhưng pipeline có network phải chạy lại vulnerability audit trước release.

## 4. SQL Server và migration

### Database hiện hữu

- Central DB kết nối được và nhận đủ bốn Central migrations hiện tại.
- Tenant `ContractManagement_Tenant_hungnd` kết nối được và nhận đủ migration đến:
  - `20260831162848_Phase10AcceptancePaymentCompletion`.
- Idempotent migration script sinh thành công:
  - Central: 6,827 bytes.
  - Tenant/Application: 156,394 bytes.

### Release gate database trống

Test `Phase12SqlServerReleaseGateTests` thực hiện:

1. Sinh hai tên database duy nhất có prefix:
   - `ContractManagement_Phase12_QA_A_*`.
   - `ContractManagement_Phase12_QA_B_*`.
2. Chạy toàn bộ Application migrations vào cả hai database.
3. Xác nhận không còn pending migration.
4. Seed Owner, Manager, employee không liên quan, customer và contract ở Tenant A.
5. Xác nhận:
   - Owner đọc/sửa contract phụ trách.
   - Manager đọc toàn tenant.
   - Manager không tự có quyền sửa contract của Owner.
   - Employee không liên quan không tìm thấy contract.
   - Dùng guessed `ContractId` của Tenant A trong Tenant B trả `ResourceNotFound`.
6. Dùng hai DbContext đồng thời để xác nhận SQL Server `rowversion` đổi sau update và stale update ném `DbUpdateConcurrencyException`.
7. Xóa hai database QA trong `finally`.

Test có safety guard: từ chối chạy/drop nếu database name không bắt đầu bằng `ContractManagement_Phase12_QA_`.

### Development SQL encryption

SQL Server local có `ForceEncryption = 0`, nhưng `Microsoft.Data.SqlClient` mặc định yêu cầu encryption và máy local báo không hỗ trợ phiên encryption đó. Đã bổ sung `Encrypt=False` cho ba connection string trong:

- `Backend/ContractManagement.API/appsettings.Development.json`.

Chỉ Development bị thay đổi. Production vẫn phải dùng TLS/encryption theo hạ tầng và secret store.

## 5. Ma trận Phase 12

### 12.1 Tenant isolation — PASS

- Hai tenant database SQL Server thật được migrate độc lập.
- Cross-tenant guessed contract ID bị chặn.
- Private storage chặn storage key của tenant khác.
- Generic file access đi qua owning-resource policy.
- Unknown object policy bị từ chối.

Coverage chính:

- `Phase12SqlServerReleaseGateTests`.
- `Slice04ResourceAuthorizationTests`.
- `LocalPrivateFileStorageTests`.
- `ContractTemplatePreviewTests.CrossTenantVersion_IsNotDiscoverableFromAnotherTenantContext`.

### 12.2 RBAC — PASS

- Owner read/write contract phụ trách.
- Employee không liên quan không discover contract.
- Manager đọc toàn tenant nhưng không sửa contract người khác.
- Submitter không tự approve.
- Owner không Mark Completed.
- Manager Mark Completed.
- Sai tenant trả ResourceNotFound.

Coverage chính:

- `Slice03PermissionEndpointTests`.
- `Slice04ResourceAuthorizationTests`.
- `ContractApprovalServicePhase8DTests`.
- `ContractCompletionServicePhase10Tests`.
- `Phase12SqlServerReleaseGateTests`.

### 12.3 Render — PASS

- Placeholder catalog và dataset.
- Tenant/customer/items/terms/totals/currency snapshot.
- DOCX mở được.
- Preview và submitted artifact dùng cùng schema v4.
- Hash và template version được lưu.
- Submitted artifact không bị ghi đè.
- Render/storage/database failure có compensation.
- LibreOffice thật chuyển DOCX thành PDF hợp lệ.

Coverage chính:

- `ContractTemplatePreviewTests`.
- `ContractDocumentPreviewServiceTests`.
- `ContractServicePhase8CSubmissionTests`.
- `Phase12LibreOfficeReleaseGateTests`.

### 12.4 Approval — PASS

- Submit happy path.
- Không có Manager hợp lệ.
- Self-approval bị chặn.
- Approve và verify immutable artifact.
- Return/Reject thành công khi có lý do.
- Return/Reject/Withdraw thiếu lý do bị chặn.
- Hai Manager quyết định đồng thời: chỉ request đầu thắng.
- Stale rowVersion không resolve request.
- Artifact vật lý bị thiếu không approve được.

Coverage chính:

- `ContractServicePhase8CSubmissionTests`.
- `ContractApprovalServicePhase8DTests`.
- `ApprovalRequestPolicyTests`.

### 12.5 Signature — PASS

- PDF/JPG/JPEG/PNG hợp lệ.
- Sai extension/content type/magic bytes bị chặn.
- Declared file vượt 20 MiB bị chặn.
- Upload signed evidence chuyển `PendingSignature → Signed`.
- Supersede giữ evidence/file cũ và tạo evidence active mới.
- Supersede sau Completed bị chặn.
- Private storage chặn cross-tenant key.

Coverage chính:

- `ContractSigningServicePhase9Tests`.
- `SignaturePolicyTests`.
- `LocalPrivateFileStorageTests`.

### 12.6 Acceptance — PASS

- Upload acceptance lưu private file metadata, evidence và audit.
- Wrong/current version thay đổi bị chặn và file đã lưu dở được xóa.
- Duplicate acceptance bị policy/service chặn.
- Mutation sau Completed bị chặn.
- File dùng chung policy evidence đã kiểm tra extension/MIME/signature/size.

Coverage chính:

- `ContractCompletionServicePhase10Tests`.
- `LocalPrivateFileStorageTests`.

### 12.7 Payment — PASS

- Amount dương; zero/negative bị chặn.
- Duplicate reference không tạo row thứ hai.
- Overpayment bị chặn.
- Partial và full payment tính lại readiness.
- Void bắt buộc lý do và tính lại paid/remaining.
- Currency mismatch bị chặn.
- Audit add/void payment được ghi.
- Mutation sau Completed bị chặn.

Coverage chính:

- `PaymentPolicyTests`.
- `ContractCompletionServicePhase10Tests`.

### 12.8 Completion — PASS

- Blocker ổn định cho Not Signed, thiếu acceptance, thiếu payment.
- `Signed + Acceptance + Paid in full → Completed`.
- Owner không Complete; Manager Complete.
- Stale contract rowVersion không đổi trạng thái.
- Backend recompute readiness trong mutation khi dữ liệu đổi giữa GET và POST.
- Complete lần hai bị chặn.
- Mọi completion mutation bị chặn sau Completed.
- Audit `ContractCompleted` ghi đúng Manager và subject.

Coverage chính:

- `ContractCompletionPolicyTests`.
- `ContractCompletionServicePhase10Tests`.
- `Phase12SqlServerReleaseGateTests`.

## 6. Frontend và SystemAdmin

### Frontend

```text
npm run typecheck: PASS
npm run build: PASS
npm run lint: 0 errors, 314 warnings
```

Production build sinh đủ route contract chính, gồm create/detail/version, approval, audit, template, public contract và catalog.

314 lint warning chủ yếu thuộc các nhóm:

- `react-hooks/set-state-in-effect`.
- `react-hooks/refs`.
- `react-hooks/incompatible-library` với TanStack Table.
- `@typescript-eslint/no-explicit-any`.
- Unused variable/import.

Không warning nào làm build fail, nhưng nên có cleanup riêng và đặt warning budget giảm dần; không nên biến toàn bộ 314 warning thành scope sửa nóng của Phase 12.

### SystemAdmin

```text
npm run typecheck: PASS
npm run build: PASS
npm run lint: 0 errors, 5 warnings
```

Warning còn lại:

- Một dependency không cần thiết trong hook audit logs.
- Một unused error variable.
- TanStack Table incompatible-library warning.
- Hai unused toast action type.

## 7. Test mới được bổ sung

- `Backend/ContractManagement.Tests/Domains/Services/Contract/ContractCompletionServicePhase10Tests.cs`.
- `Backend/ContractManagement.Tests/Integration/Phase12SqlServerReleaseGateTests.cs`.
- `Backend/ContractManagement.Tests/Integration/Phase12LibreOfficeReleaseGateTests.cs`.
- Bổ sung case vào:
  - `ContractApprovalServicePhase8DTests.cs`.
  - `ContractCompletionPolicyTests.cs`.
  - `LocalPrivateFileStorageTests.cs`.

Integration test tự skip khi thiếu environment variable. Release pipeline phải cấp cả hai biến để bảo đảm kết quả cuối cùng là `Skipped: 0`.

## 8. Checklist thủ công trước production go-live

Các mục sau phụ thuộc deployment URL, certificate, cookie, SMS provider và tài khoản thật nên không thể được chứng minh chỉ bằng test process local:

- [ ] Employee login qua HTTPS; cookie có `Secure`, `HttpOnly`, SameSite đúng.
- [ ] System Admin login/logout/me trên production origin.
- [ ] CORS chỉ chấp nhận hai frontend origin được duyệt.
- [ ] Chạy browser E2E: create → negotiate → public comment → submit → approve → upload signed scan → acceptance → payment → Completed.
- [ ] Browser E2E Return/Reject/Withdraw và 409 stale rowVersion.
- [ ] Download DOCX/PDF submitted artifact qua authorization thật.
- [ ] Upload/download/delete attachment và evidence qua HTTP multipart thật.
- [ ] OTP provider thật gửi được; production không dùng Fake provider.
- [ ] Backup và restore thử Central DB + tenant DB + private storage cùng checkpoint.
- [ ] Xác nhận private storage production nằm ngoài source/publish/wwwroot.
- [ ] Kiểm tra log/audit/correlation ID không chứa password, OTP, token hoặc connection string.
- [ ] Chạy dependency vulnerability audit khi NuGet/npm registry có network.

Chỉ khi checklist này pass mới đổi `Production go-live gate` từ `CONDITIONAL` thành `PASS`.

## 9. Lệnh chạy lại release gate

Backend + SQL Server + LibreOffice:

```powershell
cd Source\Backend
$env:PHASE12_SQLSERVER_CONNECTION = "<QA SQL connection to master; never production DB>"
$env:PHASE12_LIBREOFFICE_PATH = "C:\Program Files\LibreOffice\program\soffice.exe"
dotnet test .\ContractManagement.Tests\ContractManagement.Tests.csproj -c Release
```

Frontend:

```powershell
cd Source\Frontend
npm run lint
npm run typecheck
npm run build
```

SystemAdmin:

```powershell
cd Source\SystemAdmin
npm run lint
npm run typecheck
npm run build
```

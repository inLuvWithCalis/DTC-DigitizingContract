# Custom contract placeholders — implementation and rollout

## Phần đã triển khai

- Quản lý placeholder scalar dùng chung theo tenant ngay trong catalog của trang template version: tạo, sửa, kích hoạt/ngừng dùng, xem version tham chiếu và sao chép token.
- Hai combobox tìm kiếm module/trường; format theo kiểu dữ liệu, giá trị khi trống, preview bằng dữ liệu mẫu, lỗi trùng key và conflict bằng tiếng Việt.
- Sáu source providers: Contract, Contract version, Customer, Tenant legal profile, Contract owner và Department. Các getter được khai báo rõ ràng, không reflection/SQL theo input người dùng. Tenant legal profile chưa có Website nên không công bố trường đó.
- Registry duy nhất qua DI; duplicate source key được phát hiện lúc khởi động.
- API dưới `/api/contract-templates`: `placeholder-catalog`, `placeholder-source-fields`, `placeholders`, `placeholders/{id}`, `placeholders/{id}/activate`, `placeholders/{id}/deactivate`, `placeholders/{id}/usage` và `DELETE placeholders/{id}`.
- Catalog hệ thống qua adapter tương thích; toàn bộ system và custom placeholders đều không bắt buộc, multiplicity `ZeroOrOne`. System keys bất biến; key custom không đổi tên sau khi tạo.
- Upload nhận definitions đã validate, kiểm tra revision trước khi persist và lưu mapping/default/format/revision trong `tbl_ContractTemplateField` cùng transaction DOCX.
- Binding hash riêng theo template version, được dùng cho preview/publish; đổi catalog không làm mapping và preview của version cũ thay đổi. Fingerprint V2/V3 của version trước migration được giữ khi chưa có binding hash.
- Snapshot custom values tại tạo hợp đồng, sửa draft, chuyển người phụ trách ở version chưa khóa và tạo vòng đàm phán. Trước khóa nguồn và submission, tự bổ sung snapshot thiếu. Version đã khóa thiếu snapshot trả lỗi, không đọc lại master data.
- `tbl_ContractVersionPlaceholderValue` lưu custom values; scalar values đã render (system + custom) còn được đưa vào JSON/hash pháp lý của artifact gửi duyệt qua thuộc tính `placeholderValues` bổ sung tương thích schema v4.
- Renderer thay scalar trên token gốc một lần, ghi OpenXML text an toàn, từ chối token còn sót; dynamic blocks hệ thống tiếp tục dùng renderer hiện có.
- Audit riêng cho catalog vì một definition không thuộc một template/version cụ thể. Audit chỉ ghi key, source, format, trạng thái và actor; không ghi default/label hoặc dữ liệu người liên hệ. Audit chỉ được thêm qua DbContext.
- Custom definition chưa từng được template version sử dụng có thể hard delete; audit tạo/sửa/xóa vẫn được giữ. Definition đã có binding lịch sử chỉ được ngừng sử dụng.
- Validation trả key đã được giới hạn về cú pháp cho placeholder không tồn tại hoặc sai kiểu chữ, để giao diện chỉ rõ token cần sửa mà không đưa nội dung DOCX tự do vào lỗi.
- Meter `ContractManagement.Placeholders`: mutations, resolved/defaulted/failed values, resolve duration và legacy fallback; không chứa giá trị dữ liệu cá nhân.

## Migration

Migration: `20260909183116_CustomContractPlaceholders` trong application/tenant DbContext. Không có migration Central DB cho module này.

Tạo ba bảng: `tbl_ContractPlaceholderDefinition`, `tbl_ContractPlaceholderAudit`, `tbl_ContractVersionPlaceholderValue`; bổ sung metadata mapping và binding hash vào template field/version.

Migration chứa mapping tương thích cho 37 system definitions lịch sử. Catalog mặc định hiện có 36 definitions: `CUSTOMER_NAME` dùng `CustomerFullName`; `CUSTOMER_REPRESENTATIVE_TITLE` chỉ còn trong mapping tương thích để template cũ tiếp tục render và có thể được tạo lại dưới dạng custom placeholder. Legacy renderer xác định hành vi bằng `PlaceholderKey`; `DataSource` trước đây chỉ là metadata và có thể khác giữa các revision. Migration chấp nhận đúng 37 key lịch sử rồi chuẩn hóa `DataSource`, `SourceFieldKey`, kind và multiplicity theo mapping cố định. Nếu gặp key lạ, migration dừng, rollback và ghi rõ key/source gây lỗi để kiểm tra trước khi chạy lại.

Requiredness và multiplicity đã snapshot trong từng template version. Version cũ giữ nguyên quy tắc đã lưu; upload lại DOCX cho draft hoặc tạo version mới để nhận catalog `ZeroOrOne` hiện tại.

SQL idempotent riêng từ migration tiền nhiệm đến module này: `Documentation/migrations/CUSTOM-CONTRACT-PLACEHOLDERS.sql`. Script dành cho tenant đã ở `20260909115747_RemoveSigningEvidenceLegacyMetadata`; các tenant cũ hơn cần chạy đầy đủ migration theo runbook chung.

**Chưa áp migration lên database tenant trong lượt triển khai code này.**

Trong `Source/Backend`, đặt `ConnectionStrings__TenantDatabaseTemplate` tới đúng tenant DB bằng cơ chế secret/config hiện có, rồi dùng:

```powershell
dotnet ef database update --context DbDtctechContext --project .\ContractManagement.Infrastructure\ContractManagement.Infrastructure.csproj --startup-project .\ContractManagement.API\ContractManagement.API.csproj
```

Không dùng giá trị mẫu đang trỏ `master`. Quy trình backup, maintenance window và rollout từng tenant theo `Documentation/production/PHASE11-PRODUCTION-RUNBOOK.md`.

## Feature flag và thứ tự bật

Config checked-in mặc định:

```json
{
  "CustomContractPlaceholders": {
    "Enabled": false,
    "TenantCodes": []
  }
}
```

- `Enabled = true` + `TenantCodes = []`: bật cho mọi tenant trên instance.
- `Enabled = true` + danh sách tenant codes: chỉ bật quản lý/upload custom cho các tenant trong danh sách.
- File `appsettings.Development.json` trên workspace đã bật tính năng cho phát triển. File này bị Git ignore; không chứa trong thay đổi deploy. Máy phát triển khác cần đặt flag bằng config riêng hoặc `CustomContractPlaceholders__Enabled=true`.
- Flag tắt chặn tạo/sửa và upload binding custom mới; tài liệu đã snapshot vẫn render được. Không xóa schema hoặc snapshot khi tắt flag.

Thứ tự: backup → migration từng tenant → restart/deploy backend → bật tenant nội bộ → cập nhật frontend → smoke test → theo dõi → mở rộng tenant allowlist.

Backend đang chạy lúc triển khai giữ DLL cũ nên chưa được restart. Sau khi migration, cần restart backend để API mới hoạt động. Build/test dùng cấu hình `PlaceholderChecks` để tránh ghi đè DLL đang sử dụng.

## Kiểm chứng

Lệnh build/test đã dùng:

```powershell
dotnet test Backend/ContractManagement.Tests/ContractManagement.Tests.csproj --no-restore -c PlaceholderChecks
```

Frontend trong `Source/Frontend`:

```powershell
npx tsc --noEmit
npx eslint components/contract-templates/contract-placeholder-form.tsx components/contract-templates/contract-template-placeholder-catalog.tsx components/contract-templates/contract-template-utils.ts services/contract-template-api.ts
```

EF `has-pending-model-changes` xác nhận model và migration khớp nhau. Các test mới phủ normalization/reserved keys/duplicate registry, quyền và tenant, conflict, audit, format/fallback, split token, inactive/multiplicity, upload/reupload/rebinding, giữ giá trị qua thay đổi master và khóa version, DOCX/submission có custom values.

Các gate SQL Server/LibreOffice sẵn có yêu cầu `PHASE12_SQLSERVER_CONNECTION` và `PHASE12_LIBREOFFICE_PATH`; chúng bị skip nếu môi trường chưa cấp các giá trị đó. Test PDF thông thường dùng renderer giả. Chưa kiểm chứng migration trên SQL Server thật hoặc rendering PDF bằng LibreOffice thật trong lượt này.

Smoke test sau migration/restart:

1. Tạo `CUSTOM_CONTACT` → Khách hàng → Người liên hệ.
2. Copy token vào DOCX có đầy đủ system placeholders, upload bản nháp, preview DOCX/PDF và publish.
3. Tạo hợp đồng với template mới, kiểm tra giá trị thật trong preview.
4. Sửa/ngừng dùng definition: published version vẫn dùng mapping cũ, upload mới báo key đã ngừng dùng.
5. Sửa master contact sau capture: snapshot version cũ giữ giá trị. Khi cập nhật draft hoặc tạo version mới, snapshot có thể được capture lại theo workflow.
6. Gửi duyệt/ký; kiểm tra JSON snapshot, DOCX/PDF lưu trữ và tải lại tài liệu.

## Giới hạn MVP

Chưa có custom collection/table, formula, arbitrary lookup hoặc cấu hình bắt buộc theo template. Muốn đổi mapping ở draft hiện có cần upload DOCX lại. Endpoint usage không đổi mapping; version Published/Retired không được sửa. Nguồn mới được bổ sung bằng provider; không đổi nghĩa hoặc xóa source key đã được version lịch sử tham chiếu.

Phần rollout tenant production và quan sát sau rollout chưa thực hiện; đây là các bước vận hành sau bàn giao code.

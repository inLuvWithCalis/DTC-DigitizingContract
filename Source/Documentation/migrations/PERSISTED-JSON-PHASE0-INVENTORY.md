# Phase 0 — Persisted JSON inventory

Ngày kiểm kê: 2026-09-13  
Phạm vi: application/tenant databases, runtime entities và các đường ghi dữ liệu trong backend.  
Nguyên tắc: không tính JSON chỉ dùng làm HTTP transport hoặc dữ liệu tạm trong memory; không thay đổi database trong bước kiểm kê.

## 1. Catalog SQL Server thực tế

Đã chạy catalog query chỉ đọc trên toàn bộ user database đang online của SQL Server development cục bộ. Script tái kiểm tra cho từng database nằm tại `Documentation/migrations/PERSISTED-JSON-PHASE0-INVENTORY.sql`.

Script lưu trong repository cũng đã được chạy kiểm chứng riêng trên `ContractManagement_Tenant_hung`: trả về đủ ba result set với 12 candidate (tám payload thuộc kế hoạch và bốn cột rich-text ngoại lệ), bốn constraint `ISJSON` và 29 cột text lớn để rà soát.

| Database | Persisted JSON candidate | Constraint dùng `ISJSON` | Nhận xét |
|---|---:|---:|---|
| `ContractManagement_Central` | 0 | 0 | Không có persisted JSON trong phạm vi |
| `ContractManagement_Tenant_hung` | 8 | 4 | Schema trước migration Phase 1–3 |
| `ContractManagement_Tenant_hungnd` | 8 | 4 | Schema trước migration Phase 1–3 |
| `ContractManagement_Tenant_seedtest` | 6 | 4 | Schema cũ hơn, chưa có placeholder audit JSON |
| `db_dtctech` | 0 | 0 | Không có candidate theo danh sách phạm vi |

Hai database tenant chính hiện có tám candidate:

1. `tbl_ContractAudit.PreviousValuesJson`
2. `tbl_ContractAudit.NewValuesJson`
3. `tbl_ContractTemplateAudit.PreviousValuesJson`
4. `tbl_ContractTemplateAudit.NewValuesJson`
5. `tbl_ContractPlaceholderAudit.PreviousValuesJson`
6. `tbl_ContractPlaceholderAudit.NewValuesJson`
7. `tbl_ContractVersion.SnapshotJson`
8. `tbl_ContractCustomerOtpDeliveryOutbox.EncryptedPayload`

Bốn constraint `ISJSON` thuộc hai cặp cột audit contract/template. Database vẫn còn các cột này vì migration `20260913045049_NormalizeAuditStoragePhase123` mới chỉ được tạo trong source, chưa được apply. Đây là trạng thái dự kiến; Phase 0 không tự ý migrate hoặc xóa database.

## 2. Đối chiếu runtime model mới

Sau Phase 1–3, runtime model và migration mới đã loại sáu cột audit JSON. Hai persisted payload ngoài text editor còn lại được phân loại đầy đủ:

| Bảng/cột | Writer/reader | Quyết định |
|---|---|---|
| `tbl_ContractVersion.SnapshotJson` | `SoftwareSupplyContractSnapshotFactory.Serialize` → `ContractService` | Xử lý ở Phase 5 bằng snapshot quan hệ; không converter/backfill |
| `tbl_ContractCustomerOtpDeliveryOutbox.EncryptedPayload` | `CustomerAccessCryptography` → `CustomerContractAccessService`; worker decrypt khi gửi | Xử lý ở Phase 4 bằng các ciphertext scalar; xóa JSON reader |

Không phát hiện cột persisted JSON ngoài inventory trên trong runtime application model.

## 3. Static serializer inventory

Toàn bộ `JsonSerializer.Serialize*`, `Deserialize` và `JsonDocument.Parse` trong runtime backend đã được phân loại:

| Khu vực | Mục đích | Persist database? | Kết luận |
|---|---|---:|---|
| `CustomerAccessCryptography` | Serialize/de-serialize OTP delivery message trước/sau AES-GCM | Có, qua `EncryptedPayload` | Phase 4 |
| `SoftwareSupplyContractSnapshotFactory` | Serialize legal contract snapshot | Có, qua `SnapshotJson` | Phase 5 |
| `ContractTermRichText` | Parse rich-text V4 | Có trong `TermContent`/`TermContentEn` | Ngoại lệ text editor |
| `SoftwareSupplyPreviewDatasetV1` | Tạo rich-text V4 mẫu | Có thể đi vào term content | Ngoại lệ text editor |
| `ContractAuditQueryService` | Serialize dictionary typed thành một field CSV | Không | HTTP/export transport, giữ nguyên |
| `ExceptionHandlingMiddleware` | Serialize HTTP error response | Không | HTTP transport, giữ nguyên |
| `ContractPlaceholderCatalog.Fingerprint` | Canonical input tạm để tính SHA-256 | Không lưu raw JSON; chỉ lưu hash | Không thuộc persisted JSON |

Không phát hiện Newtonsoft JSON, `JsonConvert`, `JObject`, `JArray` hoặc JSON writer khác trong runtime backend.

## 4. Large text catalog

Catalog cũng liệt kê các cột `varchar(max)`/`nvarchar(max)`/`varbinary(max)`. Static tracing cho thấy các nhóm sau là scalar/free text hoặc binary, không có serializer JSON ghi vào:

- item descriptions;
- legal-basis content;
- placeholder raw/rendered scalar values;
- customer interaction/notification content;
- product/service descriptive content.

Các cột này không thuộc kế hoạch chuyển JSON. Tên cột dài hoặc kiểu `nvarchar(max)` tự nó không chứng minh dữ liệu là JSON.

## 5. Contract schema đã chốt

- Contract audit dùng `FieldCode` từ `1..93` và đúng một trong năm kind: Integer, Decimal, String, DateTime hoặc Boolean. Không tạo `GuidValue` vì hiện không có field thực tế cần dùng.
- Contract template audit có đúng 11 `FieldCode`, dùng `IntegerValue`, `LongValue` hoặc `StringValue` theo constraint của từng field.
- Placeholder audit có ba scalar before/after: `SourceFieldKey`, `FormatString`, `IsActive`.
- `TermContent` và `TermContentEn` trên contract/template term là ngoại lệ text-editor duy nhất trong đợt này.
- Không có converter, fallback, legacy reader, dual-read, dual-write hoặc backfill cho sáu cột audit đã loại bỏ.

## 6. Kết luận Phase 0

Inventory đã đóng: không còn vị trí persist JSON chưa được phân loại. Thứ tự công việc tiếp theo giữ nguyên:

1. Phase 4 loại `EncryptedPayload` JSON.
2. Phase 5 loại `SnapshotJson`.
3. Phase 6 drop/recreate database test, apply toàn bộ migration và chạy release gate.

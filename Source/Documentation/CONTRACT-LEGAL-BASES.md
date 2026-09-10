# Căn cứ hợp đồng tùy chỉnh

## Mục đích

Module cho phép Admin Officer quản lý các căn cứ pháp lý theo từng template version và chèn toàn bộ danh sách vào DOCX bằng placeholder tùy chọn `{{CONTRACT_LEGAL_BASES}}`. Không cần thêm trường nguồn vào một module nghiệp vụ khác và không cần sửa backend khi người dùng thêm một căn cứ mới.

## Mô hình dữ liệu

- `tbl_ContractTemplateLegalBasis`: dữ liệu biên soạn của template version, gồm mã ổn định, nội dung Việt/Anh và thứ tự.
- `tbl_ContractLegalBasis`: snapshot của từng contract version. Dữ liệu được sao chép khi tạo hợp đồng và khi tạo vòng đàm phán mới.
- Hai bảng dùng `RowVersion` để chống ghi đè đồng thời. Chỉ template version ở trạng thái Draft được thêm, sửa, xóa hoặc sắp xếp căn cứ.
- Không có căn cứ là trạng thái hợp lệ. Placeholder `CONTRACT_LEGAL_BASES` cũng có multiplicity `ZeroOrOne`.

Việc tách căn cứ khỏi `tbl_ContractTemplateTerm` giữ đúng vị trí pháp lý của phần mở đầu hợp đồng và tránh đưa căn cứ vào luồng comment/đàm phán điều khoản.

## API quản trị

Các route yêu cầu quyền `TemplateManage`:

- `POST /api/contract-templates/versions/{versionId}/legal-bases`
- `PUT /api/contract-templates/versions/{versionId}/legal-bases/{legalBasisId}`
- `DELETE /api/contract-templates/versions/{versionId}/legal-bases/{legalBasisId}`
- `PUT /api/contract-templates/versions/{versionId}/legal-bases/order`

Danh sách căn cứ được trả trong `GET /api/contract-templates/versions/{versionId}` qua thuộc tính `legalBases`.

## Cách sử dụng

1. Mở một template version Draft và chọn tab **Căn cứ**.
2. Thêm nội dung tiếng Việt; thêm tiếng Anh nếu template song ngữ.
3. Dùng nút lên/xuống để thay đổi thứ tự.
4. Chèn `{{CONTRACT_LEGAL_BASES}}` thành một paragraph riêng trong main document của DOCX.
5. Upload, kiểm tra preview và phát hành template như bình thường.

Khi render hợp đồng, hệ thống chỉ đọc `tbl_ContractLegalBasis` của contract version hiện hành. Sửa template sau đó không làm thay đổi hợp đồng đã tạo.

## Triển khai database

Migration EF: `20260910045945_AddContractLegalBases`.

Script idempotent: `Documentation/migrations/CONTRACT-LEGAL-BASES.sql`.

Chạy migration theo quy trình triển khai hiện có trước khi khởi động phiên bản API mới.

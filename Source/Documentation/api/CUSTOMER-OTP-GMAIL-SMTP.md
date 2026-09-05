# Gửi OTP khách hàng bằng Gmail SMTP

## Luồng đang triển khai

1. Khách mở link public còn hiệu lực, nhập số điện thoại đã được nhân viên chọn cho hợp đồng.
2. Backend kiểm tra link, số điện thoại và giới hạn yêu cầu OTP như trước.
3. Khi `CustomerOtp:Provider=Smtp`, lấy `tbl_Customer.CustomerEmail` của khách hàng thuộc hợp đồng trong tenant hiện tại. Không nhận email từ request public.
4. Email, OTP và hạn sử dụng được mã hóa cùng nhau trong outbox. Worker gửi email qua Gmail SMTP, ghi trạng thái/audit và retry khi lỗi.
5. Khách nhập OTP, backend cấp cookie HttpOnly như trước. OTP có hiệu lực 5 phút tính từ lúc yêu cầu; gửi lại tối thiểu sau 60 giây, tối đa 3 yêu cầu trong 15 phút theo link.

Đây là xác minh quyền truy cập **email trong hồ sơ khách hàng**. Số điện thoại vẫn là điều kiện đối chiếu trước khi gửi, nhưng nhận mã qua email không chứng minh khách đang giữ số điện thoại đó. Với số chọn `Manual`, email nhận mã vẫn là `CustomerEmail` của khách hàng trong hợp đồng.

Không cần migration: tái sử dụng email hiện có và cột `EncryptedPayload`. Payload cũ vẫn đọc được cho Fake/Http; khi đổi sang SMTP hãy yêu cầu OTP mới vì payload cũ không chứa email.

## 1. Chuẩn bị Gmail gửi thư

- Dùng tài khoản Gmail riêng để gửi thư của ứng dụng.
- Bật **2-Step Verification** trong Google Account.
- Mở [App Passwords](https://myaccount.google.com/apppasswords), tạo mật khẩu ứng dụng tên `ContractManagement Local`.
- Sao chép App Password 16 ký tự, bỏ khoảng trắng phân nhóm khi nhập cấu hình. Không dùng mật khẩu đăng nhập Gmail thông thường.
- Không cần Google Cloud project hoặc API key cho cách gửi SMTP này.

Nếu không có mục App Passwords: kiểm tra tài khoản tổ chức có chặn tính năng này, Advanced Protection hoặc thiết lập 2 bước chỉ dùng security key. Đổi mật khẩu Google sẽ thu hồi App Password cũ.

Tham khảo: [Google App Passwords](https://support.google.com/accounts/answer/185833), [Gmail SMTP](https://support.google.com/a/answer/176600).

## 2. Cấu hình local bằng .NET User Secrets

Chạy PowerShell 7 tại `Source/Backend/ContractManagement.API`. Project đã có `UserSecretsId`; không cần chạy `user-secrets init`.

```powershell
dotnet user-secrets set "CustomerOtp:Provider" "Smtp"
dotnet user-secrets set "CustomerOtp:Smtp:Username" "your-sender@gmail.com"
dotnet user-secrets set "CustomerOtp:Smtp:FromAddress" "your-sender@gmail.com"
dotnet user-secrets set "CustomerOtp:Smtp:FromName" "Contract Management"
```

Nhập App Password không đưa giá trị vào lịch sử lệnh PowerShell:

```powershell
$otpSmtpCredential = Get-Credential -UserName "your-sender@gmail.com" -Message "Nhập Gmail App Password"
dotnet user-secrets set "CustomerOtp:Smtp:AppPassword" ($otpSmtpCredential.GetNetworkCredential().Password)
Remove-Variable otpSmtpCredential
```

Secret được lưu ngoài repo trong hồ sơ Windows của bạn. User Secrets không phải kho bí mật được mã hóa; dùng cho Development và không gửi file secrets cho người khác. Đừng commit App Password vào appsettings, .env hoặc frontend.

Mặc định có sẵn `smtp.gmail.com`, port **587**, bắt buộc STARTTLS, timeout 30 giây. Provider này dùng STARTTLS, không dùng port 465. `FromAddress` nên giống `Username`; nếu dùng alias phải cấu hình quyền gửi alias bên Gmail.

`appsettings.json` giữ `Provider=Fake` để checkout mới vẫn khởi động được khi chưa có credential. Các lệnh trên override thành Smtp ở Development. Khởi động lại backend sau khi cấu hình. Cấu hình thiếu sẽ bị báo lỗi khi startup, không âm thầm quay về log OTP.

### Giữ key ổn định qua các lần restart

Nếu đã cấu hình `CustomerOtp:HashKey` và `CustomerOtp:EncryptionKey`, **giữ nguyên** các giá trị đó. Không tạo lại mỗi lần chạy. Nếu hiện chưa có key cố định, chạy một lần:

```powershell
dotnet user-secrets set "CustomerOtp:HashKey" ([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)))
dotnet user-secrets set "CustomerOtp:EncryptionKey" ([Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)))
```

Đổi HashKey làm token/link cũ không tra cứu được; đổi EncryptionKey khiến payload outbox cũ không giải mã được. Sau lần chuyển từ key ngẫu nhiên sang key cố định, thay/tạo link mới ở màn Truy cập khách hàng và yêu cầu OTP mới. Không chạy các lệnh tạo key trên hệ thống đã có key đang sử dụng.

## 3. Test gửi thư

1. Vào hồ sơ khách hàng, điền email bạn có thể mở vào trường Email và lưu.
2. Dùng hợp đồng đang đàm phán có link còn hiệu lực; kiểm tra số xác minh đã chọn.
3. Mở link public, nhập đúng số và yêu cầu mã.
4. Chờ worker xử lý (chu kỳ khoảng 10 giây; có thể lâu hơn nếu có backlog), kiểm tra Inbox/Spam của **email khách hàng**, không phải email sender.
5. Nhập mã 6 số còn hạn để truy cập hợp đồng.
6. Thử sai số, OTP sai/hết hạn và link đã thu hồi: không được truy cập hợp đồng.

Response public cố ý không tiết lộ email, không phân biệt email thiếu/sai với thông tin không hợp lệ. `deliveryChannel=Email` chỉ cho biết kênh cấu hình; request được nhận không đồng nghĩa email đã gửi thành công.

## 4. Sửa nội dung email

Sửa `Backend/ContractManagement.API/Domains/CustomerAccess/CustomerOtpEmailTemplate.cs`:

- `Subject`: tiêu đề email (`Mã xác thực truy cập hợp đồng điện tử`).
- `BuildBody`: nội dung HTML email chuẩn responsive, thẻ hiển thị OTP nổi bật, thời gian hết hạn (Việt Nam GMT+7 và UTC) và cảnh báo bảo mật. Đã bật `IsBodyHtml = true` trong `SmtpCustomerOtpDeliveryProvider.cs`.
- `BuildPlainTextBody`: nội dung văn bản thuần dự phòng (plain-text alternate view).

## 5. Kiểm tra khi không nhận được email

- Startup lỗi options: kiểm tra đủ Username, FromAddress, AppPassword, port 587; Development mới tự đọc User Secrets.
- `SmtpException` với trạng thái xác thực: kiểm tra App Password, 2-Step Verification, tài khoản gửi; không dùng password Gmail thường.
- `TimeoutException` / lỗi kết nối: kiểm tra kết nối ra `smtp.gmail.com:587`, firewall/VPN.
- Không tạo outbox: kiểm tra link đang đàm phán, đúng số xác minh, cooldown và `CustomerEmail` hợp lệ. Email thiếu/sai được ghi audit `CustomerOtpFailed`.
- Outbox `Pending`: đợi worker; kiểm tra Central DB, tenant Active và DB tenant truy cập được.
- Outbox `Failed`: xem `LastFailure`, `AttemptCount` và log theo tenant/outbox ID. Tối đa 3 lần gửi với khoảng retry mặc định 30 giây; OTP hết hạn sẽ ngừng gửi.
- `CryptographicException`: kiểm tra key có bị thay đổi/restart khi chưa cố định key; yêu cầu OTP mới với link hiện hành.
- Outbox `Sent` nhưng Inbox chưa thấy: SMTP đã nhận thư, kiểm tra Spam, địa chỉ khách hàng và giới hạn/quy tắc Gmail.

Log SMTP không chứa OTP, App Password, payload hoặc email người nhận; không bật log nội dung SMTP khi test với dữ liệu thật. OTP chỉ còn được ghi console nếu chủ động dùng `Provider=Fake` ở Development.

## Production

Truyền cùng cấu hình bằng secret manager/environment variables (`CustomerOtp__Provider=Smtp`, `CustomerOtp__Smtp__Username`, `CustomerOtp__Smtp__AppPassword`, `CustomerOtp__Smtp__FromAddress`), cùng hai key Base64 32 byte cố định. Không dùng User Secrets cho Production. Startup chặn Fake ở ngoài Development. Gmail có giới hạn gửi thư; theo dõi lỗi/quota khi tăng tải.

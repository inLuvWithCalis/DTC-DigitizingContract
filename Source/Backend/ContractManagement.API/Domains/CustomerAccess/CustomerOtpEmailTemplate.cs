using System.Globalization;
using System.Net;

namespace ContractManagement.API.Domains.CustomerAccess;

/// <summary>
/// Email template for customer OTP access verification.
/// Provides both rich HTML email layout and plain text fallback.
/// </summary>
public static class CustomerOtpEmailTemplate
{
    public const string Subject = "Mã xác thực truy cập hợp đồng điện tử";
    public const string LogoContentId = "dtc-customer-otp-logo";

    /// <summary>
    /// Builds a modern, responsive HTML email body.
    /// </summary>
    public static string BuildBody(string otp, DateTime expiresAt)
    {
        var safeOtp = WebUtility.HtmlEncode(otp);
        var vnTime = expiresAt.AddHours(7).ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture);
        var utcTime = expiresAt.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture);
        var year = DateTime.UtcNow.Year;

        return $$"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>{{Subject}}</title>
              <style>
                body, table, td, a { -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }
                table, td { mso-table-lspace: 0pt; mso-table-rspace: 0pt; }
                img { -ms-interpolation-mode: bicubic; border: 0; outline: none; text-decoration: none; }
              </style>
            </head>
            <body style="margin: 0; padding: 0; background-color: #f1f5f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;">
              <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="background-color: #f1f5f9; padding: 32px 12px;">
                <tr>
                  <td align="center">
                    <table role="presentation" border="0" cellpadding="0" cellspacing="0" width="100%" style="max-width: 580px; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -2px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0;">
                      
                      <!-- Header Banner -->
                      <tr>
                        <td style="background: linear-gradient(135deg, #0f172a 0%, #1e3a8a 100%); padding: 36px 28px; text-align: center;">
                          <img src="cid:{{LogoContentId}}" width="72" height="72" alt="DTC Digitizing Contract" style="display: inline-block; width: 72px; height: 72px; object-fit: contain; margin-bottom: 14px; border: 0; outline: none; text-decoration: none;">
                          <h1 style="margin: 0; font-size: 20px; font-weight: 700; color: #ffffff; letter-spacing: 0.5px; text-transform: uppercase;">
                            XÁC THỰC TRUY CẬP HỢP ĐỒNG
                          </h1>
                          <p style="margin: 8px 0 0 0; font-size: 13px; color: #93c5fd; font-weight: 400;">
                            Hệ thống Quản lý &amp; Số hóa Hợp đồng Điện tử
                          </p>
                        </td>
                      </tr>

                      <!-- Main Content -->
                      <tr>
                        <td style="padding: 36px 32px 28px 32px;">
                          <p style="margin: 0 0 16px 0; font-size: 15px; line-height: 1.6; color: #334155;">
                            Xin chào <strong>Quý khách</strong>,
                          </p>
                          <p style="margin: 0 0 24px 0; font-size: 14px; line-height: 1.6; color: #475569;">
                            Bạn vừa thực hiện yêu cầu lấy mã xác thực (OTP) để đăng nhập và xem chi tiết hợp đồng điện tử. Vui lòng sử dụng mã bên dưới để hoàn tất xác thực:
                          </p>

                          <!-- OTP Display Box -->
                          <div style="background-color: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 14px; padding: 24px 16px; text-align: center; margin: 24px 0;">
                            <div style="font-size: 12px; font-weight: 700; color: #64748b; letter-spacing: 1.5px; text-transform: uppercase; margin-bottom: 8px;">
                              MÃ XÁC THỰC CỦA BẠN
                            </div>
                            <div style="font-size: 38px; font-weight: 800; color: #1e3a8a; letter-spacing: 8px; font-family: 'Courier New', Consolas, Monaco, monospace; margin: 10px 0; padding-left: 8px;">
                              {{safeOtp}}
                            </div>
                            <div style="font-size: 13px; color: #64748b; margin-top: 10px; line-height: 1.5;">
                              ⏱️ Mã có hiệu lực trong vòng <strong>5 phút</strong><br>
                              <span style="font-size: 11px; color: #94a3b8;">(Hết hạn lúc: {{vnTime}} GMT+7 / {{utcTime}} UTC)</span>
                            </div>
                          </div>

                          <!-- Security Warning Box -->
                          <div style="background-color: #fffbeb; border-left: 4px solid #f59e0b; border-radius: 8px; padding: 14px 16px; margin: 24px 0;">
                            <div style="font-size: 13px; font-weight: 700; color: #92400e; margin-bottom: 6px;">
                              🛡️ Lưu ý an toàn bảo mật:
                            </div>
                            <ul style="margin: 0; padding-left: 18px; font-size: 13px; color: #78350f; line-height: 1.6;">
                              <li style="margin-bottom: 4px;">Tuyệt đối <strong>không cung cấp mã OTP này</strong> cho bất kỳ ai, kể cả nhân viên hỗ trợ hệ thống.</li>
                              <li>Nếu bạn <strong>không yêu cầu mã này</strong>, vui lòng bỏ qua email hoặc liên hệ với đại diện phụ trách hợp đồng để đảm bảo an toàn.</li>
                            </ul>
                          </div>

                          <p style="margin: 28px 0 0 0; font-size: 14px; line-height: 1.6; color: #475569;">
                            Trân trọng cảm ơn,<br>
                            <strong style="color: #1e293b;">Ban Quản lý Hợp đồng Điện tử</strong>
                          </p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="background-color: #f8fafc; padding: 22px 28px; text-align: center; border-top: 1px solid #e2e8f0;">
                          <p style="margin: 0; font-size: 12px; color: #64748b; line-height: 1.6;">
                            Email này được gửi tự động từ hệ thống <strong>Digitizing Contract</strong>.<br>
                            Vui lòng không trả lời trực tiếp email này.
                          </p>
                          <p style="margin: 10px 0 0 0; font-size: 11px; color: #94a3b8;">
                            &copy; {{year}} DTC Digitizing Contract. All rights reserved.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Builds a fallback plain-text email body.
    /// </summary>
    public static string BuildPlainTextBody(string otp, DateTime expiresAt)
    {
        var vnTime = expiresAt.AddHours(7).ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture);
        var utcTime = expiresAt.ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture);

        return $"""
            XÁC THỰC TRUY CẬP HỢP ĐỒNG ĐIỆN TỬ
            Hệ thống Quản lý & Số hóa Hợp đồng

            Xin chào Quý khách,

            Mã xác thực (OTP) của bạn là: {otp}

            Mã có hiệu lực trong vòng 5 phút (hết hạn lúc {vnTime} GMT+7 / {utcTime} UTC).

            LƯU Ý BẢO MẬT:
            - Tuyệt đối không chia sẻ mã này với bất kỳ ai, kể cả nhân viên hỗ trợ hệ thống.
            - Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.

            Trân trọng,
            Ban Quản lý Hợp đồng Điện tử
            """;
    }
}

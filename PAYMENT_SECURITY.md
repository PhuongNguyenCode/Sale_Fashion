# Cấu hình thanh toán an toàn

## Trên máy phát triển

Các khóa đang dùng đã được chuyển khỏi `appsettings.json` vào ASP.NET User Secrets của máy này. Chúng chỉ được nạp khi chạy với môi trường `Development`.

Nếu thiết lập máy phát triển mới, dùng `dotnet user-secrets set --project FashionStore.csproj "PaymentGateways:Casso:ApiKey" "<khóa mới>"` và lặp lại cho `ClientId`, `ChecksumKey`, `SecureToken`, `PaymentGateways:MoMo:AccessKey`, `PaymentGateways:MoMo:SecretKey`, `PaymentGateways:VNPay:HashSecret`. Không đưa giá trị thật vào tệp dự án, log, hoặc tin nhắn.

## Trên máy chủ Production

Thiết lập `ConnectionStrings__DefaultConnection` và các khóa của cổng đang bật bằng biến môi trường hoặc kho bí mật của nhà cung cấp hosting. Dấu `__` tương ứng dấu `:` trong cấu hình ASP.NET Core. Ứng dụng sẽ từ chối khởi động nếu thiếu chuỗi kết nối hoặc khóa bắt buộc của cổng đang bật.

Các khóa cũ đã từng nằm trong cấu hình dự án và gói triển khai. Hãy tạo khóa mới tại payOS/Casso, MoMo, VNPay và đổi mật khẩu cơ sở dữ liệu Production trước khi phát hành bản mới. Sau đó cập nhật máy chủ bằng khóa mới và loại bỏ mọi gói triển khai cũ.

## Kiểm tra sau khi phát hành

1. `POST /payment/simulate/{orderCode}` không còn là endpoint thanh toán; phản hồi phải là `404` hoặc `405` và đơn hàng không đổi trạng thái.
2. Đơn chuyển khoản mới hiển thị trạng thái chờ, đúng số tiền, mã đơn và mã QR.
3. Webhook giả, sai chữ ký, sai số tiền hoặc gửi cho đơn đã hủy phải bị từ chối.
4. Webhook hợp lệ xác nhận đúng một lần; gửi lại cùng mã giao dịch không được trừ tồn kho lần hai.

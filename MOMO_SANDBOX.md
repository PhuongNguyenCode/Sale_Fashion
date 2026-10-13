# Thử thanh toán MoMo trên sandbox

MoMo cấp bộ **Partner Code, Access Key, Secret Key** riêng cho môi trường Test trong tài khoản [MoMo for Business](https://business.momo.vn/). Đăng ký hoặc đăng nhập tại đó và lấy **Testing Credentials**. Không dùng khóa production và không đưa khóa vào `appsettings.json`, Git, ảnh chụp hay khung chat.

## Cấu hình trên máy này

Trong thư mục dự án, nhập ba giá trị được cấp vào ASP.NET User Secrets:

```powershell
dotnet user-secrets set --project FashionStore.csproj "PaymentGateways:MoMo:PartnerCode" "<Partner Code test>"
dotnet user-secrets set --project FashionStore.csproj "PaymentGateways:MoMo:AccessKey" "<Access Key test>"
dotnet user-secrets set --project FashionStore.csproj "PaymentGateways:MoMo:SecretKey" "<Secret Key test>"
```

Chạy ứng dụng ở `http://localhost:5244`, sau đó chạy `ngrok http 5244`. Lấy HTTPS URL hiện tại của ngrok, ví dụ `https://your-name.ngrok-free.dev`, và cấu hình:

```powershell
dotnet user-secrets set --project FashionStore.csproj "Payment:PublicBaseUrl" "https://your-name.ngrok-free.dev"
```

Khởi động lại ứng dụng sau khi đổi User Secrets. URL IPN gửi cho MoMo sẽ là `https://your-name.ngrok-free.dev/api/payment/webhook/momo`; URL trở về là `/payment/momo-return`. Đảm bảo ngrok đang chạy trong suốt giao dịch. Mở cửa hàng bằng URL ngrok để kiểm tra toàn bộ luồng trên cùng một địa chỉ công khai.

## Kiểm tra

1. Dùng một sản phẩm đang bán với số tiền từ 1.000 VND, tạo đơn MoMo mới trên sandbox. API tạo thanh toán phải trả `resultCode = 0` cùng `payUrl`, rồi trình duyệt chuyển tới MoMo Test.
2. Thanh toán bằng [ứng dụng/ví MoMo Test](https://developers.momo.vn/v3/vi/docs/payment/onboarding/test-instructions/). Không dùng ví MoMo thật.
3. Trong ngrok inspector tại `http://127.0.0.1:4040`, kiểm tra `POST /api/payment/webhook/momo` trả **204**. Trang kết quả và SQL phải hiện `Paid` / `Confirmed`; tồn kho giảm đúng một lần.
4. Thử thêm giao dịch hủy, IPN gửi lại, sai chữ ký và sai số tiền. Chỉ IPN hợp lệ của đúng đơn mới được cập nhật thanh toán.

Nếu MoMo báo đã thanh toán mà đơn còn chờ, đối chiếu `orderId` trên MoMo với mã đơn trong SQL và kiểm tra log của webhook. Không tự đổi trạng thái SQL thành Paid dựa trên URL trình duyệt quay về.

Tài liệu chính thức: [tích hợp môi trường Test](https://developers.momo.vn/v3/vi/docs/payment/onboarding/integration-process/), [API thanh toán ví](https://developers.momo.vn/v3/vi/docs/payment/api/wallet/onetime/), [thông báo IPN](https://developers.momo.vn/v3/vi/docs/payment/api/result-handling/notification/).

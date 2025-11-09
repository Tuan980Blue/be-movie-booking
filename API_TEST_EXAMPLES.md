# API Test Examples - Payment & Booking Flow

## 🔐 Authentication
Tất cả các API (trừ VNPay callbacks) đều yêu cầu JWT token trong header:
```
Authorization: Bearer <your_jwt_token>
```

---

## 1. 📝 Tạo Booking (Draft)

**Endpoint:** `POST /api/bookings`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>",
  "Content-Type": "application/json"
}
```

**Payload mẫu:**
```json
{
  "showtimeId": "123e4567-e89b-12d3-a456-426614174000",
  "seatIds": [
    "223e4567-e89b-12d3-a456-426614174001",
    "323e4567-e89b-12d3-a456-426614174002"
  ],
  "promotionCode": null
}
```

**Response mẫu:**
```json
{
  "id": "423e4567-e89b-12d3-a456-426614174003",
  "userId": "523e4567-e89b-12d3-a456-426614174004",
  "status": 1,
  "totalAmountMinor": 200000,
  "currency": "VND",
  "customerInfo": {
    "fullName": "Nguyễn Văn A",
    "email": "user@example.com",
    "phone": "0123456789"
  },
  "showtime": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "movieTitle": "Avengers: Endgame",
    "cinemaName": "CGV Vincom",
    "roomName": "Phòng 1",
    "startUtc": "2024-12-25T10:00:00Z",
    "format": "2D"
  },
  "seats": [
    {
      "seatId": "223e4567-e89b-12d3-a456-426614174001",
      "rowLabel": "A",
      "seatNumber": 5,
      "seatPriceMinor": 100000
    },
    {
      "seatId": "323e4567-e89b-12d3-a456-426614174002",
      "rowLabel": "A",
      "seatNumber": 6,
      "seatPriceMinor": 100000
    }
  ],
  "createdAt": "2024-12-25T09:00:00Z",
  "expiresAt": "2024-12-25T09:03:00Z"
}
```

**Lưu ý:** 
- `bookingId` từ response này sẽ dùng cho bước tiếp theo
- Booking draft sẽ hết hạn sau 3 phút nếu không tạo payment

---

## 2. 💳 Tạo Payment

**Endpoint:** `POST /api/payments`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>",
  "Content-Type": "application/json"
}
```

**Payload mẫu:**
```json
{
  "bookingId": "423e4567-e89b-12d3-a456-426614174003",
  "provider": 1,
  "returnUrl": "http://localhost:3000/booking/payment/return",
  "notifyUrl": null
}
```

**Giá trị Provider:**
- `1` = VnPay
- `2` = MoMo
- `3` = Stripe

**Response mẫu:**
```json
{
  "id": "623e4567-e89b-12d3-a456-426614174005",
  "bookingId": "423e4567-e89b-12d3-a456-426614174003",
  "provider": 1,
  "amountMinor": 200000,
  "currency": "VND",
  "status": 2,
  "providerTxnId": null,
  "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=200000&vnp_Command=pay&vnp_CreateDate=20241225090000&vnp_CurrCode=VND&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Thanh%20to%C3%A1n%20%C4%91%E1%BA%B7t%20v%C3%A9%20-%20Booking%20&vnp_OrderType=other&vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A3000%2Fbooking%2Fpayment%2Freturn&vnp_TmnCode=VLWS50WU&vnp_TxnRef=623e4567-e89b-12d3-a456-426614174005&vnp_Version=2.1.0&vnp_SecureHash=abc123...",
  "createdAt": "2024-12-25T09:01:00Z",
  "updatedAt": null
}
```

**Lưu ý:**
- `paymentUrl` là URL để redirect user đến trang thanh toán VNPay
- Sau khi tạo payment, draft TTL và seat locks sẽ được extend lên 10 phút
- `paymentId` từ response này sẽ dùng để confirm booking

---

## 3. 🔍 Lấy thông tin Payment

**Endpoint:** `GET /api/payments/{paymentId}`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>"
}
```

**Example:**
```
GET /api/payments/623e4567-e89b-12d3-a456-426614174005
```

**Response mẫu:**
```json
{
  "id": "623e4567-e89b-12d3-a456-426614174005",
  "bookingId": "423e4567-e89b-12d3-a456-426614174003",
  "provider": 1,
  "amountMinor": 200000,
  "currency": "VND",
  "status": 3,
  "providerTxnId": "12345678",
  "paymentUrl": null,
  "createdAt": "2024-12-25T09:01:00Z",
  "updatedAt": "2024-12-25T09:05:00Z"
}
```

**Giá trị Status:**
- `1` = Initiated
- `2` = Pending
- `3` = Succeeded
- `4` = Failed
- `5` = Canceled
- `6` = Refunded
- `7` = PartiallyRefunded

---

## 4. 🔎 Tìm kiếm Payments

**Endpoint:** `GET /api/payments`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>"
}
```

**Query Parameters:**
```
?bookingId=423e4567-e89b-12d3-a456-426614174003
&provider=1
&status=3
&page=1
&pageSize=20
&sortBy=CreatedAt
&sortOrder=desc
```

**Example:**
```
GET /api/payments?bookingId=423e4567-e89b-12d3-a456-426614174003&status=3
```

**Response mẫu:**
```json
{
  "items": [
    {
      "id": "623e4567-e89b-12d3-a456-426614174005",
      "bookingId": "423e4567-e89b-12d3-a456-426614174003",
      "provider": 1,
      "amountMinor": 200000,
      "currency": "VND",
      "status": 3,
      "providerTxnId": "12345678",
      "createdAt": "2024-12-25T09:01:00Z",
      "updatedAt": "2024-12-25T09:05:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1,
  "totalPages": 1
}
```

---

## 5. ✅ Xác nhận Booking (Manual - Optional)

**Endpoint:** `POST /api/bookings/{bookingId}/confirm`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>",
  "Content-Type": "application/json"
}
```

**Payload mẫu:**
```json
{
  "paymentId": "623e4567-e89b-12d3-a456-426614174005"
}
```

**Example:**
```
POST /api/bookings/423e4567-e89b-12d3-a456-426614174003/confirm
```

**Lưu ý:**
- API này thường không cần gọi thủ công vì booking sẽ tự động được confirm khi VNPay IPN được xử lý
- Chỉ dùng khi IPN không hoạt động và cần confirm thủ công

---

## 6. ❌ Hủy Booking

**Endpoint:** `POST /api/bookings/{bookingId}/cancel`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>",
  "Content-Type": "application/json"
}
```

**Payload mẫu (optional):**
```json
{
  "reason": "Người dùng hủy"
}
```

**Example:**
```
POST /api/bookings/423e4567-e89b-12d3-a456-426614174003/cancel
```

**Response mẫu:**
```json
{
  "id": "423e4567-e89b-12d3-a456-426614174003",
  "code": "",
  "userId": "523e4567-e89b-12d3-a456-426614174004",
  "status": 3,
  "totalAmountMinor": 200000,
  "currency": "VND",
  "createdAt": "2024-12-25T09:00:00Z",
  "updatedAt": "2024-12-25T09:10:00Z"
}
```

---

## 7. 🔄 VNPay Return URL (Callback từ VNPay)

**Endpoint:** `GET /api/payments/vnpay-return`

**Không cần authentication** - VNPay sẽ gọi endpoint này

**Query Parameters mẫu (VNPay sẽ gửi):**
```
?vnp_Amount=20000000
&vnp_BankCode=NCB
&vnp_CardType=ATM
&vnp_OrderInfo=Thanh+toan+dat+ve+-+Booking+
&vnp_PayDate=20241225090500
&vnp_ResponseCode=00
&vnp_SecureHash=abc123def456...
&vnp_TmnCode=VLWS50WU
&vnp_TransactionNo=12345678
&vnp_TxnRef=623e4567-e89b-12d3-a456-426614174005
&vnp_TransactionStatus=00
```

**Response:**
- Redirect đến frontend với payment status
- Success: `http://localhost:3000/booking/payment/success?paymentId={paymentId}`
- Failed: `http://localhost:3000/booking/payment/failed?paymentId={paymentId}`

**Lưu ý:**
- `vnp_ResponseCode=00` = Payment thành công
- `vnp_TxnRef` = Payment ID (được dùng làm orderId khi tạo payment URL)

---

## 8. 📨 VNPay IPN (Instant Payment Notification)

**Endpoint:** `POST /api/payments/vnpay-ipn`

**Không cần authentication** - VNPay server sẽ gọi endpoint này

**Query Parameters mẫu (VNPay sẽ gửi):**
```
?vnp_Amount=20000000
&vnp_BankCode=NCB
&vnp_CardType=ATM
&vnp_OrderInfo=Thanh+toan+dat+ve+-+Booking+
&vnp_PayDate=20241225090500
&vnp_ResponseCode=00
&vnp_SecureHash=abc123def456...
&vnp_TmnCode=VLWS50WU
&vnp_TransactionNo=12345678
&vnp_TxnRef=623e4567-e89b-12d3-a456-426614174005
&vnp_TransactionStatus=00
```

**Response mẫu (Success):**
```json
{
  "RspCode": "00",
  "Message": "Success"
}
```

**Response mẫu (Error):**
```json
{
  "RspCode": "01",
  "Message": "Payment not found"
}
```

**Lưu ý:**
- IPN là cách an toàn nhất để xác nhận payment
- Booking sẽ tự động được confirm khi IPN thành công
- Phải trả về `RspCode=00` để VNPay biết đã nhận được notification

---

## 9. 📋 Lấy thông tin Booking

**Endpoint:** `GET /api/bookings/{bookingId}`

**Headers:**
```json
{
  "Authorization": "Bearer <your_jwt_token>"
}
```

**Example:**
```
GET /api/bookings/423e4567-e89b-12d3-a456-426614174003
```

**Response mẫu (Draft - chưa confirm):**
```json
{
  "id": "423e4567-e89b-12d3-a456-426614174003",
  "code": "",
  "userId": "523e4567-e89b-12d3-a456-426614174004",
  "status": 1,
  "totalAmountMinor": 200000,
  "currency": "VND",
  "customerInfo": {
    "fullName": "Nguyễn Văn A",
    "email": "user@example.com",
    "phone": "0123456789"
  },
  "createdAt": "2024-12-25T09:00:00Z",
  "updatedAt": null,
  "items": [...],
  "tickets": []
}
```

**Response mẫu (Confirmed - đã thanh toán):**
```json
{
  "id": "423e4567-e89b-12d3-a456-426614174003",
  "code": "BK20241225001",
  "userId": "523e4567-e89b-12d3-a456-426614174004",
  "status": 2,
  "totalAmountMinor": 200000,
  "currency": "VND",
  "bookingQr": "BK20241225001",
  "customerInfo": {
    "fullName": "Nguyễn Văn A",
    "email": "user@example.com",
    "phone": "0123456789"
  },
  "createdAt": "2024-12-25T09:00:00Z",
  "updatedAt": "2024-12-25T09:05:00Z",
  "items": [
    {
      "id": "723e4567-e89b-12d3-a456-426614174006",
      "showtimeId": "123e4567-e89b-12d3-a456-426614174000",
      "seatId": "223e4567-e89b-12d3-a456-426614174001",
      "seatPriceMinor": 100000,
      "status": 2,
      "createdAt": "2024-12-25T09:05:00Z"
    }
  ],
  "tickets": [
    {
      "id": "823e4567-e89b-12d3-a456-426614174007",
      "ticketCode": "TK20241225001",
      "showtimeId": "123e4567-e89b-12d3-a456-426614174000",
      "seatId": "223e4567-e89b-12d3-a456-426614174001",
      "status": 1,
      "issuedAt": "2024-12-25T09:05:00Z"
    }
  ]
}
```

---

## 🔄 Complete Flow Test

### Bước 1: Tạo Booking
```bash
POST /api/bookings
{
  "showtimeId": "123e4567-e89b-12d3-a456-426614174000",
  "seatIds": ["223e4567-e89b-12d3-a456-426614174001"]
}
```
→ Lưu `bookingId` từ response

### Bước 2: Tạo Payment
```bash
POST /api/payments
{
  "bookingId": "<bookingId_from_step_1>",
  "provider": 1
}
```
→ Lưu `paymentUrl` và `paymentId` từ response

### Bước 3: Test VNPay Return (Simulate)
```bash
GET /api/payments/vnpay-return?vnp_Amount=20000000&vnp_ResponseCode=00&vnp_TxnRef=<paymentId>&vnp_TransactionNo=12345678&vnp_SecureHash=...
```

### Bước 4: Test VNPay IPN (Simulate)
```bash
POST /api/payments/vnpay-ipn?vnp_Amount=20000000&vnp_ResponseCode=00&vnp_TxnRef=<paymentId>&vnp_TransactionNo=12345678&vnp_SecureHash=...
```

### Bước 5: Kiểm tra Booking đã được confirm
```bash
GET /api/bookings/<bookingId>
```
→ Status phải là `2` (Confirmed) và có `code`, `tickets`

---

## 🧪 Test với VNPay Sandbox

### Test Card (VNPay Sandbox):
- **Bank:** NCB
- **Card Number:** `9704198526191432198`
- **Cardholder Name:** `NGUYEN VAN A`
- **Issue Date:** `07/15`
- **OTP Password:** `123456`

### Test Scenarios:

1. **Payment Success:**
   - Dùng test card trên
   - Nhập OTP: `123456`
   - Payment sẽ thành công và booking sẽ được auto-confirm

2. **Payment Failed:**
   - Nhập sai OTP hoặc cancel payment
   - Payment status = Failed
   - Booking vẫn ở trạng thái Pending

3. **Payment Timeout:**
   - Không thanh toán trong 10 phút
   - Draft và seat locks sẽ hết hạn
   - Cần tạo booking mới

---

## 📝 Notes

1. **Amount Format:**
   - `amountMinor` = số tiền nhân với 100 (ví dụ: 200000 = 2,000 VND)
   - VNPay nhận amount dạng integer (ví dụ: 200000 cho 2,000 VND)

2. **Status Values:**
   - Booking: `1=Pending, 2=Confirmed, 3=Canceled, 4=Expired`
   - Payment: `1=Initiated, 2=Pending, 3=Succeeded, 4=Failed`

3. **Timing:**
   - Initial lock: 3 phút
   - After payment created: 10 phút (draft + seat locks)
   - Seat locks chỉ extend được 1 lần (IsExtended flag)

4. **Security:**
   - Tất cả VNPay callbacks đều được validate signature
   - Payment phải có status Succeeded mới confirm được booking


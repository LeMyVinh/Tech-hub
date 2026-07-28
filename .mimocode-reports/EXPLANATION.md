# TechHub E-Commerce Platform — Code Explanation

## 1. Kiến trúc tổng quan (Layered Architecture)

```
Request → Controller → Service → Repository → DbContext → MySQL
```

Mỗi tầng chỉ giao tiếp với tầng liền kề:
- **Controller**: Nhận HTTP request, validate input, gọi Service
- **Service**: Xử lý business logic, gọi Repository
- **Repository**: Truy vấn dữ liệu qua EF Core
- **Domain**: Các Entity biểu diễn bảng trong DB

---

## 2. Module Authentication

**Cơ chế JWT:**
```
Login → Server tạo JWT access token (short-lived) + refresh token (long-lived)
→ Client lưu cả 2 vào localStorage
→ Mỗi request gửi Authorization: Bearer <token>
→ Middleware xác thực JWT → Inject userId vào Claims
→ Controller lấy userId từ Claims
```

**Files chính:**
- `AuthService.cs`: Đăng ký (hash BCrypt), Đăng nhập (verify BCrypt), Refresh token rotation
- `JwtTokenGenerator.cs`: Tạo JWT token với密钥 từ config
- `AuthController.cs`: 7 endpoints (register, login, refresh, logout, forgot-password, reset-password, change-password)

---

## 3. Module Product Management + Catalog

**Cấu trúc Product:**
```
Product (1) ──→ (N) ProductVariant (1) ──→ (N) ProductImage
     │                    │
     ├── Category (FK)    └── Stock, Price, SKU
     └── Brand (FK)
```

**BR-01**: Mỗi sản phẩm có nhiều biến thể (variant), mỗi variant quản lý tồn kho riêng. Ví dụ: Laptop Dell có variant "8GB RAM" và "16GB RAM", mỗi variant có giá và số lượng tồn kho khác nhau.

**Files chính:**
- `ProductService.cs`: CRUD sản phẩm, search với filter (keyword, category, brand, price range, sort, pagination)
- `AdminProductController.cs`: `[Authorize(Roles = "Admin")]` — chỉ Admin mới tạo/sửa/xóa
- `ProductController.cs`: Public — ai cũng xem được danh sách và chi tiết

---

## 4. Module Shopping Cart

**BR-08**: Giỏ hàng gắn với Customer đã đăng nhập (1 User = 1 Cart).

**Logic chính:**
```csharp
// Thêm vào giỏ: kiểm tra tồn kho trước
if (variant.StockQuantity < request.Quantity)
    throw new CartException(400, "Không đủ tồn kho.");

// Nếu sản phẩm đã có trong giỏ → cộng dồn số lượng
var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == variantId);
if (existingItem != null)
    existingItem.Quantity += request.Quantity;
else
    cart.CartItems.Add(new CartItem { ... });
```

---

## 5. Module Order Flow (Checkout → Order → Payment)

**Luồng đặt hàng:**
```
Cart → Checkout (chọn địa chỉ, phương thức vận chuyển)
     → Tạo Order (status = "Pending")
     → Giảm tồn kho (StockQuantity -= quantity)
     → Xóa Cart
     → Tạo Payment
```

**BR-02**: Đơn hàng chỉ tạo khi toàn bộ sản phẩm còn đủ hàng.

**BR-03**: Trạng thái đơn hàng theo quy trình:
```
Pending → Confirmed → Processing → Shipping → Delivered
   └──────────┴──────────┴──────────┘
                    ↓
              Cancelled (khi chưa Delivered)
```

**BR-10**: Khi hủy đơn → hoàn trả tồn kho:
```csharp
foreach (var item in order.OrderItems)
{
    variant.StockQuantity += item.Quantity;  // Hoàn kho
}
```

---

## 6. Module Payment

**BR-04/BR-05**:
- **COD**: Tự động xác nhận ngay → Order status = "Confirmed"
- **VNPay**: Tạo payment Pending → Redirect VNPay gateway → Callback xác nhận

```csharp
// COD
payment.Status = "Success";
order.Status = "Confirmed";  // Auto-confirm

// VNPay
payment.Status = "Pending";
// ... redirect to VNPay ...
// On callback: verify signature → update status
```

---

## 7. Module Review & Rating

**BR-06**: Mỗi lần mua chỉ đánh giá đúng 1 lần (UNIQUE constraint trên OrderItemId).

**BR-07**: Đánh giá mới = "Pending" → Admin duyệt → "Approved" → Hiển thị công khai.

```csharp
// Customer tạo review
review.Status = "Pending";  // Chờ Admin duyệt

// Admin duyệt
review.Status = "Approved";  // Hiển thị trên trang sản phẩm
```

---

## 8. Module User & Account Management

**Customer:**
- Xem/sửa thông tin cá nhân
- Quản lý địa chỉ giao hàng (nhiều địa chỉ, 1 default)

**Admin:**
- Xem danh sách người dùng
- Khóa/mở khóa tài khoản

**BR-11**: Nhiều địa chỉ, nhưng chỉ 1 default tại 1 thời điểm:
```csharp
// Khi set default mới → unset default cũ
foreach (var existing in addresses.Where(a => a.IsDefault))
    existing.IsDefault = false;
address.IsDefault = true;
```

---

## 9. Module Dashboard

**4 API chính:**
- `GET /dashboard/summary` → Tổng quan: doanh thu, đơn hàng, khách hàng, sản phẩm
- `GET /dashboard/revenue` → Báo cáo doanh thu theo ngày và danh mục
- `GET /dashboard/top-products` → Sản phẩm bán chạy
- `GET /dashboard/inventory` → Báo cáo tồn kho (sản phẩm sắp hết hàng)

**Repository Pattern**: Dùng `IDashboardRepository` thay vì inject `AppDbContext` trực tiếp vào Service (tuân thủ nguyên tắc Clean Architecture).

---

## 10. Frontend (Angular)

**Cấu trúc:**
```
features/
├── auth/          → Login, Register, ForgotPassword
├── catalog/       → ProductList, ProductDetail, CategoryManage, BrandManage
├── cart/          → CartComponent (thêm/xóa/sửa/số lượng)
├── wishlist/      → WishlistComponent (danh sách yêu thích)
├── checkout/      → CheckoutComponent (chọn địa chỉ, vận chuyển)
├── orders/        → OrderList (danh sách đơn, chi tiết, hủy)
└── admin/         → ProductManage (CRUD sản phẩm)
```

**Pattern sử dụng:**
- **Signals** thay thế BehaviorSubject
- **Standalone Components** (không cần NgModule)
- **AuthService** quản lý JWT token trong localStorage
- **HTTP Interceptor** tự động attach token vào mỗi request

---

## 11. Business Rules Summary

| Code | Rule | Implement ở đâu |
|------|------|------------------|
| BR-01 | Variant quản lý tồn kho riêng | ProductVariant entity |
| BR-02 | Đơn chỉ tạo khi đủ hàng | OrderService.CreateOrderAsync |
| BR-03 | Trạng thái đơn theo quy trình | OrderService.UpdateOrderStatusAsync |
| BR-04 | VNPay cần xác nhận thanh toán | PaymentService.ProcessVnpayCallbackAsync |
| BR-05 | COD tự động xác nhận | PaymentService.CreatePaymentAsync |
| BR-06 | Mỗi sản phẩm chỉ đánh giá 1 lần | Review UNIQUE constraint |
| BR-07 | Review chờ Admin duyệt | ReviewService.ApproveReviewAsync |
| BR-08 | Giỏ hàng gắn Customer | Cart.UserId UNIQUE |
| BR-09 | Sản phẩm hết hàng không hiển thị | ProductService.SearchAsync filter |
| BR-10 | Hủy đơn hoàn kho | OrderService.CancelOrderAsync |
| BR-11 | Chỉ 1 địa chỉ default | UserService.SetDefaultAddressAsync |
| BR-12 | Tài khoản bị khóa không đăng nhập được | JWT middleware checks IsActive |

---

## 12. API Endpoints Tổng hợp

### Authentication
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/v1/auth/register` | Public | Đăng ký |
| POST | `/api/v1/auth/login` | Public | Đăng nhập |
| POST | `/api/v1/auth/refresh` | Public | Làm mới token |
| POST | `/api/v1/auth/logout` | Public | Đăng xuất |
| POST | `/api/v1/auth/forgot-password` | Public | Quên mật khẩu |
| POST | `/api/v1/auth/reset-password` | Public | Đặt lại mật khẩu |
| PUT | `/api/v1/auth/change-password` | JWT | Đổi mật khẩu |

### Products
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/v1/products` | Public | Tìm kiếm sản phẩm |
| GET | `/api/v1/products/{id}` | Public | Chi tiết sản phẩm |
| POST | `/api/v1/admin/products` | Admin | Tạo sản phẩm |
| PUT | `/api/v1/admin/products/{id}` | Admin | Sửa sản phẩm |
| DELETE | `/api/v1/admin/products/{id}` | Admin | Xóa sản phẩm |

### Cart
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/v1/cart` | Customer | Xem giỏ hàng |
| POST | `/api/v1/cart/items` | Customer | Thêm sản phẩm |
| PUT | `/api/v1/cart/items/{id}` | Customer | Cập nhật số lượng |
| DELETE | `/api/v1/cart/items/{id}` | Customer | Xóa sản phẩm |
| DELETE | `/api/v1/cart` | Customer | Xóa toàn bộ giỏ |

### Wishlist
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/v1/wishlist` | Customer | Danh sách yêu thích |
| POST | `/api/v1/wishlist` | Customer | Thêm vào yêu thích |
| DELETE | `/api/v1/wishlist/{productId}` | Customer | Xóa khỏi yêu thích |
| POST | `/api/v1/wishlist/{productId}/move-to-cart` | Customer | Chuyển sang giỏ |

### Orders
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/v1/orders` | Customer | Tạo đơn hàng |
| GET | `/api/v1/orders` | Customer/Admin | Danh sách đơn |
| GET | `/api/v1/orders/{id}` | Customer | Chi tiết đơn |
| PUT | `/api/v1/orders/{id}/cancel` | Customer | Hủy đơn |
| PUT | `/api/v1/orders/{id}/status` | Admin | Cập nhật trạng thái |

### Payments
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/v1/payments` | Customer | Tạo thanh toán |
| POST | `/api/v1/payments/vnpay/callback` | Public | VNPay callback |
| GET | `/api/v1/payments/order/{orderId}` | Customer | Lịch sử thanh toán |

### Reviews
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/v1/products/{id}/reviews` | Customer | Tạo đánh giá |
| GET | `/api/v1/products/{id}/reviews` | Public | Danh sách đánh giá |
| PUT | `/api/v1/admin/reviews/{id}/approve` | Admin | Duyệt đánh giá |
| PUT | `/api/v1/admin/reviews/{id}/reject` | Admin | Từ chối đánh giá |

### Users
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/v1/users/me` | Customer | Thông tin tài khoản |
| PUT | `/api/v1/users/me` | Customer | Cập nhật thông tin |
| GET | `/api/v1/users/me/addresses` | Customer | Danh sách địa chỉ |
| POST | `/api/v1/users/me/addresses` | Customer | Thêm địa chỉ |
| GET | `/api/v1/admin/users` | Admin | Danh sách người dùng |
| PUT | `/api/v1/admin/users/{id}/lock` | Admin | Khóa tài khoản |
| PUT | `/api/v1/admin/users/{id}/unlock` | Admin | Mở khóa tài khoản |

### Dashboard
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/v1/dashboard/summary` | Admin | Tổng quan |
| GET | `/api/v1/dashboard/revenue` | Admin | Báo cáo doanh thu |
| GET | `/api/v1/dashboard/top-products` | Admin | Sản phẩm bán chạy |
| GET | `/api/v1/dashboard/inventory` | Admin | Báo cáo tồn kho |

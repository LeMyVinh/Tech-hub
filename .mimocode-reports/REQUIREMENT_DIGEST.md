# REQUIREMENT_DIGEST

## Overview
**TechHub** — Nền tảng thương mại điện tử chuyên biệt cho ngành hàng công nghệ (laptop, linh kiện máy tính, thiết bị ngoại vi, phụ kiện). Hệ thống gồm 2 phần: Storefront (khách hàng) và Admin Portal (quản trị viên).

**Tech Stack:** Angular + ASP.NET Core + MySQL + EF Core + JWT + Repository Pattern + Layered Architecture

**Actors:** Guest (chưa đăng nhập), Customer (đã đăng nhập), Admin (quản trị viên)

---

## Modules

### Module 1: Authentication
- **Description:** Đăng ký, đăng nhập, quên mật khẩu, đổi mật khẩu
- **Entities:** User, Role
- **API Endpoints:**
  - `POST /api/v1/auth/register` — Đăng ký (Public)
  - `POST /api/v1/auth/login` — Đăng nhập (Public)
  - `POST /api/v1/auth/forgot-password` — Quên mật khẩu (Public)
  - `POST /api/v1/auth/change-password` — Đổi mật khẩu (Authenticated)
- **Dependencies:** None (base module)

### Module 2: Product Management
- **Description:** CRUD sản phẩm, hình ảnh, biến thể (variant), tồn kho, trạng thái
- **Entities:** Product, ProductVariant, ProductImage, Category, Brand
- **API Endpoints:**
  - `GET /api/v1/products` — Danh sách sản phẩm
  - `GET /api/v1/products/{id}` — Chi tiết sản phẩm
  - `POST /api/v1/products` — Tạo sản phẩm (Admin)
  - `PUT /api/v1/products/{id}` — Cập nhật sản phẩm (Admin)
  - `DELETE /api/v1/products/{id}` — Xóa sản phẩm (Admin)
  - `POST /api/v1/products/{id}/variants` — Quản lý biến thể
- **Dependencies:** Category, Brand (từ Catalog)

### Module 3: Catalog & Search
- **Description:** Quản lý danh mục, thương hiệu; tìm kiếm, lọc, sắp xếp, phân trang
- **Entities:** Category, Brand
- **API Endpoints:**
  - `GET /api/v1/categories` — Danh mục
  - `POST /api/v1/categories` — Tạo danh mục (Admin)
  - `GET /api/v1/brands` — Thương hiệu
  - `POST /api/v1/brands` — Tạo thương hiệu (Admin)
  - `GET /api/v1/products/search` — Tìm kiếm sản phẩm
- **Dependencies:** None (base module)

### Module 4: Shopping Cart
- **Description:** Thêm/sửa/xóa sản phẩm trong giỏ, tính tổng tiền
- **Entities:** Cart, CartItem
- **API Endpoints:**
  - `GET /api/v1/cart` — Xem giỏ hàng (Customer)
  - `POST /api/v1/cart/items` — Thêm sản phẩm (Customer)
  - `PUT /api/v1/cart/items/{id}` — Cập nhật số lượng (Customer)
  - `DELETE /api/v1/cart/items/{id}` — Xóa sản phẩm (Customer)
- **Dependencies:** Product, User

### Module 5: Wishlist
- **Description:** Thêm/xóa sản phẩm yêu thích, chuyển sang giỏ hàng
- **Entities:** Wishlist, WishlistItem
- **API Endpoints:**
  - `GET /api/v1/wishlist` — Danh sách yêu thích (Customer)
  - `POST /api/v1/wishlist` — Thêm vào yêu thích (Customer)
  - `DELETE /api/v1/wishlist/{productId}` — Xóa khỏi yêu thích (Customer)
  - `POST /api/v1/wishlist/{productId}/move-to-cart` — Chuyển sang giỏ (Customer)
- **Dependencies:** Product, User

### Module 6: Checkout
- **Description:** Nhập địa chỉ giao hàng, chọn phương thức vận chuyển, xác nhận đơn hàng
- **Entities:** Order, OrderItem, ShippingAddress
- **API Endpoints:**
  - `POST /api/v1/checkout` — Tạo đơn hàng từ giỏ (Customer)
  - `GET /api/v1/checkout/preview` — Xem tóm tắt trước khi đặt
- **Dependencies:** Cart, Product, User

### Module 7: Online Payment
- **Description:** Thanh toán COD, VNPay, theo dõi trạng thái & lịch sử thanh toán
- **Entities:** Payment, PaymentTransaction
- **API Endpoints:**
  - `POST /api/v1/payments` — Tạo thanh toán (Customer)
  - `POST /api/v1/payments/vnpay/callback` — VNPay callback (Public)
  - `GET /api/v1/payments/{orderId}` — Lịch sử thanh toán
- **Dependencies:** Order

### Module 8: Order Management
- **Description:** Chi tiết đơn hàng, theo dõi, hủy đơn, lịch sử đơn hàng
- **Entities:** Order, OrderItem, OrderStatusHistory
- **API Endpoints:**
  - `GET /api/v1/orders` — Danh sách đơn (Customer/Admin)
  - `GET /api/v1/orders/{id}` — Chi tiết đơn (Customer/Admin)
  - `PUT /api/v1/orders/{id}/cancel` — Hủy đơn (Customer)
  - `PUT /api/v1/orders/{id}/status` — Cập nhật trạng thái (Admin)
- **Dependencies:** Checkout, Payment, Product

### Module 9: Review & Rating
- **Description:** Đánh giá sao, bình luận, upload hình ảnh, kiểm duyệt bởi Admin
- **Entities:** Review, ReviewImage
- **API Endpoints:**
  - `GET /api/v1/products/{id}/reviews` — Danh sách đánh giá
  - `POST /api/v1/products/{id}/reviews` — Tạo đánh giá (Customer)
  - `PUT /api/v1/reviews/{id}/approve` — Duyệt đánh giá (Admin)
  - `PUT /api/v1/reviews/{id}/reject` — Từ chối đánh giá (Admin)
- **Dependencies:** Product, Order, User

### Module 10: Dashboard
- **Description:** Thống kê doanh thu, đơn hàng, sản phẩm bán chạy, tồn kho
- **Entities:** (Aggregate từ các module khác)
- **API Endpoints:**
  - `GET /api/v1/dashboard/summary` — Tổng quan (Admin)
  - `GET /api/v1/dashboard/revenue` — Báo cáo doanh thu (Admin)
  - `GET /api/v1/dashboard/top-products` — Sản phẩm bán chạy (Admin)
  - `GET /api/v1/dashboard/inventory` — Báo cáo tồn kho (Admin)
- **Dependencies:** Order, Product, Payment

### Module 11: User & Account Management
- **Description:** Quản lý thông tin tài khoản, địa chỉ, quản lý người dùng (Admin)
- **Entities:** User, Address
- **API Endpoints:**
  - `GET /api/v1/users/me` — Thông tin tài khoản (Customer)
  - `PUT /api/v1/users/me` — Cập nhật thông tin (Customer)
  - `GET /api/v1/users/me/addresses` — Danh sách địa chỉ (Customer)
  - `POST /api/v1/users/me/addresses` — Thêm địa chỉ (Customer)
  - `GET /api/v1/admin/users` — Danh sách người dùng (Admin)
  - `PUT /api/v1/admin/users/{id}/lock` — Khóa tài khoản (Admin)
- **Dependencies:** User (từ Authentication)

---

## Database Entities

| Entity | Mô tả |
|--------|-------|
| User | Người dùng (Customer/Admin) |
| Role | Vai trò (Customer, Admin) |
| Product | Sản phẩm |
| ProductVariant | Biến thể sản phẩm (RAM, dung lượng...) |
| ProductImage | Hình ảnh sản phẩm |
| Category | Danh mục (hierarchy) |
| Brand | Thương hiệu |
| Cart | Giỏ hàng |
| CartItem | Sản phẩm trong giỏ |
| Wishlist | Danh sách yêu thích |
| WishlistItem | Sản phẩm yêu thích |
| Order | Đơn hàng |
| OrderItem | Sản phẩm trong đơn |
| OrderStatusHistory | Lịch sử trạng thái đơn |
| ShippingAddress | Địa chỉ giao hàng |
| Address | Địa chỉ người dùng |
| Payment | Thanh toán |
| PaymentTransaction | Giao dịch thanh toán |
| Review | Đánh giá sản phẩm |
| ReviewImage | Hình ảnh đánh giá |

---

## Business Rules

| Code | Rule |
|------|------|
| BR-01 | Mỗi sản phẩm có thể có nhiều lựa chọn (biến thể); mỗi lựa chọn được bán và quản lý tồn kho riêng. |
| BR-02 | Đơn hàng chỉ được tạo khi toàn bộ sản phẩm trong đơn còn đủ hàng tại thời điểm đặt. |
| BR-03 | Trạng thái đơn hàng: Chờ xử lý → Đã xác nhận → Đang xử lý → Đang giao → Đã giao; hoặc → Đã huỷ. |
| BR-04 | Đơn thanh toán qua VNPay chỉ được xác nhận khi giao dịch thanh toán thành công. |
| BR-05 | Đơn thanh toán COD được tự động xác nhận ngay sau khi đặt. |
| BR-06 | Mỗi lần mua (mỗi sản phẩm trong một đơn) chỉ được đánh giá đúng một lần. |
| BR-07 | Đánh giá mới luôn ở trạng thái chờ duyệt, chờ Admin xét duyệt trước khi hiển thị công khai. |
| BR-08 | Giỏ hàng gắn với Customer đã đăng nhập; chưa hỗ trợ giỏ hàng cho khách chưa đăng nhập. |
| BR-09 | Sản phẩm ngừng kinh doanh hoặc hết hàng không hiển thị trong kết quả tìm kiếm mặc định. |
| BR-10 | Khi một đơn hàng bị huỷ, số lượng hàng đã giữ cho đơn đó được hoàn trả lại kho. |
| BR-11 | Customer có thể có nhiều địa chỉ giao hàng nhưng chỉ một địa chỉ được đặt làm mặc định tại một thời điểm. |
| BR-12 | Tài khoản bị khoá (locked) không thể đăng nhập hoặc thực hiện bất kỳ giao dịch nào trên hệ thống. |

---

## Module Build Order

```
1. Authentication (base)
2. Product Management + Catalog & Search (base, song song)
3. Shopping Cart (depends: Product, User)
4. Wishlist (depends: Product, User)
5. Checkout + Order Management (depends: Cart, Product)
6. Online Payment (depends: Order)
7. Review & Rating (depends: Product, Order, User)
8. User & Account Management (depends: User)
9. Dashboard (depends: Order, Product, Payment)
```

---

## Integration Points

1. **Auth → All modules:** JWT token used across all authenticated endpoints
2. **Product ↔ Cart:** Cart references ProductVariant, checks stock
3. **Cart → Checkout → Order → Payment:** Order flow chain
4. **Order → Review:** Review only allowed for completed orders
5. **Order → Dashboard:** Revenue stats from confirmed/delivered orders
6. **Product ↔ Catalog:** Product belongs to Category and Brand

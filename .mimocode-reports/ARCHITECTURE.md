# ARCHITECTURE — TechHub E-Commerce Platform

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      CLIENT (Browser)                       │
│                   Angular SPA + Angular Material            │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP/HTTPS (REST API)
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    API GATEWAY (Optional)                    │
│              Rate Limiting, CORS, Logging                    │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET CORE WEB API                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              Middleware Pipeline                      │   │
│  │  Exception → Logging → Authentication (JWT) → CORS   │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              CONTROLLER LAYER                        │   │
│  │  AuthController, ProductController, CartController,  │   │
│  │  OrderController, PaymentController, ReviewController│   │
│  │  DashboardController, UserController, etc.           │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              SERVICE LAYER (Business Logic)          │   │
│  │  AuthService, ProductService, CartService,           │   │
│  │  OrderService, PaymentService, ReviewService,        │   │
│  │  DashboardService, UserService, etc.                 │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              REPOSITORY LAYER (Data Access)          │   │
│  │  UserRepository, ProductRepository, CartRepository,  │   │
│  │  OrderRepository, PaymentRepository, ReviewRepository│   │
│  │  DashboardRepository, UserRepository, etc.           │   │
│  └─────────────────────────────────────────────────────┘   │
│                           │                                 │
│                           ▼                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              ENTITY FRAMEWORK CORE (DbContext)       │   │
│  │  TechHubDbContext                                    │   │
│  └─────────────────────────────────────────────────────┘   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      MySQL DATABASE                         │
│  Users, Roles, Products, Categories, Brands, Cart,          │
│  Orders, Payments, Reviews, etc.                            │
└─────────────────────────────────────────────────────────────┘
```

---

## Entity Relationship Diagram (ERD)

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│     Role     │       │     User     │       │   Address    │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ Id (PK)      │◄──┐   │ Id (PK)      │◄──┐   │ Id (PK)      │
│ Name         │   │   │ FullName     │   │   │ UserId (FK)  │──────┐
│ Description  │   │   │ Email        │   │   │ Street       │      │
└──────────────┘   │   │ PasswordHash │   │   │ Ward         │      │
                   │   │ Phone        │   │   │ District     │      │
                   │   │ RoleId (FK)  │───┘   │ City         │      │
                   │   │ IsActive     │       │ IsDefault    │      │
                   │   │ CreatedAt    │       └──────────────┘      │
                   │   │ UpdatedAt    │                             │
                   │   └──────────────┘                             │
                   │          │                                     │
                   │          │ 1:N                                 │
                   │          ▼                                     │
                   │   ┌──────────────┐                             │
                   │   │     Cart     │                             │
                   │   ├──────────────┤                             │
                   │   │ Id (PK)      │                             │
                   │   │ UserId (FK)  │                             │
                   │   │ CreatedAt    │                             │
                   │   └──────────────┘                             │
                   │          │                                     │
                   │          │ 1:N                                 │
                   │          ▼                                     │
                   │   ┌──────────────┐                             │
                   │   │  CartItem    │                             │
                   │   ├──────────────┤                             │
                   │   │ Id (PK)      │                             │
                   │   │ CartId (FK)  │                             │
                   │   │ VariantId(FK)│                             │
                   │   │ Quantity     │                             │
                   │   └──────────────┘                             │
                   │                                                │
                   │   ┌──────────────┐       ┌──────────────┐     │
                   │   │  Wishlist    │       │ WishlistItem │     │
                   │   ├──────────────┤       ├──────────────┤     │
                   │   │ Id (PK)      │◄──┐   │ Id (PK)      │     │
                   │   │ UserId (FK)  │   │   │ WishlistId   │     │
                   │   └──────────────┘   │   │ VariantId(FK)│     │
                   │          │           │   └──────────────┘     │
                   │          │ 1:N       │                        │
                   │          └───────────┘                        │
                   │                                               │
                   │   ┌──────────────┐       ┌──────────────┐    │
                   │   │    Order     │       │  OrderItem   │    │
                   │   ├──────────────┤       ├──────────────┤    │
                   │   │ Id (PK)      │◄──┐   │ Id (PK)      │    │
                   │   │ UserId (FK)  │───┘   │ OrderId (FK) │    │
                   │   │ AddressId(FK)│───────│ VariantId(FK)│    │
                   │   │ OrderCode    │       │ Quantity     │    │
                   │   │ TotalAmount  │       │ Price        │    │
                   │   │ Status       │       └──────────────┘    │
                   │   │ Note         │                            │
                   │   │ CreatedAt    │       ┌──────────────┐    │
                   │   └──────────────┘       │OrderStatusHx │    │
                   │          │               ├──────────────┤    │
                   │          │ 1:N           │ Id (PK)      │    │
                   │          └───────────────│ OrderId (FK) │    │
                   │                          │ Status       │    │
                   │   ┌──────────────┐       │ Note         │    │
                   │   │   Payment    │       │ CreatedAt    │    │
                   │   ├──────────────┤       └──────────────┘    │
                   │   │ Id (PK)      │                            │
                   │   │ OrderId (FK) │                            │
                   │   │ Method       │                            │
                   │   │ Amount       │                            │
                   │   │ Status       │                            │
                   │   │ TransactionId│                            │
                   │   │ CreatedAt    │                            │
                   │   └──────────────┘                            │
                   │                                               │
└──────────────────┘                                               │
                                                                   │
┌──────────────┐       ┌──────────────┐       ┌──────────────┐    │
│   Category   │       │    Brand     │       │   Product    │    │
├──────────────┤       ├──────────────┤       ├──────────────┤    │
│ Id (PK)      │◄──┐   │ Id (PK)      │◄──┐   │ Id (PK)      │────┘
│ Name         │   │   │ Name         │   │   │ Name         │
│ Slug         │   │   │ Slug         │   │   │ Slug         │
│ ParentId(FK) │───┘   │ Logo         │   │   │ Description  │
│ IsActive     │       │ IsActive     │   │   │ CategoryId(FK)│
│ SortOrder    │       └──────────────┘   │   │ BrandId (FK) │
│ CreatedAt    │                          │   │ IsActive     │
└──────────────┘                          │   │ IsFeatured   │
                                          │   │ CreatedAt    │
                                          │   └──────────────┘
                                          │          │
                                          │          │ 1:N
                                          │          ▼
                                          │   ┌──────────────┐
                                          │   │ProductVariant│
                                          │   ├──────────────┤
                                          │   │ Id (PK)      │
                                          │   │ ProductId(FK)│
                                          │   │ SKU          │
                                          │   │ Name         │
                                          │   │ Price        │
                                          │   │ Stock        │
                                          │   │ IsActive     │
                                          │   └──────────────┘
                                          │          │
                                          │          │ 1:N
                                          │          ▼
                                          │   ┌──────────────┐
                                          │   │ ProductImage │
                                          │   ├──────────────┤
                                          │   │ Id (PK)      │
                                          │   │ ProductId(FK)│
                                          │   │ Url          │
                                          │   │ AltText      │
                                          │   │ SortOrder    │
                                          │   │ IsPrimary    │
                                          │   └──────────────┘
                                          │
                                          │   ┌──────────────┐
                                          │   │    Review    │
                                          │   ├──────────────┤
                                          │   │ Id (PK)      │
                                          │   │ UserId (FK)  │
                                          │   │ ProductId(FK)│
                                          │   │ OrderItemId  │
                                          │   │ Rating       │
                                          │   │ Comment      │
                                          │   │ IsApproved   │
                                          │   │ CreatedAt    │
                                          │   └──────────────┘
                                          │          │
                                          │          │ 1:N
                                          │          ▼
                                          │   ┌──────────────┐
                                          │   │ ReviewImage  │
                                          │   ├──────────────┤
                                          │   │ Id (PK)      │
                                          │   │ ReviewId(FK) │
                                          │   │ Url          │
                                          │   └──────────────┘
                                          │
                                          └──────────────────
```

---

## Database Schema (MySQL)

### 1. Users & Authentication

```sql
CREATE TABLE Roles (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255)
);

CREATE TABLE Users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Phone VARCHAR(20),
    RoleId INT NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

CREATE TABLE Addresses (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    Street VARCHAR(255) NOT NULL,
    Ward VARCHAR(100),
    District VARCHAR(100) NOT NULL,
    City VARCHAR(100) NOT NULL,
    IsDefault BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### 2. Product Catalog

```sql
CREATE TABLE Categories (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    ParentId INT,
    IsActive BOOLEAN DEFAULT TRUE,
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ParentId) REFERENCES Categories(Id)
);

CREATE TABLE Brands (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100) NOT NULL,
    Slug VARCHAR(100) NOT NULL UNIQUE,
    Logo VARCHAR(255),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Products (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(255) NOT NULL,
    Slug VARCHAR(255) NOT NULL UNIQUE,
    Description TEXT,
    CategoryId INT NOT NULL,
    BrandId INT,
    IsActive BOOLEAN DEFAULT TRUE,
    IsFeatured BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    FOREIGN KEY (BrandId) REFERENCES Brands(Id)
);

CREATE TABLE ProductVariants (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ProductId INT NOT NULL,
    SKU VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(255) NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

CREATE TABLE ProductImages (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ProductId INT NOT NULL,
    Url VARCHAR(500) NOT NULL,
    AltText VARCHAR(255),
    SortOrder INT DEFAULT 0,
    IsPrimary BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);
```

### 3. Cart & Wishlist

```sql
CREATE TABLE Carts (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL UNIQUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE CartItems (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    CartId INT NOT NULL,
    VariantId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CartId) REFERENCES Carts(Id) ON DELETE CASCADE,
    FOREIGN KEY (VariantId) REFERENCES ProductVariants(Id),
    UNIQUE(CartId, VariantId)
);

CREATE TABLE Wishlists (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL UNIQUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE WishlistItems (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    WishlistId INT NOT NULL,
    VariantId INT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WishlistId) REFERENCES Wishlists(Id) ON DELETE CASCADE,
    FOREIGN KEY (VariantId) REFERENCES ProductVariants(Id),
    UNIQUE(WishlistId, VariantId)
);
```

### 4. Orders & Payments

```sql
CREATE TABLE Orders (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    AddressId INT NOT NULL,
    OrderCode VARCHAR(50) NOT NULL UNIQUE,
    TotalAmount DECIMAL(12,2) NOT NULL,
    Status ENUM('Pending','Confirmed','Processing','Shipping','Delivered','Cancelled') DEFAULT 'Pending',
    Note TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (AddressId) REFERENCES Addresses(Id)
);

CREATE TABLE OrderItems (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    OrderId INT NOT NULL,
    VariantId INT NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(12,2) NOT NULL,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    FOREIGN KEY (VariantId) REFERENCES ProductVariants(Id)
);

CREATE TABLE OrderStatusHistory (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    OrderId INT NOT NULL,
    Status VARCHAR(50) NOT NULL,
    Note TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE
);

CREATE TABLE Payments (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    OrderId INT NOT NULL,
    Method ENUM('COD','VNPay') NOT NULL,
    Amount DECIMAL(12,2) NOT NULL,
    Status ENUM('Pending','Completed','Failed') DEFAULT 'Pending',
    TransactionId VARCHAR(255),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);
```

### 5. Reviews

```sql
CREATE TABLE Reviews (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    OrderItemId INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment TEXT,
    IsApproved BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    UNIQUE(UserId, OrderItemId)
);

CREATE TABLE ReviewImages (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    ReviewId INT NOT NULL,
    Url VARCHAR(500) NOT NULL,
    FOREIGN KEY (ReviewId) REFERENCES Reviews(Id) ON DELETE CASCADE
);
```

---

## Backend Structure (ASP.NET Core)

```
backend/
├── TechHub.API/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ProductController.cs
│   │   ├── CategoryController.cs
│   │   ├── BrandController.cs
│   │   ├── CartController.cs
│   │   ├── WishlistController.cs
│   │   ├── CheckoutController.cs
│   │   ├── OrderController.cs
│   │   ├── PaymentController.cs
│   │   ├── ReviewController.cs
│   │   ├── DashboardController.cs
│   │   └── UserController.cs
│   ├── Program.cs
│   └── appsettings.json
├── TechHub.Application/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IProductService.cs
│   │   ├── ICartService.cs
│   │   ├── IOrderService.cs
│   │   ├── IPaymentService.cs
│   │   ├── IReviewService.cs
│   │   ├── IDashboardService.cs
│   │   └── IUserService.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── ProductService.cs
│   │   ├── CartService.cs
│   │   ├── OrderService.cs
│   │   ├── PaymentService.cs
│   │   ├── ReviewService.cs
│   │   ├── DashboardService.cs
│   │   └── UserService.cs
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── Product/
│   │   ├── Cart/
│   │   ├── Order/
│   │   ├── Payment/
│   │   ├── Review/
│   │   ├── Dashboard/
│   │   └── User/
│   ├── Validators/
│   │   └── [Dto]Validator.cs (FluentValidation)
│   └── Mappings/
│       └── MappingProfile.cs (AutoMapper)
├── TechHub.Infrastructure/
│   ├── Data/
│   │   ├── TechHubDbContext.cs
│   │   └── Configurations/ (EF Core Fluent API)
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   ├── ProductRepository.cs
│   │   ├── CartRepository.cs
│   │   ├── OrderRepository.cs
│   │   └── ...
│   └── Migrations/
└── TechHub.Domain/
    ├── Entities/
    │   ├── User.cs
    │   ├── Role.cs
    │   ├── Product.cs
    │   ├── ProductVariant.cs
    │   ├── ProductImage.cs
    │   ├── Category.cs
    │   ├── Brand.cs
    │   ├── Cart.cs
    │   ├── CartItem.cs
    │   ├── Wishlist.cs
    │   ├── WishlistItem.cs
    │   ├── Order.cs
    │   ├── OrderItem.cs
    │   ├── OrderStatusHistory.cs
    │   ├── Address.cs
    │   ├── Payment.cs
    │   ├── Review.cs
    │   └── ReviewImage.cs
    └── Enums/
        ├── OrderStatus.cs
        ├── PaymentMethod.cs
        └── PaymentStatus.cs
```

---

## Frontend Structure (Angular)

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── guards/
│   │   │   │   ├── auth.guard.ts
│   │   │   │   └── admin.guard.ts
│   │   │   ├── interceptors/
│   │   │   │   └── jwt.interceptor.ts
│   │   │   ├── services/
│   │   │   │   ├── auth.service.ts
│   │   │   │   └── storage.service.ts
│   │   │   └── models/
│   │   │       ├── user.model.ts
│   │   │       └── auth.model.ts
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── header/
│   │   │   │   ├── footer/
│   │   │   │   └── loading/
│   │   │   └── pipes/
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── login/
│   │   │   │   ├── register/
│   │   │   │   └── forgot-password/
│   │   │   ├── products/
│   │   │   │   ├── product-list/
│   │   │   │   ├── product-detail/
│   │   │   │   └── product-search/
│   │   │   ├── cart/
│   │   │   │   └── cart.component.ts
│   │   │   ├── wishlist/
│   │   │   │   └── wishlist.component.ts
│   │   │   ├── checkout/
│   │   │   │   └── checkout.component.ts
│   │   │   ├── orders/
│   │   │   │   ├── order-list/
│   │   │   │   └── order-detail/
│   │   │   ├── reviews/
│   │   │   │   └── review-form/
│   │   │   ├── account/
│   │   │   │   ├── profile/
│   │   │   │   └── addresses/
│   │   │   └── admin/
│   │   │       ├── dashboard/
│   │   │       ├── products/
│   │   │       ├── orders/
│   │   │       ├── users/
│   │   │       └── reviews/
│   │   ├── app.module.ts
│   │   ├── app-routing.module.ts
│   │   └── app.component.ts
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   └── assets/
└── angular.json
```

---

## Module Build Order (Final)

```
Phase 1: Foundation (Base modules)
├── 1.1 Authentication (User, Role, JWT)
└── 1.2 Catalog & Search (Category, Brand)

Phase 2: Product (Depends on Phase 1)
└── 2.1 Product Management (Product, Variant, Image)

Phase 3: Shopping (Depends on Phase 2)
├── 3.1 Shopping Cart
└── 3.2 Wishlist

Phase 4: Order Flow (Depends on Phase 3)
├── 4.1 Checkout
├── 4.2 Order Management
└── 4.3 Online Payment

Phase 5: Engagement (Depends on Phase 2, 4)
└── 5.1 Review & Rating

Phase 6: Admin & Analytics (Depends on all)
├── 6.1 User & Account Management
└── 6.2 Dashboard
```

---

## Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Layered (Controller → Service → Repository) | Separation of concerns, testability |
| ORM | Entity Framework Core | Reduce boilerplate, migrations support |
| Authentication | JWT (stateless) | API-first, scalable |
| Password Hashing | BCrypt | Industry standard, salt auto-generated |
| Stock Management | Per Variant (not per Product) | BR-01 requirement |
| Order Status | Enum with history table | Track state transitions |
| Review Approval | Admin must approve | BR-07 requirement |

---

## Gate 2 Check

- [x] ERD covers all entities from REQUIREMENT_DIGEST
- [x] No circular dependencies in module build order
- [x] All API endpoints defined
- [x] Business rules mapped to implementation
- [x] Backend/Frontend structure defined

**Status: APPROVED** → `phase = "architecture_approved"`

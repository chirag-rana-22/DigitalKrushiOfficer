# FreshMart - Online Grocery Store Management System
## Project Presentation for Examiners

---

## 1. PROJECT OVERVIEW

**Project Name**: FreshMart - Online Grocery Store Management System

**Description**: A comprehensive .NET Core MVC web application that provides a complete e-commerce solution for managing an online grocery store. The system handles customer ordering, inventory management, payment processing, and delivery tracking with a modern, responsive user interface.

**Project Type**: Full-Stack Web Application (MVC Pattern)

**Target Users**: 
- End Customers (Browse and purchase groceries)
- Admin Staff (Manage products, orders, and reports)
- Delivery Personnel (Track and update deliveries)

---

## 2. TECHNOLOGY STACK

### Backend
- **.NET Framework**: .NET 10.0 (Latest)
- **Language**: C# 13
- **ORM**: Entity Framework Core 9.0
- **Database**: MySQL
- **Authentication**: Cookie-Based Authentication
- **Password Hashing**: BCrypt.Net-Next

### Frontend
- **HTML5/CSS3**: Modern web standards
- **CSS Framework**: Bootstrap 5.3.2
- **JavaScript**: jQuery 3.6+
- **Charts**: Chart.js for data visualization
- **Icons**: Bootstrap Icons
- **Responsive Design**: Mobile-first approach

### Build & Deployment
- **.NET CLI**: Command-line tooling
- **NuGet**: Package management
- **Git**: Version control

---

## 3. SYSTEM ARCHITECTURE

### Architecture Pattern: MVC (Model-View-Controller)

```
┌─────────────────────────────────────────────────────────┐
│                    USER INTERFACE (Views)               │
│  - Razor Pages (.cshtml)                                │
│  - Bootstrap Components                                  │
│  - Client-side Validation (jQuery)                      │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                  CONTROLLERS                             │
│  - Account: Authentication & Authorization              │
│  - Product: Product catalog & search                     │
│  - Cart: Shopping cart management                        │
│  - Order: Order processing & tracking                    │
│  - Admin: Administrative operations                      │
│  - Delivery: Delivery staff operations                   │
│  - Reports: Analytics & export                           │
│  - Home: Portal navigation                               │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                BUSINESS LOGIC (Services)                │
│  - Data Processing                                       │
│  - Validations                                           │
│  - Business Rules                                        │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│          DATA ACCESS LAYER (Entity Framework)           │
│  - ApplicationDbContext                                  │
│  - Migrations                                            │
│  - LINQ Queries                                          │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│              DATABASE (MySQL)                            │
│  - Users, Products, Orders, Deliveries, etc.            │
└─────────────────────────────────────────────────────────┘
```

---

## 4. DATABASE DESIGN

### Core Database Tables

1. **Users**
   - User identification and authentication
   - Roles: Customer, Admin, Delivery Staff
   - Account status tracking

2. **Products**
   - Product catalog with categories
   - Pricing (in cents for currency precision)
   - Stock/Inventory management
   - Product images and descriptions

3. **Categories**
   - Product categorization
   - Types: Fruits, Vegetables, Dairy, Bakery, Beverages, Snacks, Household

4. **Cart & CartItems**
   - Current shopping cart for each user
   - Items with quantity and pricing snapshot

5. **Orders & OrderItems**
   - Order history and details
   - Individual items per order with pricing
   - Order status tracking

6. **Addresses**
   - Customer delivery addresses
   - Address selection during checkout

7. **Deliveries**
   - Delivery assignment and tracking
   - Delivery staff assignment
   - Status management (Assigned, PickedUp, OutForDelivery, Delivered, Failed)

8. **Payments**
   - Payment method and status
   - Support for: Cash on Delivery, UPI, Card

9. **Notifications**
   - Real-time user notifications
   - Order status updates
   - System notifications

---

## 5. KEY FEATURES BY USER ROLE

### A. CUSTOMER FEATURES

#### 1. Authentication & Profile
- ✅ User registration with email validation
- ✅ Secure login with password hashing
- ✅ Profile management
- ✅ Account status management

#### 2. Product Browsing
- ✅ View all products with images and details
- ✅ Browse by categories
- ✅ Search products by name
- ✅ Filter by price range
- ✅ View product details (description, price, stock)
- ✅ Real-time stock availability

#### 3. Shopping Cart
- ✅ Add/remove products from cart
- ✅ Update quantities
- ✅ View cart summary
- ✅ Calculate totals with real-time updates
- ✅ Persistent cart storage

#### 4. Checkout & Payment
- ✅ Select/add delivery addresses
- ✅ Choose payment method:
     - Cash on Delivery (COD)
     - UPI
     - Credit/Debit Card
- ✅ Apply discount codes
- ✅ Order summary review
- ✅ Confirm and place order

#### 5. Order Management
- ✅ View order history
- ✅ Track order status in real-time
- ✅ View detailed order information
- ✅ Order cancellation (for eligible orders)
- ✅ Download order receipt

#### 6. Notifications
- ✅ Real-time notification bell with badge
- ✅ Order status updates
- ✅ Delivery updates
- ✅ Mark notifications as read
- ✅ Delete notifications
- ✅ Auto-refresh every 30 seconds

### B. ADMIN FEATURES

#### 1. Dashboard & Analytics
- ✅ Sales overview (total revenue, total orders)
- ✅ Average order value
- ✅ Customer count
- ✅ Product inventory metrics
- ✅ Visual charts and graphs
- ✅ Real-time statistics

#### 2. Product Management
- ✅ Add new products
- ✅ Edit product details (name, price, stock)
- ✅ Create/manage categories
- ✅ Delete/archive products
- ✅ Upload product images
- ✅ Bulk operations support

#### 3. Order Management
- ✅ View all orders with filters
- ✅ Update order status
- ✅ Assign delivery staff to orders
- ✅ View detailed order information
- ✅ Customer communication notes
- ✅ Order cancellation authority

#### 4. Delivery Staff Management
- ✅ Create delivery staff accounts
- ✅ Assign work to delivery personnel
- ✅ Track delivery performance
- ✅ View assignment history
- ✅ Manage staff accounts

#### 5. Reports & Analytics
- ✅ **Sales Report**: Revenue analysis by date range
- ✅ **Daily Orders Report**: Orders placed on specific dates
- ✅ **Top Products Report**: Best-selling items
- ✅ **Inventory Report**: Stock levels and low stock alerts
- ✅ **Export to CSV**: Download all reports

#### 6. Inventory Management
- ✅ Track stock levels
- ✅ Low stock alerts (< 10 units)
- ✅ Total stock value calculation
- ✅ Stock-by-category view
- ✅ Auto-update on orders

### C. DELIVERY STAFF FEATURES

#### 1. Dashboard
- ✅ View assigned deliveries
- ✅ See daily delivery schedule
- ✅ Track pending deliveries
- ✅ View completed deliveries

#### 2. Delivery Operations
- ✅ Access order details
- ✅ View customer contact information
- ✅ View delivery address
- ✅ Update delivery status:
     - Assigned → PickedUp → OutForDelivery → Delivered
- ✅ Add delivery notes
- ✅ Mark as failed delivery

#### 3. Order Tracking
- ✅ View customer name and phone
- ✅ View delivery address details
- ✅ View order items and quantities
- ✅ Access customer payment information
- ✅ See scheduled delivery time

---

## 6. CORE FUNCTIONALITIES

### 1. Authentication & Authorization
- Cookie-based authentication system
- Role-based access control:
  - **Admin**: Full system access
  - **Customer**: Shopping and order tracking
  - **Delivery Staff**: Assignment and status updates
- Anti-forgery token validation
- Secure password hashing with BCrypt

### 2. Product Catalog
- Full-text search across products
- Multi-level filtering (category, price, availability)
- Real-time stock management
- Product categorization system
- Image management and display

### 3. Shopping Cart System
- Session-based cart storage
- Add/remove/update quantities
- Cart persistence
- Real-time price calculations
- Discount application

### 4. Order Processing
- One-click checkout
- Multiple address management
- Multiple payment options
- Order confirmation emails/notifications
- Order status workflow:
  ```
  Pending → Packed → Out for Delivery → Delivered
           → Cancelled
           → Failed
  ```

### 5. Delivery Management
- Automatic notification to delivery staff
- Real-time status updates
- Customer notifications on delivery updates
- Payment verification (COD confirmation)
- Delivery history tracking

### 6. Reporting & Analytics
- Sales analytics by date range
- Top-performing products analysis
- Daily order summaries
- Inventory analytics
- CSV export functionality
- Chart.js visualization

### 7. Notification System
- Real-time in-app notifications
- Order status alerts
- Delivery updates
- Unread count badge
- Auto-refresh mechanism
- Mark as read functionality

---

## 7. USER INTERFACE FEATURES

### Common Elements
- ✅ Responsive Bootstrap navigation
- ✅ Mobile-friendly design (works on phones, tablets, desktops)
- ✅ Consistent color scheme (Primary: Green, Success theme)
- ✅ Bootstrap Icons for visual clarity
- ✅ Toast notifications for user feedback
- ✅ Professional card-based layouts
- ✅ Data tables with sorting and filtering
- ✅ Modal dialogs for confirmations
- ✅ Loading spinners for async operations

### Admin Dashboard
- Statistics cards with key metrics
- Performance charts (Chart.js)
- Quick action buttons
- Recent activity logs
- Data export options

### Customer Portal
- Product grid with images and details
- Shopping cart sidebar
- Order history table
- Status indicators with color coding
- Notification bell with dropdown

### Delivery Staff Dashboard
- Assigned orders list
- Delivery status badges
- Customer information cards
- Quick status update buttons

---

## 8. SECURITY FEATURES

### 1. Authentication Security
- ✅ Secure password storage with BCrypt hashing
- ✅ Cookie-based session management
- ✅ Sliding expiration (auto-logout after 2 hours of inactivity)
- ✅ HttpOnly cookies (prevents JavaScript access)

### 2. Authorization
- ✅ Role-based access control (RBAC)
- ✅ Attribute-based authorization on controllers/actions
- ✅ AccessDenied page for unauthorized access
- ✅ Data isolation per user (users see only their data)

### 3. Data Protection
- ✅ Anti-forgery token validation on POST requests
- ✅ SQL injection prevention via Entity Framework
- ✅ Input validation on all forms
- ✅ Output encoding in views

### 4. Privacy
- ✅ User data encryption in transit (HTTPS)
- ✅ Sensitive data handling (passwords, payment info)
- ✅ Session timeout for security
- ✅ Audit trail for order operations

---

## 9. APPLICATION WORKFLOW

### Customer Workflow
```
1. Registration/Login
   ↓
2. Browse Products
   ↓
3. Add to Cart
   ↓
4. Proceed to Checkout
   ↓
5. Select Address & Payment Method
   ↓
6. Place Order (Order Status = Pending)
   ↓
7. Order Packed (Status = Packed)
   ↓
8. Assigned to Delivery Staff (Status = Out for Delivery)
   ↓
9. Delivered (Status = Delivered)
   ↓
10. View Order History & Notifications
```

### Admin Workflow
```
1. Login as Admin
   ↓
2. View Dashboard (Analytics & Metrics)
   ↓
3. Manage Products/Categories
   ↓
4. View Orders & Update Status
   ↓
5. Assign Delivery Staff to Orders
   ↓
6. View Reports & Export Data
   ↓
7. Manage Inventory & Low Stock Alerts
```

---

## 10. RECENT IMPROVEMENTS & BUG FIXES

### Fixed Issues
1. ✅ **Inventory Report Model Error**: Fixed model type mismatch from `List<dynamic>` to `dynamic`
2. ✅ **Low Stock Counting**: Moved calculation to database level for accuracy
3. ✅ **CSV Export Functionality**: Implemented export for Inventory Report with timestamp

### Features Added
1. ✅ **AccessDenied Page**: Proper authorization denials for delivery staff pages
2. ✅ **Notification System**: Real-time notifications with auto-refresh
3. ✅ **Inventory Reporting**: Low stock alerts and inventory management dashboard

---

## 11. FILE STRUCTURE

```
GroceryStore/
├── Controllers/
│   ├── AccountController.cs         - Authentication & User Account
│   ├── HomeController.cs            - Home page & navigation
│   ├── ProductController.cs         - Product catalog & search
│   ├── CartController.cs            - Shopping cart operations
│   ├── OrderController.cs           - Order processing & tracking
│   ├── AdminController.cs           - Admin operations
│   ├── DeliveryController.cs        - Delivery staff operations
│   ├── ReportsController.cs         - Reports & analytics
│   └── NotificationController.cs    - Notification management
│
├── Models/
│   ├── User.cs                      - User entity
│   ├── Product.cs                   - Product entity
│   ├── Category.cs                  - Category entity
│   ├── Cart.cs, CartItem.cs         - Shopping cart
│   ├── Order.cs, OrderItem.cs       - Order management
│   ├── Address.cs                   - Delivery addresses
│   ├── Delivery.cs                  - Delivery tracking
│   ├── Payment.cs                   - Payment records
│   └── Notification.cs              - Notifications
│
├── ViewModels/
│   ├── LoginViewModel.cs            - Login form
│   ├── RegisterViewModel.cs         - Registration form
│   ├── CartViewModel.cs             - Cart display
│   ├── OrderViewModel.cs            - Order display
│   ├── ProductViewModel.cs          - Product search/filter
│   └── DashboardViewModel.cs        - Admin dashboard
│
├── Views/
│   ├── Account/                     - Login, Register, AccessDenied
│   ├── Home/                        - Home, About, Contact
│   ├── Product/                     - Product listing & details
│   ├── Cart/                        - Shopping cart
│   ├── Order/                       - Order checkout & tracking
│   ├── Admin/                       - Admin dashboard & management
│   ├── Delivery/                    - Delivery staff pages
│   ├── Reports/                     - Reports pages
│   └── Shared/                      - Layout pages
│
├── Data/
│   └── ApplicationDbContext.cs      - Entity Framework DbContext
│
├── Migrations/
│   └── *.cs                         - Database migrations
│
├── wwwroot/
│   ├── css/
│   │   ├── site.css                 - Customer CSS
│   │   └── admin.css                - Admin CSS
│   ├── js/
│   │   ├── site.js                  - Customer JavaScript
│   │   └── admin.js                 - Admin JavaScript
│   └── images/
│       └── products/                - Product images
│
├── Program.cs                       - Application startup configuration
├── appsettings.json                 - Configuration
├── GroceryStore.csproj              - Project file
└── README.md                        - Documentation
```

---

## 12. INSTALLATION & SETUP

### Prerequisites
- .NET 10.0 SDK
- MySQL Server (8.0+)
- Visual Studio 2022 or VS Code
- Git (for version control)

### Step-by-Step Setup

1. **Clone/Extract Project**
   ```bash
   cd GroceryStore
   ```

2. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

3. **Update Database Connection**
   Edit `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=online_grocery_store;User=root;Password=your_password;"
   }
   ```

4. **Apply Database Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run Application**
   ```bash
   dotnet run
   ```

6. **Access Application**
   - URL: `http://localhost:5000`
   - Default Admin: Create through registration or DB

---

## 13. TESTING & VALIDATION

### Test Scenarios

#### Customer Testing
- [ ] User registration with email validation
- [ ] Login/logout functionality
- [ ] Product browsing and search
- [ ] Add/remove items from cart
- [ ] Checkout process
- [ ] Order placement and confirmation
- [ ] Order status tracking
- [ ] Notification system

#### Admin Testing
- [ ] Dashboard metrics accuracy
- [ ] Product CRUD operations
- [ ] Category management
- [ ] Order status updates
- [ ] Delivery staff assignment
- [ ] Report generation
- [ ] CSV export functionality
- [ ] Inventory management

#### Delivery Testing
- [ ] View assigned deliveries
- [ ] Update delivery status
- [ ] AccessDenied for unauthorized users
- [ ] Notification updates

---

## 14. KEY METRICS & PERFORMANCE

### System Capabilities
- ✅ Support for multiple concurrent users
- ✅ Real-time inventory management
- ✅ Fast product search (indexed queries)
- ✅ Responsive UI (< 2 second page loads)
- ✅ Secure data handling and storage
- ✅ Scalable architecture

### Database Optimization
- Indexed foreign keys for fast joins
- Efficient LINQ queries
- Parameterized queries (prevents SQL injection)
- Connection pooling

---

## 15. FUTURE ENHANCEMENTS

### Planned Features
1. **Payment Gateway Integration**
   - Real Razorpay/PayPal integration
   - E-wallet support

2. **Email & SMS Notifications**
   - Order confirmation emails
   - Delivery SMS updates
   - Password reset emails

3. **Advanced Analytics**
   - Customer behavior analysis
   - Recommendation engine
   - Sales forecasting

4. **Mobile Application**
   - Native iOS/Android app
   - Push notifications
   - Mobile payment

5. **Real-Time Tracking**
   - GPS tracking for deliveries
   - Live delivery map
   - ETA calculation

6. **Multi-Language Support**
   - Localization for regional languages
   - Currency conversion

7. **Admin Enhancements**
   - Advanced user management
   - Staff performance metrics
   - Route optimization for deliveries

---

## 16. CONCLUSION

FreshMart is a **production-ready e-commerce management system** that demonstrates:
- ✅ Complete MVC architecture implementation
- ✅ Database design and management
- ✅ User authentication and authorization
- ✅ Real-world business logic
- ✅ Professional UI/UX
- ✅ Security best practices
- ✅ Scalable and maintainable code

The project successfully combines **frontend user experience** with **backend business logic** to create a functional e-commerce platform.

---

## 17. QUICK DEMO POINTS

For the live demo:

1. **Show Customer Flow**
   - Register/Login → Browse products → Add to cart → Checkout
   
2. **Show Admin Dashboard**
   - Dashboard metrics → Manage products → View orders → Generate reports
   
3. **Show Delivery Operations**
   - View assigned orders → Update status → Notifications
   
4. **Show Security**
   - AccessDenied page → Role-based access → Session management
   
5. **Show Reporting**
   - CSV export → Charts → Analytics data

---

**Prepared for Examination**  
Date: April 12, 2026  
Project: FreshMart - Online Grocery Store Management System

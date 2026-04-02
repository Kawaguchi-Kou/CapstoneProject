# TravelPlanner Backend (Capstone SP26)

Tài liệu này là **nguồn mô tả hệ thống tổng hợp** để agent/DEV nắm nhanh kiến trúc, domain, API và các quy ước hiện tại của codebase.

## 1) Tổng quan hệ thống

TravelPlanner Backend là hệ thống .NET 8 theo hướng layered architecture, cung cấp:
- Xác thực tài khoản (JWT + Refresh Token + OTP Email)
- Quản lý user/profile/preference
- Quản lý POI (Point of Interest), location
- Lập kế hoạch chuyến đi (trip/segment/itinerary)
- Quảng cáo và gói subscription quảng cáo
- Thanh toán (SePay webhook flow)
- Promotions & saved promotions
- Theo dõi rủi ro thời tiết + thông báo
- Realtime notification qua SignalR
- Tác vụ nền định kỳ bằng Hangfire

---

## 2) Cấu trúc solution

Solution: `TravelPlanner.sln`

### Projects
- `WebAPI`  
  Entry point HTTP API, cấu hình DI/Auth/CORS/Swagger/Hangfire/SignalR.
- `Application`  
  Business services, DTOs, interfaces, mapping profiles, hub contracts, weather risk logic.
- `Domain`  
  Entities + enums + domain interfaces.
- `Infrastructure`  
  EF Core `DbContext`, entity configurations, repositories, migrations, external API integrations (OpenMeteo, SePay, Mapbox), background jobs.
- `McpServer`  
  Hiện tại chưa triển khai nghiệp vụ (program mẫu).

---

## 3) Runtime stack & thư viện chính

- .NET 8 (`net8.0`)
- ASP.NET Core Web API
- EF Core + PostgreSQL (Npgsql, Supabase)
- JWT Bearer Authentication
- Swagger/OpenAPI
- Hangfire + Hangfire.PostgreSql
- SignalR
- AutoMapper
- EPPlus (import/export excel)
- DotNetEnv

---

## 4) Kiến trúc & luồng phụ thuộc

Mô hình phụ thuộc tổng quát:

- `WebAPI` -> gọi `Application` interfaces/services
- `Application` -> dùng `Domain` abstractions + DTO mapping
- `Infrastructure` -> implement repository/external services + EF Core persistence
- `Domain` -> thuần model/enums/business types

### DI container (đăng ký trong `WebAPI/Program.cs`)

Nhóm service chính đã đăng ký:
- Auth: `IAuthService`, `IRefreshTokenService`, `IEmailService`
- User/Profile: `IUserService`, `ICloudinaryService`
- Preference: `IPreferenceService`
- POI: `IPOIService`
- Ads + Subscription + Payment: `IAdvertisementService`, `IAdSubscriptionPackageService`, `IAccountSubscriptionService`, `IPaymentService`
- Trip/Planner: `ITripService`, `ITripQueryService`, `ITripSegmentService`
- Notification: `INotificationService`, `INotificationRecipientService`
- Weather: `IOpenMeteoService`, `IAdaptiveWeatherRiskEngine`, `IWeatherRiskScanService`, `IWeatherMonitorJob`
- External: `ISePayService`, `IGeocodingService (Mapbox)`

Repositories tương ứng được inject đầy đủ ở Infrastructure.

---

## 5) Hạ tầng kỹ thuật runtime

### Authentication & Authorization
- JWT Bearer auth.
- Claim mapping:
  - `NameClaimType = ClaimTypes.NameIdentifier`
  - `RoleClaimType = ClaimTypes.Role`
- SignalR hub hỗ trợ nhận token qua query `access_token` tại path `/hubs/notification`.

### CORS
- Policy `AllowSpecificOrigins`
- Allowed origin mặc định: `http://localhost:3000`

### Swagger
- Endpoint: `/swagger`
- Có khai báo security scheme `Bearer`.

### Hangfire
- Dashboard: `/hangfire`
- Recurring job:
  - `weather-hourly-scan` chạy `IWeatherMonitorJob.ScanUpcomingTripsAsync()` theo `Cron.Hourly`.

### SignalR
- Hub endpoint: `/hubs/notification`
- Custom user id provider: `CustomUserIdProvider`

---

## 6) Domain model (Entity summary)

### Auth & Account
- `Account`: thông tin định danh, role, profile fields, trạng thái kích hoạt.
- `Role`: role hệ thống.
- `RefreshToken`: refresh token theo account.
- `OtpVerification`: OTP cho đăng ký/quên mật khẩu.

### Trip Planning
- `Trip`: chuyến đi tổng.
- `TripSegment`: chặng trong chuyến.
- `Itinerary`: lịch trình sinh cho segment.
- `ItineraryDetail`: item chi tiết lịch trình, có thể gắn POI.
- `Location`: địa điểm địa lý (name/lat/lng).
- `Participant`: thành viên tham gia trip.

### POI & Preference
- `POI`: điểm tham quan (address/city/cost/time/status/partner/location).
- `Preference`: loại sở thích.
- `POIPreference`: bảng nối nhiều-nhiều POI <-> Preference.
- `UserPreference`: sở thích user.

### Advertisement & Commerce
- `AdSubscriptionPackage`: gói quảng cáo.
- `AccountSubscription`: subscription của account theo package.
- `Advertisement`: quảng cáo gắn POI.
- `AdPayment`: thanh toán cho subscription/package.
- `Promotion`: ưu đãi gắn 1-1 với advertisement.
- `SavedPromotion`: ưu đãi user lưu lại.

### Notification & Weather
- `Notification`: thông báo.
- `NotificationRecipient`: người nhận/read state.
- `WeatherForecast`: dữ liệu dự báo thời tiết theo location/city/date.

---

## 7) Enum nghiệp vụ chính

- `POIStatus`: `Pending`, `Rejected`, `Active`, `Inactive`
- `AdStatus`: `PendingApproval`, `Active`, `Paused`, `Expired`, `Rejected`
- `PromotionStatus`: `Pending`, `Active`, `Inactive`, `Rejected`, `Expired`
- `SubStatus`, `PaymentStatus`
- `TripStatus`: `InProgress`, `Completed`, `Cancelled`
- `TripType`: `RoundTrip`, `OneWay`
- `ParticipantRole`, `ParticipantStatus`, `NotificationType`, `SegmentType`

---

## 8) Database & persistence

- DbContext: `Infrastructure/EntitiesConfigurations/AppDbContext.cs`
- Provider: PostgreSQL
- Migrations: `Infrastructure/Migrations`
- Table naming dùng snake_case (vd: `accounts`, `trips`, `trip_segments`, `advertisements`, ...)

### Quan hệ đáng chú ý
- `Account` 1-n `RefreshToken`
- `Trip` 1-n `TripSegment`
- `TripSegment` 1-n `Itinerary`
- `Itinerary` 1-n `ItineraryDetail`
- `POI` 1-n `ItineraryDetail` (nullable FK ở detail)
- `POI` n-1 `Location`
- `POI` n-1 `Account` qua `PartnerId` (POI partner-owned)
- `Advertisement` n-1 `Account`, n-1 `POI`, n-1 nullable `AdSubscriptionPackage`
- `Promotion` 1-1 `Advertisement`
- `SavedPromotion` n-1 `Promotion`, n-1 `Account` + unique `(PromotionId, AccountId)`

---

## 9) API catalog (theo controller)

Base URL local dev (thường): `https://localhost:<port>` hoặc `http://localhost:<port>` theo launch profile.

### Auth (`/api/auth`)
- `POST /register`
- `POST /verify-register-otp`
- `GET /resend-register-otp`
- `POST /login`
- `POST /request-password-reset`
- `POST /verify-reset-password-otp`
- `POST /reset-password`
- `POST /change-password` (auth)
- `POST /refresh-token`
- `GET /me` (auth)
- `GET /account-by-email`
- `GET /all` (Admin)

### User (`/api/user`)
- `GET /{id}`
- `POST /batch`
- `GET /all`
- `PUT /update` (auth)
- `POST /update-preference` (auth)
- `GET /user-preferences` (auth)

### Preference (`/api/preferences`)
- `GET /get-all`

### POI public (`/api/pois`)
- `GET /recommended` (auth)

### Manager POI (`/api/manager/pois`)
- `GET /`
- `GET /{id}`
- `POST /`
- `PUT /{id}`
- `DELETE /{id}`
- `POST /import`
- `POST /upload-image` (Staff)

### Admin POI (`/api/admin/pois`)
- `GET /`
- `GET /{id}`
- `POST /`
- `PUT /{id}`
- `DELETE /{id}`

### Manager Location (`/api/manager/locations`)
- `GET /`
- `GET /{id}`
- `POST /`
- `PUT /{id}`
- `DELETE /{id}`
- `POST /import`

### Advertisement (`/api/advertisements`)
- `POST /`
- `GET /{id}`
- `GET /active`
- `GET /my-ads`
- `POST /{id}/approve` (Admin)
- `POST /{id}/reject` (Admin)
- `GET /` (Staff)
- `GET /pending` (Staff)

### Admin Advertisement (`/api/admin/advertisements`)
- `GET /pending/accounts`
- `GET /pending`

### Ad Subscription Package (`/api/ad-subscription-packages`)
- `POST /`
- `GET /{id}`
- `GET /`
- `GET /filter`
- `PUT /{id}`
- `DELETE /{id}`
- `PUT /{id}/activate`
- `PUT /{id}/deactivate`

### Account Subscription (`/api/account-subscriptions`)
- `POST /subscribe`
- `GET /my-subscription`
- `GET /my-subscriptions`

### Payment (`/api/payments`)
- `POST /create` (auth)
- `POST /webhook` (SePay callback)
- `GET /{id}` (auth)
- `GET /subscription/{subscriptionId}` (auth)

### Promotions (`/api/promotions`)
- `POST /{promotionId}/save`
- `GET /api/users/me/saved-promotions`

### Planner (`/api/planner`)
- `POST /{tripId}/generate` (auth)
- Có các endpoint preview/apply/update/delete itinerary detail đang comment (chưa bật).

### Trip (`/api/trip`)
- `POST /create` (auth)
- `POST /{tripId}/segments`

### Tools (MCP-like endpoints)
- `GET /tools` -> list tool schemas
- `POST /tools/get_weather`
- `POST /tools/get_coordinates`

### Geocode
- Controller route: `/Geocode` (`[Route("[controller]")]`)
- `GET /Geocode`

### WeatherForecast sample
- `GET /api/WeatherForecast`

---

## 10) Quy ước role/permission hiện có (từ attribute)

Các role xuất hiện trong code:
- `Admin`
- `Manager`
- `Staff`

Ngoài ra có logic domain liên quan `Partner` trong POI/ads checklist nhưng cần đối chiếu controller/service để thống nhất cuối cùng.

---

## 11) Tích hợp external services

- **OpenMeteo**: lấy dữ liệu thời tiết phục vụ planner/risk scan.
- **Mapbox Geocoding**: chuyển đổi địa danh -> tọa độ.
- **SePay**: tạo payment QR, nhận webhook đối soát giao dịch.
- **Cloudinary**: lưu trữ media (avatar, POI image, ad image/video url references).
- **SMTP (Gmail)**: gửi OTP/reset email.

---

## 12) Realtime & background processing

### Notification realtime
- Client kết nối SignalR `/hubs/notification`.
- JWT truyền qua query `access_token` khi handshake websocket.

### Background scan thời tiết
- Job chạy mỗi giờ.
- Mục tiêu: quét trip sắp tới và đánh giá weather risk để cảnh báo/notify.

---

## 13) Cấu hình môi trường

Các section config hiện dùng:
- `ConnectionStrings:Supabase`
- `JwtSettings`
- `ResetJwt`
- `CloudinarySettings`
- `EmailSettings`
- `OpenMeteo`
- `SePay`
- `ClientUrl`
- `TimeZoneId`

Khuyến nghị vận hành:
- Không commit secrets thật trong `appsettings.json`.
- Chuyển secrets sang `.env`, user-secrets hoặc secret manager (Azure KeyVault/AWS/GCP).

---

## 14) Chạy dự án local

### Prerequisites
- .NET SDK 8+
- PostgreSQL/Supabase database khả dụng

### Steps
1. Cập nhật cấu hình trong `WebAPI/appsettings.json` hoặc biến môi trường.
2. Chạy migration DB (nếu cần):
   - `dotnet ef database update --project Infrastructure --startup-project WebAPI`
3. Chạy API:
   - `dotnet run --project WebAPI`
4. Mở Swagger:
   - `/swagger`
5. Hangfire dashboard:
   - `/hangfire`

---

## 15) Ghi chú cho agent khi làm việc trên repo

1. **Ưu tiên đọc**:
   - `WebAPI/Program.cs` (composition root)
   - `Infrastructure/EntitiesConfigurations/AppDbContext.cs` (schema + relationships)
   - `WebAPI/Controllers/*` (API contract hiện hữu)
   - `Application/Interfaces` + `Application/Services` (business flow)

2. **Khi thay đổi API**:
   - Cập nhật controller + service + repo đồng bộ.
   - Kiểm tra role authorize và response codes.
   - Cập nhật README section API catalog này.

3. **Khi thay đổi entity/mapping**:
   - Cập nhật Domain entity + DbContext mapping.
   - Tạo migration mới.
   - Kiểm tra tác động tới DTO/AutoMapper/service.

4. **POI & Ads roadmap**:
   - Xem `POI_ADS_IMPLEMENTATION_CHECKLIST.md` để biết các rule/endpoint còn pending và business constraints đã chốt.

---

## 16) Known gaps / technical debt (hiện trạng)

- `McpServer` chưa có triển khai thực tế.
- Một số endpoint planner đang comment out (chưa active).
- Cần đồng bộ tuyệt đối rule role `Partner` giữa controller/service/domain (nếu roadmap yêu cầu).
- Cần harden bảo mật cấu hình secrets trước môi trường production.

---

## 17) Mục tiêu của file này

README này đóng vai trò **snapshot kiến trúc + domain + API contract hiện tại** để khi dùng coding agent, agent có thể hiểu nhanh hệ thống và thao tác nhất quán với codebase đang có.
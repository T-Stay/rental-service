# CODEBASE OVERVIEW - Trọ Tốt

> **Xem thêm:**
> - [BIGUPDATE.md](BIGUPDATE.md): Kế hoạch cập nhật lớn, định hướng business mới, checklist chi tiết các task cần làm.
> - [HALFDAYTARGET.md](HALFDAYTARGET.md): Danh sách các mục tiêu có thể hoàn thành trong nửa ngày, tập trung vào model và UI hiển thị.

## 1. Tổng quan dự án
- Dự án xây dựng trên nền tảng ASP.NET Core MVC, sử dụng Entity Framework Core cho ORM.
- Cấu trúc chuẩn gồm các thư mục: Controllers, Models, Views, Data, Services, Migrations, wwwroot.
- Database đã có migration, sử dụng AppDbContext.
- Hệ thống phân quyền: khách, chủ trọ (host), admin, có thể mở rộng thêm consultant.

## 2. Các thành phần chính

### Controllers
- Quản lý các luồng nghiệp vụ: đăng nhập, đăng ký, quản lý phòng, đặt phòng, lịch hẹn, quản lý bài đăng, admin duyệt bài, v.v.
- Các controller chính: AccountController, RoomsController, HostRoomsController, BookingRequestsController, ViewAppointmentsController, AdminController...

### Models
- Đã có các model cơ bản: User, Host, Room, BookingRequest, ViewAppointment, Review, Notification, ContactInformation, Building, Amenity, v.v.
- Các model lưu trữ thông tin phòng, người dùng, booking, lịch hẹn, đánh giá, tiện nghi, báo cáo, hợp đồng, v.v.

### Data & Migrations
- AppDbContext quản lý DbSet cho các entity chính.
- Đã có migration cho các bảng cơ bản, có thể mở rộng thêm các bảng mới cho business mới.

### Views
- Sử dụng Razor View, chia theo từng module: Account, Rooms, HostRooms, BookingRequests, ViewAppointments, Admin, Shared...
- Giao diện đã có form đăng ký, đăng nhập, đăng phòng, đặt lịch, quản lý booking, dashboard cho host và admin.

### Services
- Có các service hỗ trợ: S3Service (upload file), SmsService (gửi SMS), Notification, v.v.

## 3. Tình hình hiện tại
- Đã có luồng đăng ký, đăng nhập, phân quyền, đăng phòng, đặt lịch xem phòng, gửi yêu cầu thuê phòng, duyệt booking, quản lý phòng, quản lý lịch hẹn, đánh giá phòng, quản lý tiện nghi, báo cáo, hợp đồng (cơ bản).
- Admin có thể duyệt bài đăng phòng, quản lý người dùng, xem báo cáo tổng quan.
- Chưa có luồng kiếm tiền thực sự (chưa có gói dịch vụ, chưa kiểm soát liên hệ, chưa có đặt cọc giữ phòng, chưa có business về quảng cáo, bump bài, thống kê lượt xem, dashboard doanh thu).
- UI/UX ở mức cơ bản, đủ dùng cho MVP, chưa tối ưu cho business mới.

## 4. Định hướng mở rộng
- Sẵn sàng mở rộng thêm các model, controller, view cho business mới (gói dịch vụ, bài quảng cáo, đặt cọc, kiểm soát liên hệ, tiện ích nâng cao).
- Có thể bổ sung migration, service, view mới mà không ảnh hưởng lớn đến core hiện tại.
- Cần refactor lại một số luồng để phù hợp với business update lớn.

## 5. Đánh giá
- Codebase sạch, chia module rõ ràng, dễ mở rộng.
- Đủ nền tảng để phát triển các tính năng business mới theo BIGUPDATE.
- Cần bổ sung test, tối ưu UI/UX, chuẩn hóa lại một số luồng khi triển khai update lớn.

# 🎯 Mục tiêu MVP (Minimum Viable Product)

**Tìm - Xem - Đăng ký - Quản lý cơ bản**

## ✅ Chức năng MVP nên gồm

### 1. Guest (Chưa đăng ký)
- Tìm kiếm phòng theo vị trí, giá
- Xem chi tiết phòng (ảnh, mô tả, giá)
- Đăng ký tài khoản

### 2. Registered Customer
- Đăng nhập / Đăng xuất
- Đặt lịch hẹn xem phòng
- Gửi yêu cầu thuê phòng
- Xem trạng thái yêu cầu

### 3. Host (Chủ trọ)
- Đăng bài cho thuê phòng (thêm ảnh, mô tả, tiện nghi)
- Quản lý bài đăng (ẩn/hiện/xoá)

### 4. Admin (Rất cơ bản)
- Duyệt bài đăng phòng

---

## ⚙️ Công nghệ

- **ASP.NET MVC** (Views + Controller + Models)
- **Entity Framework Core** (Code First)
- **SQL Server**
- **Razor Views** (UI đơn giản, responsive cơ bản)
- **Session/cookie** để lưu favorite (chưa cần database)
- **Auth:** ASP.NET Identity

---

## 🗂️ DB Table tối thiểu

- `Users` (role: customer, host, admin)
- `Buildings`, `Rooms`, `RoomImages`
- `ViewAppointments`
- `BookingRequests`
- `Amenities` + `RoomAmenities` (nếu còn thời gian)

---

## 🧊 Các chức năng nên để lại cho giai đoạn sau

- Chat
- Hợp đồng thuê
- Thông báo, thống kê
- Tư vấn viên
- Quản lý báo cáo vi phạm

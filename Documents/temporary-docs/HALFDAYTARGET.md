# HALFDAY TARGET - BIG UPDATE Trọ Tốt 2025

> **Xem thêm:**
> - [codebase.md](codebase.md): Tổng quan hệ thống, kiến trúc, các thành phần chính.
> - [BIGUPDATE.md](BIGUPDATE.md): Kế hoạch cập nhật lớn, checklist chi tiết các task business.

## Mục tiêu trong nửa ngày (ưu tiên model database & UI hiển thị)

### 1. Model & Database
- [ ] Tạo model UserAdPackage (hoặc HostAdPackage): lưu thông tin gói mà chủ trọ đã mua (loại gói, ngày mua, ngày hết hạn, số lượng bài còn lại, trạng thái...)
- [ ] Tạo model AdPost: tiêu đề, nội dung, ảnh, chủ trọ, trạng thái, liên kết tới UserAdPackage, loại gói, thứ tự ưu tiên, badge, lượt xem, ngày tạo...
- [ ] Thêm enum AdPackageType (Free, Dong, Bac, Vang, KimCuong)
- [ ] Thêm trường liên kết nhiều phòng vào AdPost (List<Room> hoặc List<Guid> RoomIds)
- [ ] Sửa bảng ViewAppointment để lưu đủ trạng thái lịch hẹn (chờ, xác nhận, từ chối, hoàn tiền)
- [ ] Sửa BookingRequest để chuẩn bị cho đặt cọc giữ phòng (thêm trường số tiền cọc, thời gian giữ, trạng thái)

### 2. UI Hiển thị (View/Form)
- [ ] Form đăng bài quảng cáo (chọn gói, nhập nội dung, upload ảnh, chọn nhiều phòng)
- [ ] Giao diện danh sách bài quảng cáo (hiển thị các trường cơ bản, badge, trạng thái, danh sách phòng liên kết)
- [ ] Giao diện danh sách gói đã mua của chủ trọ
- [ ] Giao diện lịch sử đặt lịch (ViewAppointment) với đủ trạng thái
- [ ] Giao diện danh sách đặt cọc giữ phòng (BookingRequest) với các trường mới

### 3. Ghi chú
- Chỉ cần tạo model, migration, và UI cơ bản để hiển thị dữ liệu (có thể hardcode hoặc lấy từ DB, chưa cần xử lý business logic phức tạp)
- Không cần kiểm tra số lượng bài còn lại, không cần xử lý thanh toán, không cần validate nghiệp vụ phức tạp
- Ưu tiên hoàn thiện phần database và giao diện để có thể demo flow tổng thể

# BIG UPDATE - Trọ Tốt 2025

> **Xem thêm:**
> - [codebase.md](codebase.md): Tổng quan kiến trúc, thành phần, tình hình hiện tại của codebase.
> - [HALFDAYTARGET.md](HALFDAYTARGET.md): Checklist các mục tiêu có thể hoàn thành nhanh (model, UI).

## 1. Thông tin chung

- **Mục tiêu:** Chuyển đổi web từ nền tảng chỉ tìm phòng sang nền tảng có thể kiếm tiền từ dịch vụ cho thuê phòng. Mọi luồng giao dịch, liên hệ đều được kiểm soát qua hệ thống để đảm bảo minh bạch và tăng doanh thu.
- **Tính năng mới:**
  - Chủ trọ có thể đăng bài quảng cáo phòng trọ với nhiều gói dịch vụ (miễn phí, đồng, bạc, vàng, kim cương).
  - Thông tin liên hệ của chủ trọ sẽ bị ẩn, chỉ hiển thị cho khách thuê khi khách trả phí đặt lịch xem phòng.
  - Khách thuê muốn giữ phòng phải đặt cọc qua hệ thống, mọi giao dịch đều được kiểm soát và có thể hoàn tiền hoặc trừ phí theo quy định.
  - Trang chủ sẽ ưu tiên hiển thị các bài quảng cáo, còn giao diện cũ sẽ chuyển sang mục "About".
  - Có các tiện ích tăng hiển thị bài (bump), thống kê lượt xem, và các nguồn thu rõ ràng cho hệ thống.

---

## 2. Danh sách các task cụ thể (có checkbox theo dõi tiến độ)

### A. Đăng bài quảng cáo & gói dịch vụ

- [x] **Thiết kế lại lưu trữ gói dịch vụ cho chủ trọ**
   - [x] Tạo model UserAdPackage (hoặc HostAdPackage): lưu thông tin gói mà chủ trọ đã mua (loại gói, ngày mua, ngày hết hạn, số lượng bài còn lại, trạng thái...)
   - [x] Một gói dịch vụ có thể cho phép đăng nhiều bài quảng cáo (UserAdPackage liên kết nhiều AdPost)
   - [ ] Khi đăng bài mới, kiểm tra số lượng bài còn lại trong gói, trừ dần khi đăng bài
   - [ ] Cho phép chủ trọ mua nhiều gói cùng lúc, mỗi gói quản lý riêng số lượng bài

- [x] **Tạo dữ liệu cho bài quảng cáo**
   - [x] Tạo model AdPost (chứa tiêu đề, nội dung, ảnh, chủ trọ, trạng thái, liên kết tới UserAdPackage, loại gói, thứ tự ưu tiên, badge, lượt xem, ngày tạo...)
   - [x] Thêm trường liên kết tới nhiều phòng (AdPost có thể chứa 1 danh sách các phòng quảng cáo, ví dụ: List<Room> hoặc List<Guid> RoomIds)
   - [x] Tạo enum AdPackageType (Free, Dong, Bac, Vang, KimCuong)

- [ ] **Luồng đăng bài cho chủ trọ**
   - [ ] Form đăng bài quảng cáo (không trùng với đăng phòng thông thường)
   - [ ] Cho phép chọn gói dịch vụ khi đăng bài
   - [ ] Cho phép upload ảnh, nhập nội dung, chọn tiện ích (bump, badge...)
   - [ ] Cho phép chọn nhiều phòng để gắn vào bài quảng cáo (hiển thị danh sách phòng của chủ trọ, tick chọn phòng muốn quảng cáo)

- [ ] **Luồng duyệt bài cho admin**
   - [ ] Trang quản trị: Danh sách bài chờ duyệt, thao tác duyệt/ẩn/xóa bài
   - [ ] Gửi thông báo cho chủ trọ khi bài được duyệt hoặc bị từ chối

- [ ] **Luồng hiển thị bài quảng cáo**
   - [ ] Trang chủ: Hiển thị bài quảng cáo theo thứ tự ưu tiên (gói cao cấp lên đầu, có badge, bump...)
   - [ ] Trong mỗi bài quảng cáo, hiển thị danh sách các phòng đã được gắn vào bài (có thể click vào từng phòng để xem chi tiết)
   - [ ] Trang "About": Hiển thị nội dung trang chủ cũ

- [ ] **Quản lý bài quảng cáo cho chủ trọ**
   - [ ] Danh sách bài đã đăng, trạng thái, lượt xem, thao tác bump, gia hạn, xóa
   - [ ] Cho phép chỉnh sửa danh sách phòng gắn vào bài quảng cáo

- [ ] **Tiện ích bump bài, thống kê lượt xem**
   - [ ] API/Service tăng view, bump bài lên top, tính phí bump

---

### B. Ẩn thông tin liên hệ & thu phí đặt lịch xem phòng

- [ ] **Ẩn thông tin liên hệ chủ trọ**
   - [ ] Số điện thoại, Zalo, email của chủ trọ sẽ bị ẩn trên phòng/bài quảng cáo
   - [ ] Chỉ hiển thị khi khách đã trả phí đặt lịch xem phòng

- [ ] **Luồng đặt lịch xem phòng có thu phí**
   - [ ] Form đặt lịch: Khách phải thanh toán phí (có thể tích hợp cổng thanh toán hoặc hướng dẫn chuyển khoản)
   - [ ] Sau khi chủ trọ xác nhận lịch, hệ thống gửi thông tin liên hệ cho cả hai bên qua email
   - [ ] Nếu chủ trọ từ chối hoặc không phản hồi, hoàn tiền cho khách (ghi log, hỗ trợ hoàn tiền thủ công nếu cần)

- [ ] **Quản lý lịch sử đặt lịch, trạng thái, hoàn tiền**
   - [ ] Sửa lại bảng ViewAppointment để lưu lịch sử đặt lịch, trạng thái (chờ, đã xác nhận, từ chối, đã hoàn tiền). Không cần tạo bảng mới, chỉ cần bổ sung/truy vết các trạng thái này trong ViewAppointment.

---

### C. Đặt cọc giữ phòng & kiểm soát booking

- [ ] **Chuyển BookingRequests thành đặt cọc giữ phòng**
   - [ ] Thêm trường số tiền cọc, thời gian giữ, trạng thái cọc vào booking
   - [ ] Tích hợp thanh toán cọc (có thể hướng dẫn chuyển khoản nếu chưa có cổng thanh toán)

- [ ] **Luồng xác nhận cọc của chủ trọ**
   - [ ] Khi chủ trọ xác nhận, hệ thống gửi thông tin liên hệ cho cả hai bên
   - [ ] Chủ trọ không thể thao tác lên phòng đã được cọc

- [ ] **Luồng hoàn tiền/ăn cọc/hoa hồng**
   - [ ] Nếu chủ trọ từ chối hoặc không phản hồi: hoàn tiền cho khách
   - [ ] Nếu khách bỏ cọc: hệ thống giữ lại phần trăm cọc cho chủ trọ
   - [ ] Nếu giao dịch thành công: hệ thống trích hoa hồng, cập nhật trạng thái phòng

- [ ] **Quản lý giao dịch cọc, lịch sử, trạng thái**
   - [ ] Giao diện cho admin kiểm soát các giao dịch cọc, hoàn tiền, hoa hồng

---

### D. Kiểm soát luồng liên hệ & lợi nhuận

- [ ] **Chặn bypass liên hệ trực tiếp**
   - [ ] Ẩn toàn bộ thông tin liên hệ nếu khách chưa trả phí
   - [ ] Ghi log lại các hành động liên hệ, kiểm soát truy cập

- [ ] **Tổng hợp các nguồn thu**
   - [ ] Phí đặt lịch xem phòng
   - [ ] Phí đặt cọc giữ phòng + trích hoa hồng
   - [ ] Gói đăng bài cao cấp
   - [ ] Tiện ích bump bài, thống kê

- [ ] **Thống kê doanh thu, báo cáo cho admin**
   - [ ] Dashboard doanh thu, số lượng giao dịch, trạng thái

---

### E. Khác & Hỗ trợ

- [ ] **Cập nhật lại UI/UX toàn bộ luồng liên quan**
   - [ ] Trang chủ, trang phòng, trang quản lý, trang admin
   - [ ] Thông báo, email template, hướng dẫn sử dụng

- [ ] **Cập nhật tài liệu, hướng dẫn sử dụng, business rule**
   - [ ] Update README, BIGUPDATE.md, tài liệu business

---

## 3. Ghi chú

- Ưu tiên làm rõ từng bước, không để sót logic hoặc chi tiết dễ gây hiểu lầm.
- Mọi giao dịch, liên hệ đều phải đi qua hệ thống để đảm bảo kiểm soát và minh bạch.
- Có thể bổ sung các tiện ích nâng cao sau khi hoàn thiện luồng chính.

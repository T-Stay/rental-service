# Database Schema

## 1. Users & Roles
```sql
Users (
  id UUID PK,
  name TEXT,
  email TEXT UNIQUE,
  password_hash TEXT,
  phone TEXT,
  avatar_url TEXT,
  role ENUM('guest', 'registered_customer', 'host', 'admin', 'consultant'),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)
```

## 2. Buildings & Rooms
```sql
Buildings (
  id UUID PK,
  host_id UUID FK -> Users(id),
  name TEXT,
  address TEXT,
  description TEXT,
  location TEXT,
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)

Rooms (
  id UUID PK,
  building_id UUID FK -> Buildings(id),
  name TEXT, -- ví dụ: "Phòng 101"
  description TEXT,
  price DECIMAL,
  status ENUM('active', 'inactive', 'hidden'),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)

RoomImages (
  id UUID PK,
  room_id UUID FK -> Rooms(id),
  image_url TEXT
)
```

## 3. Tiện nghi phòng
```sql
Amenities (
  id UUID PK,
  name TEXT
)

RoomAmenities (
  room_id UUID FK -> Rooms(id),
  amenity_id UUID FK -> Amenities(id),
  PRIMARY KEY(room_id, amenity_id)
)
```

## 4. Favorites (Yêu thích phòng)
```sql
Favorites (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  created_at TIMESTAMP
)
```

## 5. Lịch hẹn xem phòng
```sql
ViewAppointments (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  appointment_time TIMESTAMP,
  status ENUM('pending', 'confirmed', 'cancelled'),
  created_at TIMESTAMP
)
```

## 6. Yêu cầu thuê phòng
```sql
BookingRequests (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  message TEXT,
  status ENUM('pending', 'approved', 'rejected', 'cancelled'),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)
```

## 7. Hợp đồng thuê (nếu có)
```sql
Contracts (
  id UUID PK,
  booking_request_id UUID FK -> BookingRequests(id),
  start_date DATE,
  end_date DATE,
  contract_file_url TEXT,
  created_at TIMESTAMP
)
```

## 8. Chat & Tin nhắn
```sql
ChatRooms (
  id UUID PK,
  user1_id UUID FK -> Users(id),
  user2_id UUID FK -> Users(id),
  created_at TIMESTAMP
)

Messages (
  id UUID PK,
  chat_room_id UUID FK -> ChatRooms(id),
  sender_id UUID FK -> Users(id),
  content TEXT,
  sent_at TIMESTAMP
)
```

## 9. Đánh giá phòng
```sql
Reviews (
  id UUID PK,
  room_id UUID FK -> Rooms(id),
  user_id UUID FK -> Users(id),
  rating INT CHECK(rating BETWEEN 1 AND 5),
  comment TEXT,
  created_at TIMESTAMP
)
```

## 10. Thông báo
```sql
Notifications (
  id UUID PK,
  user_id UUID FK -> Users(id),
  title TEXT,
  message TEXT,
  is_read BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP
)
```

## 11. Báo cáo vi phạm / khiếu nại
```sql
Reports (
  id UUID PK,
  reported_by UUID FK -> Users(id),
  reported_user UUID FK -> Users(id),
  reason TEXT,
  status ENUM('open', 'resolved'),
  created_at TIMESTAMP
)
```

## 12. Danh mục dùng chung (tiện nghi, khu vực…)
```sql
Categories (
  id UUID PK,
  name TEXT,
  type ENUM('amenity', 'location')
)
```

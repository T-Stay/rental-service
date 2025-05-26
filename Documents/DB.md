# Database Schema (Updated to match C# Models)

## 1. Users & Roles
```sql
Users (
  id UUID PK,
  name TEXT,
  email TEXT UNIQUE,
  password_hash TEXT,
  phone TEXT,
  avatar_url TEXT,
  role ENUM('Guest', 'RegisteredCustomer', 'Host', 'Admin', 'Consultant'),
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
  name TEXT, -- e.g. "Phòng 101"
  description TEXT,
  price DECIMAL,
  status ENUM('Active', 'Inactive', 'Hidden'),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)

RoomImages (
  id UUID PK,
  room_id UUID FK -> Rooms(id),
  image_url TEXT
)
```

## 3. Room Amenities
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

## 4. Favorites
```sql
Favorites (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  created_at TIMESTAMP
)
```

## 5. View Appointments
```sql
ViewAppointments (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  appointment_time TIMESTAMP,
  status ENUM('Pending', 'Confirmed', 'Cancelled'),
  created_at TIMESTAMP
)
```

## 6. Booking Requests
```sql
BookingRequests (
  id UUID PK,
  user_id UUID FK -> Users(id),
  room_id UUID FK -> Rooms(id),
  message TEXT,
  status ENUM('Pending', 'Approved', 'Rejected', 'Cancelled'),
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)
```

## 7. Contracts
```sql
Contracts (
  id UUID PK,
  booking_request_id UUID FK -> BookingRequests(id),
  start_date TIMESTAMP,
  end_date TIMESTAMP,
  contract_file_url TEXT,
  created_at TIMESTAMP
)
```

## 8. Chat & Messages
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

## 9. Reviews
```sql
Reviews (
  id UUID PK,
  room_id UUID FK -> Rooms(id),
  user_id UUID FK -> Users(id),
  rating INT,
  comment TEXT,
  created_at TIMESTAMP
)
```

## 10. Notifications
```sql
Notifications (
  id UUID PK,
  user_id UUID FK -> Users(id),
  title TEXT,
  message TEXT,
  is_read BOOLEAN,
  created_at TIMESTAMP
)
```

## 11. Reports
```sql
Reports (
  id UUID PK,
  reported_by UUID FK -> Users(id),
  reported_user UUID FK -> Users(id),
  reason TEXT,
  status ENUM('Open', 'Resolved'),
  created_at TIMESTAMP
)
```

## 12. Categories
```sql
Categories (
  id UUID PK,
  name TEXT,
  type ENUM('Amenity', 'Location')
)
```

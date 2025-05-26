# Entities Design

## User (Base class)
```csharp
public class User {
  public Guid Id { get; set; }
  public string Name { get; set; }
  public string Email { get; set; }
  public string PasswordHash { get; set; }
  public string Phone { get; set; }
  public string AvatarUrl { get; set; }
  public UserRole Role { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }

  public void Login() { }
  public void UpdateProfile() { }
}

public enum UserRole {
  Guest,
  RegisteredCustomer,
  Host,
  Admin,
  Consultant
}
```

### Customer extends User
```csharp
public class Customer : User {
  public List<Room> Favorites { get; set; }
  public List<BookingRequest> Bookings { get; set; }
  public List<ViewAppointment> ViewAppointments { get; set; }
  public List<Review> Reviews { get; set; }
  public List<Notification> Notifications { get; set; }

  public List<Room> SearchRooms(object filters) { return null; }
  public Room ViewRoomDetails(Guid roomId) { return null; }
  public void AddFavorite(Guid roomId) { }
  public void RemoveFavorite(Guid roomId) { }
  public BookingRequest CreateBookingRequest(Guid roomId, string message) { return null; }
  public ViewAppointment CreateViewAppointment(Guid roomId, DateTime time) { return null; }
  public ChatRoom ChatWith(Guid userId) { return null; }
  public Review ReviewRoom(Guid roomId, int rating, string comment) { return null; }
}
```

### Host extends User
```csharp
public class Host : User {
  public List<Building> Buildings { get; set; }

  public Room PostRoom(Guid buildingId, object roomData) { return null; }
  public void UpdateRoom(Guid roomId, object data) { }
  public void ToggleRoomVisibility(Guid roomId) { }
  public void DeleteRoom(Guid roomId) { }
  public void RespondToBooking(Guid requestId, object action) { }
  public object ViewStatistics() { return null; }
  public Contract ManageContract(Guid bookingId, object data) { return null; }
}
```

### Admin extends User
```csharp
public class Admin : User {
  public void ApproveRoomPost(Guid roomId) { }
  public List<User> ManageUsers() { return null; }
  public List<Report> HandleReports() { return null; }
  public List<Category> ManageCategories() { return null; }
  public void SendSystemNotification(Notification notification) { }
  public object ViewDashboard() { return null; }
}
```

### Consultant extends User
```csharp
public class Consultant : User {
  public void AssistCustomer(Guid customerId) { }
  public List<Room> SuggestRooms(Guid customerId, object criteria) { return null; }
}
```

---

## Building
```csharp
public class Building {
  public Guid Id { get; set; }
  public Guid HostId { get; set; }
  public string Name { get; set; }
  public string Address { get; set; }
  public string Description { get; set; }
  public string Location { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }

  public List<Room> GetRooms() { return null; }
}
```

## Room
```csharp
public class Room {
  public Guid Id { get; set; }
  public Guid BuildingId { get; set; }
  public string Name { get; set; }
  public string Description { get; set; }
  public decimal Price { get; set; }
  public RoomStatus Status { get; set; }
  public List<RoomImage> Images { get; set; }
  public List<Amenity> Amenities { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }

  public List<Review> GetReviews() { return null; }
}

public enum RoomStatus {
  Active,
  Inactive,
  Hidden
}
```

## Amenity
```csharp
public class Amenity {
  public Guid Id { get; set; }
  public string Name { get; set; }
}
```

## RoomImage
```csharp
public class RoomImage {
  public Guid Id { get; set; }
  public Guid RoomId { get; set; }
  public string ImageUrl { get; set; }
}
```

## Favorite
```csharp
public class Favorite {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid RoomId { get; set; }
  public DateTime CreatedAt { get; set; }
}
```

## ViewAppointment
```csharp
public class ViewAppointment {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid RoomId { get; set; }
  public DateTime AppointmentTime { get; set; }
  public ViewAppointmentStatus Status { get; set; }
  public DateTime CreatedAt { get; set; }
}

public enum ViewAppointmentStatus {
  Pending,
  Confirmed,
  Cancelled
}
```

## BookingRequest
```csharp
public class BookingRequest {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public Guid RoomId { get; set; }
  public string Message { get; set; }
  public BookingRequestStatus Status { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}

public enum BookingRequestStatus {
  Pending,
  Approved,
  Rejected,
  Cancelled
}
```

## Contract
```csharp
public class Contract {
  public Guid Id { get; set; }
  public Guid BookingRequestId { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public string ContractFileUrl { get; set; }
  public DateTime CreatedAt { get; set; }
}
```

## ChatRoom
```csharp
public class ChatRoom {
  public Guid Id { get; set; }
  public Guid User1Id { get; set; }
  public Guid User2Id { get; set; }
  public DateTime CreatedAt { get; set; }

  public Message SendMessage(Guid senderId, string content) { return null; }
  public List<Message> GetMessages() { return null; }
}
```

## Message
```csharp
public class Message {
  public Guid Id { get; set; }
  public Guid ChatRoomId { get; set; }
  public Guid SenderId { get; set; }
  public string Content { get; set; }
  public DateTime SentAt { get; set; }
}
```

## Review
```csharp
public class Review {
  public Guid Id { get; set; }
  public Guid RoomId { get; set; }
  public Guid UserId { get; set; }
  public int Rating { get; set; } // 1-5
  public string Comment { get; set; }
  public DateTime CreatedAt { get; set; }
}
```

## Notification
```csharp
public class Notification {
  public Guid Id { get; set; }
  public Guid UserId { get; set; }
  public string Title { get; set; }
  public string Message { get; set; }
  public bool IsRead { get; set; }
  public DateTime CreatedAt { get; set; }

  public void MarkAsRead() { }
}
```

## Report
```csharp
public class Report {
  public Guid Id { get; set; }
  public Guid ReportedBy { get; set; }
  public Guid ReportedUser { get; set; }
  public string Reason { get; set; }
  public ReportStatus Status { get; set; }
  public DateTime CreatedAt { get; set; }

  public void Resolve() { }
}

public enum ReportStatus {
  Open,
  Resolved
}
```

## Category
```csharp
public class Category {
  public Guid Id { get; set; }
  public string Name { get; set; }
  public CategoryType Type { get; set; }
}

public enum CategoryType {
  Amenity,
  Location
}
```

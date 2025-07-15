using System.Threading.Tasks;

namespace RentalService.Services
{
    public interface IEmailService
    {
        Task SendNewViewAppointmentNotificationAsync(string hostEmail, string hostName, string roomName, DateTime appointmentTime, string detailsUrl);
        Task SendViewAppointmentStatusUpdateAsync(string customerEmail, string customerName, string roomName, string status, string detailsUrl, string? hostContactInfo = null);
        Task SendNewBookingRequestNotificationAsync(string hostEmail, string hostName, string roomName, string customerName, string detailsUrl);
        Task SendBookingRequestStatusUpdateAsync(string customerEmail, string customerName, string roomName, string status, string detailsUrl, string? hostContactInfo = null);
    }
}

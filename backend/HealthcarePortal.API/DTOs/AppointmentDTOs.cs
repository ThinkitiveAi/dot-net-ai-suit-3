using System.ComponentModel.DataAnnotations;
using HealthcarePortal.API.Models;

namespace HealthcarePortal.API.DTOs
{
    public class BookAppointmentDto
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
    }

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public string PatientPhone { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderSpecialty { get; set; } = string.Empty;
        public string ProviderPhone { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Computed properties for frontend convenience
        public string AppointmentDate => AppointmentDateTime.ToString("yyyy-MM-dd");
        public string AppointmentTime => AppointmentDateTime.ToString("HH:mm");
        public string FormattedDateTime => AppointmentDateTime.ToString("MMM dd, yyyy 'at' h:mm tt");
        public bool CanBeCancelled => Status == "Scheduled" && AppointmentDateTime > DateTime.Now;
        public bool IsUpcoming => AppointmentDateTime > DateTime.Now && Status == "Scheduled";
        public bool IsPast => AppointmentDateTime <= DateTime.Now;
    }

    public class AvailableSlotDto
    {
        public DateTime DateTime { get; set; }
        public bool IsAvailable { get; set; }
        public string TimeSlot => DateTime.ToString("h:mm tt");
        public string DateTimeString => DateTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }

    public class UpdateAppointmentStatusDto
    {
        [Required]
        public AppointmentStatus Status { get; set; }

        public string? Notes { get; set; }
    }

    public class AppointmentSummaryDto
    {
        public int TotalAppointments { get; set; }
        public int ScheduledAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int NoShowAppointments { get; set; }
        public List<AppointmentResponseDto> UpcomingAppointments { get; set; } = new();
        public List<AppointmentResponseDto> TodayAppointments { get; set; } = new();
    }

    public class ProviderDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalAppointments { get; set; }
        public double AverageRating { get; set; }
        public bool IsAvailableToday { get; set; }
    }

    public class PatientDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalAppointments { get; set; }
        public DateTime? LastAppointmentDate { get; set; }
    }

    public class AppointmentSearchDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public int? PatientId { get; set; }
        public int? ProviderId { get; set; }
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class RescheduleAppointmentDto
    {
        [Required]
        public DateTime NewAppointmentDateTime { get; set; }

        public string? RescheduleReason { get; set; }
    }
}

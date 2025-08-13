using System.ComponentModel.DataAnnotations;

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
        
        public string? Notes { get; set; }
    }
    
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    
    public class AvailableSlotDto
    {
        public DateTime DateTime { get; set; }
        public bool IsAvailable { get; set; }
    }
    
    public class ProviderDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ClinicAddress { get; set; } = string.Empty;
    }
    
    public class PatientDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

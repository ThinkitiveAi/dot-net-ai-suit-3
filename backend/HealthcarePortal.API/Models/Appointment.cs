using System.ComponentModel.DataAnnotations;

namespace HealthcarePortal.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        
        [Required]
        public int PatientId { get; set; }
        
        [Required]
        public int ProviderId { get; set; }
        
        [Required]
        public DateTime AppointmentDateTime { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Patient Patient { get; set; } = null!;
        public virtual Provider Provider { get; set; } = null!;
    }
    
    public enum AppointmentStatus
    {
        Scheduled,
        Completed,
        Cancelled,
        NoShow
    }
}

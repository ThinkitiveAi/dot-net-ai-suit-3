using Microsoft.EntityFrameworkCore;
using HealthcarePortal.API.Data;
using HealthcarePortal.API.DTOs;
using HealthcarePortal.API.Models;

namespace HealthcarePortal.API.Services
{
    public interface IAppointmentService
    {
        Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int providerId, DateTime date);
        Task<bool> IsSlotAvailableAsync(int providerId, DateTime appointmentDateTime);
    }
    
    public class AppointmentService : IAppointmentService
    {
        private readonly HealthcareDbContext _context;
        
        public AppointmentService(HealthcareDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int providerId, DateTime date)
        {
            var slots = new List<AvailableSlotDto>();
            var startTime = new TimeSpan(9, 0, 0); // 9 AM
            var endTime = new TimeSpan(17, 0, 0);  // 5 PM
            var slotDuration = TimeSpan.FromMinutes(30); // 30-minute slots
            
            // Get existing appointments for the provider on the given date
            var existingAppointments = await _context.Appointments
                .Where(a => a.ProviderId == providerId && 
                           a.AppointmentDateTime.Date == date.Date &&
                           a.Status == AppointmentStatus.Scheduled)
                .Select(a => a.AppointmentDateTime)
                .ToListAsync();
            
            var currentTime = startTime;
            while (currentTime < endTime)
            {
                var slotDateTime = date.Date.Add(currentTime);
                var isAvailable = !existingAppointments.Any(a => a == slotDateTime) && 
                                 slotDateTime > DateTime.Now; // Don't show past slots
                
                slots.Add(new AvailableSlotDto
                {
                    DateTime = slotDateTime,
                    IsAvailable = isAvailable
                });
                
                currentTime = currentTime.Add(slotDuration);
            }
            
            return slots;
        }
        
        public async Task<bool> IsSlotAvailableAsync(int providerId, DateTime appointmentDateTime)
        {
            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.ProviderId == providerId && 
                              a.AppointmentDateTime == appointmentDateTime &&
                              a.Status == AppointmentStatus.Scheduled);
            
            return !existingAppointment && appointmentDateTime > DateTime.Now;
        }
    }
}

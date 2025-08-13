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
        Task<AppointmentSummaryDto> GetAppointmentSummaryAsync(int userId, string userType);
        Task<List<AvailableSlotDto>> GetAvailableSlotsForWeekAsync(int providerId, DateTime startDate);
        Task<bool> HasConflictingAppointmentAsync(int patientId, int providerId, DateTime appointmentDateTime);
    }

    public class AppointmentService : IAppointmentService
    {
        private readonly HealthcareDbContext _context;

        // Business hours configuration
        private readonly TimeSpan _startTime = new(9, 0, 0);   // 9:00 AM
        private readonly TimeSpan _endTime = new(17, 0, 0);    // 5:00 PM
        private readonly TimeSpan _slotDuration = TimeSpan.FromMinutes(30); // 30-minute slots
        private readonly TimeSpan _lunchStart = new(12, 0, 0); // 12:00 PM
        private readonly TimeSpan _lunchEnd = new(13, 0, 0);   // 1:00 PM

        public AppointmentService(HealthcareDbContext context)
        {
            _context = context;
        }

        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(int providerId, DateTime date)
        {
            var slots = new List<AvailableSlotDto>();

            // Don't show slots for weekends
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                return slots;
            }

            // Don't show slots for past dates
            if (date.Date < DateTime.Today)
            {
                return slots;
            }

            // Get existing appointments for the provider on the given date
            var existingAppointments = await _context.Appointments
                .Where(a => a.ProviderId == providerId &&
                           a.AppointmentDateTime.Date == date.Date &&
                           a.Status == AppointmentStatus.Scheduled)
                .Select(a => a.AppointmentDateTime)
                .ToListAsync();

            var currentTime = _startTime;

            while (currentTime < _endTime)
            {
                // Skip lunch hour
                if (currentTime >= _lunchStart && currentTime < _lunchEnd)
                {
                    currentTime = currentTime.Add(_slotDuration);
                    continue;
                }

                var slotDateTime = date.Date.Add(currentTime);

                // Don't show past slots for today
                var isPastSlot = date.Date == DateTime.Today && slotDateTime <= DateTime.Now.AddMinutes(30);

                var isBooked = existingAppointments.Any(a => a == slotDateTime);
                var isAvailable = !isBooked && !isPastSlot;

                slots.Add(new AvailableSlotDto
                {
                    DateTime = slotDateTime,
                    IsAvailable = isAvailable
                });

                currentTime = currentTime.Add(_slotDuration);
            }

            return slots;
        }

        public async Task<List<AvailableSlotDto>> GetAvailableSlotsForWeekAsync(int providerId, DateTime startDate)
        {
            var allSlots = new List<AvailableSlotDto>();

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startDate.AddDays(i);
                var daySlots = await GetAvailableSlotsAsync(providerId, currentDate);
                allSlots.AddRange(daySlots);
            }

            return allSlots;
        }

        public async Task<bool> IsSlotAvailableAsync(int providerId, DateTime appointmentDateTime)
        {
            // Check business hours
            var timeOfDay = appointmentDateTime.TimeOfDay;
            if (timeOfDay < _startTime || timeOfDay >= _endTime)
            {
                return false;
            }

            // Check if it's during lunch hour
            if (timeOfDay >= _lunchStart && timeOfDay < _lunchEnd)
            {
                return false;
            }

            // Check if it's a weekend
            if (appointmentDateTime.DayOfWeek == DayOfWeek.Saturday ||
                appointmentDateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            // Check if it's in the past
            if (appointmentDateTime <= DateTime.Now.AddMinutes(30))
            {
                return false;
            }

            // Check if slot is already booked
            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.ProviderId == providerId &&
                              a.AppointmentDateTime == appointmentDateTime &&
                              a.Status == AppointmentStatus.Scheduled);

            return !existingAppointment;
        }

        public async Task<bool> HasConflictingAppointmentAsync(int patientId, int providerId, DateTime appointmentDateTime)
        {
            // Check if patient already has an appointment with this provider on the same day
            var existingAppointment = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientId &&
                              a.ProviderId == providerId &&
                              a.AppointmentDateTime.Date == appointmentDateTime.Date &&
                              a.Status == AppointmentStatus.Scheduled);

            return existingAppointment;
        }

        public async Task<AppointmentSummaryDto> GetAppointmentSummaryAsync(int userId, string userType)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .AsQueryable();

            if (userType == "Provider")
            {
                query = query.Where(a => a.ProviderId == userId);
            }
            else if (userType == "Patient")
            {
                query = query.Where(a => a.PatientId == userId);
            }

            var appointments = await query.ToListAsync();
            var today = DateTime.Today;

            var summary = new AppointmentSummaryDto
            {
                TotalAppointments = appointments.Count,
                ScheduledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                NoShowAppointments = appointments.Count(a => a.Status == AppointmentStatus.NoShow),

                UpcomingAppointments = appointments
                    .Where(a => a.AppointmentDateTime > DateTime.Now && a.Status == AppointmentStatus.Scheduled)
                    .OrderBy(a => a.AppointmentDateTime)
                    .Take(5)
                    .Select(MapToResponseDto)
                    .ToList(),

                TodayAppointments = appointments
                    .Where(a => a.AppointmentDateTime.Date == today && a.Status == AppointmentStatus.Scheduled)
                    .OrderBy(a => a.AppointmentDateTime)
                    .Select(MapToResponseDto)
                    .ToList()
            };

            return summary;
        }

        private static AppointmentResponseDto MapToResponseDto(Appointment appointment)
        {
            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                PatientId = appointment.PatientId,
                PatientName = appointment.Patient.FullName,
                PatientEmail = appointment.Patient.Email,
                PatientPhone = appointment.Patient.PhoneNumber,
                ProviderId = appointment.ProviderId,
                ProviderName = appointment.Provider.FullName,
                ProviderSpecialty = appointment.Provider.Specialty,
                ProviderPhone = appointment.Provider.PhoneNumber,
                ClinicAddress = appointment.Provider.ClinicAddress,
                AppointmentDateTime = appointment.AppointmentDateTime,
                Notes = appointment.Notes,
                Status = appointment.Status.ToString(),
                CreatedAt = appointment.CreatedAt
            };
        }
    }
}

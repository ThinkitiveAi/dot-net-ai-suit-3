using Microsoft.EntityFrameworkCore;
using HealthcarePortal.API.Models;

namespace HealthcarePortal.API.Data
{
    public class HealthcareDbContext : DbContext
    {
        public HealthcareDbContext(DbContextOptions<HealthcareDbContext> options) : base(options)
        {
        }
        
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure Provider entity
            modelBuilder.Entity<Provider>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Specialty).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ClinicAddress).IsRequired().HasMaxLength(200);
            });
            
            // Configure Patient entity
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Gender).IsRequired().HasMaxLength(10);
                entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            });
            
            // Configure Appointment entity
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.Status).HasConversion<string>();
                
                // Configure relationships
                entity.HasOne(e => e.Patient)
                      .WithMany(p => p.Appointments)
                      .HasForeignKey(e => e.PatientId)
                      .OnDelete(DeleteBehavior.Restrict);
                      
                entity.HasOne(e => e.Provider)
                      .WithMany(p => p.Appointments)
                      .HasForeignKey(e => e.ProviderId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

using ITS_Task.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ITS_Task.DataAccess
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<LeaveType> LeaveTypes { get; set; }

        public DbSet<LeaveBalance> LeaveBalances { get; set; }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();



            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Name)
                .IsUnique();

            modelBuilder.Entity<LeaveType>()
                .HasIndex(l => l.Name)
                .IsUnique();

            modelBuilder.Entity<LeaveType>().HasIndex(t => t.IsAnnual).IsUnique().HasFilter("[IsAnnual] = 1");

            modelBuilder.Entity<LeaveBalance>()
                .HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId })
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(e => e.Employees)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
    .HasOne(e => e.User)
    .WithOne(u => u.Employee)
    .HasForeignKey<Employee>(e => e.UserId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
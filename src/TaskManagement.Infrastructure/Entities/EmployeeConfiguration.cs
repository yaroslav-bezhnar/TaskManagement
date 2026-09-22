using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Entities;

internal class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100).HasColumnName("first_name");
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100).HasColumnName("last_name");
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200).HasColumnName("email");
        builder.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ux_employees_email");
        builder.Property(e => e.IsActive).IsRequired().HasColumnName("is_active");
        builder.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
    }
}

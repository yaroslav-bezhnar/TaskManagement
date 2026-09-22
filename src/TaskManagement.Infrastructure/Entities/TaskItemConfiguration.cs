using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Entities;

internal class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks", t =>
        {
            t.HasCheckConstraint("ck_tasks_due_after_start", "due_at >= planned_start_at");
            t.HasCheckConstraint("ck_tasks_completed_at_required", "(status = 'Completed' AND completed_at IS NOT NULL) OR (status <> 'Completed' AND completed_at IS NULL)");
            t.HasCheckConstraint("ck_tasks_status_values", "status IN ('Pending','InProgress','Completed','Cancelled')");
            t.HasCheckConstraint("ck_tasks_creator_assignee_different", "created_by_employee_id <> assignee_employee_id");
        });
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
        builder.Property(t => t.Description).HasMaxLength(1000).HasColumnName("description");
        builder.Property(t => t.CreatedByEmployeeId).IsRequired().HasColumnName("created_by_employee_id");
        builder.Property(t => t.AssigneeEmployeeId).IsRequired().HasColumnName("assignee_employee_id");
        builder.Property(t => t.PlannedStartAt).IsRequired().HasColumnName("planned_start_at");
        builder.Property(t => t.DueAt).IsRequired().HasColumnName("due_at");
        builder.Property(t => t.CompletedAt).HasColumnName("completed_at");
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasColumnName("status");
        builder.Property(t => t.CreatedAt).IsRequired().HasColumnName("created_at");
        builder.Property(t => t.UpdatedAt).IsRequired().HasColumnName("updated_at");

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(t => t.AssigneeEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.AssigneeEmployeeId).HasDatabaseName("ix_tasks_assignee");
        builder.HasIndex(t => t.Status).HasDatabaseName("ix_tasks_status");
        builder.HasIndex(t => t.DueAt).HasDatabaseName("ix_tasks_dueat");

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .IsRowVersion()
            .IsConcurrencyToken();
    }
}

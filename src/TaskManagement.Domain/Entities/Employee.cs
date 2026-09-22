using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

public sealed class Employee
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Employee() { }

    private Employee(Guid id, string firstName, string lastName, string email, bool isActive, DateTime createdAt)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        IsActive = isActive;
        CreatedAt = createdAt;
    }

    public static Employee Create(Guid id, string firstName, string lastName, string email, bool isActive = true, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new BusinessRuleViolationException("FirstName is required");
        if (string.IsNullOrWhiteSpace(lastName)) throw new BusinessRuleViolationException("LastName is required");
        if (string.IsNullOrWhiteSpace(email)) throw new BusinessRuleViolationException("Email is required");

        var utcNow = createdAt ?? DateTime.UtcNow;

        return new Employee(id, firstName, lastName, email.ToLowerInvariant(), isActive, utcNow);
    }
}

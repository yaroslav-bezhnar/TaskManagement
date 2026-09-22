namespace TaskManagement.Application.Exceptions;

public class ConcurrencyConflictException : AppException
{
    public ConcurrencyConflictException(string message) : base(message) { }
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException) { }
}

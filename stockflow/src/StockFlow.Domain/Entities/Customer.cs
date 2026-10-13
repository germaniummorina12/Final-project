using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

public class Customer : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Customer() { }

    public Customer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Customer name is required.");
        if(string.IsNullOrWhiteSpace(email) || !email.Contains("@")) throw new DomainException("A valid email address is required.");

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}
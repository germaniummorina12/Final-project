using StockFlow.Domain.Exceptions;

namespace StockFlow.Domain.Entities;

public class Category : Entity
{
    public string Name { get; private set; } = string.Empty;

    private Category() { }

    public Category(string name) => Rename(name);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Category name is required.");

        var trimmed = name.Trim();
        if (trimmed.Length > 100) throw new DomainException("Category name cannot exceed 100 characters.");

        Name = trimmed;
    }
}
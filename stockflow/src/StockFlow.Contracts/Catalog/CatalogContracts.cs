using System.ComponentModel.DataAnnotations;

namespace StockFlow.Contracts.Catalog;

public sealed record CategoryDto(int Id, string Name);

public sealed class CreateCategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = string.Empty;
}

public sealed record CustomerDto(int Id, string Name, string Email);

public sealed class CreateCustomerRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; init; } = string.Empty;
}
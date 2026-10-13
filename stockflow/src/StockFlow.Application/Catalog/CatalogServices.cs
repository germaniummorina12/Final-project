using StockFlow.Application.Abstractions;
using StockFlow.Application.Exceptions;
using StockFlow.Contracts.Catalog;
using StockFlow.Domain.Entities;

namespace StockFlow.Application.Catalog;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
}

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
}

internal sealed class CategoryService(ICategoryRepository categories, IUnitOfWork unitOfWork) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default) =>
        (await categories.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category(request.Name);

        if (await categories.NameExistsAsync(category.Name, ct))
        {
            throw new ConflictException($"A category named '{category.Name}' already exists.");
        }

        categories.Add(category);
        await unitOfWork.SaveChangesAsync(ct);
        return category.ToDto();
    }
}

internal sealed class CustomerService(ICustomerRepository customers, IUnitOfWork unitOfWork) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default) =>
        (await customers.GetAllAsync(ct)).Select(c => c.ToDto()).ToList();

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer(request.Name, request.Email);

        if (await customers.EmailExistsAsync(customer.Email, ct))
        {
            throw new ConflictException($"A customer with the email '{customer.Email}' already exists.");
        }

        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return customer.ToDto();
    }
}
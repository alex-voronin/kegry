using SharedCore.Interfaces;

namespace SharedCore.Services;

public class DomainService<TRepository> where TRepository : IRepository
{
    protected readonly TRepository Repository;

    public DomainService(TRepository repository)
    {
        Repository = repository;
    }
}
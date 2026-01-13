using SharedCore.Interfaces;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Domain.Repositories;

public interface IUserRepository : IRepositoryOutbox<User>, ISoftDeleteRepository<User, Guid>;
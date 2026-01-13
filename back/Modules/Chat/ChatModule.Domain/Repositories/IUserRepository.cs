using ChatModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace ChatModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>, ISoftDeleteRepository<User, Guid>;

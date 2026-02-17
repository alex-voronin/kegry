using EngagementModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace EngagementModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>, ISoftDeleteRepository<User, Guid>;
using FeedModule.Domain.Aggregates.ReadOnly;
using SharedCore.Interfaces;

namespace FeedModule.Domain.Repositories;

public interface IUserRepository : IRepository<User>;
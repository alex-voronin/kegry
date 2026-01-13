using SharedCore.Interfaces;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Domain.Repositories;

public interface IUserProfileRepository : IRepositoryOutbox<User>;
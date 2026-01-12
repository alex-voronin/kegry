using SharedCore.Interfaces;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Domain.Repostories;

public interface IUserProfileRepository : IRepositoryOutbox<UserProfile>;
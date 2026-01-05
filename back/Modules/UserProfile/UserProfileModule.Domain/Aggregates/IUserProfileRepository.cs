using SharedCore.Interfaces;

namespace UserProfileModule.Domain.Aggregates;

public interface IUserProfileRepository : IRepositoryOutbox<UserProfile>;
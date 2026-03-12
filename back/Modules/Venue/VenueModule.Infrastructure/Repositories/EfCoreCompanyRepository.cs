using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreCompanyRepository : Repository<VenueContext, Company>, ICompanyRepository
{
    public EfCoreCompanyRepository(VenueContext context) : base(context)
    {
    }
}
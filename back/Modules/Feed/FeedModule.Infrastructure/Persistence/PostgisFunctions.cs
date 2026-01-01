using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace FeedModule.Infrastructure.Persistence;

public static class PostgisFunctions
{
    [DbFunction("ST_DistanceSphere", IsBuiltIn = true)]
    public static double DistanceSphere(Point a, Point b)
        => throw new NotSupportedException();
}
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.Domain.Aggregates;

namespace Tests.VenueModule.Domain;

public class DiscountTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public void Percent_WhenOutOfRange_ThenThrows(int invalidPercent)
    {
        var discount = CreateValidDiscount();
        Should.Throw<HttpCodeException>(() => discount.Percent = invalidPercent)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Percent_WhenInRange_ThenSet(int validPercent)
    {
        var discount = CreateValidDiscount();
        discount.Percent = validPercent;
        discount.Percent.ShouldBe(validPercent);
    }

    private static Discount CreateValidDiscount()
    {
        return new Discount()
        {
            Id = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            LoyaltyLevelId = 1,
            Percent = 1
        };
    }
}
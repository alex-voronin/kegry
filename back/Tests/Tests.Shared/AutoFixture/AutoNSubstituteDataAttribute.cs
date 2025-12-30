using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Time.Testing;

namespace Tests.Shared.AutoFixture;

public class AutoNSubstituteDataAttribute() : AutoDataAttribute(() =>
    {
       var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
       var fakeTimeProvider = new FakeTimeProvider();
       fixture.Inject<TimeProvider>(fakeTimeProvider);
       fixture.Inject<FakeTimeProvider>(fakeTimeProvider);
       return fixture;
    }
);
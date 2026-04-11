namespace SharedCore.Interfaces;

public interface IUserContext
{
    TimeZoneInfo UserTimeZone { get; }
    DateTimeOffset LocalNow { get; }
    DateOnly LocalDate => DateOnly.FromDateTime(LocalNow.DateTime);
}

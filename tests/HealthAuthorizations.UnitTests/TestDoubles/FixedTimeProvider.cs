namespace HealthAuthorizations.UnitTests.TestDoubles;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(Default)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}

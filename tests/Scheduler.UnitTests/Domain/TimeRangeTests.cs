using Scheduler.Domain.ValueObjects;

namespace Scheduler.UnitTests.Domain;

public class TimeRangeTests
{
    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        var start = new DateTime(2026, 9, 7, 10, 0, 0);
        var end = start.AddMinutes(-30);

        Assert.Throws<ArgumentException>(() => new TimeRange(start, end));
    }

    [Fact]
    public void Constructor_EndEqualsStart_Throws()
    {
        var start = new DateTime(2026, 9, 7, 10, 0, 0);

        Assert.Throws<ArgumentException>(() => new TimeRange(start, start));
    }

    [Theory]
    [InlineData(10, 0, 11, 0, 10, 30, 11, 30, true)]  // partial overlap
    [InlineData(10, 0, 11, 0, 9, 0, 10, 0, false)]    // adjacent, touching at boundary — not overlapping
    [InlineData(10, 0, 11, 0, 11, 0, 12, 0, false)]   // adjacent, other side
    [InlineData(10, 0, 12, 0, 10, 30, 11, 30, true)]  // fully contains
    [InlineData(10, 0, 11, 0, 13, 0, 14, 0, false)]   // disjoint
    public void Overlaps_VariousRanges_ReturnsExpected(
        int h1, int m1, int h2, int m2,
        int h3, int m3, int h4, int m4,
        bool expected)
    {
        var date = new DateTime(2026, 9, 7);
        var a = new TimeRange(date.AddHours(h1).AddMinutes(m1), date.AddHours(h2).AddMinutes(m2));
        var b = new TimeRange(date.AddHours(h3).AddMinutes(m3), date.AddHours(h4).AddMinutes(m4));

        Assert.Equal(expected, a.Overlaps(b));
        Assert.Equal(expected, b.Overlaps(a));
    }

    [Fact]
    public void Equality_SameStartEnd_AreEqual()
    {
        var start = new DateTime(2026, 9, 7, 10, 0, 0);
        var end = start.AddMinutes(30);

        var a = new TimeRange(start, end);
        var b = new TimeRange(start, end);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }
}

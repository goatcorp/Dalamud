using Dalamud.Interface.Internal.Windows.Data.Widgets;

using Xunit;

namespace Dalamud.Test.Game.Network;

public class OpCodeFilterTests
{
    [Fact]
    public void EmptyFilter_MatchesAll()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered(string.Empty, 100));
        Assert.True(NetworkMonitorWidget.IsFiltered(string.Empty, 0));
        Assert.True(NetworkMonitorWidget.IsFiltered(string.Empty, ushort.MaxValue));
    }

    [Fact]
    public void WhitespaceOnlyFilter_MatchesAll()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("   ", 100));
    }

    [Fact]
    public void SingleExactMatch()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("100", 100));
    }

    [Fact]
    public void SingleExact_NoMatch()
    {
        Assert.False(NetworkMonitorWidget.IsFiltered("100", 200));
    }

    [Fact]
    public void CommaSeparatedList_MatchesIncluded()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("100,200,300", 200));
    }

    [Fact]
    public void CommaSeparatedList_RejectsExcluded()
    {
        Assert.False(NetworkMonitorWidget.IsFiltered("100,200,300", 150));
    }

    [Fact]
    public void Range_MatchesWithinBounds()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("100-200", 150));
    }

    [Fact]
    public void Range_MatchesLowerBound()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("100-200", 100));
    }

    [Fact]
    public void Range_MatchesUpperBound()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("100-200", 200));
    }

    [Fact]
    public void Range_RejectsOutside()
    {
        Assert.False(NetworkMonitorWidget.IsFiltered("100-200", 50));
        Assert.False(NetworkMonitorWidget.IsFiltered("100-200", 250));
    }

    [Fact]
    public void OpenEndedRangeStart_MatchesUpTo()
    {
        // "-400" means everything up to 400
        Assert.True(NetworkMonitorWidget.IsFiltered("-400", 0));
        Assert.True(NetworkMonitorWidget.IsFiltered("-400", 200));
        Assert.True(NetworkMonitorWidget.IsFiltered("-400", 400));
        Assert.False(NetworkMonitorWidget.IsFiltered("-400", 401));
    }

    [Fact]
    public void OpenEndedRangeEnd_MatchesFrom()
    {
        // "700-" means everything from 700 onward
        Assert.True(NetworkMonitorWidget.IsFiltered("700-", 700));
        Assert.True(NetworkMonitorWidget.IsFiltered("700-", 1000));
        Assert.True(NetworkMonitorWidget.IsFiltered("700-", ushort.MaxValue));
        Assert.False(NetworkMonitorWidget.IsFiltered("700-", 699));
    }

    [Fact]
    public void Exclusion_ExcludesSingleOpCode()
    {
        // Only exclusion, no inclusion -> matches everything except excluded
        Assert.False(NetworkMonitorWidget.IsFiltered("!50", 50));
        Assert.True(NetworkMonitorWidget.IsFiltered("!50", 100));
    }

    [Fact]
    public void Exclusion_ExcludesRange()
    {
        Assert.False(NetworkMonitorWidget.IsFiltered("!50-100", 75));
        Assert.True(NetworkMonitorWidget.IsFiltered("!50-100", 150));
    }

    [Fact]
    public void MixedInclusionAndExclusion()
    {
        // Include 0-400, but exclude 50-100
        var filter = "-400,!50-100";
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 10));
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 75));
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 200));
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 500));
    }

    [Fact]
    public void ComplexFilter()
    {
        // Example from UI tooltip: -400,!50-100,650,700-980,!941
        var filter = "-400,!50-100,650,700-980,!941";

        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 10));    // in -400 range
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 75));   // excluded by !50-100
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 300));   // in -400 range
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 500));  // not in any include
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 650));   // exact match
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 700));   // in 700-980
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 800));   // in 700-980
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 941));  // excluded by !941
        Assert.True(NetworkMonitorWidget.IsFiltered(filter, 980));   // in 700-980
        Assert.False(NetworkMonitorWidget.IsFiltered(filter, 990));  // not in any include
    }

    [Fact]
    public void SpacesAreStripped()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered(" 100 , 200 ", 100));
        Assert.True(NetworkMonitorWidget.IsFiltered(" 100 , 200 ", 200));
        Assert.False(NetworkMonitorWidget.IsFiltered(" 100 , 200 ", 150));
    }

    [Fact]
    public void InvalidFilter_ReturnsFalse()
    {
        Assert.False(NetworkMonitorWidget.IsFiltered("abc", 100));
    }

    [Fact]
    public void ExclusionOnly_MatchesEverythingElse()
    {
        // No include entries -> treat as "match all except excluded"
        Assert.True(NetworkMonitorWidget.IsFiltered("!50,!100", 200));
        Assert.False(NetworkMonitorWidget.IsFiltered("!50,!100", 50));
        Assert.False(NetworkMonitorWidget.IsFiltered("!50,!100", 100));
    }

    [Fact]
    public void BoundaryValues()
    {
        Assert.True(NetworkMonitorWidget.IsFiltered("0", 0));
        Assert.True(NetworkMonitorWidget.IsFiltered("65535", ushort.MaxValue));
    }
}

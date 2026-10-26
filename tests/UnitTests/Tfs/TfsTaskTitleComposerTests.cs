using HonestTimeTracker.Application.Tfs;

namespace HonestTimeTracker.UnitTests.Tfs;

public class TfsTaskTitleComposerTests
{
    [Fact]
    public void Joins_parent_and_title() =>
        Assert.Equal("Story - Task", TfsTaskTitleComposer.Compose(" Story ", " Task "));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Uses_title_only_without_parent(string? parent) =>
        Assert.Equal("Task", TfsTaskTitleComposer.Compose(parent, "Task"));

    [Fact]
    public void Shortens_parent_when_result_exceeds_limit()
    {
        var result = TfsTaskTitleComposer.Compose(new string('P', 30), "Task title", maxLength: 25);

        Assert.Equal(25, result.Length);
        Assert.EndsWith("… - Task title", result);
        Assert.StartsWith("PPPP", result);
    }

    [Fact]
    public void Drops_parent_when_no_room_is_left() =>
        Assert.Equal("Very long task", TfsTaskTitleComposer.Compose("Parent", "Very long task", maxLength: 16));

    [Fact]
    public void Truncates_overlong_title()
    {
        var result = TfsTaskTitleComposer.Compose(null, new string('T', 30), maxLength: 20);

        Assert.Equal(20, result.Length);
        Assert.EndsWith("…", result);
    }
}

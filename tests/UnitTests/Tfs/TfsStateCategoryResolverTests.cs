using HonestTimeTracker.Application.Tfs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HonestTimeTracker.UnitTests.Tfs;

public class TfsStateCategoryResolverTests
{
    private static readonly TfsConnection Connection = new("https://tfs.test/tfs/c1", "pat");

    private readonly ITfsClient _client = Substitute.For<ITfsClient>();

    public TfsStateCategoryResolverTests()
    {
        _client.GetWorkItemTypeStatesAsync(Connection, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([new TfsWorkItemTypeState("Active", "InProgress"), new TfsWorkItemTypeState("Closed", "Completed"), new TfsWorkItemTypeState("Odd", "Weird")]);
    }

    private TfsStateCategoryResolver CreateResolver() => new(_client, Connection, "C1", NullLogger.Instance);

    private static TfsWorkItem Item(string? state, string? type = "Task", string? project = "Alpha") =>
        new(1, "Title", type, state, project, null);

    [Fact]
    public async Task Resolves_state_case_insensitively_and_caches_per_project_and_type()
    {
        var resolver = CreateResolver();

        Assert.Equal("Completed", await resolver.ResolveAsync(Item("closed"), CancellationToken.None));
        Assert.Equal("InProgress", await resolver.ResolveAsync(Item("Active"), CancellationToken.None));
        Assert.Equal("Completed", await resolver.ResolveAsync(Item("Closed", type: "Bug"), CancellationToken.None));

        await _client.Received(1).GetWorkItemTypeStatesAsync(Connection, "Alpha", "Task", Arg.Any<CancellationToken>());
        await _client.Received(1).GetWorkItemTypeStatesAsync(Connection, "Alpha", "Bug", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("Odd")]
    public async Task Returns_null_for_unknown_state_or_category(string state) =>
        Assert.Null(await CreateResolver().ResolveAsync(Item(state), CancellationToken.None));

    [Fact]
    public async Task Returns_null_when_required_fields_are_missing()
    {
        Assert.Null(await CreateResolver().ResolveAsync(Item("Closed", project: null), CancellationToken.None));
        await _client.DidNotReceiveWithAnyArgs().GetWorkItemTypeStatesAsync(default!, default!, default!, default);
    }
}

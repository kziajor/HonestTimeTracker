using HonestTimeTracker.Infrastructure.Security;

namespace HonestTimeTracker.IntegrationTests.Security;

public class DpapiSecretProtectorTests
{
    [Fact]
    public void Round_trips_secret_without_storing_plain_text()
    {
        var protector = new DpapiSecretProtector();

        var protectedText = protector.Protect("my-personal-access-token");

        Assert.DoesNotContain("my-personal-access-token", protectedText);
        Assert.Equal("my-personal-access-token", protector.Unprotect(protectedText));
    }
}

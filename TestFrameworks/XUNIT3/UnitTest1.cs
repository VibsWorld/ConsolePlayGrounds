namespace XUNIT3;

public class UnitTest1
{
    [Fact]
    public void Skip()
    {
        Assert.Skip("Skipping this test for demonstration.");
    }

    [Fact]
    public void SkipUnless()
    {
        Assert.SkipUnless(
            Environment.OSVersion.Platform == PlatformID.Win32NT,
            "Only runs on Windows."
        );
    }

    [Fact]
    public void SkipWhen()
    {
        bool featureFlag = false;
        Assert.SkipWhen(featureFlag, "Skipping because feature flag is enabled.");
    }
}

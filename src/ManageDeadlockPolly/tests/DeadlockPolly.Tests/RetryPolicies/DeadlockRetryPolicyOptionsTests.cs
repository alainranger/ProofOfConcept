using DeadlockPolly.Core.RetryPolicies;

namespace DeadlockPolly.Tests.RetryPolicies;

public class DeadlockRetryPolicyOptionsTests
{
    [Fact]
    public void Validate_WithDefaultOptions_DoesNotThrow()
    {
        var options = new DeadlockRetryPolicyOptions();
        options.Validate(); // doit passer sans exception
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidMaxRetries_Throws(int maxRetries)
    {
        var options = new DeadlockRetryPolicyOptions { MaxRetries = maxRetries };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidInitialDelayMs_Throws(int delayMs)
    {
        var options = new DeadlockRetryPolicyOptions { InitialDelayMs = delayMs };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithNegativeMaxJitterMs_Throws()
    {
        var options = new DeadlockRetryPolicyOptions { MaxJitterMs = -1 };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithZeroMaxJitterMs_DoesNotThrow()
    {
        // Jitter = 0 est valide (pas de jitter)
        var options = new DeadlockRetryPolicyOptions { MaxJitterMs = 0 };
        options.Validate();
    }
}

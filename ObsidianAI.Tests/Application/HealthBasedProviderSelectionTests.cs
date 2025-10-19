namespace ObsidianAI.Tests.Application;

using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Ports;

public class HealthBasedProviderSelectionTests
{
    private readonly IAIClientFactory _factoryMock;
    private readonly ILogger<HealthBasedProviderSelection> _loggerMock;
    private readonly IOptions<AIProviderOptions> _options;

    public HealthBasedProviderSelectionTests()
    {
        _factoryMock = Substitute.For<IAIClientFactory>();
        _loggerMock = Substitute.For<ILogger<HealthBasedProviderSelection>>();
        
        _options = Options.Create(new AIProviderOptions
        {
            DefaultProvider = "OpenRouter",
            FallbackProvider = "NanoGpt"
        });
    }

    [Fact]
    public async Task SelectProviderAsync_ShouldReturnUserPreferenceWhenHealthy()
    {
        // Arrange
        var mockClient = Substitute.For<IAIClient>();
        mockClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(true);
        
        _factoryMock.GetClient("OpenRouter").Returns(mockClient);

        var strategy = new HealthBasedProviderSelection(
            _factoryMock,
            _options,
            _loggerMock);

        // Act
        var result = await strategy.SelectProviderAsync("OpenRouter");

        // Assert
        Assert.Equal("OpenRouter", result);
    }

    [Fact]
    public async Task SelectProviderAsync_ShouldFallbackWhenPreferredUnhealthy()
    {
        // Arrange
        var unhealthyClient = Substitute.For<IAIClient>();
        unhealthyClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(false);
        
        var healthyClient = Substitute.For<IAIClient>();
        healthyClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(true);
        
        _factoryMock.GetClient("OpenRouter").Returns(unhealthyClient);
        _factoryMock.GetClient("NanoGpt").Returns(healthyClient);

        var strategy = new HealthBasedProviderSelection(
            _factoryMock,
            _options,
            _loggerMock);

        // Act
        var result = await strategy.SelectProviderAsync("OpenRouter");

        // Assert
        Assert.Equal("NanoGpt", result);
    }
}
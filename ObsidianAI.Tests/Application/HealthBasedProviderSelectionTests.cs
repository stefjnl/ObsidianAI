namespace ObsidianAI.Tests.Application;

using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using ObsidianAI.Domain.Ports;

public class HealthBasedProviderSelectionTests
{
    private readonly IAIClientFactory _factoryMock;
    private readonly ILogger<HealthBasedProviderSelection> _loggerMock;

    public HealthBasedProviderSelectionTests()
    {
        _factoryMock = Substitute.For<IAIClientFactory>();
        _loggerMock = Substitute.For<ILogger<HealthBasedProviderSelection>>();
    }

    [Fact]
    public async Task SelectProviderAsync_ShouldReturnNanoGptWhenHealthy()
    {
        // Arrange
        var mockClient = Substitute.For<IAIClient>();
        mockClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(true);

        _factoryMock.GetClient("NanoGPT").Returns(mockClient);

        var strategy = new HealthBasedProviderSelection(
            _factoryMock,
            _loggerMock);

        // Act
        var result = await strategy.SelectProviderAsync();

        // Assert
        Assert.Equal("NanoGPT", result);
    }

    [Fact]
    public async Task SelectProviderAsync_ShouldThrowWhenNanoGptUnhealthy()
    {
        // Arrange
        var unhealthyClient = Substitute.For<IAIClient>();
        unhealthyClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(false);

        _factoryMock.GetClient("NanoGPT").Returns(unhealthyClient);

        var strategy = new HealthBasedProviderSelection(
            _factoryMock,
            _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<System.InvalidOperationException>(
            () => strategy.SelectProviderAsync());
    }
}
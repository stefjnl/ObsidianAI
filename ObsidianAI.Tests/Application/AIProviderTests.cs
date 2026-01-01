namespace ObsidianAI.Tests.Application;

using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Models;

public class AIProviderTests
{
    private readonly IAIClientFactory _factoryMock;
    private readonly ILogger<AIProvider> _loggerMock;
    private readonly IOptions<AIProviderOptions> _options;

    public AIProviderTests()
    {
        _factoryMock = Substitute.For<IAIClientFactory>();
        _loggerMock = Substitute.For<ILogger<AIProvider>>();

        _options = Options.Create(new AIProviderOptions
        {
            DefaultProvider = "NanoGPT",
            DefaultModel = "zai-org/glm-4.7"
        });
    }

    [Fact]
    public async Task GenerateContentAsync_ShouldCallNanoGptProvider()
    {
        // Arrange
        var mockClient = Substitute.For<IAIClient>();
        mockClient.ProviderName.Returns("NanoGPT");
        mockClient.CallAsync(Arg.Any<AIRequest>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new AIResponse
            {
                Content = "Test response",
                ProviderName = "NanoGPT"
            });

        _factoryMock.GetClient("NanoGPT").Returns(mockClient);

        var provider = new AIProvider(
            _factoryMock,
            _options,
            _loggerMock);

        // Act
        var result = await provider.GenerateContentAsync("Test prompt");

        // Assert
        Assert.Equal("Test response", result);
        await mockClient.Received(1).CallAsync(
            Arg.Any<AIRequest>(),
            Arg.Any<System.Threading.CancellationToken>());
    }

    [Fact]
    public async Task GenerateContentAsync_ShouldThrowWhenPromptIsEmpty()
    {
        // Arrange
        var provider = new AIProvider(
            _factoryMock,
            _options,
            _loggerMock);

        // Act & Assert
        await Assert.ThrowsAsync<System.ArgumentException>(
            () => provider.GenerateContentAsync(""));
    }

    [Fact]
    public async Task IsProviderAvailableAsync_ShouldReturnTrueForHealthyProvider()
    {
        // Arrange
        var mockClient = Substitute.For<IAIClient>();
        mockClient.IsHealthyAsync(Arg.Any<System.Threading.CancellationToken>()).Returns(true);

        _factoryMock.GetClient("NanoGPT").Returns(mockClient);

        var provider = new AIProvider(
            _factoryMock,
            _options,
            _loggerMock);

        // Act
        var result = await provider.IsProviderAvailableAsync("NanoGPT");

        // Assert
        Assert.True(result);
    }
}
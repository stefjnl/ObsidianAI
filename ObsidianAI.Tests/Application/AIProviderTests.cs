namespace ObsidianAI.Tests.Application;

using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ObsidianAI.Application.Services;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Domain.Models;

public class AIProviderTests
{
    private readonly IAIClientFactory _factoryMock;
    private readonly IProviderSelectionStrategy _strategyMock;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AIProvider> _loggerMock;
    private readonly IOptions<AIProviderOptions> _options;

    public AIProviderTests()
    {
        _factoryMock = Substitute.For<IAIClientFactory>();
        _strategyMock = Substitute.For<IProviderSelectionStrategy>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = Substitute.For<ILogger<AIProvider>>();
        
        _options = Options.Create(new AIProviderOptions
        {
            DefaultProvider = "OpenRouter",
            EnableCaching = false,
            EnableFallback = false
        });
    }

    [Fact]
    public async Task GenerateContentAsync_ShouldCallSelectedProvider()
    {
        // Arrange
        var mockClient = Substitute.For<IAIClient>();
        mockClient.ProviderName.Returns("OpenRouter");
        mockClient.CallAsync(Arg.Any<AIRequest>(), Arg.Any<System.Threading.CancellationToken>())
            .Returns(new AIResponse
            {
                Content = "Test response",
                ProviderName = "OpenRouter"
            });

        _factoryMock.GetClient("OpenRouter").Returns(mockClient);
        _strategyMock.SelectProviderAsync(null, Arg.Any<System.Threading.CancellationToken>())
            .Returns("OpenRouter");

        var provider = new AIProvider(
            _factoryMock,
            _strategyMock,
            _cache,
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
            _strategyMock,
            _cache,
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
        
        _factoryMock.GetClient("OpenRouter").Returns(mockClient);

        var provider = new AIProvider(
            _factoryMock,
            _strategyMock,
            _cache,
            _options,
            _loggerMock);

        // Act
        var result = await provider.IsProviderAvailableAsync("OpenRouter");

        // Assert
        Assert.True(result);
    }
}
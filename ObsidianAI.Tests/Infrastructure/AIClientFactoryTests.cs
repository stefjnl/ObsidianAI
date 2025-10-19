namespace ObsidianAI.Tests.Infrastructure;

using System.Linq;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.LLM.Factories;

public class AIClientFactoryTests
{
    [Fact]
    public void GetClient_ShouldReturnClientWhenExists()
    {
        // Arrange
        var client1 = Substitute.For<IAIClient>();
        client1.ProviderName.Returns("OpenRouter");
        
        var client2 = Substitute.For<IAIClient>();
        client2.ProviderName.Returns("NanoGpt");
        
        var clients = new[] { client1, client2 };
        var logger = Substitute.For<ILogger<AIClientFactory>>();
        
        var factory = new AIClientFactory(clients, logger);

        // Act
        var result = factory.GetClient("OpenRouter");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OpenRouter", result.ProviderName);
    }

    [Fact]
    public void GetClient_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var clients = new IAIClient[0];
        var logger = Substitute.For<ILogger<AIClientFactory>>();
        
        var factory = new AIClientFactory(clients, logger);

        // Act
        var result = factory.GetClient("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAllClients_ShouldReturnAllRegisteredClients()
    {
        // Arrange
        var client1 = Substitute.For<IAIClient>();
        var client2 = Substitute.For<IAIClient>();
        
        var clients = new[] { client1, client2 };
        var logger = Substitute.For<ILogger<AIClientFactory>>();
        
        var factory = new AIClientFactory(clients, logger);

        // Act
        var result = factory.GetAllClients();

        // Assert
        Assert.Equal(2, result.Count());
    }
}
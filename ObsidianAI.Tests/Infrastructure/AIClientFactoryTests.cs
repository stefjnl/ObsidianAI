namespace ObsidianAI.Tests.Infrastructure;

using System;
using System.Linq;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.LLM.Factories;
using ObsidianAI.Infrastructure.LLM;

public class AIClientFactoryTests
{
    [Fact]
    public void GetClient_ShouldReturnNullWhenProviderNotExists()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(Arg.Any<Type>()).Returns((object?)null);
        
        var logger = Substitute.For<ILogger<AIClientFactory>>();
        var factory = new AIClientFactory(serviceProvider, logger);

        // Act
        var result = factory.GetClient("NonExistent");

        // Assert
        Assert.Null(result);
    }

    // NOTE: The following tests are integration-level and require real DI container
    // to properly instantiate concrete agent classes. These are tested via integration tests instead.
    // Unit testing the factory with mocks is not feasible due to the sealed concrete agent classes
    // and their complex construction requirements (IChatClient, IConfiguration, etc.).
}
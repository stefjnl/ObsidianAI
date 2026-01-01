namespace ObsidianAI.Tests.Infrastructure;

using System;
using Xunit;

public class AIClientFactoryTests
{
    [Fact(Skip = "AIClientFactory requires integration testing with real DI container and NanoGptChatAgent. " +
                "NanoGptChatAgent is a sealed class with complex dependencies (IChatClient, IConfiguration) " +
                "that cannot be easily mocked in unit tests.")]
    public void GetClient_RequiresIntegrationTest()
    {
        // This test is skipped because AIClientFactory.GetClient() calls GetRequiredService<NanoGptChatAgent>()
        // which requires a properly configured DI container with all NanoGptChatAgent dependencies.
        // Integration tests should verify:
        // 1. Factory returns NanoGPT client for any provider name
        // 2. Factory logs warning when non-NanoGPT provider requested
        // 3. Factory works with real DI container setup
    }

    // NOTE: Integration tests in a separate test project should verify AIClientFactory
    // works correctly with real service provider and NanoGptChatAgent registration.
}
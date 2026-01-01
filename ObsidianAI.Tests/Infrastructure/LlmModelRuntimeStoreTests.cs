namespace ObsidianAI.Tests.Infrastructure;

using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsidianAI.Infrastructure.Configuration;
using ObsidianAI.Infrastructure.LLM;

public class LlmModelRuntimeStoreTests
{
    private static IOptionsMonitor<AppSettings> CreateOptionsMonitor(NanoGptSettings nanoGptSettings)
    {
        var appSettings = new AppSettings
        {
            LLM = new LlmSettings
            {
                NanoGPT = nanoGptSettings
            }
        };

        var monitor = Substitute.For<IOptionsMonitor<AppSettings>>();
        monitor.CurrentValue.Returns(appSettings);
        return monitor;
    }

    [Fact]
    public void Constructor_ShouldSetDefaultModelFromConfig()
    {
        var nanoGpt = new NanoGptSettings
        {
            DefaultModel = "zai-org/glm-4.7",
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" }
            }
        };
        var options = CreateOptionsMonitor(nanoGpt);
        var logger = Substitute.For<ILogger<LlmProviderRuntimeStore>>();

        var store = new LlmProviderRuntimeStore(options, logger);

        Assert.Equal("NanoGPT", store.CurrentProvider);
        Assert.Equal("zai-org/glm-4.7", store.CurrentModel);
    }

    [Fact]
    public void TrySwitchModel_ShouldSwitchToValidModel()
    {
        var nanoGpt = new NanoGptSettings
        {
            DefaultModel = "zai-org/glm-4.7",
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" },
                new NanoGptModelConfig { Name = "MiniMax M2.1", Identifier = "minimax/minimax-m2.1" }
            }
        };
        var options = CreateOptionsMonitor(nanoGpt);
        var logger = Substitute.For<ILogger<LlmProviderRuntimeStore>>();

        var store = new LlmProviderRuntimeStore(options, logger);
        var result = store.TrySwitchModel("minimax/minimax-m2.1", out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("minimax/minimax-m2.1", store.CurrentModel);
    }

    [Fact]
    public void TrySwitchModel_ShouldFailForInvalidModel()
    {
        var nanoGpt = new NanoGptSettings
        {
            DefaultModel = "zai-org/glm-4.7",
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" }
            }
        };
        var options = CreateOptionsMonitor(nanoGpt);
        var logger = Substitute.For<ILogger<LlmProviderRuntimeStore>>();

        var store = new LlmProviderRuntimeStore(options, logger);
        var result = store.TrySwitchModel("invalid-model", out var error);

        Assert.False(result);
        Assert.NotNull(error);
        Assert.Equal("zai-org/glm-4.7", store.CurrentModel); // unchanged
    }

    [Fact]
    public void GetAvailableModels_ShouldReturnAllConfiguredModels()
    {
        var nanoGpt = new NanoGptSettings
        {
            DefaultModel = "zai-org/glm-4.7",
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" },
                new NanoGptModelConfig { Name = "MiniMax M2.1", Identifier = "minimax/minimax-m2.1" }
            }
        };
        var options = CreateOptionsMonitor(nanoGpt);
        var logger = Substitute.For<ILogger<LlmProviderRuntimeStore>>();

        var store = new LlmProviderRuntimeStore(options, logger);
        var models = store.GetAvailableModels();

        Assert.Equal(2, models.Count);
    }
}

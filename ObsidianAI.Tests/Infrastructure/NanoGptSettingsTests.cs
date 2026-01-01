namespace ObsidianAI.Tests.Infrastructure;

using System.Linq;
using Xunit;
using ObsidianAI.Infrastructure.Configuration;

public class NanoGptSettingsTests
{
    [Fact]
    public void Models_ShouldBeEmptyByDefault()
    {
        var settings = new NanoGptSettings();
        Assert.NotNull(settings.Models);
        Assert.Empty(settings.Models);
    }

    [Fact]
    public void Models_ShouldContainConfiguredModels()
    {
        var settings = new NanoGptSettings
        {
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" },
                new NanoGptModelConfig { Name = "MiniMax M2.1", Identifier = "minimax/minimax-m2.1" }
            }
        };

        Assert.Equal(2, settings.Models.Length);
        Assert.Equal("GLM 4.7", settings.Models[0].Name);
        Assert.Equal("zai-org/glm-4.7", settings.Models[0].Identifier);
    }

    [Fact]
    public void GetModelIdentifierByName_ShouldReturnIdentifier()
    {
        var settings = new NanoGptSettings
        {
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" }
            }
        };

        var identifier = settings.GetModelIdentifierByName("GLM 4.7");
        Assert.Equal("zai-org/glm-4.7", identifier);
    }

    [Fact]
    public void GetModelIdentifierByName_ShouldReturnNullForUnknownModel()
    {
        var settings = new NanoGptSettings
        {
            Models = new[]
            {
                new NanoGptModelConfig { Name = "GLM 4.7", Identifier = "zai-org/glm-4.7" }
            }
        };

        var identifier = settings.GetModelIdentifierByName("Unknown Model");
        Assert.Null(identifier);
    }

    [Fact]
    public void GetAvailableModelNames_ShouldReturnAllNames()
    {
        var settings = new NanoGptSettings
        {
            Models = new[]
            {
                new NanoGptModelConfig { Name = "Model A", Identifier = "id-a" },
                new NanoGptModelConfig { Name = "Model B", Identifier = "id-b" }
            }
        };

        var names = settings.GetAvailableModelNames().ToList();
        Assert.Equal(2, names.Count);
        Assert.Contains("Model A", names);
        Assert.Contains("Model B", names);
    }
}

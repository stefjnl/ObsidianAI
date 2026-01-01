using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using ObsidianAI.Application.Contracts;
using ObsidianAI.Application.Services;
using ObsidianAI.Application.UseCases;
using Xunit;

namespace ObsidianAI.Tests.Application;

public sealed class ListVaultContentsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidMcpClient_ReturnsVaultContents()
    {
        // Arrange
        var mockMcpClientProvider = Substitute.For<IMcpClientProvider>();
        var mockMcpClient = Substitute.For<McpClient>();
        var mockVaultIndexCache = Substitute.For<IVaultIndexCache>();
        var logger = NullLogger<ListVaultContentsUseCase>.Instance;

        // Setup the mock to return the client
        mockMcpClientProvider.GetClientAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<McpClient?>(mockMcpClient));

        // Setup the mock client to return a valid response
        var mockResponse = CreateMockCallToolResult(new List<string>
        {
            "folder1/",
            "file1.md",
            "file2.txt"
        });

        mockMcpClient.CallToolAsync(
            "obsidian_list_files_in_vault",
            Arg.Any<Dictionary<string, object?>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => ValueTask.FromResult(mockResponse));

        // Setup cache to return false (not cached)
        mockVaultIndexCache.TryGet(Arg.Any<string>(), out Arg.Any<VaultIndexEntry?>())
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });

        var useCase = new ListVaultContentsUseCase(
            mockMcpClientProvider,
            mockVaultIndexCache,
            logger);

        // Act
        var result = await useCase.ExecuteAsync(null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal("/", result.CurrentPath);
        Assert.Equal(3, result.Items.Count);

        // Verify the cache was called
        mockVaultIndexCache.Received(1).Set(
            Arg.Any<string>(),
            Arg.Any<List<string>>(),
            Arg.Any<IReadOnlyList<VaultItemDto>>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullMcpClient_ReturnsEmptyContents()
    {
        // Arrange
        var mockMcpClientProvider = Substitute.For<IMcpClientProvider>();
        var mockVaultIndexCache = Substitute.For<IVaultIndexCache>();
        var logger = NullLogger<ListVaultContentsUseCase>.Instance;

        // Setup the mock to return null (MCP client unavailable)
        mockMcpClientProvider.GetClientAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<McpClient?>(null));

        // Setup cache to return false (not cached)
        mockVaultIndexCache.TryGet(Arg.Any<string>(), out Arg.Any<VaultIndexEntry?>())
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });

        var useCase = new ListVaultContentsUseCase(
            mockMcpClientProvider,
            mockVaultIndexCache,
            logger);

        // Act
        var result = await useCase.ExecuteAsync(null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.Equal("/", result.CurrentPath);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WithFolderPath_CallsCorrectTool()
    {
        // Arrange
        var mockMcpClientProvider = Substitute.For<IMcpClientProvider>();
        var mockMcpClient = Substitute.For<McpClient>();
        var mockVaultIndexCache = Substitute.For<IVaultIndexCache>();
        var logger = NullLogger<ListVaultContentsUseCase>.Instance;

        mockMcpClientProvider.GetClientAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<McpClient?>(mockMcpClient));

        var mockResponse = CreateMockCallToolResult(new List<string>
        {
            "subfolder/",
            "subfile.md"
        });

        mockMcpClient.CallToolAsync(
            "obsidian_list_files_in_dir",
            Arg.Is<Dictionary<string, object?>>(d => d.ContainsKey("dirpath") && d["dirpath"]!.ToString() == "TestFolder"),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo => ValueTask.FromResult(mockResponse));

        mockVaultIndexCache.TryGet(Arg.Any<string>(), out Arg.Any<VaultIndexEntry?>())
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });

        var useCase = new ListVaultContentsUseCase(
            mockMcpClientProvider,
            mockVaultIndexCache,
            logger);

        // Act
        var result = await useCase.ExecuteAsync("TestFolder", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestFolder", result.CurrentPath);
        Assert.Equal(2, result.Items.Count);

        // Verify the correct tool was called
        await mockMcpClient.Received(1).CallToolAsync(
            "obsidian_list_files_in_dir",
            Arg.Is<Dictionary<string, object?>>(d => d.ContainsKey("dirpath")),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    private static CallToolResult CreateMockCallToolResult(List<string> paths)
    {
        // Create a JSON array format response (as the MCP server returns)
        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(paths);
        var textContent = new TextContentBlock { Text = jsonResponse };
        var contentBlocks = new List<ContentBlock> { textContent };

        return new CallToolResult
        {
            Content = contentBlocks
        };
    }
}

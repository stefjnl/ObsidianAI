using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ObsidianAI.Api.Services;
using Xunit;

namespace ObsidianAI.Tests.Api;

/// <summary>
/// Tests for McpClientService focusing on thread-safe concurrent access patterns.
/// Verifies that the Lazy&lt;Task&lt;T&gt;&gt; pattern correctly handles race conditions.
/// </summary>
public sealed class McpClientServiceTests : IDisposable
{
    private readonly TestLogger _logger;
    private string? _originalMcpEndpoint;

    public McpClientServiceTests()
    {
        _logger = new TestLogger();
        _originalMcpEndpoint = Environment.GetEnvironmentVariable("MCP_ENDPOINT");
    }

    public void Dispose()
    {
        // Restore original environment variable
        if (_originalMcpEndpoint != null)
        {
            Environment.SetEnvironmentVariable("MCP_ENDPOINT", _originalMcpEndpoint);
        }
        else
        {
            Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        }
    }

    /// <summary>
    /// Simple test logger that tracks log calls for verification.
    /// </summary>
    private class TestLogger : ILogger<McpClientService>
    {
        private readonly List<(LogLevel Level, string Message)> _logs = new();
        private readonly object _lock = new();

        public IReadOnlyList<(LogLevel Level, string Message)> Logs
        {
            get
            {
                lock (_lock)
                {
                    return _logs.ToList();
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            lock (_lock)
            {
                _logs.Add((logLevel, message));
            }
        }
    }

    [Fact]
    public async Task GetClientAsync_ConcurrentCalls_ReturnsSameInstance()
    {
        // Arrange: Set up valid MCP endpoint (will fail to connect but that's ok for this test)
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", "http://localhost:9999/mcp");
        var service = new McpClientService(_logger);

        // Act: Simulate 100 concurrent callers
        const int concurrentCallers = 100;
        var tasks = Enumerable.Range(0, concurrentCallers)
            .Select(_ => Task.Run(() => service.GetClientAsync(CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All calls should return the same result (null in this case since endpoint is invalid)
        // The key is that all tasks complete and get the same cached result
        var firstResult = results[0];
        Assert.All(results, result => Assert.Equal(firstResult, result));
    }

    [Fact]
    public async Task GetClientAsync_ConcurrentCalls_InitializesOnlyOnce()
    {
        // Arrange: Set up invalid endpoint to trigger initialization failure
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", "http://localhost:9999/mcp");
        var service = new McpClientService(_logger);

        // Act: Simulate 50 concurrent callers
        const int concurrentCallers = 50;
        var tasks = Enumerable.Range(0, concurrentCallers)
            .Select(_ => Task.Run(() => service.GetClientAsync(CancellationToken.None)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert: Verify that initialization was attempted exactly once
        // We should see exactly one "Failed to connect to MCP server" warning
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("Failed to connect to MCP server"));
        
        Assert.Equal(1, warnings);
    }

    [Fact]
    public async Task GetClientAsync_WithoutMcpEndpoint_ReturnsNull()
    {
        // Arrange: Clear MCP endpoint
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        var service = new McpClientService(_logger);

        // Act
        var client = await service.GetClientAsync(CancellationToken.None);

        // Assert
        Assert.Null(client);
        
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("MCP_ENDPOINT environment variable not set"));
        
        Assert.Equal(1, warnings);
    }

    [Fact]
    public async Task GetClientAsync_ConcurrentCalls_AllGetSameNull()
    {
        // Arrange: Clear MCP endpoint to ensure null result
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        var service = new McpClientService(_logger);

        // Act: Simulate 100 concurrent callers
        const int concurrentCallers = 100;
        var tasks = Enumerable.Range(0, concurrentCallers)
            .Select(_ => Task.Run(() => service.GetClientAsync(CancellationToken.None)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All calls should return null
        Assert.All(results, result => Assert.Null(result));
        
        // Verify warning logged exactly once (initialization happens once)
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("MCP_ENDPOINT environment variable not set"));
        
        Assert.Equal(1, warnings);
    }

    [Fact]
    public async Task StartAsync_WithInvalidEndpoint_LogsErrorAndContinues()
    {
        // Arrange: Set invalid endpoint
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", "http://localhost:9999/mcp");
        var service = new McpClientService(_logger);

        // Act: StartAsync should not throw even if initialization fails
        var exception = await Record.ExceptionAsync(async () =>
            await service.StartAsync(CancellationToken.None));

        // Assert: No exception should be thrown
        Assert.Null(exception);
        
        // Verify error logging
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("Failed to connect to MCP server"));
        
        Assert.Equal(1, warnings);
    }

    [Fact]
    public async Task StartAsync_WithoutMcpEndpoint_LogsWarningAndContinues()
    {
        // Arrange: Clear MCP endpoint
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        var service = new McpClientService(_logger);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert: Should log warning about missing endpoint and about null result
        var missingEndpointWarnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("MCP_ENDPOINT environment variable not set"));
        
        var nullResultWarnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("MCP client initialization returned null"));
        
        Assert.Equal(1, missingEndpointWarnings);
        Assert.Equal(1, nullResultWarnings);
    }

    [Fact]
    public async Task GetClientAsync_AfterStartAsyncCompletes_ReturnsCachedResult()
    {
        // Arrange: Set up service with no endpoint
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        var service = new McpClientService(_logger);

        // Act: Call StartAsync first (eager initialization)
        await service.StartAsync(CancellationToken.None);
        
        // Then call GetClientAsync multiple times
        var client1 = await service.GetClientAsync(CancellationToken.None);
        var client2 = await service.GetClientAsync(CancellationToken.None);
        var client3 = await service.GetClientAsync(CancellationToken.None);

        // Assert: All calls should return null and initialization should happen only once
        Assert.Null(client1);
        Assert.Null(client2);
        Assert.Null(client3);
        
        // Verify warning logged exactly once (during StartAsync)
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("MCP_ENDPOINT environment variable not set"));
        
        Assert.Equal(1, warnings);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", null);
        var service = new McpClientService(_logger);

        // Act & Assert: StopAsync should complete without exceptions
        var exception = await Record.ExceptionAsync(async () =>
            await service.StopAsync(CancellationToken.None));
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetClientAsync_StressTest_NoConcurrencyIssues()
    {
        // Arrange: This is a stress test to verify no deadlocks or race conditions occur
        Environment.SetEnvironmentVariable("MCP_ENDPOINT", "http://localhost:9999/mcp");
        var service = new McpClientService(_logger);
        
        // Track all unique results
        var results = new ConcurrentBag<object?>();

        // Act: Launch 1000 concurrent tasks
        const int stressTestCalls = 1000;
        var tasks = Enumerable.Range(0, stressTestCalls)
            .Select(_ => Task.Run(async () =>
            {
                var client = await service.GetClientAsync(CancellationToken.None);
                results.Add(client);
            }))
            .ToArray();

        // Wait for all tasks to complete with timeout
        var allTasksTask = Task.WhenAll(tasks);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var completedTask = await Task.WhenAny(allTasksTask, timeoutTask);

        // Assert: All tasks should complete within timeout
        Assert.True(completedTask == allTasksTask, "Stress test did not complete within 10 seconds - possible deadlock");
        
        // All results should be the same (null since endpoint is invalid)
        Assert.Equal(stressTestCalls, results.Count);
        var distinctResults = results.Distinct().ToList();
        Assert.Single(distinctResults);
        
        // Initialization should happen exactly once
        var warnings = _logger.Logs.Count(log => 
            log.Level == LogLevel.Warning && 
            log.Message.Contains("Failed to connect to MCP server"));
        
        Assert.Equal(1, warnings);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ObsidianAI.Infrastructure.Middleware;
using Xunit;

using FunctionContext = ObsidianAI.Infrastructure.Middleware.FunctionInvocationContext;

namespace ObsidianAI.Tests.Infrastructure;

public sealed class TestFunctionCallMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AllowsNonDestructiveCalls()
    {
        using var harness = MiddlewareHarness.Create();
        var middleware = harness.Middleware;
        var context = CreateContext("obsidian_append_content", new Dictionary<string, object?> { { "path", "vault/note.md" } });

        var nextInvocations = 0;
        ValueTask<object?> Next()
        {
            nextInvocations++;
            return ValueTask.FromResult<object?>("APPENDED");
        }

        var result = await middleware.InvokeAsync(context, Next, CancellationToken.None);

        Assert.Equal("APPENDED", result);
        Assert.False(context.Terminate);
        Assert.Equal(1, nextInvocations);
        Assert.Contains(harness.Logs, entry => entry.Level == LogLevel.Information && entry.Message.Contains("obsidian_append_content", StringComparison.Ordinal));
        Assert.Contains(harness.Logs, entry => entry.Message.Contains("\"path\":\"vault/note.md\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_BlocksDeleteOperations()
    {
        using var harness = MiddlewareHarness.Create();
        var middleware = harness.Middleware;
        var context = CreateContext("obsidian_delete_file", new Dictionary<string, object?> { { "path", "vault/note.md" } });

        var nextInvoked = false;
        ValueTask<object?> Next()
        {
            nextInvoked = true;
            return ValueTask.FromResult<object?>(null);
        }

        var result = await middleware.InvokeAsync(context, Next, CancellationToken.None);

        Assert.Equal("DELETE BLOCKED BY TEST MIDDLEWARE", result);
        Assert.True(context.Terminate);
        Assert.False(nextInvoked);
        Assert.Contains(harness.Logs, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("DESTRUCTIVE", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_DelegatesToNext()
    {
        using var harness = MiddlewareHarness.Create();
        var middleware = harness.Middleware;
        var context = CreateContext("obsidian_append_content", new Dictionary<string, object?> { { "payload", "{}" } });

        var nextCallCount = 0;
        ValueTask<object?> Next()
        {
            nextCallCount++;
            if (nextCallCount == 1)
            {
                throw new InvalidOperationException("Boom");
            }

            return ValueTask.FromResult<object?>("RECOVERED");
        }

        var result = await middleware.InvokeAsync(context, Next, CancellationToken.None);

        Assert.Equal("RECOVERED", result);
        Assert.Equal(2, nextCallCount);
        Assert.Contains(harness.Logs, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Test middleware failed", StringComparison.Ordinal));
    }

    private static FunctionContext CreateContext(string functionName, object? arguments = null)
    {
        var func = AIFunctionFactory.Create(
            (Func<ValueTask<object?>>)(() => ValueTask.FromResult<object?>(null)),
            name: functionName);

        var argsDict = new Dictionary<string, object?>();
        if (arguments is IReadOnlyDictionary<string, object?> readOnly)
        {
            foreach (var kvp in readOnly)
            {
                argsDict[kvp.Key] = kvp.Value;
            }
        }
        else if (arguments is IDictionary<string, object?> dict)
        {
            foreach (var kvp in dict)
            {
                argsDict[kvp.Key] = kvp.Value;
            }
        }
        else if (arguments is not null)
        {
            argsDict["value"] = arguments;
        }

        return new FunctionContext(
            function: func,
            arguments: argsDict
        );
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class MiddlewareHarness : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly LogCollectorProvider _provider;

        private MiddlewareHarness(ILoggerFactory loggerFactory, LogCollectorProvider provider, TestFunctionCallMiddleware middleware)
        {
            _loggerFactory = loggerFactory;
            _provider = provider;
            Middleware = middleware;
        }

        public TestFunctionCallMiddleware Middleware { get; }

        public IReadOnlyList<LogEntry> Logs => _provider.Entries;

        public static MiddlewareHarness Create()
        {
            var provider = new LogCollectorProvider();
            var factory = LoggerFactory.Create(builder => builder.AddProvider(provider));
            var logger = factory.CreateLogger<TestFunctionCallMiddleware>();
            var middleware = new TestFunctionCallMiddleware(logger);
            return new MiddlewareHarness(factory, provider, middleware);
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        private sealed class LogCollectorProvider : ILoggerProvider
        {
            private readonly List<LogEntry> _entries = new();

            public IReadOnlyList<LogEntry> Entries => _entries;

            public ILogger CreateLogger(string categoryName) => new LogCollectorLogger(_entries);

            public void Dispose()
            {
            }

            private sealed class LogCollectorLogger : ILogger
            {
                private readonly List<LogEntry> _entries;

                public LogCollectorLogger(List<LogEntry> entries)
                {
                    _entries = entries;
                }

                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                {
                    var message = formatter(state, exception);
                    _entries.Add(new LogEntry(logLevel, message, exception));
                }
            }
        }
    }
}

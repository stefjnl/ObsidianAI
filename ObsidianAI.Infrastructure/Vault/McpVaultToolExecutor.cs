namespace ObsidianAI.Infrastructure.Vault
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using ModelContextProtocol.Protocol;
    using ObsidianAI.Application.Services;
    using ObsidianAI.Domain.Models;
    using ObsidianAI.Domain.Ports;

    /// <summary>
    /// Infrastructure implementation of IVaultToolExecutor that wraps Model Context Protocol (MCP) client tool calls
    /// for vault file operations (append, patch, delete, create).
    /// </summary>
    public class McpVaultToolExecutor : IVaultToolExecutor
    {
    private readonly IMcpClientProvider _clientProvider;
    private readonly ILogger<McpVaultToolExecutor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpVaultToolExecutor"/> class.
        /// </summary>
        /// <param name="clientProvider">Provider for accessing the MCP client instance.</param>
        /// <param name="logger">Logger used to record execution details.</param>
        public McpVaultToolExecutor(IMcpClientProvider clientProvider, ILogger<McpVaultToolExecutor> logger)
        {
            _clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<OperationResult> AppendAsync(string filePath, string content, CancellationToken ct = default)
        {
            var args = new Dictionary<string, object?>
            {
                ["filepath"] = filePath,
                ["content"] = content
            };

            return await ExecuteAsync("obsidian_append_content", args, filePath, ct);
        }

        /// <inheritdoc/>
        public async Task<OperationResult> PatchAsync(string filePath, string content, string operation, CancellationToken ct = default)
        {
            var args = new Dictionary<string, object?>
            {
                ["filepath"] = filePath,
                ["content"] = content,
                ["operation"] = operation
            };

            return await ExecuteAsync("obsidian_patch_content", args, filePath, ct);
        }

        /// <inheritdoc/>
        public async Task<OperationResult> DeleteAsync(string filePath, CancellationToken ct = default)
        {
            var args = new Dictionary<string, object?>
            {
                ["filepath"] = filePath
            };

            return await ExecuteAsync("obsidian_delete_file", args, filePath, ct);
        }

        /// <inheritdoc/>
        public async Task<OperationResult> CreateAsync(string filePath, string content, CancellationToken ct = default)
        {
            var args = new Dictionary<string, object?>
            {
                ["filepath"] = filePath,
                ["content"] = content
            };

            return await ExecuteAsync("obsidian_create_file", args, filePath, ct);
        }

        /// <summary>
        /// Executes an MCP tool call and maps the result to an OperationResult.
        /// </summary>
        /// <param name="toolName">The name of the MCP tool to execute.</param>
        /// <param name="args">The arguments to pass to the tool.</param>
        /// <param name="filePath">The file path associated with the operation.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
        private async Task<OperationResult> ExecuteAsync(string toolName, IReadOnlyDictionary<string, object?> args, string filePath, CancellationToken ct)
        {
            try
            {
                var client = await _clientProvider.GetClientAsync(ct).ConfigureAwait(false);
                if (client == null)
                {
                    _logger.LogWarning("MCP client unavailable. Tool {ToolName} cannot be executed.", toolName);
                    return new OperationResult(false, "MCP server is not available. Vault operations are disabled.", filePath);
                }

                var result = await client.CallToolAsync(toolName, args, cancellationToken: ct).ConfigureAwait(false);

                bool isSuccess = !(result.IsError ?? false);
                var message = BuildMessage(result, isSuccess);

                return new OperationResult(isSuccess, message, filePath);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Vault tool {ToolName} execution was cancelled.", toolName);
                return new OperationResult(false, "Operation was cancelled.", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing vault tool {ToolName} for {FilePath}", toolName, filePath);
                return new OperationResult(false, ex.Message, filePath);
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static string BuildMessage(CallToolResult result, bool isSuccess)
        {
            if (result.Content is null)
            {
                return isSuccess ? "Operation completed successfully." : "Operation failed: No response from tool.";
            }

            var builder = new StringBuilder();
            foreach (var block in result.Content)
            {
                var formatted = FormatContentBlock(block);
                if (string.IsNullOrWhiteSpace(formatted))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(formatted.Trim());
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }

            return isSuccess ? "Operation completed successfully." : "Operation failed: Tool response contained no readable content.";
        }

        private static string FormatContentBlock(ContentBlock block)
        {
            switch (block)
            {
                case TextContentBlock textBlock:
                    return textBlock.Text ?? string.Empty;
                default:
                    var valueProp = block.GetType().GetProperty("Value");
                    if (valueProp != null)
                    {
                        var value = valueProp.GetValue(block);
                        var serialized = SerializeValue(value);
                        if (!string.IsNullOrWhiteSpace(serialized))
                        {
                            return serialized!;
                        }
                    }

                    try
                    {
                        return JsonSerializer.Serialize(block, JsonOptions);
                    }
                    catch
                    {
                        return block.ToString() ?? string.Empty;
                    }
            }
        }

        private static string? SerializeValue(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s)
            {
                return s;
            }

            if (value is JsonElement element)
            {
                return element.GetRawText();
            }

            if (value.GetType().FullName is "System.BinaryData")
            {
                var text = value.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                try
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(text);
                    return parsed.GetRawText();
                }
                catch (JsonException)
                {
                    return text;
                }
            }

            try
            {
                return JsonSerializer.Serialize(value, JsonOptions);
            }
            catch
            {
                return value.ToString();
            }
        }
    }
}
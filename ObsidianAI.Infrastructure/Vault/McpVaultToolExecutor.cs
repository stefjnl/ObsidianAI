namespace ObsidianAI.Infrastructure.Vault
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ModelContextProtocol.Client;
    using ModelContextProtocol.Protocol;
    using ObsidianAI.Domain.Models;
    using ObsidianAI.Domain.Ports;

    /// <summary>
    /// Infrastructure implementation of IVaultToolExecutor that wraps Model Context Protocol (MCP) client tool calls
    /// for vault file operations (append, patch, delete, create).
    /// </summary>
    public class McpVaultToolExecutor : IVaultToolExecutor
    {
        private readonly McpClient _mcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="McpVaultToolExecutor"/> class.
        /// </summary>
        /// <param name="mcpClient">The MCP client instance used to execute tool calls. Can be null if MCP is unavailable.</param>
        public McpVaultToolExecutor(McpClient mcpClient)
        {
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
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
                var result = await _mcpClient.CallToolAsync(toolName, args);

                bool isSuccess = !(result.IsError ?? false);

                string message = result.Content?.FirstOrDefault() is TextContentBlock t
                    ? t.Text
                    : "Operation failed: No response from tool.";

                return new OperationResult(isSuccess, message, filePath);
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message, filePath);
            }
        }
    }
}
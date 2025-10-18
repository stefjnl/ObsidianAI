using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;

namespace ObsidianAI.Api.Models;

#region Validation Attributes

/// <summary>
/// Validates that a file path is safe and doesn't contain directory traversal attempts.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SafeFilePathAttribute : ValidationAttribute
{
    private static readonly Regex InvalidPathCharsRegex = new(@"[<>:""|?*\x00-\x1F]", RegexOptions.Compiled);
    private static readonly string[] DangerousPatterns = ["..\\", "../", ".."];

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
        {
            return ValidationResult.Success!; // Let [Required] handle null/empty
        }

        if (value is not string path)
        {
            return new ValidationResult("Path must be a string.");
        }

        // Check for directory traversal attempts
        var normalizedPath = path.Replace('\\', '/');
        foreach (var pattern in DangerousPatterns)
        {
            if (normalizedPath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult("Path contains invalid directory traversal pattern (..)");
            }
        }

        // Check for invalid characters
        if (InvalidPathCharsRegex.IsMatch(path))
        {
            return new ValidationResult("Path contains invalid characters.");
        }

        // Check for absolute paths starting with drive letters or network shares
        if (Path.IsPathRooted(path) && !path.StartsWith('/'))
        {
            return new ValidationResult("Absolute paths are not allowed. Use relative paths only.");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validates that a string is a valid vault operation type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class VaultOperationAttribute : ValidationAttribute
{
    private static readonly string[] ValidOperations = ["create", "update", "delete", "rename", "move"];

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
        {
            return ValidationResult.Success!; // Let [Required] handle null/empty
        }

        if (value is not string operation)
        {
            return new ValidationResult("Operation must be a string.");
        }

        var normalizedOp = operation.ToLowerInvariant().Trim();
        if (!Array.Exists(ValidOperations, op => op.Equals(normalizedOp, StringComparison.Ordinal)))
        {
            return new ValidationResult(
                $"Invalid operation. Allowed operations: {string.Join(", ", ValidOperations)}");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validates that pagination parameters are within acceptable ranges.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PaginationRangeAttribute : ValidationAttribute
{
    public int MinValue { get; set; } = 0;
    public int MaxValue { get; set; } = 100;

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success!; // Nullable parameters are valid
        }

        if (value is not int intValue)
        {
            return new ValidationResult("Value must be an integer.");
        }

        if (intValue < MinValue || intValue > MaxValue)
        {
            return new ValidationResult(
                $"Value must be between {MinValue} and {MaxValue}.");
        }

        return ValidationResult.Success!;
    }
}

#endregion

#region Request/Response Models

/// <summary>
/// Request to send a chat message.
/// </summary>
public record ChatRequest(
    [Required]
    [StringLength(10000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 10000 characters")]
    string Message,
    Guid? ConversationId = null);

/// <summary>
/// Response containing chat message details.
/// </summary>
public record ChatMessage(string Role, string Content, FileOperationData? FileOperation = null);

/// <summary>
/// Request to search the vault.
/// </summary>
public record SearchRequest(
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Query must be between 1 and 500 characters")]
    string Query);

/// <summary>
/// Response containing search results.
/// </summary>
public record SearchResponse(List<SearchResult> Results);

/// <summary>
/// Individual search result item.
/// </summary>
public record SearchResult(string FilePath, float Score, string Content);

/// <summary>
/// Request to reorganize vault contents.
/// </summary>
public record ReorganizeRequest(
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Strategy);

/// <summary>
/// Response from reorganize operation.
/// </summary>
public record ReorganizeResponse(string Status, int FilesAffected);

/// <summary>
/// File operation details.
/// </summary>
public record FileOperationData(string Action, string FilePath);

/// <summary>
/// Request to modify vault content.
/// </summary>
public record ModifyRequest(
    [Required]
    [VaultOperation]
    string Operation,
    
    [Required]
    [SafeFilePath]
    [StringLength(512, MinimumLength = 1)]
    string FilePath,
    
    [StringLength(1_000_000, ErrorMessage = "Content must not exceed 1MB")]
    string Content,
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string ConfirmationId);

/// <summary>
/// Response from modify operation.
/// </summary>
public record ModifyResponse(bool Success, string Message, string FilePath);

/// <summary>
/// Request to read a file from the vault.
/// </summary>
public record ReadFileRequest(
    [Required]
    [SafeFilePath]
    [StringLength(512, MinimumLength = 1, ErrorMessage = "File path must be between 1 and 512 characters")]
    string Path);

/// <summary>
/// Request to browse vault contents.
/// </summary>
public record BrowseVaultRequest(
    [SafeFilePath]
    [StringLength(512, ErrorMessage = "Path must not exceed 512 characters")]
    string? Path = null);

/// <summary>
/// Request to list conversations with pagination.
/// </summary>
public record ListConversationsRequest(
    [PaginationRange(MinValue = 0, MaxValue = 1000)]
    int? Skip = null,
    
    [PaginationRange(MinValue = 1, MaxValue = 100)]
    int? Take = null);

/// <summary>
/// Request to create a new conversation.
/// </summary>
public record CreateConversationRequest(
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    string? Title,
    
    [StringLength(100, ErrorMessage = "UserId must not exceed 100 characters")]
    string? UserId);

/// <summary>
/// Request to update conversation details.
/// </summary>
public record UpdateConversationRequest(
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    string? Title,
    bool? IsArchived);

/// <summary>
/// Request to update message artifacts (action card or file operation).
/// </summary>
public record UpdateMessageArtifactsRequest(ActionCardPayload? ActionCard, FileOperationPayload? FileOperation);

/// <summary>
/// Action card payload for updates.
/// </summary>
public record ActionCardPayload(
	string? Id,
	string? Title,
	string? Status,
	string? Operation,
	string? StatusMessage,
	DateTime? CreatedAt,
	DateTime? CompletedAt,
	List<PlannedActionPayload>? PlannedActions);

/// <summary>
/// Planned action details.
/// </summary>
public record PlannedActionPayload(
	string? Id,
	string? Type,
	string? Source,
	string? Destination,
	string? Description,
	string? Operation,
	string? Content,
	int? SortOrder);

/// <summary>
/// File operation payload.
/// </summary>
public record FileOperationPayload(string Action, string FilePath, DateTime? Timestamp);

#endregion


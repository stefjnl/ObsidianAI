using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;

namespace ObsidianAI.Web.Endpoints;

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
            return ValidationResult.Success!;
        }

        if (value is not string path)
        {
            return new ValidationResult("Path must be a string.");
        }

        var normalizedPath = path.Replace('\\', '/');
        foreach (var pattern in DangerousPatterns)
        {
            if (normalizedPath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult("Path contains invalid directory traversal pattern (..)");
            }
        }

        if (InvalidPathCharsRegex.IsMatch(path))
        {
            return new ValidationResult("Path contains invalid characters.");
        }

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
            return ValidationResult.Success!;
        }

        if (value is not string operation)
        {
            return new ValidationResult("Operation must be a string.");
        }

        var normalizedOp = operation.ToLowerInvariant().Trim();
        if (!Array.Exists(ValidOperations, op => op.Equals(normalizedOp, StringComparison.Ordinal)))
        {
            return new ValidationResult($"Invalid operation. Allowed operations: {string.Join(", ", ValidOperations)}");
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
            return ValidationResult.Success!;
        }

        if (value is not int intValue)
        {
            return new ValidationResult("Value must be an integer.");
        }

        if (intValue < MinValue || intValue > MaxValue)
        {
            return new ValidationResult($"Value must be between {MinValue} and {MaxValue}.");
        }

        return ValidationResult.Success!;
    }
}

#endregion

#region Request/Response Models

public record ChatRequest(
    [Required]
    [StringLength(10000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 10000 characters")]
    string Message,
    Guid? ConversationId = null);

public record ChatMessage(string Role, string Content, FileOperationData? FileOperation = null);

public record SearchRequest(
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Query must be between 1 and 500 characters")]
    string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);

public record FileOperationData(string Action, string FilePath);

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

public record ModifyResponse(bool Success, string Message, string FilePath);

public record ReadFileRequest(
    [Required]
    [SafeFilePath]
    [StringLength(512, MinimumLength = 1, ErrorMessage = "File path must be between 1 and 512 characters")]
    string Path);

public record BrowseVaultRequest(
    [SafeFilePath]
    [StringLength(512, ErrorMessage = "Path must not exceed 512 characters")]
    string? Path = null);

public record ListConversationsRequest(
    [PaginationRange(MinValue = 0, MaxValue = 1000)]
    int? Skip = null,
    
    [PaginationRange(MinValue = 1, MaxValue = 100)]
    int? Take = null);

public record CreateConversationRequest(
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    string? Title,
    
    [StringLength(100, ErrorMessage = "UserId must not exceed 100 characters")]
    string? UserId);

public record UpdateConversationRequest(
    [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
    string? Title,
    bool? IsArchived);

public record UpdateMessageArtifactsRequest(ActionCardPayload? ActionCard, FileOperationPayload? FileOperation);

public record ActionCardPayload(
	string? Id,
	string? Title,
	string? Status,
	string? Operation,
	string? StatusMessage,
	DateTime? CreatedAt,
	DateTime? CompletedAt,
	List<PlannedActionPayload>? PlannedActions);

public record PlannedActionPayload(
	string? Id,
	string? Type,
	string? Source,
	string? Destination,
	string? Description,
	string? Operation,
	string? Content,
	int? SortOrder);

public record FileOperationPayload(string Action, string FilePath, DateTime? Timestamp);

#endregion

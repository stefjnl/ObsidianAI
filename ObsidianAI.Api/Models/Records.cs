using System;
using System.Collections.Generic;

namespace ObsidianAI.Api.Models;

public record ChatRequest(string Message, Guid? ConversationId = null);

public record ChatMessage(string Role, string Content, FileOperationData? FileOperation = null);

public record SearchRequest(string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);

public record FileOperationData(string Action, string FilePath);

public record ModifyRequest(string Operation, string FilePath, string Content, string ConfirmationId);

public record ModifyResponse(bool Success, string Message, string FilePath);

public record CreateConversationRequest(string? Title, string? UserId);

public record UpdateConversationRequest(string? Title, bool? IsArchived);

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

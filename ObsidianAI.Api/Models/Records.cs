namespace ObsidianAI.Api.Models;

public record ChatRequest(string Message, List<ChatMessage>? History = null);

public record ChatMessage(string Role, string Content, FileOperationData? FileOperation = null);

public record SearchRequest(string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);

public record FileOperationData(string Action, string FilePath);

public record ModifyRequest(string Operation, string FilePath, string Content, string ConfirmationId);

public record ModifyResponse(bool Success, string Message, string FilePath);
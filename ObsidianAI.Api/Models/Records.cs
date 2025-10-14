namespace ObsidianAI.Api.Models;

public record ChatRequest(string Message, List<ChatMessage>? History = null);

public record ChatMessage(string Role, string Content, FileOperationResultData? FileOperationResult = null);

public record SearchRequest(string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);

public record FileOperationResultData(bool Success, string Operation, string FilePath, string? Message = null);
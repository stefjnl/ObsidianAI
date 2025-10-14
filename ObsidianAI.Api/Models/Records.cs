namespace ObsidianAI.Api.Models;

record ChatRequest(string Message, List<ChatMessage>? History = null);

record ChatMessage(string Role, string Content);

public record SearchRequest(string Query);

public record SearchResponse(List<SearchResult> Results);

public record SearchResult(string FilePath, float Score, string Content);

public record ReorganizeRequest(string Strategy);

public record ReorganizeResponse(string Status, int FilesAffected);
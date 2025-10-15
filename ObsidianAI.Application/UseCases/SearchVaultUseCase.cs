namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Placeholder use case for searching the vault.
/// TODO: Wire to a future search port.
/// </summary>
public class SearchVaultUseCase
{
    /// <summary>
    /// Executes the search vault use case.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The search response with empty results.</returns>
    public Task<Contracts.SearchResponse> ExecuteAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or whitespace.", nameof(query));
        }

        return Task.FromResult(new Contracts.SearchResponse(new System.Collections.Generic.List<Contracts.SearchResultItem>(), 0, 0));
    }
}
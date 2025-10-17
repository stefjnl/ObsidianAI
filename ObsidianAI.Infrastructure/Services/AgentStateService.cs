using System.Collections.Concurrent;

namespace ObsidianAI.Infrastructure.Services;

/// <summary>
/// Service for storing and retrieving agent state data.
/// </summary>
public interface IAgentStateService
{
    /// <summary>
    /// Stores a value with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Retrieves a value by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the value.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>The stored value, or default if not found or wrong type.</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Removes a value by key.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    void Clear(string key);
}

/// <summary>
/// Simple in-memory implementation of IAgentStateService using ConcurrentDictionary.
/// </summary>
public class AgentStateService : IAgentStateService
{
    private readonly ConcurrentDictionary<string, object?> _storage = new();

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage[key] = value;
    }

    /// <inheritdoc />
    public T? Get<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_storage.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    /// <inheritdoc />
    public void Clear(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage.TryRemove(key, out _);
    }
}
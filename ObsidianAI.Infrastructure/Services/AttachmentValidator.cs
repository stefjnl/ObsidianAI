using System.Collections.Generic;
using Microsoft.Extensions.Options;
using ObsidianAI.Domain.Ports;
using ObsidianAI.Infrastructure.Configuration;

namespace ObsidianAI.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IAttachmentValidator"/> using configuration.
/// </summary>
public class AttachmentValidator : IAttachmentValidator
{
    public AttachmentValidator(IOptions<AppSettings> appSettings)
    {
        AllowedFileTypes = appSettings.Value.AllowedAttachmentTypes ?? new[] { ".txt", ".md", ".json" };
    }

    public IReadOnlyList<string> AllowedFileTypes { get; }

    public bool IsFileTypeAllowed(string fileType)
    {
        return AllowedFileTypes.Contains(fileType.ToLowerInvariant());
    }
}
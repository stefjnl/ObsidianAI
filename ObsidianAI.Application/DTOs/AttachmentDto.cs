using System;

namespace ObsidianAI.Application.DTOs;

/// <summary>
/// Attachment data transfer object for API responses.
/// </summary>
public sealed record AttachmentDto(
    Guid Id,
    Guid ConversationId,
    string Filename,
    string Content,
    string FileType,
    DateTime CreatedAt);
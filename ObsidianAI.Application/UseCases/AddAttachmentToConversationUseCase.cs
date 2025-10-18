using System;
using System.Threading;
using System.Threading.Tasks;
using ObsidianAI.Domain.Entities;
using ObsidianAI.Domain.Ports;

namespace ObsidianAI.Application.UseCases;

/// <summary>
/// Use case responsible for adding attachments to conversations.
/// </summary>
public sealed class AddAttachmentToConversationUseCase
{
    private readonly IAttachmentRepository _attachmentRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IAttachmentValidator _attachmentValidator;

    public AddAttachmentToConversationUseCase(
        IAttachmentRepository attachmentRepository,
        IConversationRepository conversationRepository,
        IAttachmentValidator attachmentValidator)
    {
        _attachmentRepository = attachmentRepository;
        _conversationRepository = conversationRepository;
        _attachmentValidator = attachmentValidator;
    }

    /// <summary>
    /// Adds an attachment to the specified conversation.
    /// </summary>
    /// <param name="conversationId">Conversation identifier.</param>
    /// <param name="filename">Original filename.</param>
    /// <param name="content">Text content.</param>
    /// <param name="fileType">File extension.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Attachment> ExecuteAsync(
        Guid conversationId,
        string filename,
        string content,
        string fileType,
        CancellationToken ct = default)
    {
        // Validate conversation exists
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct: ct);
        if (conversation is null)
        {
            throw new ArgumentException("Conversation not found", nameof(conversationId));
        }

        // Validate file type
        if (!_attachmentValidator.IsFileTypeAllowed(fileType))
        {
            throw new ArgumentException($"Unsupported file type '{fileType}'. Allowed types: {string.Join(", ", _attachmentValidator.AllowedFileTypes)}", nameof(fileType));
        }

        var attachment = new Attachment(
            Guid.NewGuid(),
            conversationId,
            filename,
            content,
            fileType);

        await _attachmentRepository.CreateAsync(attachment, ct).ConfigureAwait(false);

        return attachment;
    }
}
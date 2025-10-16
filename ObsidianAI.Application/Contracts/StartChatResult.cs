using System;

namespace ObsidianAI.Application.Contracts;

/// <summary>
/// Represents the result of starting a chat, including the response text and any extracted file operation.
/// </summary>
/// <param name="ConversationId">Identifier of the conversation associated with the interaction.</param>
/// <param name="UserMessageId">Identifier of the persisted user message.</param>
/// <param name="AssistantMessageId">Identifier of the persisted assistant message.</param>
/// <param name="Text">The full text response from the AI agent.</param>
/// <param name="FileOperation">An optional file operation extracted from the response.</param>
public sealed record StartChatResult(
	Guid ConversationId,
	Guid UserMessageId,
	Guid AssistantMessageId,
	string Text,
	ObsidianAI.Domain.Models.FileOperation? FileOperation);
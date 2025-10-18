# Attachment AI Instructions Update

## Issue
The AI was attempting to call `obsidian_get_file_contents` to read attached files even though their content was already provided in the message context. This caused a "404 Not Found" error since the attached files aren't in the Obsidian vault.

## Solution
Updated the `AgentInstructions.ObsidianAssistant` prompt to explicitly instruct the AI about attached files:

```
ATTACHED FILES:
- If the user message includes a section marked [ATTACHED FILES] at the beginning, those files have ALREADY been provided to you
- The content is included inline in the message between '--- BEGIN FILE CONTENT ---' and '--- END FILE CONTENT ---' markers
- DO NOT attempt to read these files using obsidian_get_file_contents or any other tool - you already have their full content
- Simply analyze and reference the attached content directly
- These attached files are NOT in the Obsidian vault - they are external files uploaded by the user
```

## Expected Behavior
When a user attaches a file and asks about it:
1. The file content is uploaded and stored in the database
2. When sending a chat message, the attachment content is prepended to the message with clear markers
3. The AI receives the formatted message with [ATTACHED FILES] section
4. The AI recognizes the content is already provided and analyzes it directly
5. The AI does NOT attempt to call obsidian_get_file_contents or any vault tools

## Example Flow
**User**: Uploads `data.json` and asks "What's in this file?"

**System sends to AI**:
```
[ATTACHED FILES - These files are provided as context for analysis]

File: data.json (.json)
--- BEGIN FILE CONTENT ---
{
  "projects": [
    { "name": ".Net", "note": "to learn" }
  ]
}
--- END FILE CONTENT ---

[USER MESSAGE]
What's in this file?
```

**AI responds**: Analyzes the JSON content directly without calling any tools

## Files Modified
- `ObsidianAI.Api/Configuration/AgentInstructions.cs` - Added ATTACHED FILES section to instructions

## Testing
The updated instructions should hot-reload in the running API. Test by:
1. Attaching a file to a conversation
2. Asking the AI to analyze it
3. Verify the AI doesn't call `obsidian_get_file_contents`
4. Verify the AI analyzes the content directly from the message

## Related Issues
The web logs show JavaScript interop errors during component initialization - these are unrelated to attachments and occur when trying to call JS during server-side prerendering. These should be addressed separately by:
- Wrapping JS calls in `OnAfterRenderAsync` lifecycle method
- Checking if rendering is interactive before calling JS

curl.exe -X POST http://localhost:5095/chat ` -H "Content-Type: application/json" ` -d '{\"message\": \"What tools do you have available?\"}'
{"text":"Here’s a quick overview of the tool set I can use to help you work with GitHub and your Obsidian vault:\n\n| Category | Tool name | What it does |\n|----------|-----------|--------------|\n| **GitHub – Repository & Code** | `create_repository`, `fork_repository` | Make new repos or fork existing ones. |\n| | `list_branches`, `push_files` | View branches, push multiple files in one commit. |\n| | `get_file_contents`, `create_or_update_file`, `delete_file` | Read, create/update, or delete files in a repo. |\n| | `search_code`, `list_commits`, `list_tags` | Search code, list commits or tags. |\n| **GitHub – Issues & Pull Requests** | `create_issue`, `update_issue`, `list_issues`, `get_issue` | Create, update, list, or view issues. |\n| | `add_sub_issue`, `remove_sub_issue`, `reprioritize_sub_issue` | Manage sub‑issues (sub‑tasks). |\n| | `create_pull_request`, `update_pull_request`, `merge_pull_request` | Open, modify, merge PRs. |\n| | `list_pull_requests`, `get_pull_request`, `get_pull_request_diff` | Browse or inspect PRs and diffs. |\n| | `add_comment_to_pending_review`, `create_pending_pull_request_review`, `submit_pending_pull_request_review` | Draft and submit PR reviews. |\n| | `request_copilot_review`, `assign_copilot_to_issue` | Ask Copilot for feedback or assign it to an issue. |\n| **GitHub – Notifications & Workflows** | `list_notifications`, `mark_all_notifications_read`, `manage_notification_subscription` | Check and manage your GitHub notifications. |\n| | `get_workflow_run`, `rerun_failed_jobs`, `run_workflow` | Inspect or trigger Actions workflows. |\n| | `list_workflow_runs`, `list_workflows`, `list_workflow_jobs` | Browse workflow runs and jobs. |\n| **GitHub – Projects & Security** | `create_project_item`, `delete_project_item`, `get_project`, `list_projects` | Work with GitHub Projects. |\n| | `list_global_security_advisories`, `get_global_security_advisory` | Search security advisories. |\n| **GitHub – Miscellaneous** | `star_repository`, `unstar_repository`, `search_repositories`, `search_users`, `search_orgs` | Manage stars, search repos/users/orgs. |\n| **Obsidian – Vault Management** | `obsidian_get_file_contents`, `obsidian_batch_get_file_contents`, `obsidian_list_files_in_vault`, `obsidian_list_files_in_dir` | Read files or list vault contents. |\n| | `obsidian_simple_search`, `obsidian_complex_search` | Find text or complex queries across notes. |\n| | `obsidian_patch_content`, `obsidian_append_content` | Insert, prepend, replace, or append content to a note. |\n| | `obsidian_get_recent_changes`, `obsidian_get_periodic_note`, `obsidian_get_recent_periodic_notes` | Get recent edits or periodic notes (daily/weekly/etc.). |\n\nThese tools let me read, edit, search, and organize your GitHub repos and Obsidian vault directly from our chat. Let me know what you’d like to do next!"}  
PS C:\git\ObsidianAI> curl.exe -X POST http://localhost:5095/chat ` -H "Content-Type: application/json" ` -d '{\"message\": \"What tools do you have available?\"}'
{"text":"Here’s a quick reference of all the tools I can use for working with GitHub and your Obsidian vault:\n\n| Category | Tool name | Purpose |\n|----------|-----------|---------|\n| **GitHub – Repository & File Operations** | `create_or_update_file` <br> `delete_file` <br> `push_files` | Create, update or delete files in a repo; batch‑push several files at once. |\n| **GitHub – Branches & Pull Requests** | `create_branch` <br> `create_pull_request` <br> `update_pull_request` <br> `merge_pull_request` <br> `update_pull_request_branch` | Manage branches, open/modify PRs and merge them. |\n| **GitHub – Issues & Projects** | `create_issue` <br> `update_issue` <br> `list_issues` <br> `add_sub_issue` <br> `remove_sub_issue` <br> `reprioritize_sub_issue` <br> `list_project_items` <br> `get_project_item` | Create/update issues, manage sub‑issues, and work with project items. |\n| **GitHub – Reviews & Comments** | `create_pending_pull_request_review` <br> `add_comment_to_pending_review` <br> `submit_pending_pull_request_review` <br> `request_copilot_review` | Draft a review, add comments to it, submit or request Copilot feedback. |\n| **GitHub – Notifications & Events** | `list_notifications` <br> `manage_notification_subscription` <br> `mark_all_notifications_read` | View and manage your GitHub notifications. |\n| **GitHub – Search & Metadata** | `search_repositories` <br> `search_issues` <br> `search_pull_requests` <br> `list_tags` <br> `get_file_contents` (repo) | Find repos, issues, PRs or tags; read file contents from a repo. |\n| **GitHub – Workflows & Actions** | `run_workflow` <br> `rerun_failed_jobs` <br> `rerun_workflow_run` <br> `get_workflow_run_logs` | Trigger and debug GitHub Actions workflows. |\n| **GitHub – Miscellaneous** | `star_repository`, `unstar_repository`, `fork_repository`, `assign_copilot_to_issue`, etc. | Various small utilities for repo management. |\n\n| Category | Tool name | Purpose |\n|----------|-----------|---------|\n| **Obsidian – File & Content** | `obsidian_get_file_contents` <br> `obsidian_batch_get_file_contents` <br> `obsidian_append_content` <br> `obsidian_patch_content` <br> `obsidian_delete_file` | Read, append or modify notes in your vault. |\n| **Obsidian – Search** | `obsidian_simple_search` <br> `obsidian_complex_search` | Find text or files using simple or advanced queries. |\n| **Obsidian – Navigation & Metadata** | `obsidian_list_files_in_vault` <br> `obsidian_list_files_in_dir` <br> `obsidian_get_recent_changes` <br> `obsidian_get_periodic_note` <br> `obsidian_get_recent_periodic_notes` | Explore the vault, get recent edits or periodic notes. |\n\nIf you need help using any of these tools for a specific task (e.g., “Create a new note titled *Project Plan*”, “Open a PR against my repo” or “List all open issues with the label `bug`”), just let me know and I’ll call the appropriate tool(s) for you."}

====================================================================

curl.exe -X POST http://localhost:5095/chat ` -H "Content-Type: application/json" ` -d '{\"message\": \"List all files in my vault\"}'

025-10-14 17:08:48  [INFO]
 [LM STUDIO SERVER] Running chat completion on conversation with 4 messages.
2025-10-14 17:08:51  [INFO]
 [openai/gpt-oss-20b] Start thinking...
2025-10-14 17:08:51  [INFO]
 [openai/gpt-oss-20b] Done thinking. Thought for 0.40 seconds.
2025-10-14 17:08:58  [INFO]
 [openai/gpt-oss-20b] Model generated tool calls:  []
2025-10-14 17:08:58  [INFO]
 [openai/gpt-oss-20b] Generated prediction:  {
  "id": "chatcmpl-y3l0el65ebqdz9zd4jkl8",
  "object": "chat.completion",
  "created": 1760454528,
  "model": "openai/gpt-oss-20b",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Here’s a quick snapshot of everything that lives in your Obsidian vault (root‑level files and folders):\n\n| # | Item |\n|---|------|\n| 1 | `pendingSync-gdrive-plugin` |\n| 2 | `⚡ Quick Capture.md` |\n| 3 | `✅ Tasks/` |\n| 4 | `✨ AI/` |\n| 5 | `🐙 Github/` |\n| 6 | `📑 Project Ideas.md` |\n| 7 | `📖 Knowledge Base/` |\n| 8 | `📚 Learning & Development/` |\n| 9 | `📜 Personal/` |\n|10 | `🗂 Projects/` |\n|11 | `🏠 Images/` |\n|12 | `⚙️ Tools & Setup/` |\n\nIf you’d like to drill down into any of those folders, just let me know and I can list their contents for you!",
        "reasoning": "We need to present list. Probably format nicely.",
        "tool_calls": []
      },
      "logprobs": null,
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 11089,
    "completion_tokens": 206,
    "total_tokens": 11295
  },
  "stats": {},
  "system_fingerprint": "openai/gpt-oss-20b"
}
Info
Context
Inference
Load
20334
Model supports up to 131072 tokens
24
/ 24

Model offload limited to dedicated GPU memory. Actual number of offloaded layers may differ


10
2048
Auto

Auto

Random Seed

4
Experimental
Experimental
Experimental
Experimental



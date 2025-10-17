namespace ObsidianAI.Web.Models
{
    /// <summary>
    /// Represents an action card that requires user confirmation
    /// </summary>
    public record ActionCardData
    {
        /// <summary>
        /// Unique identifier for the action card
        /// </summary>
        public string Id { get; init; } = System.Guid.NewGuid().ToString();
        
        /// <summary>
        /// Title of the action card
        /// </summary>
        public string Title { get; init; } = string.Empty;
        
        /// <summary>
        /// List of planned actions to be displayed
        /// </summary>
        public List<PlannedAction> Actions { get; init; } = new();
        
        /// <summary>
        /// Whether to show a "view all" option when there are many actions
        /// </summary>
        public bool HasMoreActions { get; init; }
        
        /// <summary>
        /// Count of additional actions not shown initially
        /// </summary>
        public int HiddenActionCount { get; init; }
        
        /// <summary>
        /// Current status of the action card
        /// </summary>
        public ActionCardStatus Status { get; init; } = ActionCardStatus.Pending;
        
        /// <summary>
        /// Status message to display when action is completed or cancelled
        /// </summary>
        public string StatusMessage { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of operation being performed
        /// </summary>
        public ActionOperationType OperationType { get; init; }
        
        /// <summary>
        /// Reflection key for server-side ActionCards (null for client-side ActionCards)
        /// </summary>
        public string? ReflectionKey { get; init; }
        
        /// <summary>
        /// Reasoning from reflection analysis (server-side ActionCards only)
        /// </summary>
        public string? ReflectionReasoning { get; init; }
        
        /// <summary>
        /// Warnings from reflection analysis (server-side ActionCards only)
        /// </summary>
        public List<string>? ReflectionWarnings { get; init; }
    }

    /// <summary>
    /// Represents a single planned action
    /// </summary>
    public record PlannedAction
    {
        /// <summary>
        /// Icon to display for the action
        /// </summary>
        public string Icon { get; init; } = "📄";
        
        /// <summary>
        /// Description of the action
        /// </summary>
        public string Description { get; init; } = string.Empty;
        
        /// <summary>
        /// Source file/path
        /// </summary>
        public string Source { get; init; } = string.Empty;
        
        /// <summary>
        /// Destination file/path (if applicable)
        /// </summary>
        public string Destination { get; init; } = string.Empty;

        /// <summary>
        /// Content payload associated with the action (e.g., text to append or patch)
        /// </summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Operation string (append, modify, delete, create) used for API routing
        /// </summary>
        public string Operation { get; init; } = string.Empty;
        
        /// <summary>
        /// Type of action
        /// </summary>
        public ActionType Type { get; init; }
    }

    /// <summary>
    /// Enum for action card status
    /// </summary>
    public enum ActionCardStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        Processing,
        Completed,
        Failed
    }

    /// <summary>
    /// Enum for operation types
    /// </summary>
    public enum ActionOperationType
    {
        Move,
        Delete,
        Create,
        Reorganize,
        Search,
        Other
    }

    /// <summary>
    /// Enum for individual action types
    /// </summary>
    public enum ActionType
    {
        Move,
        Delete,
        Create,
        Copy,
        Rename,
        Modify,
        Other
    }
}
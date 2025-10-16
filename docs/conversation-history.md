# Conversation History Persistence Implementation Plan

## 1. Architecture Overview

**Storage Strategy**: SQLite database with Entity Framework Core
**Scope**: Persist chat sessions, messages, and associated metadata
**Integration Points**: Domain → Infrastructure → API → Web layers

---

## 2. Database Design

### Core Tables

**Conversations**
- `Id` (Guid, PK)
- `UserId` (string, nullable - future multi-user support)
- `Title` (string, auto-generated from first message)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime)
- `IsArchived` (bool)
- `Provider` (enum: LmStudio, OpenRouter)
- `ModelName` (string)

**Messages**
- `Id` (Guid, PK)
- `ConversationId` (Guid, FK)
- `Role` (enum: User, Assistant, System)
- `Content` (string)
- `Timestamp` (DateTime)
- `TokenCount` (int, nullable)
- `IsProcessing` (bool)

**ActionCards**
- `Id` (Guid, PK)
- `MessageId` (Guid, FK)
- `Title` (string)
- `Status` (enum: Pending, Processing, Completed, Failed, Cancelled)
- `StatusMessage` (string, nullable)
- `CreatedAt` (DateTime)
- `CompletedAt` (DateTime, nullable)

**PlannedActions**
- `Id` (Guid, PK)
- `ActionCardId` (Guid, FK)
- `Type` (enum: Create, Modify, Move, Delete)
- `Source` (string)
- `Destination` (string, nullable)
- `Description` (string)
- `Operation` (string)
- `Content` (string, nullable)
- `SortOrder` (int)

**FileOperations**
- `Id` (Guid, PK)
- `MessageId` (Guid, FK)
- `Action` (string)
- `FilePath` (string)
- `Timestamp` (DateTime)

---

## 3. Domain Layer Changes

### New Entities
**Location**: `ObsidianAI.Domain/Entities/`

Create domain entities:
- `Conversation` - Aggregate root with messages collection
- `Message` - Value object with content, role, metadata
- `ActionCardRecord` - Persisted action card state
- `PlannedActionRecord` - Individual action within card

### Repository Interfaces
**Location**: `ObsidianAI.Domain/Ports/`

Define contracts:
- `IConversationRepository`
  - `CreateAsync(Conversation)`
  - `GetByIdAsync(Guid id)`
  - `GetAllAsync(userId, includeArchived, skip, take)`
  - `UpdateAsync(Conversation)`
  - `DeleteAsync(Guid id)`
  - `ArchiveAsync(Guid id)`
  
- `IMessageRepository`
  - `AddAsync(Message)`
  - `GetByConversationIdAsync(Guid conversationId)`
  - `UpdateAsync(Message)` - for attaching ActionCards/FileOperations

---

## 4. Infrastructure Layer Implementation

### EF Core DbContext
**Location**: `ObsidianAI.Infrastructure/Data/ObsidianAIDbContext.cs`

**Configuration**:
- DbSet for each entity
- Fluent API configuration for relationships
- Enum-to-string conversions
- Index on `ConversationId`, `Timestamp`, `UpdatedAt`
- Cascade delete for conversation → messages → action cards

### Repository Implementations
**Location**: `ObsidianAI.Infrastructure/Data/Repositories/`

Implement:
- `ConversationRepository` - CRUD with eager loading
- `MessageRepository` - Batch operations support
- Use `AsNoTracking()` for read-only queries
- Pagination support via `skip` and `take`

### Migration Strategy
**Initial Migration**: `dotnet ef migrations add InitialCreate`
**Location**: `ObsidianAI.Infrastructure/Data/Migrations/`

---

## 5. Application Layer Changes

### New Use Cases
**Location**: `ObsidianAI.Application/UseCases/`

**ConversationManagement**:
- `CreateConversationUseCase` - Initialize new chat session
- `LoadConversationUseCase` - Retrieve full conversation with messages
- `ListConversationsUseCase` - Get paginated conversation list
- `ArchiveConversationUseCase` - Soft delete
- `DeleteConversationUseCase` - Hard delete

**Message Persistence**:
- Modify `StreamChatUseCase` to persist messages after completion
- Modify `StartChatUseCase` to persist both user and assistant messages
- Add `UpdateMessageUseCase` - for attaching ActionCards/FileOperations post-parse

### DTOs
**Location**: `ObsidianAI.Application/DTOs/`

Create transfer objects:
- `ConversationDto` - Summary for list view (id, title, updated, message count)
- `ConversationDetailDto` - Full conversation with messages
- `MessageDto` - Message with nested ActionCard/FileOperation

---

## 6. API Layer Integration

### New Endpoints
**Location**: `ObsidianAI.Api/Program.cs` or separate endpoint file

**Routes**:
```
GET    /conversations              - List all conversations (paginated)
GET    /conversations/{id}         - Get conversation with messages
POST   /conversations              - Create new conversation
PUT    /conversations/{id}         - Update conversation (title, archive)
DELETE /conversations/{id}         - Delete conversation
GET    /conversations/{id}/export  - Export as JSON/Markdown
```

### Modify Existing Endpoints

**`POST /chat`**:
- Accept optional `conversationId` parameter
- If null, create new conversation
- Persist user message before agent call
- Persist assistant message after completion
- Return `conversationId` in response

**`POST /vault/modify`**:
- After successful operation, update message with FileOperation details
- Persist ActionCard status changes

---

## 7. Web Layer Changes

### State Management
**Location**: `ObsidianAI.Web/Components/Pages/Chat.razor`

**Current State** (in-memory):
- `List<ChatMessage> conversationHistory`

**New State**:
- `Guid? currentConversationId` - Track active conversation
- `bool isLoadingHistory` - Loading indicator
- `List<ConversationDto> conversationList` - For sidebar

### UI Components

**New Components**:

**ConversationSidebar** (`Shared/ConversationSidebar.razor`):
- Toggle button in header
- List of conversations (title, last message time)
- "New Chat" button
- Archive/delete actions (context menu)
- Search/filter capability

**ConversationHeader** (modify existing):
- Display current conversation title
- Edit title inline
- Show conversation metadata (created date, message count)

### Service Layer
**Location**: `ObsidianAI.Web/Services/ChatService.cs`

Add methods:
- `LoadConversationAsync(Guid id)` → `ConversationDetailDto`
- `ListConversationsAsync(skip, take)` → `List<ConversationDto>`
- `CreateConversationAsync()` → `Guid`
- `DeleteConversationAsync(Guid id)`
- `ExportConversationAsync(Guid id)` → download file

### Chat Flow Modifications

**On Page Load**:
1. Check if `conversationId` query parameter exists
2. If yes, load conversation and populate `conversationHistory`
3. If no, initialize empty chat

**On Send Message**:
1. If `currentConversationId == null`, create new conversation
2. Add user message to API with `conversationId`
3. Receive response with persisted message IDs
4. Update local state with IDs for future updates

**On Action Confirmation**:
1. Execute operation
2. Update ActionCard status in database via API
3. Attach FileOperation to message
4. Refresh message in UI

---

## 8. Configuration & Dependencies

### NuGet Packages
**Add to** `ObsidianAI.Infrastructure.csproj`:
- `Microsoft.EntityFrameworkCore.Sqlite` (9.x)
- `Microsoft.EntityFrameworkCore.Design` (9.x)
- `Microsoft.EntityFrameworkCore.Tools` (9.x)

### Connection String
**Location**: `ObsidianAI.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "ObsidianAI": "Data Source=obsidianai.db"
  }
}
```

**For Aspire**, add to `ObsidianAI.AppHost/AppHost.cs`:
- Pass connection string as environment variable to API project
- Consider Aspire SQL Server resource for production (optional)

### Service Registration
**Location**: `ObsidianAI.Api/Program.cs`

```csharp
// DbContext registration
builder.Services.AddDbContext<ObsidianAIDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ObsidianAI")));

// Repository registration
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Use case registration
builder.Services.AddScoped<CreateConversationUseCase>();
builder.Services.AddScoped<LoadConversationUseCase>();
// ... etc
```

---

## 9. Migration & Data Initialization

### Database Initialization
**Location**: `ObsidianAI.Api/Program.cs` (before `app.Run()`)

```csharp
// Ensure database is created and migrations applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ObsidianAIDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

### Seed Data (Optional)
- Sample conversation for testing
- System message templates

---

## 10. Testing Strategy

### Unit Tests
**Location**: `ObsidianAI.Tests/Infrastructure/Repositories/`

Test:
- Repository CRUD operations
- Entity relationships
- Query filtering and pagination

**Location**: `ObsidianAI.Tests/Application/UseCases/`

Test:
- Use case logic with in-memory database
- Message persistence workflow
- Conversation lifecycle

### Integration Tests
**Location**: `ObsidianAI.Tests/Api/`

Test:
- End-to-end conversation flow
- API endpoint behavior
- Database state after operations

---

## 11. Performance Considerations

### Indexing Strategy
- Index on `ConversationId` in Messages table
- Index on `UpdatedAt` in Conversations table
- Composite index on `UserId, IsArchived` for user queries

### Query Optimization
- Use `.AsNoTracking()` for read-only operations
- Implement pagination (default 20 conversations per page)
- Lazy load messages only when conversation opened
- Consider `Select()` projections for list views

### Caching (Future)
- In-memory cache for active conversation
- Redis for distributed scenarios

---

## 12. Implementation Order

**Phase 1: Foundation** (Day 1)
1. Create domain entities
2. Define repository interfaces
3. Set up EF Core DbContext
4. Create initial migration

**Phase 2: Infrastructure** (Day 2)
5. Implement repositories
6. Add service registration
7. Test database operations

**Phase 3: Application** (Day 3)
8. Create use cases
9. Modify existing chat use cases
10. Add DTOs

**Phase 4: API** (Day 4)
11. Add new endpoints
12. Modify existing endpoints
13. Test API integration

**Phase 5: Web** (Day 5)
14. Update ChatService
15. Modify Chat.razor
16. Create ConversationSidebar component
17. Add conversation management UI

**Phase 6: Polish** (Day 6)
18. Add export functionality
19. Implement search/filter
20. Write tests
21. Documentation

---

## 13. Edge Cases & Error Handling

**Scenarios**:
- Database file missing → auto-create
- Conversation not found → return 404
- Message parsing fails → persist raw content
- Concurrent updates → use optimistic concurrency (RowVersion)
- Large conversations → implement message pagination within conversation

**Rollback Strategy**:
- If message persistence fails, still return response to user
- Log errors but don't block chat functionality
- Implement retry logic for transient failures

---

## 14. Security Considerations

**Data Privacy**:
- Store vault content references, not full content
- Consider encryption at rest for sensitive conversations
- Implement conversation ownership (UserId field)

**Access Control** (Future):
- User authentication via ASP.NET Identity
- Ensure users can only access their conversations
- Admin role for conversation management

---

## 15. Observability

**Logging**:
- Log conversation creation/deletion
- Log message persistence operations
- Track database query performance

**Metrics**:
- Average conversation length
- Messages per conversation
- Most used file operations

**Health Checks**:
- Add database connectivity health check
- Monitor database file size

---

This plan maintains clean architecture boundaries, follows existing patterns, and integrates seamlessly with current functionality. Ready to proceed with Phase 1 implementation?   
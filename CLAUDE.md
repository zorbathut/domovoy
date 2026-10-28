# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Domovoy is a telemetry and error tracking system for games built with C# and ASP.NET Core 10. It collects and visualizes two types of reports:
- **Events** - Game analytics and custom events (e.g., level completions, user actions)
- **Errors** - Error conditions and crashes with stack traces

The system consists of multiple services that work together to collect, store, and visualize these reports.

## Interaction Guidelines

**Answer questions before coding**: When asked a question, provide an actual answer first. Don't leap straight to writing code.

**Never commit unless explicitly told to**: Complete the work and leave it uncommitted in the working tree. Only `git commit` when the current request explicitly asks for it ("commit this", "make a checkin"); a phrase like "let's make that a separate checkin" describes how the work should eventually be grouped, not permission to commit it yourself, and permission granted for one task never carries over to the next.

**Split significant work into small self-contained commits**: A significant change lands as a sequence of minimal commits, not one lump. Split along seams that carry meaning, each commit with a one-sentence story — never mechanically per-file or per-layer. The seams that matter:

- A pure refactor of existing code that the feature merely motivated (extracting an interface, collapsing duplicated lookups) is its own commit, landing *before* the feature that wanted it.
- A pre-existing bug fixed along the way is its own commit, however small.
- A behavior change to an existing system is separate from both the refactor that enabled it and the feature that exposed it — behavior changes are the commits people hunt for later.
- A vendored third-party drop stands alone.
- Conversely, keep together what only works together: the halves of a feature that can't be exercised separately, data plus the code that loads it.

Tests go in the commit that makes them meaningful, written against subjects the series doesn't later mutate. Every commit must build and pass the suite on its own — the history should be bisectable. Late fixes (review feedback included) get folded into the commit they belong to via fixup/autosquash, not appended as cleanup commits.

When I've told you to commit the work, apply this by default. When the work stays uncommitted, still build it as one unit in the working tree — but when you finish, point out that it's a good candidate for splitting and propose the commit sequence.

**Evaluate, don't assume**: "Why don't we X?" is a request for evaluation, not a suggestion to do X. Explain the tradeoffs, potential issues, or reasons why X might or might not be a good idea.

**Debug by evidence, not by guess**: When investigating a bug you don't fully understand, prefer adding diagnostic instrumentation or asking focused questions over making speculative changes. A confident theory backed by reading the code is fine to act on; a vibe is not. If a fix doesn't solve the user's problem, that's a signal that the theory was wrong — gather more data before trying again. Two consecutive failed fixes mean stop guessing entirely: pause, instrument, and ask. Rapid-fire blind changes waste the user's attention and erode trust.

**Err on the side of more diagnostic data, not less**: When you ask the user to run something — a probe build, a manual test, a copy-paste session — the expensive part is the round trip itself. The marginal cost of one more printed value, one more covered code path, one more chapter to click is small. So when you instrument, instrument generously: log every variable that could plausibly disambiguate the bug, exercise every endpoint of the parameter space (V=0, V=0.5, V=1, not just whichever was easy), include both the suspected-correct prediction *and* the alternatives so residuals are immediately visible. A diagnostic that prints 30 lines and answers the question on the first try is far cheaper than three diagnostics that each print 3 lines. Make the round trip pay for itself.

**Waiting on background work is not a tool call**: When you've backgrounded a long command (e.g. the full test suite, which exceeds the foreground timeout) and have nothing else productive to do, just end the turn — its completion notification will re-invoke you automatically. Don't emit no-op commands (`echo "waiting"`, re-reads, status pings) to stay "active"; ending on plain text is the correct way to wait, not a hand-off. Conversely, if a command fits the foreground timeout and you'd only wait for it anyway, run it in the foreground so the result returns in the same call. Backgrounding *and* polling is the worst of both.

## Workflow

**Step 1 — Plan.** Enter plan mode (the actual `EnterPlanMode` tool — not a freeform text plan) and research the task and produce a plan. Skippable for trivial changes (under ~a dozen lines). Include unit tests in the plan whenever they're plausible to add — UI generally can't be tested, most other things can.

**Step 2 — Hostile-review the plan.** Before leaving plan mode, spawn a hostile-review agent (run it on Opus — `model: "opus"`) against the plan itself. Brief it like a design reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), give it the plan, but do not justify the plan's choices. Give it enough feedback space to actually push back on the approach. Apply the same adjudication rules as the final review (below). Fold valid objections into the plan, then exit plan mode.

**Step 3 — Tests first (when applicable).** For bugfixes, or any feature whose tests can be sensibly written before the implementation exists, write the tests first and verify they fail. Then complete the implementation.

**Step 4 — Run all tests.** Always, even when the change seems unrelated. If anything breaks, return to step 3 — or step 1 if the fix requires significant redesign. For UI changes that can't be unit-tested, explicitly say so rather than claiming success.

Don't treat a failing test as a hard veto on the change. Tests exist to catch *unintentional* drift — a test that pins behavior the change deliberately replaced should be updated alongside the code, not worked around to preserve the old behavior. Fix the test to match the new intent; only fall back to step 3 / step 1 when the failure exposes an actual regression.

**Step 5 — Hostile review.** Spawn a hostile-review agent (run it on Opus — `model: "opus"`). Brief it like a PR reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), but do not explain or justify the implementation. Explicitly ask it to **review the general architecture** too, not just the diff — does the chosen approach fit the surrounding code, are there cleaner factorings, does it introduce abstractions that don't pay rent, etc. Give it enough feedback space to cover both the local change and the architectural read effectively (don't cap it to a terse response). Then:
  - If it raises valid objections, fix them. Significant redesign → back to step 1; code changes → back to step 3.
  - If I disagree with an objection, push back once. If it still objects and I'm still confident, surface the disagreement to the user for adjudication rather than looping.
  - Either way — adjudication needed or not — give the user a quick summary of the review at the end.

## Coding Guidelines

**KISS / YAGNI / MVP**: Keep it simple. Write the simplest code that solves the current problem. Include what's necessary, not more. Don't build abstractions, features, or speculative generality that aren't immediately needed. Three similar lines is better than a premature abstraction.

**No backwards compatibility for its own sake**: Remove stubs and dead code completely. If something is unused or being replaced, delete it outright — don't leave shims, renamed `_unused` vars, `// removed` comments, or compatibility re-exports behind. The git history is the backwards compatibility.

**Error handling**:
- Don't add excessive or preemptive error handling. Don't validate everything before it's ever been an issue. Trust internal code and framework guarantees; only validate at system boundaries (user input, external APIs).
- **Silent error handling is banned.** Never swallow exceptions or ignore error conditions. If something fails, it must be reported (via the project's logging facility) or thrown.
- For services that face users, distinguish bugs from user mistakes in your status codes / error types. A user submitting bad input should get a specific, helpful error — not a generic 500-equivalent. Reserve "internal error" responses for actual bugs and infrastructure failures. When adding new features, ask: "Can a user trigger this exception through normal usage?" If yes, return a specific error with a helpful message.

**Don't hand-wrap lines**: One thought, one line — however long. Editors soft-wrap; you don't need to. The only exceptions are:
- **Distinct paragraphs** in a comment: separate with a **blank line** (true paragraph break), not just a `\n`.
- **Structurally-aligned expressions**: one argument per line, one chained call per line, etc.

A multi-sentence single-thought comment is still one line. "It reads better wrapped" is not an exception — that's the rule talking.

**Composition over inheritance**: Prefer building behavior out of small composable pieces (functions, components, properties, modules) over deep class hierarchies. Inheritance is a tool, not a default.

**Data-driven where it pays**: When a category of behavior is open-ended (content, configuration, content variants), prefer data files and a small interpreter over hardcoded code paths. When it's closed and unlikely to grow, just write the code.

### Style

**Always use braces**: Always include `{}` for `if`, `else`, `for`, `foreach`, `while`, etc., even for single-line bodies.
```csharp
// Good
if (condition)
{
    return;
}

// Bad
if (condition) return;
if (condition)
    return;
```

**Avoid expression-bodied members (`=>`)**: Prefer block bodies with explicit `return` statements for methods and properties. Expression bodies obscure control flow.
```csharp
// Good
public int GetValue()
{
    return value;
}

// Bad
public int GetValue() => value;
```

**Usings**: Where implicit usings are disabled, all `using` directives must be explicit and alphabetized at the top of each file.

### Default Parameters and Overloads

The deciding axis is the *nature of the parameter*, not the mechanism. A default parameter is the right tool for a conceptually optional thing; an overload is for a signature that is genuinely different, not "a default parameter wearing a funny hat."

- **A new parameter the function genuinely needs is mandatory.** Add it without a default and update the call sites. Don't reflexively give every new parameter a default value just to avoid touching callers — that's the main thing this rule exists to prevent.
- **A default value is for a *conceptually optional* parameter** — one with a principled "absent" value: a nullable callback or override (`Action onDone = null`, `Func<T, bool> filter = null`), or a natural identity like `double steepness = 1.0`. It is *not* for an arbitrary tuning constant that merely happens to suit most callers — something like `attemptsPerIteration = 30` should be mandatory or a named constant, not a default.
- **Overloads are for genuinely different signatures** — different parameter *types* or *shapes* that can't collapse into one signature, or a meaningfully different operation. Do not write an overload pair whose only difference is that one omits a trailing optional argument — use a default parameter instead. (For example, `Sigmoid(x)` + `Sigmoid(x, steepness)` should collapse to one `Sigmoid(double x, double steepness = 1.0)` — the natural-identity case above.)
- **A behavior-switching bool may be a default-`false` parameter only when it's a rider on the same operation** — the result is the same kind of thing, the flag just tweaks a side aspect, and it's almost always off ("sweep, *and while you're at it* ignore platforms": `Sweep(…, bool ignorePlatforms = false)`). When the flag changes *what the function fundamentally means* — the question it answers — it shouldn't be a flag: make it a separate, differently-named function, or handle it at the call site. Name any split function category first, per Naming below.
- **Hard exception**: compiler-attribute parameters (`[CallerFilePath]`, `[CallerLineNumber]`, `[CallerMemberName]`) must be default parameters — there is no overload form, so these don't count against the rule.

**Error handling (C#)**: on top of the general rule above, an empty `catch` is a bug — report via the project's logging facility or rethrow.

## Commenting

A comment earns its place by saying something the code cannot. That's usually one of: a non-obvious "why", a subtle constraint, a surprising choice or tradeoff — or signposting the flow of a long linear process. A one-line summary of what the next chunk of a long function is doing ("Accumulate the asymptotes" over ten lines of dense math; "Resolve overlaps, nearest first" over a loop) is genuinely useful, and often cleaner than extracting that chunk into a function called exactly once. A comment that restates what the name or a single line of code already makes plain is noise.

**Write for a reader who never saw the old code.** A comment that earns its place only by contrast with a previous version — reassuring that a value isn't what it once meant, noting the code no longer does X, explaining that something is "now" done differently — is history in disguise. The tell: it answers a question a fresh reader would never think to ask (nobody wonders whether a `0` expectation is "really a skip" unless they know it once was). State only what's true now, and delete the rest. The one exception is history that constrains the present — a warning against a change someone might actually make ("don't revert this to the double-precision form; it loses the low bits at fixed-point scale") or a deliberate deviation to reconcile later (a clearly-marked local patch to vendored code). That is a load-bearing *why*, not nostalgia; the test is whether the history guards against a real regression or merely explains away a non-question. Police this mainly in your own new comments — by the removal asymmetry below, don't strip others' on suspicion alone.

**Adding — be conservative, but signpost freely.** Don't narrate the obvious: a `bool allowFlips` field needs no `// when false, flipping is disabled`; a `// set the flip` above `flip = …` adds nothing. Reach first for self-explanatory code (good names, clear structure), and comment the part code can't carry — usually the *why*, not the *what*. The exception is flow signposting in long procedures, where a sparse trail of one-line "what next" headers is a real readability win; use them. When you explain a "why", keep it tight; one good line beats a paragraph. Don't reference the current task, fix, or callers ("used by X", "added for the Y flow", "handles the case from issue #123") — those belong in the commit message and rot as the codebase evolves.

**Removing — be generous about keeping.** Existing comments are there for a reason. If one is out of date or actively misleading, fix or remove it. Otherwise leave it alone — don't strip a comment just because it explains the "what", or because you wouldn't have written it yourself. The asymmetry is deliberate: conservative about adding your own, generous about keeping others'.

## Naming

**C# conventions**: PascalCase for public members, types, and static fields; camelCase for private/protected fields and parameters; `I` prefix for interfaces (e.g., `IComponent`).

**Category-instance prefix**: When a name combines a category with an instance, put the category first so related names group alphabetically and the category reads as the classification. `SpawnerBurst`, `ShapeRadial`, `AttackStart()` — not `BurstSpawner`, `RadialShape`, `StartAttack()`. The category is the "kind of thing"; the instance is the specific variant. Apply this to types, functions, files, and config keys alike.

## Critical Rules

1. **Always use absolute paths** in file operations. Relative paths break under tooling that runs from a different working directory than expected.
2. **Tests live next to the code they test** in spirit even if not in directory layout. New code without tests is a debt that compounds.

## Solution Structure

The solution contains the following projects:

- **Domovoy.Shared** - Shared models (`Report` base class, `Event`, `Error`, `Attachment` types) and DTOs used across all projects
- **Domovoy.Database** - EF Core DbContext, migrations, and database configuration
- **Domovoy.Intake** - ASP.NET Core Web API that receives events, errors, and attachments via HTTP
- **Domovoy.Web** - Blazor Server web interface for viewing events, errors, and attachments
- **Domovoy.Client** - Client library for games to send events, errors, and attachments to the Intake API
- **Domovoy.MinIO** - Shared MinIO/S3 integration library for attachment storage (used by Intake and Web)

## Common Development Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Domovoy.Intake/Domovoy.Intake.csproj
```

### Running Services

**Local development (services outside Docker):**
```bash
# Start PostgreSQL and MinIO
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up postgres minio

# Run Intake API (in separate terminal)
dotnet run --project src/Domovoy.Intake/Domovoy.Intake.csproj

# Run Web UI (in separate terminal)
dotnet run --project src/Domovoy.Web/Domovoy.Web.csproj
```

**Full Docker deployment:**
```bash
docker-compose up
```

### Database Migrations

Migrations are automatically applied on startup by both Intake and Web services (see Program.cs in each project).

To create a new migration:
```bash
# Must be run from the Domovoy.Database directory
cd src/Domovoy.Database
dotnet ef migrations add MigrationName
```

To manually apply migrations:
```bash
cd src/Domovoy.Database
dotnet ef database update
```

### Testing

**Prerequisites:** Docker daemon must be running. Tests automatically spin up their own PostgreSQL and MinIO containers using Docker.DotNet — no manual `docker-compose` needed.

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~IntakeApiTests"
```

#### Test Project Structure

The solution includes a single test project `Domovoy.Tests` that contains integration tests for all components:

- **IntakeApiTests** - Tests the Intake API HTTP endpoints and database persistence
- **ReportServiceTests** - Tests the ReportService directly, including event and error creation
- **DomovoyClientTests** - Tests the client library's end-to-end integration with the Intake API
- **WebUiTests** - Tests Blazor components using bUnit, verifying UI rendering and data display
- **AttachmentTests** - Tests attachment upload, validation, metadata persistence, and client integration

#### Test Database

Tests use a separate `domovoy_test` database to avoid interfering with development data:

- The `DatabaseFixture` (in `tests/Domovoy.Tests/Infrastructure/DatabaseFixture.cs`) manages test container and database lifecycle
- PostgreSQL and MinIO containers are started automatically per test run using Docker.DotNet with random port allocation
- Tests clean up after themselves using `IAsyncLifetime.DisposeAsync()`
- Connection string is dynamically generated with a random port from the test container

#### Test Infrastructure

The test project includes several infrastructure components:

- **IntakeApiFactory** - Custom `WebApplicationFactory` for testing the Intake API
- **WebUiFactory** - Custom `WebApplicationFactory` for testing the Web UI
- **DatabaseFixture** - Manages Docker containers (PostgreSQL, MinIO), test database creation, cleanup, and provides helper methods
- **TestPostgresContainer** - Spins up a PostgreSQL container with random port allocation and tmpfs for speed
- **TestMinioContainer** - Spins up a MinIO container with random port allocation
- **TestDataBuilder** - Fluent builder for creating test event and error report data

#### What the Tests Cover

1. **API Integration** - Full HTTP request/response cycle including serialization, validation, and error handling
2. **Database Operations** - Actual PostgreSQL queries, migrations, and data persistence
3. **Report Storage** - Each event and error report is stored as a unique record (no deduplication)
4. **Client Library** - End-to-end event and error submission from client to database
5. **UI Components** - Blazor component rendering, data binding, and user interactions

#### Running Tests Locally

1. Ensure Docker daemon is running
2. Run tests: `dotnet test`
3. Containers and the test database are created automatically on each run
4. Each test class cleans up after itself, so tests can be run repeatedly
5. Containers are automatically removed after the test run (or by a watchdog if the process crashes)

**Note**: Tests are configured to run sequentially (not in parallel) via `xunit.runner.json` because they share a single test database. This ensures reliability but makes tests slightly slower (~2-4 seconds total).

### Testing Philosophy

Run the full test suite on every change, not just the tests you think are related — that's the whole point of having a suite. If the project ships with a watch mode or a fast subset, prefer that during the inner loop, but the final pre-commit step is the full run.

Write tests against the *seam* you actually want to defend — pure functions, deterministic state machines, parsers, classifiers — and don't try to retrofit unit tests around UI, rendering, or process-orchestration code that has no testable seam. For those, say so explicitly when reporting status, and rely on a manual smoke instead of pretending coverage you don't have.

Integration tests that spin up real dependencies (databases, message brokers, container runtimes) are usually worth the slowness over mocks: mocks pass when the contract drifts, real dependencies fail loudly. When mocking is unavoidable, mock at the *outermost* boundary you reasonably can.

**Don't pin user-facing copy in tests.** Test text-producing logic relationally instead of asserting exact strings: capture outputs and assert relations between them — non-empty, distinct across states that should read differently, equal across states that should read the same (priority/stability), exact match only for sentinels like `""`. This lets copy be rewritten (or moved to data files) without touching tests. Exact-match assertions remain correct when the exact output *is* the contract (serialization, parsers, formatters) or when the string is fixture data the test itself authored — this rule is about production copy shown to end users. Apply when writing or modifying tests; don't mass-convert existing tests unless asked.

## Architecture Details

### Service Communication

- **Intake API** (port 1973) and **Web UI** (port 1975) do NOT communicate with each other
- Both services independently connect to the same PostgreSQL database and MinIO object store
- Events and errors submitted to the Intake API are stored in the database and retrieved by the Web UI
- Attachments: binary files are stored in MinIO, metadata is stored in PostgreSQL. Intake handles uploads, Web proxies downloads to the browser (MinIO is never exposed to end users)

### Data Model Design

The system uses a Table-Per-Type (TPT) pattern with a `Report` base class and two concrete types. This provides normalized storage with separate tables for each type while maintaining a shared base table for common fields:

**Database Tables:**

1. **Reports** (base table)
   - `Id` (Guid, PK)
   - `Timestamp` (DateTime, indexed)
   - `ReportType` (enum: Event=1, Error=2)
   - `GameVersion` (string, max 50 chars, indexed)
   - `Platform` (string, max 50 chars, indexed)

2. **Events** (inherits from Reports via FK)
   - `Id` (Guid, PK/FK to Reports.Id)
   - `Data_Name` (string, max 200 chars) - Event name
   - `Data_Category` (string, max 100 chars) - Event category
   - `Data_Value` (decimal?) - Optional numeric value
   - `Data_UserId` (string?, max 100 chars, indexed) - Optional user identifier
   - `Data_Metadata` (jsonb) - Optional JSONB metadata

3. **Errors** (inherits from Reports via FK)
   - `Id` (Guid, PK/FK to Reports.Id)
   - `Data_Severity` (string, max 20 chars, indexed) - Error severity level
   - `Data_Code` (string?, max 100 chars) - Optional error code
   - `Data_Message` (string, max 2000 chars) - Error message
   - `Data_ExceptionType` (string?, max 500 chars) - Exception type
   - `Data_StackTrace` (text) - Full stack trace
   - `Data_Context` (text) - Additional context

4. **Attachments** (linked to Reports via FK)
   - `Id` (Guid, PK)
   - `ReportId` (Guid, FK to Reports.Id, indexed, cascade delete)
   - `Filename` (string, max 255 chars)
   - `ContentType` (string, max 100 chars)
   - `SizeBytes` (long)
   - `StorageKey` (string, max 500 chars) - Path in MinIO
   - `CreatedAt` (DateTime)

**TPT Benefits:**
- ✅ No NULL columns - each table only contains relevant fields
- ✅ Can enforce NOT NULL at database level for type-specific fields
- ✅ Clearer schema - separate tables for logically distinct types
- ✅ Efficient indexes - no wasted index space on irrelevant rows
- ✅ Extensible - easy to add new report types in the future

**Shared Payload Types**: `EventPayload` and `ErrorPayload` are used in both DTOs (SubmitEventRequest, SubmitErrorRequest) and entities (Event, Error) to eliminate duplication and ensure consistency. EF Core automatically handles JOINs between Reports and Events/Errors tables when querying. See `DomovoyDbContext.cs` for the EF Core TPT configuration.

**Important**: Each submission creates a new record - there is NO deduplication. Every event and error is stored individually.

### Attachment Storage

Binary attachments (crash dumps, screenshots, log files, up to 50 MB) are stored in MinIO (S3-compatible object store). Metadata is in PostgreSQL, bytes are in MinIO at `{reportId}/{attachmentId}/{filename}`.

**Intake API endpoints:**
- `POST /api/v1/reports/{reportId}/attachments` - Upload attachment (multipart form data)

**Web API endpoints (proxied, MinIO never exposed to end users):**
- `GET /api/attachments/by-report/{reportId}` - List attachments for a report
- `GET /api/attachments/{attachmentId}/download` - Download attachment

The `Domovoy.MinIO` project contains `AttachmentStorageService` and `MinioServiceExtensions` used by both Intake and Web. MinIO settings are configured in `appsettings.json` under the `Minio` section.

### Connection String Configuration

- **Development (local)**: `Host=localhost;Database=domovoy;Username=domovoy;Password=domovoy`
- **Docker**: `Host=postgres;Database=domovoy;Username=domovoy;Password=domovoy`
- Configured in `appsettings.json` (or `appsettings.Development.json`) in both Intake and Web projects
- Docker services use environment variable `ConnectionStrings__DefaultConnection` (see docker-compose.yml)

### Database Design Time Factory

`DomovoyDbContextFactory.cs` provides a design-time factory for EF Core migrations. It uses a hardcoded localhost connection string that is only used during `dotnet ef migrations add` commands, not at runtime.

## Service Ports

- **Web UI**: 1975
- **Intake API**: 1973
- **PostgreSQL**: 5432
- **MinIO S3 API**: 9000 (dev only, not exposed in production Docker)
- **MinIO Console**: 9001 (dev only)

Both Intake and Web expose `/health` endpoints for health checks.

## Logging

Both services use Serilog configured via `appsettings.json`. Structured logging is enabled and request logging middleware is configured in both Program.cs files.

## Client Library Usage

The `Domovoy.Client` project provides a stateless client for games to send events and errors.

### Basic Setup

```csharp
using var client = new DomovoyClient("http://localhost:1973");

// Create a standard payload that will be sent with each report
var standard = new StandardPayload
{
    GameVersion = "1.0.0",
    Platform = "Windows",
    Environment = Environment.Dev,
    UserId = Guid.NewGuid(),
    ComputerId = Guid.NewGuid(),
    GameId = Guid.NewGuid(),
    GameSequenceIds = [Guid.NewGuid()]  // List of sequence IDs
};
```

### Sending Events

```csharp
var eventData = new EventPayload
{
    Name = "LevelCompleted",
    Category = "Gameplay",
    Value = 1,
    UserId = "player123",
    Metadata = new Dictionary<string, object>
    {
        { "level", 5 },
        { "timeSeconds", 120.5 }
    }
};

Guid? reportId = await client.SendEventAsync(standard, eventData);
```

### Sending Errors

```csharp
// Send a custom error
var errorData = new ErrorPayload
{
    Severity = Severity.Error,
    Message = "Failed to load texture",
    StackTrace = "at Game.TextureLoader.Load() in TextureLoader.cs:line 42",
    Log = "Full error log..."
};

Guid? reportId = await client.SendErrorAsync(standard, errorData);

// Send a crash report from an exception (convenience method)
try
{
    // Game code
}
catch (Exception ex)
{
    Guid? reportId = await client.SendCrashAsync(standard, ex);
}
```

### Sending Attachments

Attachments use a two-phase upload: first submit a report and get back the report ID, then upload files against that ID.

```csharp
// Submit error and get report ID
var reportId = await client.SendCrashAsync(standard, exception);

// Upload attachment (up to 50 MB)
if (reportId != null)
{
    using var stream = File.OpenRead("screenshot.png");
    await client.SendAttachmentAsync(reportId.Value, stream, "screenshot.png", "image/png");
}
```

### Fire-and-Forget Methods

For scenarios where you don't want to wait for the result:

```csharp
client.SendEventFireAndForget(standard, eventData);
client.SendErrorFireAndForget(standard, errorData);
client.SendCrashFireAndForget(standard, exception);
client.SendAttachmentFireAndForget(reportId, stream, "file.dat");
```

**Note**: The client is stateless - you provide the StandardPayload with each request, allowing you to easily vary platform, version, and other metadata per report. The `Send*Async` methods return `Guid?` (the report ID on success, null on failure). The client is designed to fail silently to avoid telemetry from crashing the game.

## Executing Actions with Care

Carefully consider the reversibility and blast radius of actions. Local, reversible actions (editing files, running tests, running ephemeral scripts) are fine to take freely. But for actions that are hard to reverse, affect shared systems beyond the local environment, or touch production, **confirm first**.

Examples that warrant confirmation:
- Destructive: deleting files/branches, dropping tables, killing processes, `rm -rf`, overwriting uncommitted changes.
- Hard to reverse: force-push, `git reset --hard`, amending published commits, dependency downgrades, CI/CD changes.
- Visible to others: pushing code, creating/closing PRs or issues, sending messages, posting to external services.
- Uploading content to third-party tools (pastebins, diagram renderers): may be cached or indexed even if later deleted.

When you encounter an obstacle, do not use destructive actions as a shortcut to make it go away. Identify root causes; don't bypass safety checks (`--no-verify`, `--no-gpg-sign`) unless the user has explicitly asked. If you discover unexpected state — unfamiliar files, branches, locks — investigate before deleting; it may be the user's in-progress work.

## Tone for Updates

Match response length to the task. A simple question gets a direct answer, not headers and sections. End-of-turn summaries should be one or two sentences — what changed and what's next. Don't narrate internal deliberation; state results and decisions directly. Brief is good; silent is not.

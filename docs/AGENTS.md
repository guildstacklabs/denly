# Agent Workflow Guide

## Overview

This document defines how to effectively use multiple LLMs (Claude, Codex, Gemini) for Denly development. The goal: increase velocity while maintaining quality, security, and consistency.

**Core principle:** You (the human) lead. LLMs are specialized tools, not persistent team members. Each session starts fresh—context must be provided explicitly.

---

## Tech Stack & Architecture

Denly is a **.NET MAUI Blazor Hybrid** application.

- **Frontend**: Blazor (Razor components) running in a WebView
- **Mobile Container**: .NET MAUI (iOS/Android)
- **Backend/Data**: Supabase (PostgreSQL, Auth, Storage) via `supabase-csharp`
- **Language**: C# 11+ (.NET 8)

### Architectural Rules
1. **Services Layer**: All data access lives in `Services/`. Components use injected interfaces (e.g., `IExpenseService`), never Supabase directly.
2. **State Management**: Delegate complex state to Services. Keep Component state minimal.
3. **Authentication**: Always check `IAuthService` before data operations.

---

## Agent Selection Guide

Choose the right tool for the job:

| Agent | Best For | Limitations |
|-------|----------|-------------|
| **Claude** | Architecture decisions, code review, complex refactoring, security audits, debugging tricky issues | Higher cost per token |
| **Codex** | Quick fixes, simple implementations, boilerplate generation, straightforward bug fixes | Smaller context window, less nuanced |
| **Gemini** | Medium complexity features, code generation, following established patterns | May diverge from existing code style |

### Selection Heuristics
- **Use Claude** when you need to think through trade-offs, review security implications, or design something new
- **Use Codex** for well-defined, isolated changes where the path is clear
- **Use Gemini** for feature implementation when patterns are already established

---

## Task Tiers

Match process overhead to task complexity:

### Tier 1: Quick (No Brief Required)
**Examples:** Typo fixes, single-line changes, adding a log statement, simple renames

**Process:**
1. Describe the change in plain language
2. Agent implements
3. You verify and commit

### Tier 2: Standard (Brief Required)
**Examples:** New component, service method, bug fix touching 2-3 files, UI changes

**Process:**
1. Create Work Brief (use `docs/WORK_BRIEF_TEMPLATE.md`)
2. Agent implements and completes the brief
3. Review changes, verify, commit

### Tier 3: Complex (Brief + Review)
**Examples:** New feature spanning multiple files, refactoring, anything touching auth or data access

**Process:**
1. Create detailed Work Brief
2. Have Claude review the brief before sending to implementer
3. Agent implements and completes the brief
4. Review changes thoroughly
5. Verify, commit

---

## Automated Task Pickup (Codex/Gemini)

The backlog in `docs/BACKLOG.md` contains active tasks. Completed items are archived in `docs/BACKLOG_ARCHIVE.md`.

### For Codex/Gemini: How to Pick Up Work

1. **Read `docs/BACKLOG.md`**
2. **Find your tasks**: Search for `Delegate: Codex` or `Delegate: Gemini` with `Status: Ready`
3. **Work in batches by priority tier** (complete all your P1 tasks, then P2, etc.)
4. For each task in the current tier:
   - Read the Problem/Solution/Review sections for your task
   - Implement the solution, targeting the files listed
   - Add a brief Completion Report
   - Update status to `Awaiting Review`
   - **Continue to next task in same tier** (don't wait for review)
5. **Stop at tier boundary** - wait for Claude to review the batch before starting next tier

### Work-in-Progress Limits
- **Only 1 P0 and 1 P1 item may be active at any time**
- "Active" means status is `In Progress` OR `Awaiting Review`
- No new P0/P1 work starts until Claude validates and archives current active items
- This prevents large changes from compounding errors
- P2/P3 items can proceed in batches as before

### Batch Rules
- Complete all your tasks within a priority tier before stopping
- If Task B modifies files already changed by Task A, do them sequentially (A fully complete before B)
- If tasks are independent (different files), you may work them in any order
- Run `dotnet build` after completing the entire batch, not after each task
- Run `dotnet test` after completing the entire batch (after `dotnet build`)
- If tests fail, fix the issue before marking batch complete

### Status Flow
```
Ready → In Progress → Awaiting Review ──┐
Ready → In Progress → Awaiting Review ──┼─→ [Claude review] → Archived
Ready → In Progress → Awaiting Review ──┘
                   ↘ Blocked (if issues) → Ready (after Claude unblocks)
```

### After Code Review (Claude Only)
When Claude approves a completed task:
1. Remove the item from `BACKLOG.md`
2. Add a summary entry to `BACKLOG_ARCHIVE.md` with:
   - Completion date
   - Agent name
   - Files modified
   - Brief summary of changes
3. This keeps the active backlog lean and saves tokens

### Completion Report Template
When you finish a task, add a brief completion report in BACKLOG.md (Claude will archive it after review):

```markdown
#### Completion Report
- **Status:** Awaiting Review
- **Agent:** Codex/Gemini
- **Files Modified:**
  - `Services/SupabaseStorageService.cs`
- **Summary:** Added 10MB file size guard before upload
- **Build:** ✅ Pass
- **Tests:** ✅ Pass | ⚠️ Needs coverage (flags gap for Claude to address)
- **Testable Behaviors:**
  - Method returns empty list when no data exists
  - Method throws ArgumentException for invalid input
- **Edge Cases:**
  - Null den ID handled gracefully
- **Mocking Needs:**
  - Requires ISupabaseClientWrapper mock for unit testing
- **Notes:** None
```

### Rules
- **Batch by tier** - complete all tasks in a priority tier, then stop for review
- **Don't skip tiers** - finish P1 before P2, P2 before P3
- **Don't modify Claude-only tasks** - they're marked for a reason
- **If stuck, set status to `Blocked`** and explain in Notes
- **File conflicts** - if two tasks touch the same file, complete first task fully before starting second

### Bootstrap Prompt (Copy-Paste to Start Agent)

Use this prompt to initialize Codex or Gemini for the automated workflow:

```
You are working on the Denly codebase.

FIRST: Read `docs/AGENTS.md` completely - it contains:
- Project context and tech stack
- Coding standards you must follow
- Security guidelines (critical)
- Testing requirements (critical)
- Your workflow instructions

THEN: Read `docs/BACKLOG.md` and find tasks assigned to you (Codex/Gemini) with Status: Ready.

Follow the workflow in docs/AGENTS.md exactly. Work in batches by priority tier.

After completing all tasks in a tier, run `dotnet test` to verify no tests were broken.
Include testable behaviors in your Completion Report for Claude to create tests.
```

---

## Adding New Work Items

When new improvements, bugs, or features are identified, add them to `docs/BACKLOG.md` using this process:

### For Claude (or Human)
1. Determine priority tier (P0-P3)
2. Assess urgency and complexity (see Sorting Criteria below)
3. Assess if delegatable (Codex/Gemini) or Claude-only
4. Add item in the appropriate section with this template:

```markdown
### [Number]. [Title]
**Source:** [Who identified it] | **Effort:** Low/Medium/High | **Risk:** Low/Medium/High

> **Delegate:** [Codex/Gemini/Claude only] | **Status:** Ready
> **Reason:** [If Claude-only, explain why]

**Problem:** [1-2 sentences]

**Solution:**
- [Bullet points]

**Targets:**
- `path/to/file.cs`

#### Delegation Prompt ([Agent])
```
[Full instructions for the agent - goal, requirements, constraints, do-not-touch]
```

#### Review Checklist
- [ ] [Specific verification items]

#### Completion Report
<!-- Agent fills this in when done -->
```

5. Insert item in correct position (see Sorting Criteria)
6. Update the Active Work table

### Sorting Criteria

The backlog is sorted by **urgency first**, then **complexity** (simpler items first within each urgency tier).

**Urgency levels:**
| Level | Criteria |
|-------|----------|
| Critical | Blocks other features, security issue, or app unusable without it |
| High | Core MVP functionality users expect, or significant pain point |
| Medium | Improves trust/compliance/UX but app functional without it |
| Low | Polish, nice-to-have, or can wait until post-launch |

**Complexity levels:**
| Level | Indicators |
|-------|------------|
| Low | 1-2 files, single component, CSS-only, no new interfaces |
| Medium | 3-4 files, model + UI changes, modifications to existing services |
| High | New services/interfaces, platform-specific code, external integrations, Edge Functions |

**Why this order:** Urgency ensures critical work isn't buried. Within each urgency tier, simpler items come first so agents can deliver quick wins while complex items are in progress.

### Item Numbering
- New P0/P1 items: Insert at correct priority position, renumber subsequent items
- New P2/P3 items: Append to end of tier, use next available number

### For Codex/Gemini: Suggesting New Items

**⚠️ CRITICAL: Codex and Gemini must NEVER modify existing backlog items or sections.**

When you discover a potential improvement, bug, or feature idea while working:

1. **DO NOT** edit any existing sections of BACKLOG.md
2. **DO NOT** add fully-specced items to P0/P1/P2/P3 sections
3. **ONLY** append to the `## Agent Suggestions` section at the bottom of BACKLOG.md

#### Suggestion Format
```markdown
### [Brief Title]
- **Suggested by:** Codex/Gemini
- **Date:** YYYY-MM-DD
- **Context:** [What were you working on when you noticed this?]
- **Idea:** [1-2 sentences describing the improvement]
- **Potential files:** [Optional: files that might be involved]
```

#### Example
```markdown
### Add loading skeleton to Calendar
- **Suggested by:** Gemini
- **Date:** 2024-01-15
- **Context:** Working on P2-3 (multi-child color coding)
- **Idea:** Calendar shows blank space while events load. A skeleton UI would improve perceived performance.
- **Potential files:** Calendar.razor, Calendar.razor.css
```

#### What Happens Next
1. Human or Claude reviews the Suggestions section periodically
2. Good ideas get promoted to proper backlog items with full specs
3. Suggestions that are implemented or rejected get removed
4. You may see your suggestion become a real task in a future session

#### Why This Process?
- Prevents accidental deletion/corruption of existing backlog items
- Ensures architectural review before commitment
- Maintains consistent formatting across the backlog
- Captures valuable insights without blocking your current work

---

## Workflow Contract

### Your Responsibilities (Human)
- Define scope clearly in the Work Brief
- Provide necessary context (LLMs don't remember previous sessions)
- Review all changes before committing
- Maintain the single source of truth (this repo)

### Agent Responsibilities
- Execute only what's in the brief
- Stop and flag if scope needs to expand
- Complete the verification section of the brief
- Never introduce new dependencies or schema changes without explicit approval

### Stop Conditions (Agents Must Halt)
- Task requires architectural changes not in the brief
- Unexpected files need modification
- Security concern discovered
- Brief is ambiguous or contradictory

---

## Handoff Checklist

### Before Sending to Agent
- [ ] Brief has clear goal (1-2 sentences)
- [ ] Files to touch are listed
- [ ] "Do not touch" areas specified
- [ ] Acceptance criteria are observable/testable

### After Receiving from Agent
- [ ] Review all changed files
- [ ] Run `dotnet build` - no errors
- [ ] Test the specific functionality manually
- [ ] Check for unintended side effects
- [ ] Commit with clear message: `[Agent] Brief description`

---

## Consistency Tips

To avoid a patchwork codebase:

1. **Stick to one agent per feature** - Don't have Codex start and Gemini finish
2. **Include code examples in briefs** - Show the pattern you want followed
3. **Review immediately** - Fix style drift before it compounds
4. **Use Claude for review** - Even if Codex/Gemini implemented, have Claude review for consistency

---

## Security Guidelines

This is a co-parenting app with sensitive data. Non-negotiable rules:

- **Never** log PII (names, emails, user IDs, invite codes, tokens)
- **Always** assume Row Level Security (RLS) is active on all tables
- **Sanitize** inputs at system boundaries
- **Validate** auth state before any data operation

```csharp
// Required pattern for data operations
if (await _authService.GetCurrentUserAsync() is not { } user)
{
    Console.WriteLine("[Context] No authenticated user");
    return;
}
```

---

## Coding Standards

### C# Conventions
- `async/await` for all I/O operations
- Nullable reference types (`string?`, `int?`)
- Private fields prefixed with `_` (e.g., `_expenseService`)
- File-scoped namespaces (`namespace Denly.Services;`)

### Blazor/Razor Conventions
- Inject services at top: `@inject IExpenseService ExpenseService`
- `@code { }` blocks at bottom
- CSS isolation: prefer `Page.razor.css` over global styles

### Error Handling Pattern
```csharp
try
{
    _isLoading = true;
    await SomeService.DoWorkAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[Context] Error: {ex.Message}");
    // Show user feedback
}
finally
{
    _isLoading = false;
}
```

---

## Project Structure

```
Components/Pages/    - Blazor pages (Views)
Components/Shared/   - Reusable UI components
Services/            - Data access and logic
Models/              - Data entities (POCOs)
wwwroot/             - Static assets
Platforms/           - iOS/Android specific (keep minimal)
```

---

## Known Constraints

- **Offline Support**: Not implemented. Avoid decisions that block future offline-first refactoring.
- **Testing**: Unit test project exists (`Denly.Tests`). Agents must follow testing requirements below.
- **Single Developer**: Process should enable velocity, not create bureaucracy.

---

## Testing Requirements

All agents must follow these testing practices to maintain code quality.

### For All Agents
- Run `dotnet test` after completing work (in addition to `dotnet build`)
- If tests fail, fix them before marking task complete
- Do not delete or skip existing tests

### For Codex/Gemini: Test Documentation
When completing a task, include testing information in your Completion Report:
- **Testable behaviors:** List 2-3 key behaviors that should be tested
- **Edge cases:** Note any edge cases discovered during implementation
- **Mocking needs:** Identify any dependencies that need mocking

### For Claude: Test Maintenance
- Claude is responsible for writing/maintaining unit tests
- When reviewing Codex/Gemini work, use their "Testable Behaviors" to write tests
- Add tests for new functionality before approving PR

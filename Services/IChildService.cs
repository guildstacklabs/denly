using Denly.Models;

namespace Denly.Services;

/// <summary>
/// Service for managing children within a den.
/// Handles CRUD, name validation, and display name logic.
/// </summary>
public interface IChildService
{
    /// <summary>
    /// Gets all active children in the current den.
    /// </summary>
    Task<List<Child>> GetActiveChildrenAsync();

    /// <summary>
    /// Gets all children in the current den (including deactivated).
    /// </summary>
    Task<List<Child>> GetAllChildrenAsync();

    /// <summary>
    /// Gets a single child by ID.
    /// </summary>
    Task<Child?> GetChildAsync(string childId);

    /// <summary>
    /// Adds a new child to the current den.
    /// </summary>
    Task<Child> AddChildAsync(Child child);

    /// <summary>
    /// Updates an existing child's information.
    /// </summary>
    Task UpdateChildAsync(Child child);

    /// <summary>
    /// Soft-deletes a child (sets deactivated_at).
    /// </summary>
    Task DeactivateChildAsync(string childId);

    /// <summary>
    /// Reactivates a deactivated child (clears deactivated_at).
    /// </summary>
    Task ReactivateChildAsync(string childId);

    /// <summary>
    /// Validates a child's name against existing children.
    /// Returns validation result with any conflicts.
    /// </summary>
    Task<ChildNameValidationResult> ValidateChildNameAsync(Child child, string? excludeChildId = null);

    /// <summary>
    /// Gets the display name for a child, handling disambiguation
    /// when multiple children share the same first name.
    /// </summary>
    string GetDisplayName(Child child, IEnumerable<Child> allChildren);

    /// <summary>
    /// Gets display names for all provided children, handling disambiguation.
    /// </summary>
    Dictionary<string, string> GetDisplayNames(IEnumerable<Child> children);
}

/// <summary>
/// Result of child name validation.
/// </summary>
public record ChildNameValidationResult(
    bool IsValid,
    NameConflictType ConflictType = NameConflictType.None,
    string? ConflictingChildName = null
);

/// <summary>
/// Types of name conflicts that can occur.
/// </summary>
public enum NameConflictType
{
    /// <summary>No conflict detected.</summary>
    None,

    /// <summary>Exact match (first + middle + last) - should be blocked.</summary>
    ExactMatch,

    /// <summary>Partial match (first + last) - should warn but allow.</summary>
    PartialMatch
}

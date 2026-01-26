using Denly.Models;
using Microsoft.Extensions.Logging;

namespace Denly.Services;

/// <summary>
/// Service for managing children within a den.
/// </summary>
public class ChildService : SupabaseServiceBase, IChildService
{
    private readonly ILogger<ChildService> _logger;

    public ChildService(IDenService denService, IAuthService authService, ILogger<ChildService> logger)
        : base(denService, authService)
    {
        _logger = logger;
    }

    public async Task<List<Child>> GetActiveChildrenAsync()
    {
        var all = await GetAllChildrenAsync();
        return all.Where(c => c.IsActive).ToList();
    }

    public async Task<List<Child>> GetAllChildrenAsync()
    {
        var denId = TryGetCurrentDenId();
        if (denId == null || SupabaseClient == null)
        {
            _logger.LogDebug("[ChildService] No client or den ID");
            return new List<Child>();
        }

        try
        {
            var response = await SupabaseClient
                .From<Child>()
                .Where(c => c.DenId == denId)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChildService] Failed to get children for den {DenId}", denId);
            return new List<Child>();
        }
    }

    public async Task<Child?> GetChildAsync(string childId)
    {
        if (SupabaseClient == null)
        {
            return null;
        }

        try
        {
            var response = await SupabaseClient
                .From<Child>()
                .Where(c => c.Id == childId)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChildService] Failed to get child {ChildId}", childId);
            return null;
        }
    }

    public async Task<Child> AddChildAsync(Child child)
    {
        var client = GetClientOrThrow();
        var denId = GetCurrentDenIdOrThrow();

        child.Id = Guid.NewGuid().ToString();
        child.DenId = denId;
        child.CreatedAt = DateTime.UtcNow;

        try
        {
            var response = await client
                .From<Child>()
                .Insert(child);

            _logger.LogDebug("[ChildService] Added child {ChildId} to den {DenId}", child.Id, denId);
            return response.Models.FirstOrDefault() ?? child;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChildService] Failed to add child to den {DenId}", denId);
            throw;
        }
    }

    public async Task UpdateChildAsync(Child child)
    {
        var client = GetClientOrThrow();

        try
        {
            await client
                .From<Child>()
                .Where(c => c.Id == child.Id)
                .Update(child);

            _logger.LogDebug("[ChildService] Updated child {ChildId}", child.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChildService] Failed to update child {ChildId}", child.Id);
            throw;
        }
    }

    public async Task DeactivateChildAsync(string childId)
    {
        var child = await GetChildAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child {childId} not found");
        }

        child.DeactivatedAt = DateTime.UtcNow;
        await UpdateChildAsync(child);

        _logger.LogDebug("[ChildService] Deactivated child {ChildId}", childId);
    }

    public async Task ReactivateChildAsync(string childId)
    {
        var child = await GetChildAsync(childId);
        if (child == null)
        {
            throw new InvalidOperationException($"Child {childId} not found");
        }

        child.DeactivatedAt = null;
        await UpdateChildAsync(child);

        _logger.LogDebug("[ChildService] Reactivated child {ChildId}", childId);
    }

    public async Task<ChildNameValidationResult> ValidateChildNameAsync(Child child, string? excludeChildId = null)
    {
        var existingChildren = await GetActiveChildrenAsync();

        // Exclude the child being edited (if updating)
        if (!string.IsNullOrEmpty(excludeChildId))
        {
            existingChildren = existingChildren.Where(c => c.Id != excludeChildId).ToList();
        }

        // Normalize names for comparison
        var firstName = NormalizeName(child.FirstName);
        var middleName = NormalizeName(child.MiddleName);
        var lastName = NormalizeName(child.LastName);

        foreach (var existing in existingChildren)
        {
            var existingFirst = NormalizeName(existing.FirstName);
            var existingMiddle = NormalizeName(existing.MiddleName);
            var existingLast = NormalizeName(existing.LastName);

            // Check for exact match (first + middle + last)
            if (firstName == existingFirst &&
                middleName == existingMiddle &&
                lastName == existingLast)
            {
                return new ChildNameValidationResult(
                    IsValid: false,
                    ConflictType: NameConflictType.ExactMatch,
                    ConflictingChildName: existing.FullName
                );
            }

            // Check for partial match (first + last only, and both have last names)
            if (firstName == existingFirst &&
                !string.IsNullOrEmpty(lastName) &&
                !string.IsNullOrEmpty(existingLast) &&
                lastName == existingLast)
            {
                return new ChildNameValidationResult(
                    IsValid: true, // Warning, not blocking
                    ConflictType: NameConflictType.PartialMatch,
                    ConflictingChildName: existing.FullName
                );
            }
        }

        return new ChildNameValidationResult(IsValid: true);
    }

    public string GetDisplayName(Child child, IEnumerable<Child> allChildren)
    {
        var activeChildren = allChildren.Where(c => c.IsActive).ToList();

        // Find children with the same first name
        var sameFirstName = activeChildren
            .Where(c => c.Id != child.Id && NormalizeName(c.FirstName) == NormalizeName(child.FirstName))
            .ToList();

        // No duplicates: show first name only
        if (sameFirstName.Count == 0)
        {
            return child.FirstName;
        }

        // Has middle name: show first + middle initial
        if (!string.IsNullOrWhiteSpace(child.MiddleName))
        {
            return $"{child.FirstName} {child.MiddleName[0]}.";
        }

        // Has last name: show first + last initial
        if (!string.IsNullOrWhiteSpace(child.LastName))
        {
            return $"{child.FirstName} {child.LastName[0]}.";
        }

        // No middle or last name, but duplicates exist - just return first name
        // (This shouldn't happen if validation is working)
        return child.FirstName;
    }

    public Dictionary<string, string> GetDisplayNames(IEnumerable<Child> children)
    {
        var childList = children.ToList();
        var result = new Dictionary<string, string>();

        foreach (var child in childList)
        {
            result[child.Id] = GetDisplayName(child, childList);
        }

        return result;
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name.Trim().ToLowerInvariant();
    }
}

namespace Denly.Tests.Services;

/// <summary>
/// Tests for Den service guardrail behaviors.
/// These tests verify that services handle missing den states correctly.
///
/// These are behavior pattern tests that document expected behavior without
/// directly referencing the MAUI project. The actual services should follow
/// these patterns.
/// </summary>
public class DenServiceBehaviorTests
{
    #region Interfaces for Testing Behavior Patterns

    /// <summary>
    /// Simplified interface representing den state provider behavior.
    /// Mirrors the essential behavior from IDenService.
    /// </summary>
    public interface IDenStateProvider
    {
        string? GetCurrentDenId();
        bool IsInitialized { get; }
    }

    /// <summary>
    /// Simplified interface representing a data service that depends on den context.
    /// Mirrors the behavior pattern from SupabaseServiceBase-derived services.
    /// </summary>
    public interface IDenAwareDataService
    {
        Task<List<string>> GetItemsAsync();
        Task SaveItemAsync(string item);
    }

    /// <summary>
    /// Sample implementation showing expected guardrail behavior.
    /// </summary>
    public class SampleDenAwareService : IDenAwareDataService
    {
        private readonly IDenStateProvider _denProvider;
        private readonly List<string> _items = new();

        public SampleDenAwareService(IDenStateProvider denProvider)
        {
            _denProvider = denProvider;
        }

        public Task<List<string>> GetItemsAsync()
        {
            // GUARDRAIL: Return empty list when no den selected
            var denId = _denProvider.GetCurrentDenId();
            if (string.IsNullOrEmpty(denId))
            {
                return Task.FromResult(new List<string>());
            }

            return Task.FromResult(_items.ToList());
        }

        public Task SaveItemAsync(string item)
        {
            // GUARDRAIL: Throw when no den selected for write operations
            var denId = _denProvider.GetCurrentDenId();
            if (string.IsNullOrEmpty(denId))
            {
                throw new InvalidOperationException("No den selected");
            }

            _items.Add(item);
            return Task.CompletedTask;
        }
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetItems_WhenNoDenSelected_ReturnsEmptyList()
    {
        // Arrange
        var denProvider = new NoDenSelectedProvider();
        var service = new SampleDenAwareService(denProvider);

        // Act
        var items = await service.GetItemsAsync();

        // Assert
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetItems_WhenDenSelected_ReturnsItems()
    {
        // Arrange
        var denProvider = new DenSelectedProvider("den-123");
        var service = new SampleDenAwareService(denProvider);
        await service.SaveItemAsync("item-1");

        // Act
        var items = await service.GetItemsAsync();

        // Assert
        Assert.Single(items);
        Assert.Contains("item-1", items);
    }

    [Fact]
    public async Task SaveItem_WhenNoDenSelected_ThrowsInvalidOperationException()
    {
        // Arrange
        var denProvider = new NoDenSelectedProvider();
        var service = new SampleDenAwareService(denProvider);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveItemAsync("test-item"));
    }

    [Fact]
    public async Task SaveItem_WhenDenSelected_Succeeds()
    {
        // Arrange
        var denProvider = new DenSelectedProvider("den-123");
        var service = new SampleDenAwareService(denProvider);

        // Act
        await service.SaveItemAsync("test-item");
        var items = await service.GetItemsAsync();

        // Assert
        Assert.Contains("test-item", items);
    }

    [Fact]
    public void GetCurrentDenId_WhenNotInitialized_ReturnsNull()
    {
        // Arrange
        var denProvider = new NoDenSelectedProvider();

        // Act
        var denId = denProvider.GetCurrentDenId();

        // Assert
        Assert.Null(denId);
    }

    [Fact]
    public void GetCurrentDenId_WhenInitialized_ReturnsDenId()
    {
        // Arrange
        var expectedDenId = "den-abc-123";
        var denProvider = new DenSelectedProvider(expectedDenId);

        // Act
        var denId = denProvider.GetCurrentDenId();

        // Assert
        Assert.Equal(expectedDenId, denId);
    }

    #endregion

    #region Test Helpers

    private class NoDenSelectedProvider : IDenStateProvider
    {
        public string? GetCurrentDenId() => null;
        public bool IsInitialized => true;
    }

    private class DenSelectedProvider : IDenStateProvider
    {
        private readonly string _denId;

        public DenSelectedProvider(string denId)
        {
            _denId = denId;
        }

        public string? GetCurrentDenId() => _denId;
        public bool IsInitialized => true;
    }

    #endregion
}

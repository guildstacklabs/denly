using System.ComponentModel;
using System.Runtime.CompilerServices;
using Denly.Components.Shared;
using Microsoft.Maui.Graphics;

namespace Denly.ViewModels;

public class ChildFilterItem : INotifyPropertyChanged
{
    private string _childId = string.Empty;
    private string _name = string.Empty;
    private Color _accentColor = Colors.Transparent;
    private Color _chipColor = DesignTokens.Colors.NookBackground;
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ChildId
    {
        get => _childId;
        set => SetField(ref _childId, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            if (SetField(ref _accentColor, value))
            {
                UpdateChipColor();
            }
        }
    }

    public Color ChipColor
    {
        get => _chipColor;
        private set => SetField(ref _chipColor, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetField(ref _isSelected, value))
            {
                UpdateChipColor();
            }
        }
    }

    private void UpdateChipColor()
    {
        ChipColor = IsSelected ? AccentColor : DesignTokens.Colors.NookBackground;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

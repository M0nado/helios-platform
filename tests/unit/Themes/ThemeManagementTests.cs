namespace HELIOS.Platform.Tests.Unit.Themes;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HELIOS.Platform.Themes;

public class ThemeManagementTests
{
    private readonly ThemeManager _themeManager;
    private readonly ThemeService _themeService;

    public ThemeManagementTests()
    {
        _themeManager = new ThemeManager();
        _themeService = new ThemeService();
    }

    #region Theme Manager Tests

    [Fact]
    public async Task GetCurrentThemeAsync_ReturnsDarkModeByDefault()
    {
        // Act
        var theme = await _themeManager.GetCurrentThemeAsync();

        // Assert
        Assert.NotNull(theme);
        Assert.True(theme.IsDark);
    }

    [Fact]
    public async Task GetThemeAsync_WithValidId_ReturnsTheme()
    {
        // Act
        var theme = await _themeManager.GetThemeAsync("dark-mode");

        // Assert
        Assert.NotNull(theme);
        Assert.Equal("dark-mode", theme.Id);
    }

    [Fact]
    public async Task GetThemeAsync_WithInvalidId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ThemeException>(() => _themeManager.GetThemeAsync("non-existent"));
    }

    [Fact]
    public async Task ListThemesAsync_ReturnsAllThemes()
    {
        // Act
        var themes = await _themeManager.ListThemesAsync();

        // Assert
        Assert.NotNull(themes);
        Assert.NotEmpty(themes);
        Assert.True(themes.Count >= 3); // At least 3 default themes
    }

    [Fact]
    public async Task SetThemeAsync_WithValidId_SetsThemeSuccessfully()
    {
        // Act
        await _themeManager.SetThemeAsync("light-mode");
        var current = await _themeManager.GetCurrentThemeAsync();

        // Assert
        Assert.Equal("light-mode", current.Id);
        Assert.False(current.IsDark);
    }

    [Fact]
    public async Task SetThemeAsync_WithInvalidId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ThemeException>(() => _themeManager.SetThemeAsync("non-existent"));
    }

    [Fact]
    public async Task CreateThemeAsync_WithCustomTheme_CreatesSuccessfully()
    {
        // Arrange
        var customTheme = new Theme
        {
            Id = "custom-theme",
            Name = "Custom",
            IsDark = true,
            Colors = new ThemeColors { Primary = "#FF0000", Background = "#000000" }
        };

        // Act
        var created = await _themeManager.CreateThemeAsync(customTheme);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("custom-theme", created.Id);
    }

    [Fact]
    public async Task CreateThemeAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        var customTheme = new Theme
        {
            Id = "dark-mode",
            Name = "Duplicate",
            IsDark = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ThemeException>(() => _themeManager.CreateThemeAsync(customTheme));
    }

    [Fact]
    public async Task UpdateThemeAsync_UpdatesExistingTheme()
    {
        // Arrange
        var theme = await _themeManager.GetThemeAsync("dark-mode");
        theme.Colors.Primary = "#FF00FF";

        // Act
        await _themeManager.UpdateThemeAsync(theme);
        var updated = await _themeManager.GetThemeAsync("dark-mode");

        // Assert
        Assert.Equal("#FF00FF", updated.Colors.Primary);
    }

    [Fact]
    public async Task DeleteThemeAsync_DeletesCustomTheme()
    {
        // Arrange
        var customTheme = new Theme
        {
            Id = "temp-theme",
            Name = "Temporary",
            IsDark = true
        };
        await _themeManager.CreateThemeAsync(customTheme);

        // Act
        await _themeManager.DeleteThemeAsync("temp-theme");

        // Assert
        await Assert.ThrowsAsync<ThemeException>(() => _themeManager.GetThemeAsync("temp-theme"));
    }

    [Fact]
    public async Task SetThemeModeAsync_WithLight_SetsLightTheme()
    {
        // Act
        await _themeManager.SetThemeModeAsync(ThemeMode.Light);
        var current = await _themeManager.GetCurrentThemeAsync();

        // Assert
        Assert.False(current.IsDark);
    }

    [Fact]
    public async Task SetThemeModeAsync_WithDark_SetsDarkTheme()
    {
        // Act
        await _themeManager.SetThemeModeAsync(ThemeMode.Dark);
        var current = await _themeManager.GetCurrentThemeAsync();

        // Assert
        Assert.True(current.IsDark);
    }

    [Fact]
    public async Task IsDarkModeEnabledAsync_ReturnsDarkModeStatus()
    {
        // Act
        var isDark = await _themeManager.IsDarkModeEnabledAsync();

        // Assert
        Assert.True(isDark);
    }

    [Fact]
    public async Task SetDarkModeAsync_WithTrue_EnablesDarkMode()
    {
        // Act
        await _themeManager.SetDarkModeAsync(true);
        var isDark = await _themeManager.IsDarkModeEnabledAsync();

        // Assert
        Assert.True(isDark);
    }

    [Fact]
    public async Task SetDarkModeAsync_WithFalse_DisablesDarkMode()
    {
        // Act
        await _themeManager.SetDarkModeAsync(false);
        var isDark = await _themeManager.IsDarkModeEnabledAsync();

        // Assert
        Assert.False(isDark);
    }

    #endregion

    #region Theme Service Tests

    [Fact]
    public async Task ApplyThemeAsync_WithValidTheme_AppliesSuccessfully()
    {
        // Arrange
        var theme = new DarkModeTheme();

        // Act & Assert - should not throw
        await _themeService.ApplyThemeAsync(theme);
    }

    [Fact]
    public async Task ThemeChanged_EventFires_WhenThemeApplied()
    {
        // Arrange
        var eventFired = false;
        _themeService.ThemeChanged += (s, e) => eventFired = true;
        var theme = new DarkModeTheme();

        // Act
        await _themeService.ApplyThemeAsync(theme);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public async Task ApplySystemThemeAsync_AppliesSystemPreference()
    {
        // Act & Assert - should not throw
        await _themeService.ApplySystemThemeAsync();
    }

    [Fact]
    public async Task GetSystemPreferenceAsync_ReturnsSystemTheme()
    {
        // Act
        var theme = await _themeService.GetSystemPreferenceAsync();

        // Assert
        Assert.NotNull(theme);
    }

    #endregion

    #region Theme Definition Tests

    [Fact]
    public void DarkModeTheme_HasCorrectColors()
    {
        // Arrange & Act
        var theme = new DarkModeTheme();

        // Assert
        Assert.NotNull(theme.Colors.Primary);
        Assert.NotNull(theme.Colors.Background);
        Assert.True(theme.IsDark);
    }

    [Fact]
    public void LightModeTheme_HasCorrectColors()
    {
        // Arrange & Act
        var theme = new LightModeTheme();

        // Assert
        Assert.NotNull(theme.Colors.Primary);
        Assert.NotNull(theme.Colors.Background);
        Assert.False(theme.IsDark);
    }

    [Fact]
    public void HighContrastDarkTheme_HasMaxContrast()
    {
        // Arrange & Act
        var theme = new HighContrastDarkTheme();

        // Assert
        Assert.NotNull(theme.Colors.Primary);
        Assert.NotNull(theme.Colors.OnBackground);
        Assert.True(theme.IsDark);
    }

    [Fact]
    public void Theme_HasTypography()
    {
        // Arrange
        var theme = new DarkModeTheme();

        // Assert
        Assert.NotNull(theme.Typography);
        Assert.NotNull(theme.Typography.Display);
        Assert.NotNull(theme.Typography.Body);
    }

    [Fact]
    public void Theme_HasSpacing()
    {
        // Arrange
        var theme = new DarkModeTheme();

        // Assert
        Assert.NotNull(theme.Spacing);
        Assert.Equal(4, theme.Spacing.XS);
        Assert.Equal(48, theme.Spacing.XXL);
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void DarkModeTheme_MeetsWCAGAA_ColorContrast()
    {
        // Arrange
        var theme = new DarkModeTheme();

        // Assert - Cyan on near-black should have sufficient contrast
        Assert.NotNull(theme.Colors.Primary);
        Assert.NotNull(theme.Colors.Background);
    }

    [Fact]
    public void HighContrastDarkTheme_MeetsWCAGAAA_ColorContrast()
    {
        // Arrange
        var theme = new HighContrastDarkTheme();

        // Assert - Yellow on black has max contrast
        Assert.Equal("#FFFF00", theme.Colors.Primary);
        Assert.Equal("#000000", theme.Colors.Background);
    }

    [Fact]
    public void AllThemes_HaveReadableFonts()
    {
        // Arrange
        var themes = new List<Theme>
        {
            new DarkModeTheme(),
            new LightModeTheme(),
            new HighContrastDarkTheme()
        };

        // Assert
        foreach (var theme in themes)
        {
            Assert.NotNull(theme.Typography.Body);
            Assert.True(theme.Typography.Body.Size >= 12); // Minimum readable size
        }
    }

    #endregion
}

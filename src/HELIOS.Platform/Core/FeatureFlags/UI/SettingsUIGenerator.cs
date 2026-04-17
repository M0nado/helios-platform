using System;
using System.Collections.Generic;
using HELIOS.Platform.Core.FeatureFlags.Models;

namespace HELIOS.Platform.Core.FeatureFlags.UI
{
    /// <summary>
    /// UI element generation for settings
    /// </summary>
    public class SettingsUIGenerator
    {
        /// <summary>
        /// Generate UI elements from settings schema
        /// </summary>
        public List<UIElement> GenerateUIElements(SettingsSchema schema)
        {
            var elements = new List<UIElement>();

            if (schema?.Properties == null)
                return elements;

            foreach (var kvp in schema.Properties)
            {
                var property = kvp.Value;
                var element = GenerateUIElement(kvp.Key, property);
                if (element != null)
                    elements.Add(element);
            }

            return elements;
        }

        /// <summary>
        /// Generate a single UI element
        /// </summary>
        private UIElement GenerateUIElement(string key, SettingProperty property)
        {
            var element = new UIElement
            {
                Id = key,
                Label = property.Name,
                Description = property.Description,
                Type = MapPropertyTypeToUIType(property.Type),
                Required = property.Required,
                ReadOnly = property.IsReadOnly,
                Value = property.Value ?? property.DefaultValue,
                Metadata = new Dictionary<string, object>()
            };

            // Add type-specific properties
            switch (property.Type?.ToLower())
            {
                case "number":
                    element.Type = "number";
                    if (property.MinValue.HasValue)
                        element.Metadata["min"] = property.MinValue;
                    if (property.MaxValue.HasValue)
                        element.Metadata["max"] = property.MaxValue;
                    break;

                case "string":
                    element.Type = "text";
                    if (!string.IsNullOrEmpty(property.Pattern))
                        element.Metadata["pattern"] = property.Pattern;
                    if (property.MinLength.HasValue)
                        element.Metadata["minLength"] = property.MinLength;
                    if (property.MaxLength.HasValue)
                        element.Metadata["maxLength"] = property.MaxLength;
                    if (property.AllowedValues?.Count > 0)
                    {
                        element.Type = "select";
                        element.Options = new List<SelectOption>();
                        foreach (var value in property.AllowedValues)
                        {
                            element.Options.Add(new SelectOption
                            {
                                Value = value.ToString(),
                                Label = value.ToString()
                            });
                        }
                    }
                    break;

                case "boolean":
                    element.Type = "checkbox";
                    break;

                case "array":
                    element.Type = "tags";
                    break;
            }

            if (property.IsSecret)
                element.Type = "password";

            return element;
        }

        /// <summary>
        /// Map property type to UI type
        /// </summary>
        private string MapPropertyTypeToUIType(string propertyType)
        {
            return propertyType?.ToLower() switch
            {
                "string" => "text",
                "number" => "number",
                "boolean" => "checkbox",
                "array" => "tags",
                "object" => "json",
                _ => "text"
            };
        }

        /// <summary>
        /// Generate a feature flag toggle UI
        /// </summary>
        public ToggleUIElement GenerateToggleUI(FeatureFlag flag)
        {
            var toggle = new ToggleUIElement
            {
                Id = flag.Id,
                Name = flag.Name,
                Description = flag.Description,
                IsEnabled = flag.Enabled,
                Category = flag.Category,
                Tags = flag.Tags,
                Type = flag.Type.ToString(),
                State = flag.State.ToString(),
                Priority = flag.Priority
            };

            if (flag.Type == FeatureFlagTypeEnum.Percentage && flag.RolloutPercentage.HasValue)
            {
                toggle.RolloutPercentage = flag.RolloutPercentage.Value;
            }

            if (flag.ExpiresAt.HasValue)
            {
                toggle.ExpiresAt = flag.ExpiresAt.Value;
            }

            return toggle;
        }

        /// <summary>
        /// Generate settings panel UI
        /// </summary>
        public SettingsPanelUI GenerateSettingsPanelUI(
            List<SettingsSchema> schemas,
            List<FeatureFlag> flags)
        {
            var panel = new SettingsPanelUI
            {
                Sections = new List<UIPanelSection>()
            };

            // Add settings sections
            foreach (var schema in schemas)
            {
                var section = new UIPanelSection
                {
                    Title = schema.SchemaName,
                    Description = schema.Description,
                    Elements = GenerateUIElements(schema)
                };

                panel.Sections.Add(section);
            }

            // Add toggles section
            if (flags?.Count > 0)
            {
                var togglesSection = new UIPanelSection
                {
                    Title = "Feature Toggles",
                    Description = "Manage feature flags",
                    Toggles = new List<ToggleUIElement>()
                };

                foreach (var flag in flags)
                {
                    togglesSection.Toggles.Add(GenerateToggleUI(flag));
                }

                panel.Sections.Add(togglesSection);
            }

            return panel;
        }
    }

    /// <summary>
    /// UI element
    /// </summary>
    public class UIElement
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public object Value { get; set; }
        public bool Required { get; set; }
        public bool ReadOnly { get; set; }
        public List<SelectOption> Options { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Select option
    /// </summary>
    public class SelectOption
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Toggle UI element
    /// </summary>
    public class ToggleUIElement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        public int Priority { get; set; }
        public int? RolloutPercentage { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Settings panel UI
    /// </summary>
    public class SettingsPanelUI
    {
        public List<UIPanelSection> Sections { get; set; } = new();
    }

    /// <summary>
    /// UI panel section
    /// </summary>
    public class UIPanelSection
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public List<UIElement> Elements { get; set; } = new();
        public List<ToggleUIElement> Toggles { get; set; } = new();
    }
}

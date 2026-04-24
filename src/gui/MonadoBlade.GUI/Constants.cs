using System.Windows.Media;

namespace MonadoBlade.GUI
{
    /// <summary>
    /// Centralized constants for MonadoBlade visual system.
    /// Eliminates magic numbers and ensures consistency across components.
    /// </summary>
    public static class BladeConstants
    {
        // Blade scale values for state transitions
        public const double SCALE_IDLE = 1.0;
        public const double SCALE_HOVER = 1.2;
        public const double SCALE_ACTIVE = 1.3;
        public const double SCALE_CHARGED = 1.4;

        // Glow opacity/intensity values
        public const double GLOW_IDLE = 0.2;
        public const double GLOW_HOVER = 0.6;
        public const double GLOW_ACTIVE = 1.0;
        public const double GLOW_CHARGING = 0.8;

        // Animation timing (milliseconds)
        public const int DURATION_IDLE_MS = 300;
        public const int DURATION_HOVER_MS = 200;
        public const int DURATION_ACTIVE_MS = 150;
        public const int DURATION_CHARGE_MS = 50;
        public const int DURATION_RELEASE_MS = 300;

        // Color values - Kanji interaction colors
        public static readonly Color COLOR_CYAN = Color.FromRgb(0, 217, 255);           // Blade base
        public static readonly Color COLOR_MAGENTA = Color.FromRgb(255, 0, 85);         // Power/Red
        public static readonly Color COLOR_GOLD = Color.FromRgb(255, 215, 0);           // Sword/Achievement
        public static readonly Color COLOR_LIGHT_BLUE = Color.FromRgb(100, 200, 255);   // Light
        public static readonly Color COLOR_GREEN = Color.FromRgb(0, 255, 65);           // Flow
        public static readonly Color COLOR_PINK = Color.FromRgb(255, 100, 150);         // Soul
        public static readonly Color COLOR_BLUE = Color.FromRgb(100, 150, 255);         // Machine

        // Additional blade colors
        public static readonly Color COLOR_WHITE = Colors.White;
        public static readonly Color COLOR_DARK_BLUE = Color.FromRgb(0, 150, 200);

        // Audio frequency values (Hz) for kanji tones
        public const int FREQ_POWER = 440;      // A4 - Power
        public const int FREQ_SWORD = 494;      // B4 - Sword
        public const int FREQ_LIGHT = 523;      // C5 - Light
        public const int FREQ_FLOW = 587;       // D5 - Flow
        public const int FREQ_SOUL = 659;       // E5 - Soul
        public const int FREQ_MACHINE = 740;    // F#5 - Machine

        // Particle system values
        public const int PARTICLES_BURST_DEFAULT = 40;
        public const int PARTICLES_BURST_SECONDARY = 20;
        public const double PARTICLE_SPEED_NORMAL = 150;
        public const double PARTICLE_SPEED_FAST = 200;
        public const double PARTICLE_LIFETIME_SHORT = 0.5;
        public const double PARTICLE_LIFETIME_NORMAL = 0.6;
        public const double PARTICLE_LIFETIME_LONG = 2.0;

        // Ripple effect values
        public const double RIPPLE_MAX_RADIUS = 150;
        public const double RIPPLE_DURATION = 0.5;

        // Glow pulsing values
        public const double GLOW_PULSE_DECAY = 0.98;
        public const double GLOW_BASE_LEVEL = 0.2;

        // Charge level control
        public const double CHARGE_INCREMENT = 0.1;
        public const double CHARGE_MAX = 1.0;

        // Advanced animation timing constants (Phase 8, Stream 2)
        // UI transitions: 300-500ms for smooth professional feel
        public const int ANIMATION_DURATION_FAST = 150;      // Quick feedback
        public const int ANIMATION_DURATION_NORMAL = 300;    // Standard transition
        public const int ANIMATION_DURATION_SLOW = 500;      // Smooth/cinematic
        
        // Boot/splash screen animations
        public const int SPLASH_BLADE_GROW_MS = 800;         // Blade expansion on load
        public const int SPLASH_KANJI_FADE_MS = 600;         // Kanji appear duration
        public const int SPLASH_APP_FADE_MS = 400;           // Application fade-in
        public const int SPLASH_COMPLETION_MS = 300;         // Final blade expansion

        // Login screen wheel animation
        public const int WHEEL_ROTATION_FULL_MS = 8000;      // Full 360° rotation
        public const double WHEEL_ROTATION_SPEED = 0.045;    // Degrees per frame
        public const int WHEEL_HOVER_GLOW_BOOST = 200;       // Glow increase on hover

        // Profile switching animation
        public const int PROFILE_TRANSITION_MS = 400;        // Profile change duration
        public const int PROFILE_ICON_SCALE_MS = 300;        // Icon animation

        // Idle breathing/pulse animation
        public const int IDLE_PULSE_FREQUENCY_MS = 2000;     // Full pulse cycle
        public const double IDLE_PULSE_MIN_GLOW = 0.25;
        public const double IDLE_PULSE_MAX_GLOW = 0.55;

        // Easing function targets
        public const string EASING_EASE_IN_OUT_CUBIC = "EaseInOutCubic";
        public const string EASING_LINEAR = "Linear";
        public const string EASING_EASE_OUT_QUAD = "EaseOutQuad";
        public const string EASING_EASE_IN_CUBIC = "EaseInCubic";
    }

    /// <summary>
    /// Kanji configuration constants with type, character, and associated properties.
    /// </summary>
    public static class KanjiConstants
    {
        public const string KANJI_POWER = "power";
        public const string KANJI_SWORD = "sword";
        public const string KANJI_LIGHT = "light";
        public const string KANJI_FLOW = "flow";
        public const string KANJI_SOUL = "soul";
        public const string KANJI_MACHINE = "machine";

        public const string CHAR_POWER = "力";
        public const string CHAR_SWORD = "刀";
        public const string CHAR_LIGHT = "光";
        public const string CHAR_FLOW = "流";
        public const string CHAR_SOUL = "魂";
        public const string CHAR_MACHINE = "機";

        public const string ICON_POWER = "⚡";
        public const string ICON_SWORD = "⚔";
        public const string ICON_LIGHT = "✨";
        public const string ICON_FLOW = "≈";
        public const string ICON_SOUL = "♡";
        public const string ICON_MACHINE = "⚙";

        public const double BASE_GLOW_POWER = 0.5;
        public const double BASE_GLOW_SWORD = 0.6;
        public const double BASE_GLOW_LIGHT = 0.7;
        public const double BASE_GLOW_FLOW = 0.5;
        public const double BASE_GLOW_SOUL = 0.6;
        public const double BASE_GLOW_MACHINE = 0.8;

        public const double EFFECT_RADIUS_DEFAULT = 100;
        public const double EFFECT_RADIUS_SWORD = 120;
        public const double EFFECT_RADIUS_LIGHT = 110;
        public const double EFFECT_RADIUS_FLOW = 105;
        public const double EFFECT_RADIUS_SOUL = 115;
    }
}

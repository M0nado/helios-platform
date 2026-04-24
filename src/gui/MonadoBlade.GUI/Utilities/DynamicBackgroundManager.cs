using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using MonadoBlade.GUI.Effects;

namespace MonadoBlade.GUI.Utilities
{
    /// <summary>
    /// Dynamic background manager - Creates layered animated backgrounds with kanji overlays.
    /// Features: Particle backgrounds, kanji rotation, color cycling, state-based transitions.
    /// </summary>
    public class DynamicBackgroundManager
    {
        private ParticleSystem _particleSystem;
        private List<FloatingKanji> _kanjiCharacters = new List<FloatingKanji>();
        private double _elapsedTime = 0;
        private BackgroundState _state = BackgroundState.Idle;
        private Color _primaryColor = Color.FromRgb(0, 217, 255);
        private Color _secondaryColor = Color.FromRgb(10, 20, 40);
        private double _intensity = 0.3;
        private Random _random = new Random();

        public Vector2 ScreenSize { get; set; }
        public Color PrimaryColor { get; set; } = Color.FromRgb(0, 217, 255);
        public Color SecondaryColor { get; set; } = Color.FromRgb(10, 20, 40);
        public bool IsActive { get; set; } = true;

        public DynamicBackgroundManager(Vector2 screenSize = default)
        {
            ScreenSize = screenSize;
            if (ScreenSize.X == 0 && ScreenSize.Y == 0)
            {
                ScreenSize = new Vector2(1920, 1080); // Default
            }

            _particleSystem = new ParticleSystem(new Vector2(ScreenSize.X / 2, ScreenSize.Y / 2));
            InitializeKanjiOverlay();
        }

        /// <summary>
        /// Initialize kanji character overlay.
        /// </summary>
        private void InitializeKanjiOverlay()
        {
            string[] kanji = { "力", "刀", "光", "流", "魂", "機" };
            
            for (int i = 0; i < 8; i++)
            {
                var character = new FloatingKanji
                {
                    Character = kanji[i % kanji.Length],
                    X = _random.NextDouble() * ScreenSize.X,
                    Y = _random.NextDouble() * ScreenSize.Y,
                    VelocityX = (_random.NextDouble() - 0.5) * 20,
                    VelocityY = (_random.NextDouble() - 0.5) * 20,
                    Size = 40 + (_random.NextDouble() * 60),
                    RotationSpeed = (_random.NextDouble() - 0.5) * 60,
                    Alpha = 0.1 + (_random.NextDouble() * 0.2),
                    Color = GetRandomKanjiColor()
                };

                _kanjiCharacters.Add(character);
            }
        }

        /// <summary>
        /// Get random kanji color based on type.
        /// </summary>
        private Color GetRandomKanjiColor()
        {
            return (_random.Next(6)) switch
            {
                0 => Color.FromRgb(255, 0, 0),       // Red (Power)
                1 => Color.FromRgb(0, 217, 255),     // Cyan (Tech)
                2 => Color.FromRgb(0, 255, 65),      // Green (Flow)
                3 => Color.FromRgb(255, 215, 0),     // Gold (Achievement)
                4 => Color.FromRgb(255, 0, 255),     // Magenta (Soul)
                5 => Color.FromRgb(100, 150, 255),   // Blue (Machine)
                _ => Color.FromRgb(0, 217, 255)
            };
        }

        /// <summary>
        /// Set background state and trigger associated effects.
        /// </summary>
        public void SetState(BackgroundState state)
        {
            _state = state;
            _intensity = state switch
            {
                BackgroundState.Idle => 0.2,
                BackgroundState.Active => 0.5,
                BackgroundState.Intense => 0.8,
                BackgroundState.Critical => 1.0,
                _ => 0.3
            };
        }

        /// <summary>
        /// Update background animation.
        /// </summary>
        public void Update(double deltaTime)
        {
            if (!IsActive) return;

            _elapsedTime += deltaTime;
            _particleSystem.Update(deltaTime);

            // Update kanji characters
            foreach (var kanji in _kanjiCharacters)
            {
                UpdateKanjiCharacter(kanji, deltaTime);
            }

            // Emit particles based on state
            if (_state == BackgroundState.Active || _state == BackgroundState.Intense)
            {
                EmitBackgroundParticles(deltaTime);
            }
        }

        /// <summary>
        /// Update individual kanji character.
        /// </summary>
        private void UpdateKanjiCharacter(FloatingKanji kanji, double deltaTime)
        {
            // Update position
            kanji.X += kanji.VelocityX * deltaTime;
            kanji.Y += kanji.VelocityY * deltaTime;

            // Wrap around screen
            if (kanji.X < -kanji.Size) kanji.X = ScreenSize.X + kanji.Size;
            if (kanji.X > ScreenSize.X + kanji.Size) kanji.X = -kanji.Size;
            if (kanji.Y < -kanji.Size) kanji.Y = ScreenSize.Y + kanji.Size;
            if (kanji.Y > ScreenSize.Y + kanji.Size) kanji.Y = -kanji.Size;

            // Update rotation
            kanji.Rotation += kanji.RotationSpeed * deltaTime;
            kanji.Rotation %= 360;

            // Pulsing alpha
            kanji.Alpha = (0.1 + 0.15) + Math.Sin(_elapsedTime * 2 + kanji.X) * 0.1;
        }

        /// <summary>
        /// Emit background particle effects.
        /// </summary>
        private void EmitBackgroundParticles(double deltaTime)
        {
            int particleCount = (int)(20 * _intensity * deltaTime);
            
            for (int i = 0; i < particleCount; i++)
            {
                Vector2 emitPos = new Vector2(
                    _random.NextDouble() * ScreenSize.X,
                    _random.NextDouble() * ScreenSize.Y
                );

                Vector2 velocity = new Vector2(
                    (_random.NextDouble() - 0.5) * 100,
                    (_random.NextDouble() - 0.5) * 100
                );

                Color particleColor = ParticleSystem.InterpolateColor(
                    _secondaryColor,
                    _primaryColor,
                    _random.NextDouble()
                );

                _particleSystem.Emit(
                    1,
                    emitPos,
                    velocity,
                    particleColor,
                    1.0 + (_random.NextDouble() * 2),
                    0.5 + (_random.NextDouble() * 1),
                    ParticleType.Glow
                );
            }
        }

        /// <summary>
        /// Create a lightning background effect.
        /// </summary>
        public void PlayLightningEffect()
        {
            // Create multiple lightning effects across background
            for (int i = 0; i < 3; i++)
            {
                Vector2 start = new Vector2(
                    _random.NextDouble() * ScreenSize.X,
                    _random.NextDouble() * ScreenSize.Y
                );

                Vector2 end = new Vector2(
                    _random.NextDouble() * ScreenSize.X,
                    _random.NextDouble() * ScreenSize.Y
                );

                // Emit burst of particles at both points
                _particleSystem.EmitBurst(
                    30,
                    start,
                    100,
                    Color.FromRgb(100, 150, 255),
                    1.5,
                    0.4,
                    ParticleType.Lightning
                );
            }
        }

        /// <summary>
        /// Create a laser effect across background.
        /// </summary>
        public void PlayLaserEffect()
        {
            Vector2 origin = new Vector2(
                _random.NextDouble() * ScreenSize.X,
                _random.NextDouble() * ScreenSize.Y
            );

            Vector2 direction = new Vector2(
                (_random.NextDouble() - 0.5) * 2,
                (_random.NextDouble() - 0.5) * 2
            ).Normalized;

            _particleSystem.EmitSpray(
                50,
                origin,
                Math.Atan2(direction.Y, direction.X),
                Math.PI / 8,
                300,
                Color.FromRgb(0, 217, 255),
                2.0,
                0.6,
                ParticleType.Laser
            );
        }

        /// <summary>
        /// Create a kanji appearance effect.
        /// </summary>
        public void PlayKanjiEffect(KanjiType type)
        {
            Vector2 centerPos = new Vector2(ScreenSize.X / 2, ScreenSize.Y / 2);

            // Emit particles around center
            _particleSystem.EmitBurst(
                80,
                centerPos,
                200,
                GetKanjiTypeColor(type),
                2.5,
                0.7,
                ParticleType.Kanji
            );
        }

        /// <summary>
        /// Get color for kanji type.
        /// </summary>
        private Color GetKanjiTypeColor(KanjiType type)
        {
            return type switch
            {
                KanjiType.Power => Color.FromRgb(255, 0, 0),
                KanjiType.Blade => Color.FromRgb(0, 217, 255),
                KanjiType.Light => Color.FromRgb(255, 215, 0),
                KanjiType.Flow => Color.FromRgb(0, 255, 65),
                KanjiType.Soul => Color.FromRgb(255, 0, 255),
                KanjiType.Machine => Color.FromRgb(100, 150, 255),
                _ => Color.FromRgb(0, 217, 255)
            };
        }

        /// <summary>
        /// Get kanji characters for rendering.
        /// </summary>
        public IEnumerable<FloatingKanji> GetKanjiCharacters()
        {
            return _kanjiCharacters;
        }

        /// <summary>
        /// Get particles for rendering.
        /// </summary>
        public IEnumerable<Particle> GetParticles()
        {
            return _particleSystem.GetActiveParticles();
        }

        /// <summary>
        /// Get current background color gradient.
        /// </summary>
        public (Color from, Color to) GetBackgroundGradient()
        {
            return (_secondaryColor, PrimaryColor);
        }

        /// <summary>
        /// Clear all effects.
        /// </summary>
        public void Clear()
        {
            _particleSystem.Clear();
            _kanjiCharacters.Clear();
        }
    }

    /// <summary>
    /// Background states that trigger different effects.
    /// </summary>
    public enum BackgroundState
    {
        Idle,       // Subtle, minimal effect
        Active,     // Normal activity
        Intense,    // Heavy effect activity
        Critical    // Maximum intensity
    }

    /// <summary>
    /// Floating kanji character data.
    /// </summary>
    public class FloatingKanji
    {
        public string Character { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Size { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; set; }
        public double Alpha { get; set; }
        public Color Color { get; set; }
    }

    public enum KanjiType
    {
        Power,
        Blade,
        Light,
        Flow,
        Soul,
        Machine
    }
}

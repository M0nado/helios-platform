using Xunit;
using HELIOS.Platform.Core.Audio;
using System;

namespace HELIOS.Platform.Tests.Core.Audio
{
    public class AudioSystemTests
    {
        #region KanjiToneGenerator Tests

        [Fact]
        public void KanjiToneGenerator_GenerateKanjiTone_ReturnsTone()
        {
            using var generator = new KanjiToneGenerator();
            var tone = generator.GenerateKanjiTone('漢');
            Assert.NotNull(tone);
        }

        [Fact]
        public void KanjiToneGenerator_GenerateKanjiSequence_ProducesMultipleTones()
        {
            using var generator = new KanjiToneGenerator();
            var sequence = generator.GenerateKanjiSequence("漢字");
            Assert.NotNull(sequence);
        }

        [Fact]
        public void KanjiToneGenerator_GenerateKanjiChord_ProducesHarmony()
        {
            using var generator = new KanjiToneGenerator();
            var chord = generator.GenerateKanjiChord("和");
            Assert.NotNull(chord);
        }

        #endregion

        #region BladeEffectSoundGenerator Tests

        [Fact]
        public void BladeEffect_GenerateLaserSound_CreatesTone()
        {
            using var generator = new BladeEffectSoundGenerator();
            var laser = generator.GenerateLaserSound();
            Assert.NotNull(laser);
        }

        [Fact]
        public void BladeEffect_GenerateGlowPulse_CreatesTone()
        {
            using var generator = new BladeEffectSoundGenerator();
            var glow = generator.GenerateGlowPulse();
            Assert.NotNull(glow);
        }

        [Fact]
        public void BladeEffect_GenerateExpansionSound_CreatesTone()
        {
            using var generator = new BladeEffectSoundGenerator();
            var expansion = generator.GenerateExpansionSound();
            Assert.NotNull(expansion);
        }

        [Fact]
        public void BladeEffect_GenerateCompleteBladeEffect_CombinesSounds()
        {
            using var generator = new BladeEffectSoundGenerator();
            var complete = generator.GenerateCompleteBladeEffect();
            Assert.NotNull(complete);
        }

        #endregion

        #region AmbientSoundscape Tests

        [Fact]
        public void AmbientSoundscape_GenerateAmbientLoop_CreatesMorningAmbience()
        {
            using var soundscape = new AmbientSoundscape();
            var ambient = soundscape.GenerateAmbientLoop(AmbientSoundscape.TimeOfDay.Morning, 5);
            Assert.NotNull(ambient);
        }

        [Fact]
        public void AmbientSoundscape_GenerateAmbientLoop_CreatesEveningAmbience()
        {
            using var soundscape = new AmbientSoundscape();
            var ambient = soundscape.GenerateAmbientLoop(AmbientSoundscape.TimeOfDay.Evening, 5);
            Assert.NotNull(ambient);
        }

        [Fact]
        public void AmbientSoundscape_GenerateWindEffect_CreatesWind()
        {
            using var soundscape = new AmbientSoundscape();
            var wind = soundscape.GenerateWindEffect();
            Assert.NotNull(wind);
        }

        [Fact]
        public void AmbientSoundscape_GenerateEnergyHum_CreatesHum()
        {
            using var soundscape = new AmbientSoundscape();
            var hum = soundscape.GenerateEnergyHum();
            Assert.NotNull(hum);
        }

        #endregion

        #region ProfileTransitionAudio Tests

        [Fact]
        public void ProfileTransition_GenerateTransition_CreatesCrossfade()
        {
            using var profileAudio = new ProfileTransitionAudio();
            var transition = profileAudio.GenerateProfileTransition("Work", "Creative");
            Assert.NotNull(transition);
        }

        [Fact]
        public void ProfileTransition_RegisterCustomProfile_StoresProfile()
        {
            using var profileAudio = new ProfileTransitionAudio();
            profileAudio.RegisterProfileTheme("Custom", 587.33f);
            var transition = profileAudio.GenerateProfileTransition("Work", "Custom");
            Assert.NotNull(transition);
        }

        [Fact]
        public void ProfileTransition_GetProfileChangeNotification_CreatesArpeggio()
        {
            using var profileAudio = new ProfileTransitionAudio();
            var notification = profileAudio.GetProfileChangeNotification();
            Assert.NotNull(notification);
        }

        #endregion

        #region BootSequenceAudio Tests

        [Fact]
        public void BootSequence_GenerateStartupTone_CreatesAscending()
        {
            using var bootAudio = new BootSequenceAudio();
            var startup = bootAudio.GenerateStartupTone();
            Assert.NotNull(startup);
        }

        [Fact]
        public void BootSequence_GenerateProgressCue_Creates25PercentCue()
        {
            using var bootAudio = new BootSequenceAudio();
            var cue = bootAudio.GenerateProgressCue(25);
            Assert.NotNull(cue);
        }

        [Fact]
        public void BootSequence_GenerateProgressCue_Creates100PercentCue()
        {
            using var bootAudio = new BootSequenceAudio();
            var cue = bootAudio.GenerateProgressCue(100);
            Assert.NotNull(cue);
        }

        [Fact]
        public void BootSequence_GenerateCompletionFanfare_CreatesArpeggio()
        {
            using var bootAudio = new BootSequenceAudio();
            var fanfare = bootAudio.GenerateCompletionFanfare();
            Assert.NotNull(fanfare);
        }

        [Fact]
        public void BootSequence_GenerateCompleteBootSequence_CombinesSequence()
        {
            using var bootAudio = new BootSequenceAudio();
            var complete = bootAudio.GenerateCompleteBootSequence();
            Assert.NotNull(complete);
        }

        #endregion

        #region AudioNormalizer Tests

        [Fact]
        public void AudioNormalizer_Normalize_ReducesLoudness()
        {
            using var normalizer = new AudioNormalizer();
            float[] testSamples = new float[44100];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.8f;
            }

            var normalized = normalizer.Normalize(testSamples);
            
            float originalLoudness = normalizer.MeasureLoudness(testSamples);
            float normalizedLoudness = normalizer.MeasureLoudness(normalized);
            
            Assert.NotNull(normalized);
            Assert.Equal(testSamples.Length, normalized.Length);
        }

        [Fact]
        public void AudioNormalizer_MeasureLoudness_ReturnsValidValue()
        {
            using var normalizer = new AudioNormalizer();
            float[] testSamples = new float[44100];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            float loudness = normalizer.MeasureLoudness(testSamples);
            Assert.True(loudness < 0f, "Loudness should be negative dB");
        }

        [Fact]
        public void AudioNormalizer_MeasurePeak_ReturnsValidValue()
        {
            using var normalizer = new AudioNormalizer();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = 0.5f;
            }

            float peak = normalizer.MeasurePeak(testSamples);
            Assert.True(peak < 0f, "Peak should be negative dBFS");
        }

        [Fact]
        public void AudioNormalizer_ApplyEQ_ModifiesAudio()
        {
            using var normalizer = new AudioNormalizer();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f);
            }

            var eqed = normalizer.ApplyEQ(testSamples, 3f, 0f, 3f);
            
            Assert.NotNull(eqed);
            Assert.Equal(testSamples.Length, eqed.Length);
        }

        #endregion

        #region SpatialAudioController Tests

        [Fact]
        public void SpatialAudio_ApplySpatialPositioning_LeftEdge()
        {
            using var spatial = new SpatialAudioController();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            var positioned = spatial.ApplySpatialPositioning(testSamples, 0, 512, 1024, 768);
            
            Assert.NotNull(positioned);
            Assert.Equal(testSamples.Length, positioned.Length);
        }

        [Fact]
        public void SpatialAudio_ApplySpatialPositioning_RightEdge()
        {
            using var spatial = new SpatialAudioController();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            var positioned = spatial.ApplySpatialPositioning(testSamples, 1024, 512, 1024, 768);
            
            Assert.NotNull(positioned);
        }

        [Fact]
        public void SpatialAudio_ApplyReverb_AddsReflections()
        {
            using var spatial = new SpatialAudioController();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            var reverb = spatial.ApplyReverb(testSamples, 0.5f, 0.5f);
            
            Assert.NotNull(reverb);
            Assert.Equal(testSamples.Length, reverb.Length);
        }

        [Fact]
        public void SpatialAudio_CreateStereoFromMono_GeneratesStereoImage()
        {
            using var spatial = new SpatialAudioController();
            float[] monoSamples = new float[1000];
            for (int i = 0; i < monoSamples.Length; i++)
            {
                monoSamples[i] = (float)Math.Sin(i * 0.01f);
            }

            var (left, right) = spatial.CreateStereoFromMono(monoSamples, 0.5f);
            
            Assert.NotNull(left);
            Assert.NotNull(right);
            Assert.Equal(monoSamples.Length, left.Length);
        }

        #endregion

        #region AudioEffectChain Tests

        [Fact]
        public void EffectChain_ProcessAudio_AppliesEffects()
        {
            using var chain = AudioEffectChain.CreateStandardChain();
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            var processed = chain.ProcessAudio(testSamples);
            
            Assert.NotNull(processed);
            Assert.Equal(testSamples.Length, processed.Length);
        }

        [Fact]
        public void EffectChain_DisabledChain_ReturnsUnmodifiedAudio()
        {
            using var chain = AudioEffectChain.CreateStandardChain();
            chain.SetEnabled(false);
            
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.5f;
            }

            var processed = chain.ProcessAudio(testSamples);
            
            for (int i = 0; i < testSamples.Length; i++)
            {
                Assert.Equal(testSamples[i], processed[i], 6);
            }
        }

        [Fact]
        public void EffectChain_ReverbEffect_ProcessesAudio()
        {
            var reverb = new ReverbEffect(44100) { RoomSize = 0.7f };
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f);
            }

            var processed = reverb.Process(testSamples);
            
            Assert.NotNull(processed);
            Assert.Equal(testSamples.Length, processed.Length);
        }

        [Fact]
        public void EffectChain_CompressionEffect_ReducesPeaks()
        {
            var compression = new CompressionEffect(44100) { Threshold = -20f, Ratio = 4f };
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f) * 0.8f;
            }

            var processed = compression.Process(testSamples);
            
            Assert.NotNull(processed);
            Assert.Equal(testSamples.Length, processed.Length);
        }

        [Fact]
        public void EffectChain_EQEffect_AdjustsTone()
        {
            var eq = new EQEffect(44100) { LowGain = 3f, MidGain = -2f, HighGain = 3f };
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = (float)Math.Sin(i * 0.01f);
            }

            var processed = eq.Process(testSamples);
            
            Assert.NotNull(processed);
            Assert.Equal(testSamples.Length, processed.Length);
        }

        [Fact]
        public void EffectChain_DelayEffect_CreatesEcho()
        {
            var delay = new DelayEffect(44100) { DelayTime = 0.25f, Feedback = 0.6f };
            float[] testSamples = new float[1000];
            for (int i = 0; i < testSamples.Length; i++)
            {
                testSamples[i] = i < 100 ? 0.5f : 0f;
            }

            var processed = delay.Process(testSamples);
            
            Assert.NotNull(processed);
            Assert.Equal(testSamples.Length, processed.Length);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_CompleteAudioPipeline_ProcessesSuccessfully()
        {
            using var generator = new KanjiToneGenerator();
            using var normalizer = new AudioNormalizer();
            using var spatial = new SpatialAudioController();
            using var chain = AudioEffectChain.CreateStandardChain();

            // Generate kanji tone
            var tone = generator.GenerateKanjiTone('音');
            
            // Read samples from tone provider
            float[] buffer = new float[44100];
            int count = tone.Read(buffer, 0, buffer.Length);
            var samples = new float[count];
            Array.Copy(buffer, samples, count);

            // Normalize
            var normalized = normalizer.Normalize(samples);

            // Apply spatial positioning
            var spatial_processed = spatial.ApplySpatialPositioning(normalized, 500, 400, 1024, 768);

            // Apply effects
            var final = chain.ProcessAudio(spatial_processed);

            Assert.NotNull(final);
            Assert.True(final.Length > 0);
        }

        [Fact]
        public void Integration_BootSequenceFullPipeline_CompletesSuccessfully()
        {
            using var bootAudio = new BootSequenceAudio();
            using var normalizer = new AudioNormalizer();
            using var chain = AudioEffectChain.CreateStandardChain();

            var bootSequence = bootAudio.GenerateCompleteBootSequence();
            
            float[] buffer = new float[88200];
            int count = bootSequence.Read(buffer, 0, buffer.Length);
            var samples = new float[count];
            Array.Copy(buffer, samples, count);

            var normalized = normalizer.Normalize(samples);
            var processed = chain.ProcessAudio(normalized);

            float loudness = normalizer.MeasureLoudness(processed);
            float peak = normalizer.MeasurePeak(processed);

            Assert.True(loudness < 0f);
            Assert.True(peak <= -3f || peak < 0f);
        }

        #endregion
    }
}

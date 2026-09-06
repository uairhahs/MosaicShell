using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using MosaicShell.Core.Services;
using NAudio.Wave;

namespace MosaicShell.Core.Tests
{
    public class AudioLevelMeterTests
    {
        [Fact]
        public void BandCount_is_sixteen()
        {
            _ = AudioLevelMeter.BandCount.Should().Be(16);
        }

        [Fact]
        public void Silent_float_buffer_yields_zero_peak_and_bands()
        {
            byte[] silence = new byte[64]; // 16 float zeros
            Span<float> bands = stackalloc float[AudioLevelMeter.BandCount];
            for (int i = 0; i < bands.Length; i++)
            {
                bands[i] = 0.5f; // should be overwritten
            }

            AudioLevelMeter.ProcessIeeeFloat(silence, bands, out double peak);

            _ = peak.Should().Be(0);
            _ = bands.ToArray().Should().OnlyContain(b => b == 0);
        }

        [Fact]
        public void Loud_samples_clamp_peak_and_bands_to_one()
        {
            float[] samples = new float[32];
            Array.Fill(samples, 1f);
            byte[] buffer = MemoryMarshal.AsBytes(samples.AsSpan()).ToArray();
            Span<float> bands = stackalloc float[AudioLevelMeter.BandCount];

            AudioLevelMeter.ProcessIeeeFloat(buffer, bands, out double peak);

            _ = peak.Should().Be(1);
            foreach (float b in bands)
            {
                _ = b.Should().BeInRange(0, 1);
            }

            _ = bands.ToArray().Should().Contain(1f);
        }

        [Fact]
        public void Short_buffer_leaves_peak_zero()
        {
            Span<float> bands = stackalloc float[AudioLevelMeter.BandCount];
            AudioLevelMeter.ProcessIeeeFloat(stackalloc byte[2], bands, out double peak);
            _ = peak.Should().Be(0);
        }

        [Fact]
        public void WindowsAudioLevelService_capture_field_is_WasapiRecorder()
        {
            FieldInfo field = typeof(WindowsAudioLevelService)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(f => f.Name == "_capture");

            _ = field.FieldType.Should().Be(typeof(WasapiRecorder),
                "NAudio 3 deprecates WasapiLoopbackCapture; Pulse metering must use WasapiRecorder");
            _ = field.FieldType.Name.Should().NotBe("WasapiLoopbackCapture");
        }
    }
}

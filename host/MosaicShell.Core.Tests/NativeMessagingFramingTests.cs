using FluentAssertions;
using MosaicShell.Core.Services.BrowserBridge;

namespace MosaicShell.Core.Tests
{
    /// <summary>
    /// Native messaging frames are a 4 byte little-endian length followed by that many bytes of UTF-8 JSON
    /// (measured with Edge 153, plan N0.2). The same framing carries messages between the relay and the Host.
    /// </summary>
    public class NativeMessagingFramingTests
    {
        private static MemoryStream Stream(params byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        private static byte[] Prefix(uint length)
        {
            return BitConverter.GetBytes(length);
        }

        [Fact]
        public void A_frame_is_a_little_endian_length_then_the_payload()
        {
            byte[] frame = NativeMessagingFraming.Encode("{}"u8);

            _ = frame.Should().Equal(0x02, 0x00, 0x00, 0x00, 0x7B, 0x7D);
        }

        [Fact]
        public void The_length_counts_bytes_not_characters()
        {
            byte[] payload = System.Text.Encoding.UTF8.GetBytes("éé");

            byte[] frame = NativeMessagingFraming.Encode(payload);

            _ = frame[..4].Should().Equal(0x04, 0x00, 0x00, 0x00);
        }

        [Fact]
        public void An_empty_payload_is_a_zero_length_frame()
        {
            _ = NativeMessagingFraming.Encode([]).Should().Equal(0x00, 0x00, 0x00, 0x00);
        }

        [Fact]
        public void A_payload_over_the_limit_cannot_be_encoded()
        {
            byte[] payload = new byte[NativeMessagingFraming.MaxToBrowserBytes + 1];

            Action act = () => NativeMessagingFraming.Encode(payload);

            _ = act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void A_payload_at_the_limit_can_be_encoded()
        {
            byte[] payload = new byte[NativeMessagingFraming.MaxToBrowserBytes];

            _ = NativeMessagingFraming.Encode(payload).Should().HaveCount(NativeMessagingFraming.MaxToBrowserBytes + 4);
        }

        [Fact]
        public async Task A_frame_reads_back_as_its_payload()
        {
            using MemoryStream stream = Stream(NativeMessagingFraming.Encode("hello"u8));

            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = frame.Should().Equal("hello"u8.ToArray());
        }

        [Fact]
        public async Task Back_to_back_frames_are_read_one_at_a_time_and_then_the_end_is_reported()
        {
            using MemoryStream stream = Stream([.. NativeMessagingFraming.Encode("one"u8), .. NativeMessagingFraming.Encode("two"u8)]);

            byte[]? first = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);
            byte[]? second = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);
            byte[]? end = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = first.Should().Equal("one"u8.ToArray());
            _ = second.Should().Equal("two"u8.ToArray());
            _ = end.Should().BeNull("the stream ended cleanly between frames");
        }

        [Fact]
        public async Task A_stream_that_hands_back_one_byte_at_a_time_still_yields_whole_frames()
        {
            using OneByteAtATimeStream stream = new(NativeMessagingFraming.Encode("partial reads"u8));

            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = frame.Should().Equal("partial reads"u8.ToArray());
        }

        [Fact]
        public async Task A_zero_length_frame_is_an_empty_payload_not_the_end()
        {
            using MemoryStream stream = Stream([.. NativeMessagingFraming.Encode([]), .. NativeMessagingFraming.Encode("x"u8)]);

            byte[]? empty = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);
            byte[]? next = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = empty.Should().NotBeNull().And.BeEmpty();
            _ = next.Should().Equal("x"u8.ToArray());
        }

        [Fact]
        public async Task An_empty_stream_is_a_clean_end()
        {
            using MemoryStream stream = Stream();

            _ = (await NativeMessagingFraming.ReadFrameAsync(stream, 1024)).Should().BeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task A_stream_that_ends_inside_the_length_is_corrupt(int prefixBytes)
        {
            using MemoryStream stream = Stream(Prefix(5)[..prefixBytes]);

            Func<Task> act = async () => await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = await act.Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task A_stream_that_ends_inside_the_payload_is_corrupt()
        {
            using MemoryStream stream = Stream([.. Prefix(10), .. "short"u8.ToArray()]);

            Func<Task> act = async () => await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = await act.Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task A_length_over_the_limit_is_refused_before_any_payload_is_read()
        {
            using MemoryStream stream = Stream([.. Prefix(1025), .. new byte[1025]]);

            Func<Task> act = async () => await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = await act.Should().ThrowAsync<InvalidDataException>();
            _ = stream.Position.Should().Be(4, "an attacker's length must not make the reader allocate or consume the payload");
        }

        [Fact]
        public async Task A_length_at_the_limit_is_accepted()
        {
            using MemoryStream stream = Stream([.. Prefix(1024), .. new byte[1024]]);

            byte[]? frame = await NativeMessagingFraming.ReadFrameAsync(stream, 1024);

            _ = frame.Should().HaveCount(1024);
        }

        [Theory]
        [InlineData(0x80000000u)]
        [InlineData(0xFFFFFFFFu)]
        public async Task A_length_that_does_not_fit_a_signed_int_is_refused(uint length)
        {
            using MemoryStream stream = Stream(Prefix(length));

            Func<Task> act = async () => await NativeMessagingFraming.ReadFrameAsync(stream, int.MaxValue);

            _ = await act.Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task Reading_stops_when_cancelled()
        {
            using CancellationTokenSource cts = new();
            await cts.CancelAsync();
            using MemoryStream stream = Stream(NativeMessagingFraming.Encode("x"u8));

            Func<Task> act = async () => await NativeMessagingFraming.ReadFrameAsync(stream, 1024, cts.Token);

            _ = await act.Should().ThrowAsync<OperationCanceledException>();
        }

        private sealed class OneByteAtATimeStream(byte[] data) : MemoryStream(data)
        {
            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, 1));
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return base.ReadAsync(buffer[..Math.Min(buffer.Length, 1)], cancellationToken);
            }
        }
    }
}

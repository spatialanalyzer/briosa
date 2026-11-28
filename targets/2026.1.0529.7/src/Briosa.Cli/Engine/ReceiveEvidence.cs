using System.Buffers.Binary;
using System.Net;
using System.Net.Http;

namespace Briosa.Cli.Engine;

/// <summary>
/// Whether a response on this connection declared a gRPC message larger than the
/// CLI's receive limit. The gRPC client reports both its own refusal of an oversized
/// message and a server's RESOURCE_EXHAUSTED status as the same status code; only
/// this observation of the received length prefix tells them apart.
/// </summary>
internal sealed class ReceiveEvidence
{
    private int _exceeded;

    public bool LimitExceeded => Volatile.Read(ref _exceeded) != 0;

    internal void Exceeded() => Interlocked.Exchange(ref _exceeded, 1);
}

/// <summary>
/// Lets every response body pass through unchanged while it reads the length prefix
/// of the first gRPC message in it. It never buffers, rewrites, or retries anything.
/// </summary>
internal sealed class ReceiveEvidenceHandler(ReceiveEvidence evidence, int limit, HttpMessageHandler inner)
    : DelegatingHandler(inner)
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The response owns its content and disposes the observed content, which disposes the original.")]
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.Content = new ObservedContent(response.Content, evidence, limit);
        return response;
    }

    private sealed class ObservedContent : HttpContent
    {
        private readonly HttpContent _inner;
        private readonly ReceiveEvidence _evidence;
        private readonly int _limit;

        public ObservedContent(HttpContent inner, ReceiveEvidence evidence, int limit)
        {
            _inner = inner;
            _evidence = evidence;
            _limit = limit;
            foreach (var header in inner.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var source = await CreateContentReadStreamAsync().ConfigureAwait(false);
            await using (source.ConfigureAwait(false))
            {
                await source.CopyToAsync(stream).ConfigureAwait(false);
            }
        }

        protected override async Task<Stream> CreateContentReadStreamAsync() =>
            new PrefixObservingStream(await _inner.ReadAsStreamAsync().ConfigureAwait(false), _evidence, _limit);

        protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
            new PrefixObservingStream(await _inner.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                _evidence, _limit);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    // A gRPC message starts with a compressed flag and a four-byte big-endian length.
    private sealed class PrefixObservingStream(Stream inner, ReceiveEvidence evidence, int limit) : Stream
    {
        private const int PrefixLength = 5;
        private readonly byte[] _prefix = new byte[PrefixLength];
        private int _observed;

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            var read = inner.Read(buffer);
            Observe(buffer[..read]);
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            Observe(buffer.Span[..read]);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }

        private void Observe(ReadOnlySpan<byte> received)
        {
            if (_observed >= PrefixLength || received.IsEmpty)
            {
                return;
            }

            var take = Math.Min(PrefixLength - _observed, received.Length);
            received[..take].CopyTo(_prefix.AsSpan(_observed));
            _observed += take;
            if (_observed == PrefixLength && BinaryPrimitives.ReadUInt32BigEndian(_prefix.AsSpan(1)) > (uint)limit)
            {
                evidence.Exceeded();
            }
        }
    }
}

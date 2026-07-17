using System.Buffers;

namespace Helios.Connect.Api;

internal readonly record struct BoundedBodyReadResult(byte[] Bytes, bool IsTooLarge);

internal static class BoundedRequestBody
{
    private const int MaximumBufferSize = 81_920;

    internal static bool Prepare(HttpContext context, int maximumBytes)
    {
        if (context.Request.ContentLength is > 0 && context.Request.ContentLength > maximumBytes)
            return false;

        // The extra byte lets ReadAsync detect an over-limit chunked body and
        // return a deterministic 413 without buffering the full request.
        var maxBodyFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        var sentinelLimit = maximumBytes + 1L;
        if (maxBodyFeature is { IsReadOnly: false } &&
            (maxBodyFeature.MaxRequestBodySize is null || maxBodyFeature.MaxRequestBodySize > sentinelLimit))
            maxBodyFeature.MaxRequestBodySize = sentinelLimit;
        return true;
    }

    internal static async Task<BoundedBodyReadResult> ReadAsync(
        Stream input,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (maximumBytes < 1) throw new ArgumentOutOfRangeException(nameof(maximumBytes));

        var bufferSize = Math.Min(MaximumBufferSize, maximumBytes + 1);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            using var output = new MemoryStream(Math.Min(maximumBytes, MaximumBufferSize));
            var remainingWithSentinel = maximumBytes + 1;
            while (remainingWithSentinel > 0)
            {
                var bytesRead = await input.ReadAsync(
                    buffer.AsMemory(0, Math.Min(buffer.Length, remainingWithSentinel)),
                    cancellationToken);
                if (bytesRead == 0) break;

                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                remainingWithSentinel -= bytesRead;
            }

            return output.Length > maximumBytes
                ? new BoundedBodyReadResult(Array.Empty<byte>(), true)
                : new BoundedBodyReadResult(output.ToArray(), false);
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException exception)
            when (exception.StatusCode == Microsoft.AspNetCore.Http.StatusCodes.Status413PayloadTooLarge)
        {
            return new BoundedBodyReadResult(Array.Empty<byte>(), true);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}

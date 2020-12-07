using System;

namespace AsyncServices.Common.Services
{
    public interface IDecoder
    {
        object Decode(EncodedData data, Type type);
        T Decode<T>(ReadOnlyMemory<byte> data);
    }
}
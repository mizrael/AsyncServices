using System;
using System.Text.Json;

namespace AsyncServices.Common.Services
{
    public class JsonEncoder : IEncoder, IDecoder
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public JsonEncoder(JsonSerializerOptions serializerOptions = null) {
            _serializerOptions = serializerOptions ?? new JsonSerializerOptions();
        }

        public EncodedData Encode<T>(T data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var encoded = JsonSerializer.SerializeToUtf8Bytes<T>(data, _serializerOptions);
            return new EncodedData(encoded);
        }

        public object Decode(EncodedData data, Type type) => JsonSerializer.Deserialize(data.Value, type, _serializerOptions);

        public T Decode<T>(ReadOnlyMemory<byte> data) => JsonSerializer.Deserialize<T>(data.Span, _serializerOptions);
    }
}

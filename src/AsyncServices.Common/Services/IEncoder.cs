namespace AsyncServices.Common.Services
{
    public interface IEncoder
    {
        EncodedData Encode<T>(T data);       
    }
}
namespace NETStandard.RestServer
{
    public interface IMimeTypeProvider
    {
        string GetMimeType(string extension);
    }
}
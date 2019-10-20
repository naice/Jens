namespace Jens.RestServer
{
    public interface IMimeTypeProvider
    {
        string GetMimeType(string extension);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    public class RestServerServiceFileRouteHandler : IRestServerRouteHandler
    {
        private readonly string _basePath;
        private readonly IFileSystem _fileSystem;

        public RestServerServiceFileRouteHandler(string basePath) : this(basePath, null) { }
        public RestServerServiceFileRouteHandler(string basePath, IFileSystem fileSystem)
        {
            _basePath = basePath;
            _fileSystem = fileSystem ?? new DefaultFileSystem(new DefaultMimeTypeProvider());
        }

        public async Task<bool> HandleRouteAsync(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "GET")
            {
                return false;
            }

            var localFilePath = GetFilePath(context.Request.Url);
            var absoluteFilePath = GetAbsoluteFilePath(localFilePath);

            IFile item;
            try
            {
                if (!_fileSystem.Exists(absoluteFilePath))
                    throw new FileNotFoundException();
                item = await _fileSystem.GetFileFromPathAsync(absoluteFilePath);
            }
            catch (FileNotFoundException)
            {
                context.Response.NotFound();
                return true;
            }


            context.Response.StatusCode = 200;
            context.Response.Headers["Content-Type"] = item.ContentType;

            var stream = await item.OpenStreamForReadAsync();
            stream.CopyTo(context.Response.OutputStream);
            return true;
        }

        private static string GetFilePath(Uri uri)
        {
            var localPath = GetLocalPath(uri);

            localPath = ParseLocalPath(localPath);
            var filePath = localPath.Replace('/', '\\');

            return filePath;
        }

        private string GetAbsoluteFilePath(string localFilePath)
        {
            var absoluteFilePath = Path.Combine(_basePath, localFilePath);
            return absoluteFilePath;
        }

        private static string GetLocalPath(Uri uri)
        {
            return uri.LocalPath.Split('?')[0];
        }

        private static string ParseLocalPath(string localPath)
        {
            if (localPath.EndsWith("/"))
            {
                localPath += "index.html";
            }

            if (localPath.StartsWith("/"))
            {
                localPath = localPath.Substring(1);
            }

            return localPath;
        }
    }
}

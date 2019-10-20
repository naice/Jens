using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    class DefaultFileSystem : IFileSystem
    {
        private readonly IMimeTypeProvider _mimeTypeProvider;

        public DefaultFileSystem(IMimeTypeProvider mimeTypeProvider)
        {
            _mimeTypeProvider = mimeTypeProvider ?? throw new ArgumentNullException(nameof(mimeTypeProvider));
        }

        public bool Exists(string absoluteBasePathUri)
        {
            return File.Exists(absoluteBasePathUri);
        }

        public Task<IFile> GetFileFromPathAsync(string path)
        {
            return Task.Run(() => GetFileFromPath(path));
        }

        private IFile GetFileFromPath(string path)
        {
            var extension = Path.GetExtension(path);
            var mimeType = _mimeTypeProvider.GetMimeType(extension);
            return new DefaultFile(path, extension, contentType: mimeType);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NETStandard.RestServer
{
    public class DefaultFile : IFile
    {
        private readonly string _filePath;
        private readonly string _fileExtension;
        private readonly string _fileContentType;

        public string ContentType => _fileContentType;
        public string Extension => _fileExtension;

        public DefaultFile(string filePath, string extension, string contentType)
        {
            _filePath = filePath;
            _fileExtension = extension;
            _fileContentType = contentType;
        }

        public Task<Stream> OpenStreamForReadAsync()
        {
            return Task.Run(()=>(Stream)File.OpenRead(_filePath));
        }
    }
}

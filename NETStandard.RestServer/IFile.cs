using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NETStandard.RestServer
{
    public interface IFile
    {
        string ContentType { get; }
        string Extension { get; }
        Task<Stream> OpenStreamForReadAsync();
    }
}

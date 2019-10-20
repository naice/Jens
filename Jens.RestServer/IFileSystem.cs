using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jens.RestServer
{
    public interface IFileSystem
    {
        bool Exists(string absoluteBasePathUri);
        Task<IFile> GetFileFromPathAsync(string path);
    }
}

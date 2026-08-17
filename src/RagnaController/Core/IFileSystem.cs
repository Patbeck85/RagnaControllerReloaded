using System.Collections.Generic;

namespace RagnaController.Core
{
    public interface IFileSystem
    {
        bool FileExists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        void CreateDirectory(string path);
        IEnumerable<string> GetFiles(string directory, string pattern);
    }

    public sealed class RealFileSystem : IFileSystem
    {
        public bool FileExists(string path) => System.IO.File.Exists(path);
        public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
        public void WriteAllText(string path, string contents) => System.IO.File.WriteAllText(path, contents);
        public void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(path);
        public IEnumerable<string> GetFiles(string directory, string pattern) => System.IO.Directory.GetFiles(directory, pattern);
    }
}
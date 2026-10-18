using System.Collections.Generic;

namespace RagnaController.Core
{
    /// <summary>
    /// Contract interface for core subsystem.
    /// </summary>
    public interface IFileSystem
    {
        bool FileExists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        void CreateDirectory(string path);
        IEnumerable<string> GetFiles(string directory, string pattern);
    }

    /// <summary>
    /// Contract interface for core subsystem.
    /// </summary>
    public sealed class RealFileSystem : IFileSystem
    {
        /// <summary>
        /// FileExists method.
        /// <param name="path">path parameter.</param>
        /// <returns>Result of FileExists.</returns>
        /// </summary>
        public bool FileExists(string path) => System.IO.File.Exists(path);
        /// <summary>
        /// ReadAllText method.
        /// <param name="path">path parameter.</param>
        /// <returns>Result of ReadAllText.</returns>
        /// </summary>
        public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
        /// <summary>
        /// WriteAllText method.
        /// <param name="path">path parameter.</param>
        /// <param name="contents">contents parameter.</param>
        /// </summary>
        public void WriteAllText(string path, string contents) => System.IO.File.WriteAllText(path, contents);
        /// <summary>
        /// CreateDirectory method.
        /// <param name="path">path parameter.</param>
        /// </summary>
        public void CreateDirectory(string path) => System.IO.Directory.CreateDirectory(path);
        /// <summary>
        /// GetFiles method.
        /// <param name="directory">directory parameter.</param>
        /// <param name="pattern">pattern parameter.</param>
        /// <returns>Result of GetFiles.</returns>
        /// </summary>
        public IEnumerable<string> GetFiles(string directory, string pattern) => System.IO.Directory.GetFiles(directory, pattern);
    }
}
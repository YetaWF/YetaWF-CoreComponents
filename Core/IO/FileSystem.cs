/* Copyright © 2020 Softel vdm, Inc. - https://yetawf.com/Documentation/YetaWF/Licensing */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace YetaWF.Core.IO {

    public static class FileSystem {

        // Data providers set by available data providers during application startup

        /// <summary>
        /// A filesystem provider that accesses/updates a permanent file system. The permanent file system is shared between all nodes of a multi-instance site.
        /// </summary>
        public static IFileSystem FileSystemProvider { get; set; } = null!;
        /// <summary>
        /// A filesystem provider that accesses/updates a temporary, single instance file system.
        /// </summary>
        public static IFileSystem TempFileSystemProvider { get; set; } = null!;

    };

    public interface IFileSystem {

        string RootFolder { get; }

        /// <summary>
        /// Locks a file or directory by name (not the folder contents).
        /// </summary>
        Task<ILockObject> LockResourceAsync(string fileOrFolder);

        // Directory I/O

        Task DeleteDirectoryAsync(string targetFolder);
        Task CreateDirectoryAsync(string targetFolder);
        Task<bool> DirectoryExistsAsync(string targetFolder);

        Task<List<string>> GetDirectoriesAsync(string targetFolder, string pattern = null);
        Task<List<string>> GetFilesAsync(string targetFolder, string pattern = null);

        Task<DateTime> GetDirectoryCreationTimeUtcAsync(string filePath);

        // File I/O

        Task<bool> FileExistsAsync(string filePath);
        Task<DateTime> GetCreationTimeUtcAsync(string filePath);
        Task<DateTime> GetLastWriteTimeUtcAsync(string filePath);
        Task SetLastWriteTimeUtcAsync(string filePath, DateTime timeUtc);
        Task SetLastWriteTimeLocalAsync(string filePath, DateTime timeLocal);
        Task<long> GetFileSizeAsync(string filePath);

        Task<List<string>> ReadAllLinesAsync(string filePath);
        Task WriteAllLinesAsync(string filePath, List<string> lines);

        Task<string> ReadAllTextAsync(string filePath);
        Task WriteAllTextAsync(string filePath, string text);
        Task AppendAllTextAsync(string filePath, string text);
        Task AppendAllLinesAsync(string filePath, List<string> lines);

        Task<byte[]> ReadAllBytesAsync(string filePath);
        Task WriteAllBytesAsync(string filePath, byte[] data);

        Task MoveFileAsync(string fromPath, string toPath);
        Task CopyFileAsync(string fromPath, string toPath);
        Task DeleteFileAsync(string filePath);

        string GetTempFile();
        string GetTempFolder();

        // Data Files

        string GetDataFileExtension();
        string MakeValidDataFileName(string name);
        string ExtractNameFromDataFileName(string name);

        // FileStream

        Task<IFileStream> OpenFileStreamAsync(string filePath);
        Task<IFileStream> CreateFileStreamAsync(string filePath);
    }

    public interface IFileStream : IDisposable {
        FileStream GetFileStream();
        long GetLength();
        Task<int> ReadAsync(byte[] btes, int offset, int length);
        Task WriteAsync(byte[] btes, int offset, int length);
        Task FlushAsync();
        Task CloseAsync();
        Task CopyToAsync(MemoryStream memStream);
    }
}

using System;
using System.Threading.Tasks;

namespace StorageSharp.Packs
{
    public interface IPacks
    {
        Task Clear();
        Task<ArchiveScheme[]> ListAll();
        Task<ArchiveScheme> Add(string directoryPath);
        Task Delete(ArchiveScheme scheme);
        Task<string> Load(ArchiveScheme archiveScheme);
        Task Unload(ArchiveScheme archiveScheme);

        public class ArchiveScheme
        {
            public ArchiveScheme(string directoryPath)
            {
                DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            }

            public string DirectoryPath { get; }
        }
    }
}
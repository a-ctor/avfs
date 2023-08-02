namespace Avfs;

/// <summary>
/// Defines method to interact with a (virtual) file system.
/// </summary>
public interface IFileSystem
{
  void Create(VfsPath path);

  void Delete(VfsPath path, bool recursive = false);

  IEnumerable<VfsPath> Enumerate(VfsPath path, string searchPattern, SearchOption searchOption, SearchTargets targets);

  bool Exists(VfsPath path);

  Stream Open(VfsPath path, FileMode mode, FileAccess access, FileShare share);
}

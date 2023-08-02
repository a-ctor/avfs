namespace Avfs;

/// <summary>
/// Wraps around another <see cref="IFileSystem"/> allowing read-only operations only.
/// </summary>
public class ReadOnlyFileSystemDecorator : IFileSystem
{
  private readonly IFileSystem _innerFileSystem;

  public ReadOnlyFileSystemDecorator(IFileSystem innerFileSystem)
  {
    _innerFileSystem = innerFileSystem;
  }

  /// <inheritdoc />
  public void Create(VfsPath path)
  {
    throw new InvalidOperationException("This file system is read-only.");
  }

  /// <inheritdoc />
  public void Delete(VfsPath path, bool recursive = false)
  {
    throw new InvalidOperationException("This file system is read-only.");
  }

  /// <inheritdoc />
  public IEnumerable<VfsPath> Enumerate(VfsPath path, string searchPattern, SearchOption searchOption, SearchTargets targets)
  {
    if (searchPattern == null)
      throw new ArgumentNullException(nameof(searchPattern));
    
    return _innerFileSystem.Enumerate(path, searchPattern, searchOption, targets);
  }

  /// <inheritdoc />
  public bool Exists(VfsPath path)
  {
    return _innerFileSystem.Exists(path);
  }

  /// <inheritdoc />
  public Stream Open(VfsPath path, FileMode mode, FileAccess access, FileShare share)
  {
    if (mode != FileMode.Open || access != FileAccess.Read)
      throw new InvalidOperationException("This file system is read-only.");

    return _innerFileSystem.Open(path, mode, access, share);
  }
}

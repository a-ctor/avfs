namespace Avfs;

using System.Text;

public class PhysicalFileSystem : IFileSystem
{
  private readonly string _basePath;

  public PhysicalFileSystem(string basePath)
  {
    if (basePath == null)
      throw new ArgumentNullException(nameof(basePath));

    var fullBasePath = Path.GetFullPath(basePath);
    if (!Path.EndsInDirectorySeparator(fullBasePath))
      fullBasePath += Path.DirectorySeparatorChar;

    _basePath = fullBasePath;
  }

  public void Create(VfsPath path)
  {
    var physicalPath = CreatePhysicalPath(path);
    if (path.IsDirectory)
    {
      Directory.CreateDirectory(physicalPath);
    }
    else
    {
      File.Create(physicalPath).Close();
    }
  }

  public void Delete(VfsPath path, bool recursive = false)
  {
    var physicalPath = CreatePhysicalPath(path);
    if (path.IsDirectory)
    {
      Directory.Delete(physicalPath, recursive);
    }
    else
    {
      File.Delete(physicalPath);
    }
  }

  public IEnumerable<VfsPath> Enumerate(VfsPath path, string searchPattern, SearchOption searchOption, SearchTargets targets)
  {
    if (!path.IsDirectory)
      throw new ArgumentException("Cannot enumerate a file.", nameof(path));
    
    var physicalPath = CreatePhysicalPath(path);
    var enumerable = targets switch
    {
      SearchTargets.File => Directory.EnumerateFiles(physicalPath, searchPattern, searchOption),
      SearchTargets.Directory => Directory.EnumerateDirectories(physicalPath, searchPattern, searchOption),
      SearchTargets.FileAndDirectory => Directory.EnumerateFileSystemEntries(physicalPath, searchPattern, searchOption),
      _ => throw new ArgumentOutOfRangeException(nameof(targets), targets, null)
    };

    foreach (var physicalChildPath in enumerable)
    {
      if (TryCreateVirtualPath(physicalChildPath, targets, out var childPath))
        yield return childPath;
    }
  }

  public bool Exists(VfsPath path)
  {
    var physicalPath = CreatePhysicalPath(path);
    return path.IsDirectory
      ? Directory.Exists(physicalPath)
      : File.Exists(physicalPath);
  }

  public Stream Open(VfsPath path, FileMode mode, FileAccess access, FileShare share)
  {
    if (!path.IsFile)
      throw new ArgumentException("Cannot open a directory.", nameof(path));

    var physicalPath = CreatePhysicalPath(path);
    return new FileStream(physicalPath, mode, access, share);
  }

  private string CreatePhysicalPath(VfsPath path)
  {
    var sb = new StringBuilder();
    sb.Append(_basePath);

    var first = true;
    foreach (var part in path.EnumerateParts())
    {
      if (!first)
        sb.Append(Path.DirectorySeparatorChar);
      first = false;

      sb.Append(part);
    }

    if (path.IsDirectory && sb[^1] != Path.DirectorySeparatorChar)
      sb.Append(Path.DirectorySeparatorChar);

    return sb.ToString();
  }

  private bool TryCreateVirtualPath(string physicalPath, SearchTargets hint, out VfsPath path)
  {
    if (!physicalPath.StartsWith(_basePath))
    {
      path = default;
      return false;
    }

    var sb = new StringBuilder();

    var lastIndex = _basePath.Length;
    while (true)
    {
      sb.Append(VfsPath.DirectorySeparatorChar);

      var index = physicalPath.AsSpan()[lastIndex..].IndexOfAny(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      if (index == -1)
      {
        sb.Append(physicalPath[lastIndex..]);
        break;
      }
      else
      {
        sb.Append(physicalPath[lastIndex..(lastIndex + index)]);
        lastIndex += index + 1;
      }
    }

    if (hint == SearchTargets.Directory || (hint == SearchTargets.FileAndDirectory && Directory.Exists(physicalPath)))
      sb.Append(VfsPath.DirectorySeparatorChar);

    return VfsPath.TryParse(sb.ToString(), out path);
  }
}

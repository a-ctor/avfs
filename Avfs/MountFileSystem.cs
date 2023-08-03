namespace Avfs;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Aggregates multiple <see cref="IFileSystem"/> using mount points.
/// </summary>
/// <remarks>
/// Each <see cref="IFileSystem"/> is mounted using a mount path.
/// Mount points cannot share an ancestor.
/// If something is mounted on '/a/b' no mount is possible on '/a/' while, for example, '/a/c/' is still possible.
/// </remarks>
public class MountFileSystem : IFileSystem
{
  internal class MountNodeTree
  {
    private volatile MountNode? _rootNode;

    public MountNodeTree()
    {
    }

    public MountNode? RootNode => _rootNode;

    public void Mount(VfsPath path, IFileSystem fileSystem)
    {
      var pathParts = new List<string> { string.Empty };
      pathParts.AddRange(path.EnumerateParts());

      while (true)
      {
        var oldRootNode = _rootNode;
        var newRootNode = MountRecursive(fileSystem, pathParts, 0, oldRootNode);
        if (Interlocked.CompareExchange(ref _rootNode, newRootNode, oldRootNode) == oldRootNode)
          return;
      }
    }

    private MountNode MountRecursive(IFileSystem fileSystem, IReadOnlyList<string> pathParts, int layer, MountNode? previous)
    {
      // Leaf node
      if (layer == pathParts.Count - 1)
      {
        var currentPath = CreateVfsPathFromParts(pathParts, layer);
        if (previous != null)
          throw new InvalidOperationException($"Cannot mount file system to '{currentPath}' as there already are mounted file systems on sub paths.");

        return new MountNode(
          currentPath,
          pathParts[layer],
          fileSystem,
          ImmutableArray<MountNode>.Empty);
      }

      var newPrevious = previous?.Nodes.FirstOrDefault(e => e.Name == pathParts[layer + 1]);
      var newNode = MountRecursive(fileSystem, pathParts, layer + 1, newPrevious);
      if (previous == null)
      {
        return new MountNode(
          CreateVfsPathFromParts(pathParts, layer),
          pathParts[layer],
          null,
          ImmutableArray.Create(newNode));
      }

      var newNodes = newPrevious != null
        ? previous.Nodes.Replace(newPrevious, newNode)
        : previous.Nodes.Add(newNode);

      return new MountNode(
        previous.Path,
        previous.Name,
        previous.FileSystem,
        newNodes);
    }

    public void Unmount(VfsPath path)
    {
      var pathParts = new List<string> { string.Empty };
      pathParts.AddRange(path.EnumerateParts());

      while (true)
      {
        var oldRootNode = _rootNode;
        if (oldRootNode == null)
          throw new InvalidOperationException($"Cannot unmount '{path}' as there is nothing mounted there.");

        var newRootNode = UnmountRecursive(path, pathParts, 0, oldRootNode);
        if (Interlocked.CompareExchange(ref _rootNode, newRootNode, oldRootNode) == oldRootNode)
          return;
      }
    }

    private MountNode? UnmountRecursive(VfsPath unmountPath, IReadOnlyList<string> pathParts, int layer, MountNode previous)
    {
      // Leaf node
      if (layer == pathParts.Count - 1)
      {
        Debug.Assert(previous.Path == unmountPath, "previous.Path == unmountPath");
        Debug.Assert(previous.Nodes.Length == 0, "previous.Nodes.Length == 0");

        return null;
      }

      var newPrevious = previous.Nodes.FirstOrDefault(e => e.Name == pathParts[layer + 1]);
      if (newPrevious == null)
        throw new InvalidOperationException($"Cannot unmount '{unmountPath}' as there is nothing mounted there.");

      var newNode = UnmountRecursive(unmountPath, pathParts, layer + 1, newPrevious);

      var newNodes = newNode != null
        ? previous.Nodes.Replace(newPrevious, newNode)
        : previous.Nodes.Remove(newPrevious);

      if (newNodes.Length == 0)
        return null;

      return new MountNode(
        previous.Path,
        previous.Name,
        previous.FileSystem,
        newNodes);
    }

    public bool TryResolve(
      VfsPath path,
      [NotNullWhen(true)] out IFileSystem? fileSystem,
      out VfsPath remainingPath)
    {
      var mountNode = _rootNode;
      using var parts = path.EnumerateParts().GetEnumerator();
      while (mountNode != null && parts.MoveNext())
      {
        var part = parts.Current;
        mountNode = mountNode.Nodes.FirstOrDefault(e => e.Name == part);
        if (mountNode is { FileSystem: not null })
        {
          fileSystem = mountNode.FileSystem;
          remainingPath = path.RemoveBasePath(mountNode.Path);
          return true;
        }
      }

      fileSystem = null;
      remainingPath = path;

      return false;
    }

    private VfsPath CreateVfsPathFromParts(IReadOnlyList<string> pathParts, int layer)
    {
      var separatorCount = layer + 1;
      var totalSize = separatorCount;
      for (var i = 0; i <= layer; i++)
        totalSize += pathParts[i].Length;

      var pathString = string.Create(
        totalSize,
        (pathParts, layer),
        static (span, state) =>
        {
          var position = 0;
          for (var i = 0; i <= state.layer; i++)
          {
            state.pathParts[i].CopyTo(span[position..]);
            position += state.pathParts[i].Length;
            span[position++] = VfsPath.DirectorySeparatorChar;
          }
        });

      return VfsPath.Parse(pathString);
    }
  }

  [DebuggerDisplay("'{Path}' {Nodes.Length} children, FileSystem = {FileSystem}")]
  internal class MountNode
  {
    public VfsPath Path { get; }

    public string Name { get; }

    public IFileSystem? FileSystem { get; }

    public ImmutableArray<MountNode> Nodes { get; }

    public MountNode(VfsPath path, string name, IFileSystem? fileSystem, ImmutableArray<MountNode> nodes)
    {
      Path = path;
      Name = name;
      FileSystem = fileSystem;
      Nodes = nodes;
    }

    public MountNode CloneWithoutParent()
    {
      return new MountNode(Path, Name, FileSystem, Nodes);
    }
  }

  private readonly MountNodeTree _mountNodeTree = new();

  public MountFileSystem()
  {
  }

  internal MountNode? RootNode => _mountNodeTree.RootNode;

  public void Mount(VfsPath path, IFileSystem fileSystem)
  {
    if (!path.IsDirectory)
      throw new ArgumentException("A file system can only be mounted on a directory path.", nameof(path));
    if (fileSystem == null)
      throw new ArgumentNullException(nameof(fileSystem));

    _mountNodeTree.Mount(path, fileSystem);
  }

  public void Unmount(VfsPath path)
  {
    if (!path.IsDirectory)
      throw new ArgumentException("A file system can only be mounted on a directory path.", nameof(path));

    _mountNodeTree.Unmount(path);
  }

  /// <inheritdoc />
  public void Create(VfsPath path)
  {
    if (_mountNodeTree.TryResolve(path, out var fileSystem, out var remainingPath))
    {
      fileSystem.Create(remainingPath);
    }
    else
    {
      throw new InvalidOperationException($"The specified path '{path}' does not map to a mounted file system.");
    }
  }

  /// <inheritdoc />
  public void Delete(VfsPath path, bool recursive = false)
  {
    if (_mountNodeTree.TryResolve(path, out var fileSystem, out var remainingPath))
    {
      fileSystem.Delete(remainingPath, recursive);
    }
    else
    {
      throw new InvalidOperationException($"The specified path '{path}' does not map to a mounted file system.");
    }
  }

  /// <inheritdoc />
  public IEnumerable<VfsPath> Enumerate(VfsPath path, string searchPattern, SearchOption searchOption, SearchTargets targets)
  {
    if (_mountNodeTree.TryResolve(path, out var fileSystem, out var remainingPath))
    {
      return fileSystem.Enumerate(remainingPath, searchPattern, searchOption, targets);
    }
    else
    {
      throw new InvalidOperationException($"The specified path '{path}' does not map to a mounted file system.");
    }
  }

  /// <inheritdoc />
  public bool Exists(VfsPath path)
  {
    if (_mountNodeTree.TryResolve(path, out var fileSystem, out var remainingPath))
    {
      return fileSystem.Exists(remainingPath);
    }
    else
    {
      throw new InvalidOperationException($"The specified path '{path}' does not map to a mounted file system.");
    }
  }

  /// <inheritdoc />
  public Stream Open(VfsPath path, FileMode mode, FileAccess access, FileShare share)
  {
    if (_mountNodeTree.TryResolve(path, out var fileSystem, out var remainingPath))
    {
      return fileSystem.Open(remainingPath, mode, access, share);
    }
    else
    {
      throw new InvalidOperationException($"The specified path '{path}' does not map to a mounted file system.");
    }
  }
}

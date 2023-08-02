namespace Avfs;

using System.Diagnostics;

/// <summary>
/// Represents a path in a virtual file system (VFS).
/// </summary>
/// <remarks>
/// VFS paths have a few restrictions:
/// <list type="number">
/// <item><description>The directory separator is always a forward slash ('/').</description></item>
/// <item><description>Paths are always absolute paths and start with a directory separator (e.g. '/').</description></item>
/// <item><description>Directory paths always end with a directory separator (e.g. '/asd/').</description></item>
/// <item><description>File paths never end in with a directory separator (e.g. '/asd').</description></item>
/// </list>
/// File and directory names in a VFS path also have a few restrictions:
/// <list type="number">
/// <item><description>The must not be empty.</description></item>
/// <item><description>
/// The can only consist of:
/// <list type="bullet">
/// <item><description>Unicode letters</description></item>
/// <item><description>Unicode numbers</description></item>
/// <item><description>Underscore ('_')</description></item>
/// <item><description>Hyphen ('-')</description></item>
/// <item><description>Dot ('.')</description></item>
/// </list>
/// </description></item>
/// <item><description>The must end with a Unicode letter or digit.</description></item>
/// <item><description>A dot symbol must not follow another dot symbol.</description></item>
/// </list>
/// </remarks>
public readonly struct VfsPath : IEquatable<VfsPath>, IComparable<VfsPath>, IComparable
{
  private sealed class CaseSensitiveComparer : IComparer<VfsPath>
  {
    public int Compare(VfsPath x, VfsPath y)
    {
      return string.Compare(x._value, y._value, StringComparison.Ordinal);
    }
  }

  private sealed class CaseInsensitiveComparer : IComparer<VfsPath>
  {
    public int Compare(VfsPath x, VfsPath y)
    {
      return string.Compare(x._value, y._value, StringComparison.OrdinalIgnoreCase);
    }
  }

  public static IComparer<VfsPath> DefaultComparer { get; } = new CaseSensitiveComparer();

  public static IComparer<VfsPath> DefaultComparerIgnoreCase { get; } = new CaseInsensitiveComparer();

  private const string c_rootPathString = "/";

  public const char DirectorySeparatorChar = '/';
  public const char ExtensionSeparatorChar = '.';

  public static VfsPath Parse(string s)
  {
    if (s == null)
      throw new ArgumentNullException(nameof(s));
    if (!TryParse(s, out var path))
      throw new FormatException($"The input string '{s}' is not a valid AVFS path.");

    return path;
  }

  public static bool TryParse(string s, out VfsPath path)
  {
    if (s == null)
      throw new ArgumentNullException(nameof(s));

    if (!TryValidateAbsolutePath(s))
    {
      path = default;
      return false;
    }

    path = new VfsPath(s);
    return true;
  }

  public static readonly VfsPath Root = new(c_rootPathString);

  private readonly string _value;

  public VfsPath()
    : this(c_rootPathString)
  {
  }

  private VfsPath(string value)
  {
    Debug.Assert(TryValidateAbsolutePath(value));

    _value = value;
  }

  public bool IsDirectory => _value.EndsWith(DirectorySeparatorChar);

  public bool IsFile => !IsDirectory;

  public bool IsRoot => _value == c_rootPathString;


  public string DirectoryName
  {
    get
    {
      var indexOfLastDirectorySeparator = _value.LastIndexOf(DirectorySeparatorChar);
      if (indexOfLastDirectorySeparator <= 0)
        return string.Empty;

      // This will always return a valid index as the path string always starts with a directory separator
      var indexOfPreviousDirectorySeparator = _value.LastIndexOf(DirectorySeparatorChar, indexOfLastDirectorySeparator - 1);
      return _value[(indexOfPreviousDirectorySeparator + 1)..indexOfLastDirectorySeparator];
    }
  }

  public string FileName
  {
    get
    {
      var indexOfLastDirectorySeparator = _value.LastIndexOf(DirectorySeparatorChar);
      return _value[(indexOfLastDirectorySeparator + 1)..];
    }
  }

  public string FileNameWithoutExtension
  {
    get
    {
      var lastIndex = _value.AsSpan().LastIndexOfAny(ExtensionSeparatorChar, DirectorySeparatorChar);
      if (lastIndex <= 0 || _value[lastIndex] == DirectorySeparatorChar)
        return FileName;

      var indexOfLastDirectorySeparator = _value.LastIndexOf(DirectorySeparatorChar, lastIndex);
      return _value[(indexOfLastDirectorySeparator + 1)..lastIndex];
    }
  }

  public string Extension
  {
    get
    {
      var lastIndex = _value.AsSpan().LastIndexOfAny(ExtensionSeparatorChar, DirectorySeparatorChar);
      if (lastIndex <= 0 || _value[lastIndex] == DirectorySeparatorChar)
        return string.Empty;

      return _value[lastIndex..];
    }
  }

  public bool HasExtension
  {
    get
    {
      var lastIndex = _value.AsSpan().LastIndexOfAny(ExtensionSeparatorChar, DirectorySeparatorChar);
      return lastIndex >= 0 && _value[lastIndex] == ExtensionSeparatorChar;
    }
  }


  public VfsPath Append(string path)
  {
    if (path == null)
      throw new ArgumentNullException(nameof(path));
    if (IsFile)
      throw new InvalidOperationException("Cannot append to a file path.");

    if (!TryValidateRelativePath(path))
      throw new ArgumentException($"The string '{path}' is not a valid relative AVFS path.", nameof(path));

    return new VfsPath(_value + path);
  }

  public VfsPath AsDirectory()
  {
    return IsDirectory
      ? this
      : new VfsPath(_value + DirectorySeparatorChar);
  }

  public VfsPath AsFile()
  {
    if (IsRoot)
      throw new InvalidOperationException("Cannot convert a root path to a file path.");

    return IsFile
      ? this
      : new VfsPath(_value[..^1]);
  }

  public VfsPath ChangeExtension(string? extension)
  {
    if (!string.IsNullOrEmpty(extension) && !extension.StartsWith('.'))
      extension = '.' + extension;

    if (!string.IsNullOrEmpty(extension) && !TryValidatePathPart(extension, out var validatedCount) && validatedCount != extension.Length)
      throw new ArgumentException($"The extension '{extension}' is not a valid AVFS extension.", nameof(extension));

    var extensionIndex = _value.AsSpan().LastIndexOfAny(ExtensionSeparatorChar, DirectorySeparatorChar);
    var hasExtension = extensionIndex >= 0 && _value[extensionIndex] == ExtensionSeparatorChar;
    var existingExtension = hasExtension
      ? _value.AsSpan()[extensionIndex..]
      : ReadOnlySpan<char>.Empty;
    var newExtension = !string.IsNullOrEmpty(extension)
      ? extension.AsMemory()
      : ReadOnlyMemory<char>.Empty;

    // Shortcut if nothing has changed
    if (existingExtension.SequenceEqual(extension))
      return this;

    var newPath = string.Create(
      extensionIndex + newExtension.Length,
      (fileName: _value.AsMemory()[..extensionIndex], newExtension),
      static (span, args) =>
      {
        args.fileName.Span.CopyTo(span);
        args.newExtension.Span.CopyTo(span[args.fileName.Length..]);
      });

    return new VfsPath(newPath);
  }

  public bool IsParentOf(VfsPath path)
  {
    return IsDirectory && path._value.StartsWith(_value);
  }

  public bool IsChildOf(VfsPath path)
  {
    return path.IsParentOf(this);
  }

  public VfsPath AddBasePath(VfsPath basePath)
  {
    if (!basePath.IsDirectory)
      throw new ArgumentException($"Base path '{basePath}' is not a directory path.", nameof(basePath));

    var newPath = string.Concat(basePath._value, _value.AsSpan()[1..]);
    return new VfsPath(newPath);
  }

  public VfsPath RemoveBasePath(VfsPath basePath)
  {
    if (!basePath.IsDirectory)
      throw new ArgumentException($"Base path '{basePath}' is not a directory path.", nameof(basePath));
    if (!_value.StartsWith(basePath._value))
      throw new InvalidOperationException($"Path '{basePath}' is not a base path of '{this}'.");

    // ReSharper disable once UseIndexFromEndExpression : incorrect suggestion
    var newPath = _value[(basePath._value.Length - 1)..];
    return new VfsPath(newPath);
  }

  public IEnumerable<string> EnumerateParts()
  {
    var startIndex = 1;
    while (true)
    {
      var index = _value.IndexOf(DirectorySeparatorChar, startIndex);
      if (index == -1)
      {
        if (startIndex < _value.Length)
          yield return _value[startIndex..];
        yield break;
      }

      yield return _value[startIndex..index];
      startIndex = index + 1;
    }
  }

  public int CompareTo(VfsPath other) => string.Compare(_value, other._value, StringComparison.Ordinal);

  public int CompareTo(object? obj)
  {
    if (ReferenceEquals(null, obj))
      return 1;

    return obj is VfsPath other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(VfsPath)}");
  }

  public bool Equals(VfsPath other) => _value == other._value;

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is VfsPath other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => _value.GetHashCode();

  /// <inheritdoc />
  public override string ToString() => _value;

  public static bool operator ==(VfsPath left, VfsPath right) => left.Equals(right);

  public static bool operator !=(VfsPath left, VfsPath right) => !left.Equals(right);

  public static VfsPath operator /(VfsPath path, string addition) => path.Append(addition);

  private static bool TryValidateAbsolutePath(ReadOnlySpan<char> value)
  {
    if (value.Length == 0 || value[0] != DirectorySeparatorChar)
      return false;

    var remainder = value[1..];
    return remainder.Length == 0 || TryValidateRelativePath(remainder);
  }

  private static bool TryValidateRelativePath(ReadOnlySpan<char> value)
  {
    if (value.Length == 0)
      return false;

    var remainder = value;
    while (remainder.Length != 0)
    {
      if (!TryValidatePathPart(remainder, out var advance))
        return false;

      remainder = remainder[advance..];
    }

    return true;
  }

  private static bool TryValidatePathPart(ReadOnlySpan<char> value, out int advance)
  {
    // Allow direct return statement
    advance = 0;

    var consumed = 0;
    bool lastWasLetterOrDigit = false, lastWasDot = false;
    foreach (var c in value)
    {
      consumed++;
      if (c == DirectorySeparatorChar)
        break;

      if (char.IsLetterOrDigit(c))
      {
        lastWasLetterOrDigit = true;
        lastWasDot = false;
        continue;
      }

      if (c == '.')
      {
        // rule: do not allow '..' in the path
        if (lastWasDot)
          return false;

        lastWasLetterOrDigit = false;
        lastWasDot = true;
        continue;
      }

      lastWasLetterOrDigit = false;
      lastWasDot = false;

      // rule: only allow letters, numbers, underscore, dot, and hyphen
      if (c != '_' && c != '-')
        return false;
    }

    // rule: do not allow empty path parts e.g. '/a//b'
    if (consumed == 0)
      return false;

    // rule: must end with letter or digit
    if (!lastWasLetterOrDigit)
      return false;

    advance = consumed;
    return true;
  }
}

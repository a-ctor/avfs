namespace Avfs.IntegrationTests;

public class PhysicalFileSystemTest : IDisposable
{
  private readonly string _physicalBasePath;
  private readonly PhysicalFileSystem _fileSystem;

  public PhysicalFileSystemTest()
  {
    _physicalBasePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_physicalBasePath);
    _fileSystem = new PhysicalFileSystem(_physicalBasePath);
  }

  public void Dispose()
  {
    Directory.Delete(_physicalBasePath, true);
  }

  [Fact]
  public void CreateDirectory()
  {
    var physicalPath = CreatePhysicalPath("a");
    Directory.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Create(VfsPath.Parse("/a/"));

    Directory.Exists(physicalPath).Should().BeTrue();
  }

  [Fact]
  public void CreateDirectory_CreatesMissingParentDirectories()
  {
    var physicalPath = CreatePhysicalPath("a", "b");
    Directory.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Create(VfsPath.Parse("/a/b/"));

    Directory.Exists(physicalPath).Should().BeTrue();
  }

  [Fact]
  public void CreateFile()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Create(VfsPath.Parse("/a"));

    File.Exists(physicalPath).Should().BeTrue();
    File.ReadAllBytes(physicalPath).Should().BeEmpty();
  }

  [Fact]
  public void DeleteDirectory()
  {
    var physicalPath = CreatePhysicalPath("a");
    Directory.CreateDirectory(physicalPath);
    Directory.Exists(physicalPath).Should().BeTrue();

    _fileSystem.Delete(VfsPath.Parse("/a/"));
    Directory.Exists(physicalPath).Should().BeFalse();
  }

  [Fact]
  public void DeleteDirectory_NonExistent_Throws()
  {
    var physicalPath = CreatePhysicalPath("a");
    Directory.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Invoking(e => e.Delete(VfsPath.Parse("/a/"))).Should()
      .Throw<DirectoryNotFoundException>();
  }

  [Fact]
  public void DeleteDirectory_NotEmpty_Throws()
  {
    var aPhysicalPath = CreatePhysicalPath("a");
    var bPhysicalPath = CreatePhysicalPath("a", "b");
    Directory.CreateDirectory(bPhysicalPath);
    Directory.Exists(aPhysicalPath).Should().BeTrue();
    Directory.Exists(bPhysicalPath).Should().BeTrue();

    _fileSystem.Invoking(e => e.Delete(VfsPath.Parse("/a/"))).Should()
      .Throw<IOException>()
      .WithMessage("The directory is not empty.*");
  }

  [Fact]
  public void DeleteDirectory_Recursive()
  {
    var aPhysicalPath = CreatePhysicalPath("a");
    var bPhysicalPath = CreatePhysicalPath("a", "b");
    Directory.CreateDirectory(bPhysicalPath);
    Directory.Exists(aPhysicalPath).Should().BeTrue();
    Directory.Exists(bPhysicalPath).Should().BeTrue();

    _fileSystem.Delete(VfsPath.Parse("/a/"), true);

    Directory.Exists(aPhysicalPath).Should().BeFalse();
    Directory.Exists(bPhysicalPath).Should().BeFalse();
  }

  [Fact]
  public void DeleteFile()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.Create(physicalPath).Close();
    File.Exists(physicalPath).Should().BeTrue();

    _fileSystem.Delete(VfsPath.Parse("/a"));
    File.Exists(physicalPath).Should().BeFalse();
  }

  [Fact]
  public void DeleteFile_NonExistent()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Invoking(e => e.Delete(VfsPath.Parse("/a"))).Should().NotThrow();
  }

  [Fact]
  public void Enumerate_DirectoryInTopDirectory()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.TopDirectoryOnly, SearchTargets.Directory).ToArray();
    var expectedPaths = CreatePaths("/a.txt/", "/b.txt/");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  [Fact]
  public void Enumerate_DirectoryInAllDirectories()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.AllDirectories, SearchTargets.Directory).ToArray();
    var expectedPaths = CreatePaths("/a.txt/", "/b.txt/", "/b/b-a.txt/");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  [Fact]
  public void Enumerate_FilesInTopDirectory()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.TopDirectoryOnly, SearchTargets.File).ToArray();
    var expectedPaths = CreatePaths("/1.txt");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  [Fact]
  public void Enumerate_FilesInAllDirectories()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.AllDirectories, SearchTargets.File).ToArray();
    var expectedPaths = CreatePaths("/1.txt", "/b/b-1.txt");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  [Fact]
  public void Enumerate_FilesAndDirectoriesInTopDirectory()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.TopDirectoryOnly, SearchTargets.FileAndDirectory).ToArray();
    var expectedPaths = CreatePaths("/a.txt/", "/b.txt/", "/1.txt");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  [Fact]
  public void Enumerate_FilesAndDirectoriesInAllDirectories()
  {
    CreateEnumerateTestSetup();

    var paths = _fileSystem.Enumerate(VfsPath.Root, "*.txt", SearchOption.AllDirectories, SearchTargets.FileAndDirectory).ToArray();
    var expectedPaths = CreatePaths("/a.txt/", "/b.txt/", "/b/b-a.txt/", "/1.txt", "/b/b-1.txt");
    paths.Should().BeEquivalentTo(expectedPaths);
  }

  // Enumerate file setup:
  // |- 1
  // |- 1.txt
  // |- a/
  // |- a.txt/
  // |- b/
  // |  |- b-1
  // |  |- b-1.txt
  // |  |- b-a/
  // |  |- b-a.txt/
  // |- b.txt/
  private void CreateEnumerateTestSetup()
  {
    Directory.CreateDirectory(CreatePhysicalPath("a"));
    Directory.CreateDirectory(CreatePhysicalPath("a.txt"));
    Directory.CreateDirectory(CreatePhysicalPath("b"));
    Directory.CreateDirectory(CreatePhysicalPath("b", "b-a"));
    Directory.CreateDirectory(CreatePhysicalPath("b", "b-a.txt"));
    Directory.CreateDirectory(CreatePhysicalPath("b.txt"));

    File.Create(CreatePhysicalPath("1")).Close();
    File.Create(CreatePhysicalPath("1.txt")).Close();
    File.Create(CreatePhysicalPath("b", "b-1")).Close();
    File.Create(CreatePhysicalPath("b", "b-1.txt")).Close();
  }

  [Fact]
  public void ExistsDirectory_ExistingDirectory()
  {
    var physicalPath = CreatePhysicalPath("a");
    Directory.CreateDirectory(physicalPath);
    Directory.Exists(physicalPath).Should().BeTrue();

    _fileSystem.Exists(VfsPath.Parse("/a/")).Should().BeTrue();
  }

  [Fact]
  public void ExistsDirectory_NonExistingDirectory()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.Create(physicalPath).Close();
    Directory.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Exists(VfsPath.Parse("/a/")).Should().BeFalse();
  }

  [Fact]
  public void ExistsFile_ExistingFile()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.Create(physicalPath).Close();
    File.Exists(physicalPath).Should().BeTrue();

    _fileSystem.Exists(VfsPath.Parse("/a")).Should().BeTrue();
  }

  [Fact]
  public void ExistsFile_NonExistingFile()
  {
    var physicalPath = CreatePhysicalPath("a");
    Directory.CreateDirectory(physicalPath);
    File.Exists(physicalPath).Should().BeFalse();

    _fileSystem.Exists(VfsPath.Parse("/a")).Should().BeFalse();
  }

  [Fact]
  public void Open()
  {
    var physicalPath = CreatePhysicalPath("a");
    File.WriteAllText(physicalPath, "hello");

    using var stream = _fileSystem.Open(VfsPath.Parse("/a"), FileMode.Open, FileAccess.Read, FileShare.None);
    using var reader = new StreamReader(stream);

    reader.ReadToEnd().Should().Be("hello");
  }

  [Fact]
  public void Open_Directory_Throws()
  {
    _fileSystem.Invoking(e => e.Open(VfsPath.Root, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
      .Should()
      .Throw<ArgumentException>()
      .WithMessage("Cannot open a directory. (Parameter 'path')");
  }

  private string CreatePhysicalPath(params string[] parts)
  {
    var args = new string[parts.Length + 1];
    args[0] = _physicalBasePath;
    Array.Copy(parts, 0, args, 1, parts.Length);

    return Path.Combine(args);
  }

  private VfsPath[] CreatePaths(params string[] paths)
  {
    return paths.Select(VfsPath.Parse).ToArray();
  }
}

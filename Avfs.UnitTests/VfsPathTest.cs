namespace Avfs.UnitTests;

using System.Xml.Xsl;
using TestData;

public class VfsPathTest
{
  [Fact]
  public void Root()
  {
    VfsPath.Root.IsRoot.Should().BeTrue();
  }

  [Fact]
  public void Parse_ValidPath()
  {
    Action a = () => VfsPath.Parse("/asd");
  }
  
  [Fact]
  public void Parse_WithInvalidPath_Throws()
  {
    Action a = () => VfsPath.Parse("");
    a.Should()
      .Throw<FormatException>()
      .WithMessage("The input string '' is not a valid AVFS path.");
  }

  [Theory]
  [InlineData("/")]
  [InlineData("/a")]
  [InlineData("/a/")]
  [InlineData("/-a")]
  [InlineData("/.3")]
  [InlineData("/_a")]
  [InlineData("/a.b")]
  public void TryParse_ValidPaths(string path)
  {
    VfsPath.TryParse(path, out _).Should().BeTrue();
  }

  [Theory]
  [InlineData("")]
  [InlineData("asd")]
  [InlineData("/a-")]
  [InlineData("/a.")]
  [InlineData("/a_")]
  [InlineData("/a..b")]
  public void TryParse_InValidPaths(string path)
  {
    VfsPath.TryParse(path, out _).Should().BeFalse();
  }
  
  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void IsDirectory(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).IsDirectory.Should().Be(record.HasFlag(VfsPathTestRecordFlags.IsDirectory));
  }

  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void IsFile(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).IsFile.Should().Be(record.HasFlag(VfsPathTestRecordFlags.IsFile));
  }
  
  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void IsRoot(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).IsRoot.Should().Be(record.HasFlag(VfsPathTestRecordFlags.IsRoot));
  }
  
  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void DirectoryName(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).DirectoryName.Should().Be(record.DirectoryName);
  }

  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void FileName(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).FileName.Should().Be(record.FileName);
  }

  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void FileNameWithoutExtension(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).FileNameWithoutExtension.Should().Be(record.FileNameWithoutExtension);
  }

  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void Extension(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).Extension.Should().Be(record.Extension);
  }

  [Theory]
  [ClassData(typeof(ValidVfsPathTestData))]
  public void HasExtension(VfsPathTestRecord record)
  {
    VfsPath.Parse(record.PathText).HasExtension.Should().Be(record.HasFlag(VfsPathTestRecordFlags.HasExtension));
  }

  [Theory]
  [InlineData("/asd", "/asd/")]
  [InlineData("/asd/", "/asd/")]
  public void AsDirectory(string path, string directoryPath)
  {
    VfsPath.Parse(path).AsDirectory().ToString().Should().Be(directoryPath);
  }

  [Theory]
  [InlineData("/asd", "/asd")]
  [InlineData("/asd/", "/asd")]
  public void AsFile(string path, string filePath)
  {
    VfsPath.Parse(path).AsFile().ToString().Should().Be(filePath);
  }

  [Fact]
  public void AsFile_WithRootPath_Throws()
  {
    var path = VfsPath.Root;
    path.Invoking(e => e.AsFile()).Should()
      .Throw<InvalidOperationException>()
      .WithMessage("Cannot convert a root path to a file path.");
  }

  [Theory]
  [InlineData("/", "asd", "/asd")]
  [InlineData("/", "asd/", "/asd/")]
  [InlineData("/bcd/", "asd/", "/bcd/asd/")]
  public void Append(string path, string addition, string result)
  {
    VfsPath.Parse(path).Append(addition).ToString().Should().Be(result);
  }

  [Fact]
  public void Append_WithInvalidAppend_Throws()
  {
    var path = VfsPath.Parse("/asd/");
    path.Invoking(e => e.Append("/asd")).Should()
      .Throw<ArgumentException>()
      .WithMessage("The string '/asd' is not a valid relative AVFS path. (Parameter 'path')");
  }

  [Fact]
  public void Append_WithFilePath_Throws()
  {
    var path = VfsPath.Parse("/asd");
    path.Invoking(e => e.Append("asd")).Should()
      .Throw<InvalidOperationException>()
      .WithMessage("Cannot append to a file path.");
  }

  [Theory]
  [InlineData("/.txt", ".test", "/.test")]
  [InlineData("/.txt", "test", "/.test")]
  [InlineData("/.txt", "", "/")]
  [InlineData("/.txt", null, "/")]
  [InlineData("/a.txt", ".test", "/a.test")]
  [InlineData("/a.txt", "test", "/a.test")]
  [InlineData("/a.txt", "", "/a")]
  [InlineData("/a.txt", null, "/a")]
  [InlineData("/a.b.txt", ".test", "/a.b.test")]
  [InlineData("/a.b.txt", "test", "/a.b.test")]
  [InlineData("/a.b.txt", "", "/a.b")]
  [InlineData("/a.b.txt", null, "/a.b")]
  [InlineData("/a.b/.txt", ".test", "/a.b/.test")]
  [InlineData("/a.b/.txt", "test", "/a.b/.test")]
  [InlineData("/a.b/.txt", "", "/a.b/")]
  [InlineData("/a.b/.txt", null, "/a.b/")]
  [InlineData("/a.b/a.txt", ".test", "/a.b/a.test")]
  [InlineData("/a.b/a.txt", "test", "/a.b/a.test")]
  [InlineData("/a.b/a.txt", "", "/a.b/a")]
  [InlineData("/a.b/a.txt", null, "/a.b/a")]
  [InlineData("/a.b/a.b.txt", ".test", "/a.b/a.b.test")]
  [InlineData("/a.b/a.b.txt", "test", "/a.b/a.b.test")]
  [InlineData("/a.b/a.b.txt", "", "/a.b/a.b")]
  [InlineData("/a.b/a.b.txt", null, "/a.b/a.b")]
  public void ChangeExtension(string path, string? newExtension, string result)
  {
    VfsPath.Parse(path).ChangeExtension(newExtension).ToString().Should().Be(result);
  }

  [Theory]
  [InlineData("/", "/asd", true)]
  [InlineData("/", "/asd/", true)]
  [InlineData("/a", "/a", false)]
  [InlineData("/a", "/a/b", false)]
  [InlineData("/a/", "/a", false)]
  [InlineData("/a/", "/a/b", true)]
  public void IsParentOf(string left, string right, bool result)
  {
    VfsPath.Parse(left).IsParentOf(VfsPath.Parse(right)).Should().Be(result);
  }

  [Fact]
  public void AddBasePath()
  {
    var basePath = VfsPath.Parse("/a/");
    VfsPath.Parse("/b").AddBasePath(basePath).ToString().Should().Be("/a/b");
  }

  [Fact]
  public void AddBasePath_WithFilePath_Throws()
  {
    var basePath = VfsPath.Parse("/a");
    var path = VfsPath.Parse("/b");
    path.Invoking(e => e.AddBasePath(basePath)).Should()
      .Throw<ArgumentException>()
      .WithMessage("Base path '/a' is not a directory path. (Parameter 'basePath')");
  }

  [Fact]
  public void RemoveBasePath()
  {
    var basePath = VfsPath.Parse("/a/");
    VfsPath.Parse("/a/b").RemoveBasePath(basePath).ToString().Should().Be("/b");
  }

  [Fact]
  public void RemoveBasePath_WithFilePath_Throws()
  {
    var basePath = VfsPath.Parse("/a");
    var path = VfsPath.Parse("/a/b");
    path.Invoking(e => e.RemoveBasePath(basePath)).Should()
      .Throw<ArgumentException>()
      .WithMessage("Base path '/a' is not a directory path. (Parameter 'basePath')");
  }

  [Fact]
  public void RemoveBasePath_WithIncorrectBase_Throws()
  {
    var basePath = VfsPath.Parse("/c/");
    var path = VfsPath.Parse("/a/b");
    path.Invoking(e => e.RemoveBasePath(basePath)).Should()
      .Throw<InvalidOperationException>()
      .WithMessage("Path '/c/' is not a base path of '/a/b'.");
  }

  [Theory]
  [InlineData("/", new string[0])]
  [InlineData("/a", new[] { "a" })]
  [InlineData("/a/b", new[] { "a", "b" })]
  [InlineData("/a/b.c/d", new[] { "a", "b.c", "d" })]
  public void EnumerateParts(string path, string[] parts)
  {
    VfsPath.Parse(path).EnumerateParts().ToArray().Should().Equal(parts);
  }

  [Fact]
  public void AppendOperator()
  {
    (VfsPath.Root / "asd").ToString().Should().Be("/asd");
  }
}

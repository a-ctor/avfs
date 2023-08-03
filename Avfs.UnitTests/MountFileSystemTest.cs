namespace Avfs.UnitTests;

public class MountFileSystemTest
{
  [Fact]
  public void Mount()
  {
    var fileSystemStub1 = Mock.Of<IFileSystem>();
    var fileSystemStub2 = Mock.Of<IFileSystem>();
    var fileSystemStub3 = Mock.Of<IFileSystem>();
    var mountFileSystem = new MountFileSystem();
    mountFileSystem.RootNode.Should().BeNull();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemStub1);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemStub2);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemStub3);

    var rootNode = mountFileSystem.RootNode!;
    rootNode.Path.Should().Be(VfsPath.Root);
    rootNode.Name.Should().BeEmpty();
    rootNode.FileSystem.Should().BeNull();
    rootNode.Nodes.Length.Should().Be(2);

    var savesNode = rootNode.Nodes[0];
    savesNode.Path.Should().Be(VfsPath.Parse("/save/"));
    savesNode.Name.Should().Be("save");
    savesNode.FileSystem.Should().BeNull();
    savesNode.Nodes.Length.Should().Be(2);

    var oneNode = savesNode.Nodes[0];
    oneNode.Path.Should().Be(VfsPath.Parse("/save/1/"));
    oneNode.Name.Should().Be("1");
    oneNode.FileSystem.Should().Be(fileSystemStub1);
    oneNode.Nodes.Should().BeEmpty();

    var twoNode = savesNode.Nodes[1];
    twoNode.Path.Should().Be(VfsPath.Parse("/save/2/"));
    twoNode.Name.Should().Be("2");
    twoNode.FileSystem.Should().Be(fileSystemStub2);
    twoNode.Nodes.Should().BeEmpty();

    var usrNode = rootNode.Nodes[1];
    usrNode.Path.Should().Be(VfsPath.Parse("/usr/"));
    usrNode.Name.Should().Be("usr");
    usrNode.FileSystem.Should().Be(fileSystemStub3);
    usrNode.Nodes.Length.Should().Be(0);
  }

  [Fact]
  public void Unmount()
  {
    var fileSystemStub1 = Mock.Of<IFileSystem>();
    var fileSystemStub2 = Mock.Of<IFileSystem>();
    var fileSystemStub3 = Mock.Of<IFileSystem>();
    var mountFileSystem = new MountFileSystem();
    mountFileSystem.RootNode.Should().BeNull();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemStub1);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemStub2);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemStub3);

    // First unmount
    mountFileSystem.Unmount(VfsPath.Parse("/save/1/"));

    mountFileSystem.RootNode!.Nodes.Length.Should().Be(2);
    mountFileSystem.RootNode.Nodes[0].Nodes.Length.Should().Be(1);
    mountFileSystem.RootNode.Nodes[0].Nodes[0].Path.Should().Be(VfsPath.Parse("/save/2/"));
    mountFileSystem.RootNode.Nodes[0].Nodes[0].FileSystem.Should().BeSameAs(fileSystemStub2);
    mountFileSystem.RootNode.Nodes[1].Nodes.Length.Should().Be(0);
    mountFileSystem.RootNode.Nodes[1].Path.Should().Be(VfsPath.Parse("/usr/"));
    mountFileSystem.RootNode.Nodes[1].FileSystem.Should().BeSameAs(fileSystemStub3);
    
    // Second unmount
    mountFileSystem.Unmount(VfsPath.Parse("/usr/"));
    
    mountFileSystem.RootNode!.Nodes.Length.Should().Be(1);
    mountFileSystem.RootNode.Nodes[0].Nodes.Length.Should().Be(1);
    mountFileSystem.RootNode.Nodes[0].Nodes[0].Path.Should().Be(VfsPath.Parse("/save/2/"));
    mountFileSystem.RootNode.Nodes[0].Nodes[0].FileSystem.Should().BeSameAs(fileSystemStub2);
    
    // Third unmount
    mountFileSystem.Unmount(VfsPath.Parse("/save/2/"));

    mountFileSystem.RootNode.Should().BeNull();
  }

  [Fact]
  public void Unmount_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Unmount(VfsPath.Parse("/save/1/")))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("Cannot unmount '/save/1/' as there is nothing mounted there.");
  }

  [Fact]
  public void Create()
  {
    var fileSystemMock1 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock1.Setup(e => e.Create(VfsPath.Parse("/a"))).Verifiable();
    
    var fileSystemMock2 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock2.Setup(e => e.Create(VfsPath.Parse("/a/b/"))).Verifiable();

    var fileSystemMock3 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock3.Setup(e => e.Create(VfsPath.Parse("/4/b"))).Verifiable();

    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemMock1.Object);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemMock2.Object);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemMock3.Object);

    mountFileSystem.Create(VfsPath.Parse("/save/1/a"));
    mountFileSystem.Create(VfsPath.Parse("/save/2/a/b/"));
    mountFileSystem.Create(VfsPath.Parse("/usr/4/b"));
    
    fileSystemMock1.Verify();
    fileSystemMock2.Verify();
    fileSystemMock3.Verify();
  }

  [Fact]
  public void Create_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Create(VfsPath.Parse("/save/1/a")))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("The specified path '/save/1/a' does not map to a mounted file system.");
  }

  [Fact]
  public void Delete()
  {
    var fileSystemMock1 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock1.Setup(e => e.Delete(VfsPath.Parse("/a"), true)).Verifiable();
    
    var fileSystemMock2 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock2.Setup(e => e.Delete(VfsPath.Parse("/a/b/"), false)).Verifiable();

    var fileSystemMock3 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock3.Setup(e => e.Delete(VfsPath.Parse("/4/b"), true)).Verifiable();

    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemMock1.Object);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemMock2.Object);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemMock3.Object);

    mountFileSystem.Delete(VfsPath.Parse("/save/1/a"), true);
    mountFileSystem.Delete(VfsPath.Parse("/save/2/a/b/"), false);
    mountFileSystem.Delete(VfsPath.Parse("/usr/4/b"), true);
    
    fileSystemMock1.Verify();
    fileSystemMock2.Verify();
    fileSystemMock3.Verify();
  }

  [Fact]
  public void Delete_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Delete(VfsPath.Parse("/save/1/a")))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("The specified path '/save/1/a' does not map to a mounted file system.");
  }

  [Fact]
  public void Enumerate()
  {
    var expectedResult1 = new [] { VfsPath.Parse("/save/1/a/test.txt") };
    var fileSystemStub1 = new Mock<IFileSystem>();
    fileSystemStub1
      .Setup(e => e.Enumerate(VfsPath.Parse("/a/"), "*.txt", SearchOption.TopDirectoryOnly, SearchTargets.File))
      .Returns(expectedResult1);

    var expectedResult2 = new [] { VfsPath.Parse("/save/2/a/b/test2"), VfsPath.Parse("/save/2/a/b/test") };
    var fileSystemStub2 = new Mock<IFileSystem>();
    fileSystemStub2
      .Setup(e => e.Enumerate(VfsPath.Parse("/a/b/"), "*", SearchOption.AllDirectories, SearchTargets.FileAndDirectory))
      .Returns(expectedResult2);

    var expectedResult3 = new [] { VfsPath.Parse("/usr/4/b/abc/") };
    var fileSystemStub3 = new Mock<IFileSystem>();
    fileSystemStub3
      .Setup(e => e.Enumerate(VfsPath.Parse("/4/b/"), "a*", SearchOption.TopDirectoryOnly, SearchTargets.Directory))
      .Returns(expectedResult3);

    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemStub1.Object);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemStub2.Object);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemStub3.Object);

    mountFileSystem.Enumerate(VfsPath.Parse("/save/1/a/"), "*.txt", SearchOption.TopDirectoryOnly, SearchTargets.File)
      .Should()
      .BeSameAs(expectedResult1);
    mountFileSystem.Enumerate(VfsPath.Parse("/save/2/a/b/"), "*", SearchOption.AllDirectories, SearchTargets.FileAndDirectory)
      .Should()
      .BeSameAs(expectedResult2);
    mountFileSystem.Enumerate(VfsPath.Parse("/usr/4/b/"), "a*", SearchOption.TopDirectoryOnly, SearchTargets.Directory)
      .Should()
      .BeSameAs(expectedResult3);
  }

  [Fact]
  public void Enumerate_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Enumerate(VfsPath.Parse("/save/1/a"), "*", SearchOption.TopDirectoryOnly, SearchTargets.File))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("The specified path '/save/1/a' does not map to a mounted file system.");
  }

  [Fact]
  public void Exists()
  {
    var fileSystemMock1 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock1
      .Setup(e => e.Exists(VfsPath.Parse("/a")))
      .Returns(true)
      .Verifiable();
    
    var fileSystemMock2 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock2
      .Setup(e => e.Exists(VfsPath.Parse("/a/b/")))
      .Returns(false)
      .Verifiable();

    var fileSystemMock3 = new Mock<IFileSystem>(MockBehavior.Strict);
    fileSystemMock3
      .Setup(e => e.Exists(VfsPath.Parse("/4/b")))
      .Returns(false)
      .Verifiable();

    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemMock1.Object);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemMock2.Object);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemMock3.Object);

    mountFileSystem.Exists(VfsPath.Parse("/save/1/a")).Should().BeTrue();
    mountFileSystem.Exists(VfsPath.Parse("/save/2/a/b/")).Should().BeFalse();
    mountFileSystem.Exists(VfsPath.Parse("/usr/4/b")).Should().BeFalse();
    
    fileSystemMock1.Verify();
    fileSystemMock2.Verify();
    fileSystemMock3.Verify();
  }

  [Fact]
  public void Exists_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Exists(VfsPath.Parse("/save/1/a")))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("The specified path '/save/1/a' does not map to a mounted file system.");
  }

  [Fact]
  public void Open()
  {
    var expectedResult1 = Mock.Of<Stream>();
    var fileSystemStub1 = new Mock<IFileSystem>();
    fileSystemStub1
      .Setup(e => e.Open(VfsPath.Parse("/a"), FileMode.Open, FileAccess.Read, FileShare.Read))
      .Returns(expectedResult1);

    var expectedResult2 = Mock.Of<Stream>();
    var fileSystemStub2 = new Mock<IFileSystem>();
    fileSystemStub2
      .Setup(e => e.Open(VfsPath.Parse("/a/b"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
      .Returns(expectedResult2);

    var expectedResult3 = Mock.Of<Stream>();
    var fileSystemStub3 = new Mock<IFileSystem>();
    fileSystemStub3
      .Setup(e => e.Open(VfsPath.Parse("/4/b"), FileMode.Truncate, FileAccess.ReadWrite, FileShare.Inheritable))
      .Returns(expectedResult3);

    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Mount(VfsPath.Parse("/save/1/"), fileSystemStub1.Object);
    mountFileSystem.Mount(VfsPath.Parse("/save/2/"), fileSystemStub2.Object);
    mountFileSystem.Mount(VfsPath.Parse("/usr/"), fileSystemStub3.Object);

    mountFileSystem.Open(VfsPath.Parse("/save/1/a"), FileMode.Open, FileAccess.Read, FileShare.Read)
      .Should()
      .BeSameAs(expectedResult1);
    mountFileSystem.Open(VfsPath.Parse("/save/2/a/b"), FileMode.CreateNew, FileAccess.Write, FileShare.None)
      .Should()
      .BeSameAs(expectedResult2);
    mountFileSystem.Open(VfsPath.Parse("/usr/4/b"), FileMode.Truncate, FileAccess.ReadWrite, FileShare.Inheritable)
      .Should()
      .BeSameAs(expectedResult3);
  }

  [Fact]
  public void Open_InvalidPath_Throws()
  {
    var mountFileSystem = new MountFileSystem();

    mountFileSystem.Invoking(e => e.Open(VfsPath.Parse("/save/1/a"), FileMode.Open, FileAccess.Read, FileShare.Read))
      .Should()
      .Throw<InvalidOperationException>()
      .WithMessage("The specified path '/save/1/a' does not map to a mounted file system.");
  }
}

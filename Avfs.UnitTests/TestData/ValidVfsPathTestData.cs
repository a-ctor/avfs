namespace Avfs.UnitTests.TestData;

using System.Collections;
using static VfsPathTestRecordFlags;

public class ValidVfsPathTestData : IEnumerable<object[]>
{
  private static readonly object[][] s_testRecords =
  {
    New("/", IsDirectory | IsRoot, "", ""),
    New("/a", IsFile, "", "a"),
    New("/a/", IsDirectory, "a", ""),
    New("/a/b", IsFile, "a", "b"),
    New("/a/b/", IsDirectory, "b", ""),

    New("/.txt", IsFile | HasExtension, "", ".txt"),
    New("/a.txt", IsFile | HasExtension, "", "a.txt"),
    New("/a.b.txt", IsFile | HasExtension, "", "a.b.txt"),
    New("/c.d/.txt", IsFile | HasExtension, "c.d", ".txt"),
    New("/c.d/a.txt", IsFile | HasExtension, "c.d", "a.txt"),
    New("/c.d/a.b.txt", IsFile | HasExtension, "c.d", "a.b.txt")
  };

  public IEnumerator<object[]> GetEnumerator() => ((IEnumerable<object[]>)s_testRecords).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  private static object[] New(
    string pathText,
    VfsPathTestRecordFlags flags,
    string directoryName,
    string fileName)
  {
    return new object[]
    {
      new VfsPathTestRecord(pathText, flags, directoryName, fileName)
    };
  }
}

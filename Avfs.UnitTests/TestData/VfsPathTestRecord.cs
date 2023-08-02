namespace Avfs.UnitTests.TestData;

public record VfsPathTestRecord(
  string PathText,
  VfsPathTestRecordFlags Flags,
  string DirectoryName,
  string FileName)
{
  public string Extension
  {
    get
    {
      var lastDot = FileName.LastIndexOf('.');
      return lastDot >= 0
        ? FileName[lastDot..]
        : "";
    }
  }

  public string FileNameWithoutExtension
  {
    get
    {
      var lastDot = FileName.LastIndexOf('.');
      return lastDot >= 0
        ? FileName[..lastDot]
        : FileName;
    }
  }
  
  public bool HasFlag(VfsPathTestRecordFlags flags)
  {
    return (Flags & flags) == flags;
  }
}

namespace Avfs.UnitTests.TestData;

[Flags]
public enum VfsPathTestRecordFlags
{
  IsDirectory = 0x1,
  IsFile = 0x2,
  IsRoot = 0x4,
  HasExtension = 0x8
}

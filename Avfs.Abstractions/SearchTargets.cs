namespace Avfs;

[Flags]
public enum SearchTargets
{
  File = 1,
  Directory = 2,

  FileAndDirectory = File | Directory,
}

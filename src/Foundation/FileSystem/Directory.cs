using System;
using NodaTime;

namespace IOKode.OpinionatedFramework.FileSystem;

/// <summary>
/// A generic representation of a directory, intended to be extended for specific filesystems.
/// <remarks>
/// This class provides basic details such as the directory's name, its full path,
/// and its creation and update times.
/// </remarks>
/// </summary>
public class Directory
{
    /// <summary>
    /// The name of the directory, which includes only the name and excludes the path.
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// The full name of the directory, which includes its path within the filesystem.
    /// </summary>
    public virtual string FullName { get; set; }

    /// <summary>
    /// The point in time when the directory was originally created.
    /// </summary>
    public virtual Instant CreationTime { get; set; }

    /// <summary>
    /// The point in time when the directory was last updated or modified.
    /// </summary>
    public virtual Instant UpdateTime { get; set; }
}
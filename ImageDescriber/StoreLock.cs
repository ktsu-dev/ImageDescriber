// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.IO;

using ktsu.AppDataStorage;

/// <summary>
/// An exclusive, cross-process hold on the description store for the length of one run.
/// </summary>
/// <remarks>
/// Every run loads the whole store and saves its whole in-memory copy back, so two runs that write
/// at the same time each overwrite what the other added. A run that writes takes this lock first
/// and reloads the store under it; a second one fails fast instead of silently losing work.
/// </remarks>
internal sealed class StoreLock : IDisposable
{
	internal const string BusyMessage = "Another ImageDescriber run is updating the description store. Wait for it to finish and try again.";

	/// <summary>
	/// The file a run that writes to the store locks, kept beside the store itself.
	/// </summary>
	internal static string StorePath { get; set; } = Path.Combine(AppData.Path.WeakString, "ImageDescriber.lock");

	private readonly FileStream stream;

	private StoreLock(FileStream stream) => this.stream = stream;

	/// <summary>
	/// Takes the lock held by the file at <paramref name="path"/>, creating it if needed.
	/// </summary>
	/// <returns>The held lock, or <see langword="null"/> when another run holds it.</returns>
	internal static StoreLock? TryAcquire(string path)
	{
		string? directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory))
		{
			Directory.CreateDirectory(directory);
		}

		try
		{
			return new(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
		}
		catch (IOException)
		{
			return null;
		}
	}

	public void Dispose() => stream.Dispose();
}

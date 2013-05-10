/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

namespace CvsGitConverter
{
	/// <summary>
	/// The contents of a file.
	/// </summary>
	class FileContent
	{
		/// <summary>
		/// The file name.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The file's data
		/// </summary>
		public readonly FileContentData Data;

		/// <summary>
		/// Is this a file deletion?
		/// </summary>
		public bool IsDead
		{
			get { return Data.Length == 0; }
		}

		public FileContent(string path, FileContentData name)
		{
			this.Name = path;
			this.Data = name;
		}

		public static FileContent CreateDeadFile(string path)
		{
			return new FileContent(path, FileContentData.Empty);
		}
	}
}
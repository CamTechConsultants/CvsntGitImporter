/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

namespace CTC.CvsntGitImporter
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
		public readonly bool IsDead;

		public FileContent(string path, FileContentData data) : this(path, data, false)
		{
		}

		private FileContent(string path, FileContentData data, bool isDead)
		{
			this.Name = path;
			this.Data = data;
			this.IsDead = isDead;
		}

		public static FileContent CreateDeadFile(string path)
		{
			return new FileContent(path, FileContentData.Empty, true);
		}
	}
}
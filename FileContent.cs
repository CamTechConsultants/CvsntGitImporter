/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Text;

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

	/// <summary>
	/// The raw data for a revision of a file.
	/// </summary>
	class FileContentData
	{
		/// <summary>
		/// The data. The buffer may be longer than the file, so the Length field must be used.
		/// </summary>
		public readonly byte[] Data;

		/// <summary>
		/// The length of the file's contents.
		/// </summary>
		public readonly long Length;

		public FileContentData(byte[] data, long length)
		{
			Data = data;
			Length = length;
		}

		public override string ToString()
		{
			return Encoding.Default.GetString(Data, 0, (int)Math.Max(Length, 0x100));
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as FileContentData);
		}

		public bool Equals(FileContentData other)
		{
			if (other == null)
				return false;

			if (this.Length != other.Length)
				return false;

			if (this.Data == other.Data)
				return true;

			for (long i = 0; i < Length; i++)
			{
				if (this.Data[i] != other.Data[i])
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			int byteCount = (int)Math.Min(64, Length);
			int hashCode = 0;
			for (int i = 0; i < byteCount; i++)
			{
				hashCode ^= Data[i];
			}

			return hashCode;
		}

		/// <summary>
		/// An empty file.
		/// </summary>
		public static readonly FileContentData Empty = new FileContentData(new byte[0], 0);
	}
}
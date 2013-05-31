/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Text;

namespace CTC.CvsntGitImporter
{
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

		public FileContentData(byte[] data) : this(data, data.Length)
		{
		}

		public FileContentData(byte[] data, long length)
		{
			Data = data;
			Length = length;
		}

		public override string ToString()
		{
			return Encoding.Default.GetString(Data, 0, (int)Length);
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
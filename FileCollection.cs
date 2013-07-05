/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Collection of files.
	/// </summary>
	class FileCollection : IEnumerable<FileInfo>
	{
		private readonly Dictionary<string, FileInfo> m_files = new Dictionary<string, FileInfo>();

		public FileCollection(IEnumerable<FileInfo> files)
		{
			foreach (var f in files)
				m_files.Add(f.Name, f);
		}

		/// <summary>
		/// Get a file by its name.
		/// </summary>
		/// <exception cref="KeyNotFoundException">if the file is not in the collection</exception>
		public FileInfo this[string filename]
		{
			get { return m_files[filename]; }
		}

		public IEnumerator<FileInfo> GetEnumerator()
		{
			return m_files.Values.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

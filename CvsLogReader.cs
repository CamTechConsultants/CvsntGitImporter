/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	///
	/// </summary>
	class CvsLogReader : IEnumerable<string>
	{
		private readonly string m_filename;
		private int m_lineNumber;

		public int LineNumber
		{
			get { return m_lineNumber; }
		}

		public CvsLogReader(string filename)
		{
			m_filename = filename;
		}

		private IEnumerable<string> ReadLines()
		{
			m_lineNumber = 0;

			using (var reader = new StreamReader(m_filename, Encoding.UTF8))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					m_lineNumber++;
					yield return line;
				}
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			return ReadLines().GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Read a log file line by line, tracking the current line number.
	/// </summary>
	class CvsLogReader : IEnumerable<string>
	{
		private readonly string m_filename;
		private readonly TextReader m_reader;
		private int m_lineNumber;

		/// <summary>
		/// Gets the current line number.
		/// </summary>
		public int LineNumber
		{
			get { return m_lineNumber; }
		}

		public CvsLogReader(string filename)
		{
			m_filename = filename;
		}

		public CvsLogReader(TextReader reader)
		{
			m_reader = reader;
			m_filename = "<stream>";
		}

		private IEnumerable<string> ReadLines()
		{
			m_lineNumber = 0;

			TextReader reader = m_reader;
			bool mustDispose = false;
			if (reader == null)
			{
				reader = new StreamReader(m_filename, Encoding.Default);
				mustDispose = true;
			}

			try
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					m_lineNumber++;
					yield return line;
				}
			}
			finally
			{
				if (mustDispose)
					reader.Dispose();
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

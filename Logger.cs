/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Logging.
	/// </summary>
	class Logger : IDisposable
	{
		private bool m_isDisposed = false;
		private readonly TextWriter m_writer;

		private const int IndentCount = 2;
		private string m_currentIndent = "";
		private readonly string m_singleIndent = new string(' ', IndentCount);

		/// <summary>
		/// Initializes a new instance of the <see cref="Logger"/> class.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <exception cref="IOException">there was an error opening the log file</exception>
		public Logger(string filename)
		{
			try
			{
				m_writer = new StreamWriter(filename, false, Encoding.UTF8);
			}
			catch (System.Security.SecurityException se)
			{
				throw new IOException(se.Message, se);
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new IOException(uae.Message, uae);
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_isDisposed && disposing)
			{
				m_writer.Close();
			}

			m_isDisposed = true;
		}


		public void Indent()
		{
			m_currentIndent += m_singleIndent;
		}

		public void Outdent()
		{
			if (m_currentIndent.Length > 0)
				m_currentIndent = m_currentIndent.Substring(0, m_currentIndent.Length - 2);
		}

		public void WriteLine()
		{
			m_writer.WriteLine();
		}

		public void WriteLine(string line)
		{
			m_writer.Write(m_currentIndent);
			m_writer.WriteLine(line);
		}

		public void WriteLine(string format, params object[] args)
		{
			m_writer.Write(m_currentIndent);
			m_writer.WriteLine(format, args);
		}

		public void RuleOff()
		{
			m_writer.WriteLine("-------------------------------------------------------------------------------");
		}

		public void DoubleRuleOff()
		{
			m_writer.WriteLine("===============================================================================");
		}
	}
}

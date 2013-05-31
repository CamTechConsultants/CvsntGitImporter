/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Application logging.
	/// </summary>
	interface ILogger
	{
		/// <summary>
		/// Are debug log files enabled?
		/// </summary>
		bool DebugEnabled { get; set; }

		/// <summary>
		/// Increase the indent for any following entries. When the returned object is disposed, the indent
		/// is removed.
		/// </summary>
		IDisposable Indent();

		/// <summary>
		/// Draw a double line in the log file
		/// </summary>
		void DoubleRuleOff();

		/// <summary>
		/// Draw a single line in the log file
		/// </summary>
		void RuleOff();

		/// <summary>
		/// Write a blank line.
		/// </summary>
		void WriteLine();

		/// <summary>
		/// Write a line of text.
		/// </summary>
		void WriteLine(string line);

		/// <summary>
		/// Write a line of text.
		/// </summary>
		void WriteLine(string format, params object[] args);

		/// <summary>
		/// Write a debug log file. If DebugEnabled is false then nothing is written.
		/// </summary>
		void WriteDebugFile(string filename, IEnumerable<string> lines);

		/// <summary>
		/// Open a debug log file. If DebugEnabled is false then nothing is written.
		/// </summary>
		TextWriter OpenDebugFile(string filename);
	}
}

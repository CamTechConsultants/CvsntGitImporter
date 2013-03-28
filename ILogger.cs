/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CvsGitConverter
{
	/// <summary>
	/// Applicatio logging.
	/// </summary>
	interface ILogger
	{
		/// <summary>
		/// Increase the indent for any following entries.
		/// </summary>
		void Indent();

		/// <summary>
		/// Decrease the indent for any following entries.
		/// </summary>
		void Outdent();
		
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
	}
}

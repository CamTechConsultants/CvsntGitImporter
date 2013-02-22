/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	/// <summary>
	///
	/// </summary>
	class CvsLogParser
	{
		private const string LogSeparator = "----------------------------";
		private const string FileSeparator = "=============================================================================";
		private readonly CvsLogReader m_reader;

		public CvsLogParser(string logFile)
		{
			m_reader = new CvsLogReader(logFile);
		}

		public IEnumerable<Commit> Parse()
		{
			var state = State.Start;
			string currentFile = null;
			string revision = null;
			Commit commit = null;

			foreach (var line in m_reader)
			{
				switch (state)
				{
					case State.Start:
						if (line.StartsWith("Working file: "))
						{
							currentFile = line.Substring(14);
							state = State.InFileHeader;
						}
						break;
					case State.InFileHeader:
						if (line == LogSeparator)
							state = State.ExpectCommitRevision;
						break;
					case State.ExpectCommitRevision:
						if (line.StartsWith("revision "))
						{
							revision = line.Substring(9);
							state = State.ExpectCommitInfo;
						}
						else
						{
							throw MakeParseException("Expected revision line, found '{0}'", line);
						}
						break;
					case State.ExpectCommitInfo:
						var match = Regex.Match(line, @"date: (\d{4}/\d\d/\d\d \d\d:\d\d:\d\d);  author: (\S+?);.*  commitid: (\S+?);");
						if (!match.Success)
							throw MakeParseException("Invalid commit info line: '{0}'", line);
						var time = DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
						commit = new Commit(currentFile, revision, time, match.Groups[2].Value, match.Groups[3].Value);
						state = State.ExpectCommitMessage;
						break;
					case State.ExpectCommitMessage:
						if (line == LogSeparator)
						{
							yield return commit;
							state = State.ExpectCommitRevision;
						}
						else if (line == FileSeparator)
						{
							yield return commit;
							state = State.Start;
						}
						else
						{
							commit.AddMessage(line);
						}
						break;
				}
			}
		}

		private ParseException MakeParseException(string format, params object[] args)
		{
			return new ParseException(String.Format("Line {0}: {1}", m_reader.LineNumber, String.Format(format, args)));
		}

		private enum State
		{
			Start = 0,
			InFileHeader,
			ExpectCommitRevision,
			ExpectCommitInfo,
			ExpectCommitMessage,
		}
	}
}

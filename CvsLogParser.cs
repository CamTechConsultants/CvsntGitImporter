/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	/// <summary>
	/// CVS log file parser.
	/// </summary>
	class CvsLogParser
	{
		private const string LogSeparator = "----------------------------";
		private const string FileSeparator = "=============================================================================";

		private readonly CvsLogReader m_reader;
		private readonly List<FileInfo> m_files = new List<FileInfo>();

		public CvsLogParser(string logFile)
		{
			m_reader = new CvsLogReader(logFile);
		}

		/// <summary>
		/// Gets a list of all the files.
		/// </summary>
		public IEnumerable<FileInfo> Files
		{
			get { return m_files; }
		}

		/// <summary>
		/// Parse the log returning a list of the individual commits to the individual files.
		/// </summary>
		public IEnumerable<FileRevision> Parse()
		{
			var state = State.Start;
			FileInfo currentFile = null;
			Revision revision = Revision.Empty;
			FileRevision commit = null;

			foreach (var line in m_reader)
			{
				switch (state)
				{
					case State.Start:
						if (line.StartsWith("Working file: "))
						{
							currentFile = new FileInfo(line.Substring(14));
							m_files.Add(currentFile);
							state = State.InFileHeader;
						}
						break;
					case State.InFileHeader:
						if (line == LogSeparator)
							state = State.ExpectCommitRevision;
						else if (line == "symbolic names:")
							state = State.InTags;
						break;
					case State.InTags:
						if (!line.StartsWith("\t"))
						{
							state = State.InFileHeader;
						}
						else
						{
							var tagMatch = Regex.Match(line, @"^\t(\S+): (\S+)");
							if (!tagMatch.Success)
								throw MakeParseException("Invalid tag line: '{0}'", line);
							currentFile.AddTag(tagMatch.Groups[1].Value, Revision.Create(tagMatch.Groups[2].Value));
						}
						break;
					case State.ExpectCommitRevision:
						if (line.StartsWith("revision "))
						{
							revision = Revision.Create(line.Substring(9));
							state = State.ExpectCommitInfo;
						}
						else
						{
							throw MakeParseException("Expected revision line, found '{0}'", line);
						}
						break;
					case State.ExpectCommitInfo:
						var match = Regex.Match(line, @"date: (\d{4}/\d\d/\d\d \d\d:\d\d:\d\d);  author: (\S+?);.*  commitid: (\S+?);  (?:mergepoint: (\S+);)?");
						if (!match.Success)
							throw MakeParseException("Invalid commit info line: '{0}'", line);

						var time = DateTime.ParseExact(match.Groups[1].Value, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
						var mergepoint = match.Groups[4].Value.Length == 0 ? Revision.Empty : Revision.Create(match.Groups[4].Value);

						commit = new FileRevision(file: currentFile, revision: revision, mergepoint: mergepoint, time: time,
								author: match.Groups[2].Value, commitId: match.Groups[3].Value);

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
						else if (!line.StartsWith("branches:  "))
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
			InTags,
			ExpectCommitRevision,
			ExpectCommitInfo,
			ExpectCommitMessage,
		}
	}
}

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
		private readonly DateTime m_startDate;
		private readonly List<FileInfo> m_files = new List<FileInfo>();

		public CvsLogParser(string logFile, DateTime startDate)
		{
			m_reader = new CvsLogReader(logFile);
			m_startDate = startDate;
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
						var infoPattern = @"date: (?<date>\d{4}/\d\d/\d\d \d\d:\d\d:\d\d);  author: (?<author>\S+?);  " +
										  @"state: (?<state>\S+?);.*  commitid: (?<commitid>\S+?);  " +
										  @"(?:mergepoint: (?<mergepoint>\S+);)?";
						var match = Regex.Match(line, infoPattern);
						if (!match.Success)
							throw MakeParseException("Invalid commit info line: '{0}'", line);

						var time = DateTime.ParseExact(match.Groups["date"].Value, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
						var mergepointStr = match.Groups["mergepoint"].Value;
						var mergepoint = mergepointStr.Length == 0 ? Revision.Empty : Revision.Create(mergepointStr);

						if (time >= m_startDate)
						{
							var commitId = match.Groups["commitid"].Value;
							if (commitId.Length == 0)
								throw MakeParseException("Commit is missing a commit id: '{0}'", line);

							commit = new FileRevision(
									file: currentFile,
									revision: revision,
									mergepoint: mergepoint,
									time: time,
									author: match.Groups["author"].Value,
									commitId: commitId,
									isDead: match.Groups["state"].Value == "dead");
						}
						else
						{
							// too early
							commit = null;
						}

						state = State.ExpectCommitMessage;
						break;
					case State.ExpectCommitMessage:
						if (line == LogSeparator)
						{
							if (commit != null)
								yield return commit;
							state = State.ExpectCommitRevision;
						}
						else if (line == FileSeparator)
						{
							if (commit != null)
								yield return commit;
							state = State.Start;
						}
						else if (!line.StartsWith("branches:  "))
						{
							if (commit != null)
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

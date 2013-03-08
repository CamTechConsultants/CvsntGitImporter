/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
		private static readonly char[] FieldDelimiter = new[] { ';' };

		private readonly CvsLogReader m_reader;
		private readonly DateTime m_startDate;
		private readonly List<FileInfo> m_files = new List<FileInfo>();

		public CvsLogParser(string logFile, DateTime startDate)
		{
			m_reader = new CvsLogReader(logFile);
			m_startDate = startDate;
		}

		public CvsLogParser(TextReader reader, DateTime startDate)
		{
			m_reader = new CvsLogReader(reader);
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
						commit = ParseFields(currentFile, revision, line);
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

		/// <summary>
		/// Parse a line of the CVS log containing data about a commit.
		/// </summary>
		/// <returns>The commit, or null if the commit is to be ignored</returns>
		private FileRevision ParseFields(FileInfo currentFile, Revision revision, string line)
		{
			var fields = line.Split(FieldDelimiter, StringSplitOptions.RemoveEmptyEntries);
			string author = null;
			string commitId = null;
			string dateStr = null;
			string mergepointStr = null;
			string state = null;

			foreach (var field in fields)
			{
				var separator = field.IndexOf(':');
				if (separator <= 0 || separator >= field.Length - 1)
					throw MakeParseException("Invalid field: '{0}'", field);

				var key = field.Remove(separator).Trim();
				var value = field.Substring(separator + 1).Trim();
				switch (key)
				{
					case "author": author = value; break;
					case "commitid": commitId = value; break;
					case "date": dateStr = value; break;
					case "mergepoint": mergepointStr = value; break;
					case "state": state = value; break;
				}
			}

			var time = DateTime.ParseExact(dateStr, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
			var mergepoint = mergepointStr == null ? Revision.Empty : Revision.Create(mergepointStr);

			if (time >= m_startDate)
			{
				if (commitId == null)
					throw MakeParseException("Commit is missing a commit id: '{0}'", line);

				return new FileRevision(
						file: currentFile,
						revision: revision,
						mergepoint: mergepoint,
						time: time,
						author: author,
						commitId: commitId,
						isDead: state == "dead");
			}
			else
			{
				// too early
				return null;
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

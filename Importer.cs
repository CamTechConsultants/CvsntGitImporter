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
	/// Run the import into git.
	/// </summary>
	class Importer : IDisposable
	{
		private static readonly Encoding m_encoding = Encoding.UTF8;
		private static readonly byte[] m_newLine = m_encoding.GetBytes("\n");

		private readonly ILogger m_log;
		private readonly Switches m_switches;
		private readonly UserMap m_userMap;
		private readonly BranchStreamCollection m_branches;
		private readonly IDictionary<string, Commit> m_tags;
		private readonly Cvs m_cvs;
		private readonly CommitPlayer m_player;
		private readonly Stream m_stream;

		private bool m_isDisposed = false;

		public Importer(ILogger log, Switches switches, UserMap userMap, BranchStreamCollection branches,
				IDictionary<string, Commit> tags, Cvs cvs)
		{
			m_log = log;
			m_switches = switches;
			m_userMap = userMap;
			m_branches = branches;
			m_tags = tags;
			m_cvs = cvs;
			m_player = new CommitPlayer(log, branches);

			m_stream = new FileStream("import.dat", FileMode.Create, FileAccess.Write);
		}


		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_isDisposed && disposing)
			{
				if (m_stream != null)
					m_stream.Close();
			}

			m_isDisposed = true;
		}


		public void Import()
		{
			m_log.DoubleRuleOff();
			m_log.WriteLine("Importing");

			bool printProgress = !Console.IsOutputRedirected;
			int totalCommits = 0;
			if (printProgress)
				totalCommits = m_player.Count;

			using (m_log.Indent())
			{
				int count = 0;
				foreach (var commit in m_player.Play())
				{
					Import(commit);

					if (printProgress)
						Console.Out.Write("\rProcessed {0} of {1} commits ({2}%)", ++count, totalCommits, count * 100 / totalCommits);
				}

				if (printProgress)
					Console.Out.WriteLine();

				ImportTags();
			}
		}

		private void Import(Commit commit)
		{
			var renamedBranch = m_switches.BranchRename.Process(commit.Branch);
			var author = m_userMap.GetUser(commit.Author);

			m_log.WriteLine("Commit {0}/{1}  branch={2} author={3} when={4}{5}", commit.CommitId, commit.Index,
					renamedBranch, commit.Author, commit.Time,
					(commit.MergeFrom == null) ? "" : String.Format(" mergefrom={0}/{1}", commit.MergeFrom.CommitId, commit.MergeFrom.Index));

			WriteLine("commit refs/heads/{0}", (commit.Branch == "MAIN") ? "master" : renamedBranch);
			WriteLine("mark :{0}", commit.Index);
			WriteLine("committer {0} {1}", WriteUser(author), DateTimeToUnixTimestamp(commit.Time));

			var msgBytes = GetBytes(commit.Message);
			WriteLine("data {0}", msgBytes.Length);
			WriteLine(msgBytes);

			if (commit.Predecessor != null)
				WriteLine("from :{0}", commit.Predecessor.Index);

			if (commit.MergeFrom != null)
				WriteLine("merge :{0}", commit.MergeFrom.Index);

			foreach (var file in m_cvs.GetCommit(commit))
			{
				if (file.IsDead)
				{
					WriteLine("D {0}", file.Name);
				}
				else
				{
					WriteLine("M 644 inline {0}", file.Name);
					WriteData(file.Data);
				}
			}

			WriteLine("");
		}

		private void ImportTags()
		{
			var renamer = m_switches.TagRename;

			foreach (var kvp in m_tags)
			{
				// ignore tags that are on branches that we're not importing
				var commit = kvp.Value;
				if (m_branches[commit.Branch] == null)
					continue;

				var tagName = renamer.Process(kvp.Key);
				m_log.WriteLine("Tag {0}: {1}/{2}", tagName, commit.CommitId, commit.Index);

				WriteLine("tag {0}", tagName);
				WriteLine("from :{0}", commit.Index);
				WriteLine("tagger {0} {1}", WriteUser(m_switches.Nobody), DateTimeToUnixTimestamp(commit.Time));
				WriteData(FileContentData.Empty);
			}
		}

		private string WriteUser(User user)
		{
			return String.Format("{0} <{1}>", user.Name, user.Email);
		}

		private static string DateTimeToUnixTimestamp(DateTime dateTime)
		{
			return String.Format("{0} +0000", (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds);
		}

		private void WriteLine(string format, params object[] args)
		{
			var line = String.Format(format, args);
			var bytes = GetBytes(line);
			WriteLine(bytes);
		}

		private void WriteLine(byte[] bytes)
		{
			m_stream.Write(bytes, 0, bytes.Length);
			m_stream.Write(m_newLine, 0, m_newLine.Length);
		}

		private void WriteData(FileContentData data)
		{
			if (data.Length > int.MaxValue)
				throw new NotSupportedException("Import cannot currently cope with files larger than 2 GB");

			WriteLine("data {0}", data.Length);

			m_stream.Write(data.Data, 0, (int)data.Length);
			m_stream.Write(m_newLine, 0, m_newLine.Length);
		}

		private byte[] GetBytes(string text)
		{
			return m_encoding.GetBytes(text);
		}
	}
}
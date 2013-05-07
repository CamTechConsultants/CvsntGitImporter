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
	/// Run the import into git.
	/// </summary>
	class Importer : IDisposable
	{
		private readonly ILogger m_log;
		private readonly Cvs m_cvs;
		private readonly CommitPlayer m_player;
		private readonly Stream m_stream;
		private static readonly Encoding m_encoding = Encoding.UTF8;
		private static readonly byte[] m_newLine = m_encoding.GetBytes("\n");

		private bool m_isDisposed = false;

		public Importer(ILogger log, BranchStreamCollection branches, Cvs cvs)
		{
			m_log = log;
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
						Console.Out.Write("\rProcessed {0} of {1} commits ({2}%)", count++, totalCommits, count * 100 / totalCommits);
				}

				if (printProgress)
					Console.Out.WriteLine();
			}
		}

		private void Import(Commit commit)
		{
			m_log.WriteLine("Commit {0}/{1}  branch={2} author={3} when={4}{5}", commit.CommitId, commit.Index,
					commit.Branch, commit.Author, commit.Time,
					(commit.MergeFrom == null) ? "" : String.Format(" mergefrom={0}/{1}", commit.MergeFrom.CommitId, commit.MergeFrom.Index));

			WriteLine("commit refs/heads/{0}", (commit.Branch == "MAIN") ? "master" : commit.Branch);
			WriteLine("mark :{0}", commit.Index);
			WriteLine("committer {0} <{0}@ctg.local> {1} +0000", commit.Author, DateTimeToUnixTimestamp(commit.Time));

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

		public static double DateTimeToUnixTimestamp(DateTime dateTime)
		{
			return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
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
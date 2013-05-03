/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Run the import into git.
	/// </summary>
	class Importer : IDisposable
	{
		private readonly ILogger m_log;
		private readonly CommitPlayer m_player;
		private readonly Stream m_stream;
		private static readonly Encoding m_encoding = Encoding.UTF8;
		private static readonly byte[] m_newLine = m_encoding.GetBytes("\n");

		private bool m_isDisposed = false;

		public Importer(ILogger log, BranchStreamCollection branches)
		{
			m_log = log;
			m_player = new CommitPlayer(log, branches);

			m_stream = Console.OpenStandardOutput();
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
			foreach (var commit in m_player.Play())
			{
				Import(commit);
			}
		}

		private void Import(Commit commit)
		{
			m_log.WriteLine("Commit {0}  branch={1} author={2} when={3}", commit.CommitId, commit.Branch, commit.Author, commit.Time);

			WriteLine("commit refs/heads/{0}", (commit.Branch == "MAIN") ? "master" : commit.Branch);
			WriteLine("mark :{0}", commit.Index);
			WriteLine("committer {0} <{0}@ctg.local> {1}", commit.Author, commit.Time.ToString("r"));

			var msgBytes = GetBytes(commit.Message);
			WriteLine("data {0}", msgBytes.Length);
			WriteLine(msgBytes);

			if (commit.Predecessor != null)
				WriteLine("from :{0}", commit.Predecessor.Index);

			WriteLine("M 644 inline file.txt");
			var content = GetBytes(String.Format("mark {0}\r\nblah\r\n", commit.Index));
			WriteLine("data {0}", content.Length);
			WriteLine(content);

			WriteLine("");
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

		private byte[] GetBytes(string text)
		{
			return m_encoding.GetBytes(text);
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CTC.CvsntGitImporter.Win32;

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
		private readonly IConfig m_config;
		private readonly UserMap m_userMap;
		private readonly BranchStreamCollection m_branches;
		private readonly IDictionary<string, Commit> m_tags;
		private readonly Cvs m_cvs;
		private readonly CommitPlayer m_player;
		private GitRepo m_git;
		private Stream m_stream;
		private bool m_brokenPipe;

		private bool m_isDisposed = false;

		public Importer(ILogger log, IConfig config, UserMap userMap, BranchStreamCollection branches,
				IDictionary<string, Commit> tags, Cvs cvs)
		{
			m_log = log;
			m_config = config;
			m_userMap = userMap;
			m_branches = branches;
			m_tags = tags;
			m_cvs = cvs;
			m_player = new CommitPlayer(log, branches);
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
			m_stream = OpenOutput();

			try
			{
				bool printProgress = !Console.IsOutputRedirected;
				int totalCommits = m_player.Count;
				var progress = new ImportProgress(totalCommits);

				var stopWatch = new Stopwatch();
				stopWatch.Start();

				using (m_log.Indent())
				{
					int count = 0;
					Commit lastMainCommit = null;
					foreach (var commit in m_player.Play())
					{
						ImportCommit(commit);

						if (commit.Branch == "MAIN")
							lastMainCommit = commit;

						count++;
						if (printProgress)
							progress.Update(stopWatch.Elapsed, count);
					}

					if (printProgress)
						Console.Out.WriteLine();

					ImportTags();

					if (lastMainCommit != null && m_config.MarkerTag != null)
						ImportTag(m_config.MarkerTag, lastMainCommit);
				}

				m_log.WriteLine();
				m_log.WriteLine("Imported {0} commits", totalCommits);
				m_log.WriteLine("Imported {0} tags", m_tags.Count);
				m_log.WriteLine();
			}
			catch (IOException ioe)
			{
				// if the error is broken pipe, then catch the exception - we should get an error in
				// GitRepo.EndImport in the finally block below
				if ((ioe.HResult & 0xffff) == (int)WinError.BrokenPipe)
					m_brokenPipe = true;
				else
					throw;
			}
			finally
			{
				CloseOutput();
			}
		}

		private Stream OpenOutput()
		{
			m_brokenPipe = false;

			if (m_config.GitDir == null)
			{
				return new FileStream("import.dat", FileMode.Create, FileAccess.Write);
			}
			else
			{
				m_git = new GitRepo(m_log, m_config.GitDir);
				m_git.Init(m_config.GitConfig);
				return m_git.StartImport();
			}
		}

		private void CloseOutput()
		{
			if (m_config.GitDir == null)
			{
				m_stream.Close();
			}
			else
			{
				try
				{
					m_git.EndImport();
				}
				catch (IOException ioe)
				{
					Console.Error.WriteLine();
					Console.Error.WriteLine(ioe.Message);

					m_log.DoubleRuleOff();
					m_log.WriteLine(ioe.Message);

					throw new ImportFailedException("Git reported an error during the import");
				}

				// this should not occur - if the stdin pipe broke, it implies that git fast-import
				// exited prematurely, which means we should have had an error from it above
				if (m_brokenPipe)
					throw new ImportFailedException("Git process exited prematurely");
			}
		}

		private void ImportCommit(Commit commit)
		{
			var renamedBranch = m_config.BranchRename.Process(commit.Branch);
			var author = m_userMap.GetUser(commit.Author);

			m_log.WriteLine("Commit {0}/{1}  branch={2} author={3} when={4}{5}", commit.CommitId, commit.Index,
					renamedBranch, commit.Author, commit.Time,
					(commit.MergeFrom == null) ? "" : String.Format(" mergefrom={0}/{1}", commit.MergeFrom.CommitId, commit.MergeFrom.Index));

			WriteLine("commit refs/heads/{0}", (commit.Branch == "MAIN") ? "master" : renamedBranch);
			WriteLine("mark :{0}", commit.Index);
			WriteLine("committer {0} {1}", WriteUser(author), UnixTime.FromDateTime(commit.Time));

			var msgBytes = GetBytes(commit.Message);
			WriteLine("data {0}", msgBytes.Length);
			WriteLine(msgBytes);

			if (commit.Predecessor != null)
				WriteLine("from :{0}", commit.Predecessor.Index);

			if (commit.MergeFrom != null)
				WriteLine("merge :{0}", commit.MergeFrom.Index);

			foreach (var cvsFile in m_cvs.GetCommit(commit))
			{
				FileContent file;
				if (CvsIgnoreFile.IsIgnoreFile(cvsFile))
					file = CvsIgnoreFile.Rewrite(cvsFile);
				else
					file = cvsFile;

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
			var renamer = m_config.TagRename;

			foreach (var kvp in m_tags)
			{
				// ignore tags that are on branches that we're not importing
				var commit = kvp.Value;
				if (m_branches[commit.Branch] == null)
					continue;

				var tagName = renamer.Process(kvp.Key);
				ImportTag(tagName, commit);
			}
		}

		private void ImportTag(string tagName, Commit commit)
		{
			m_log.WriteLine("Tag {0}: {1}/{2}", tagName, commit.CommitId, commit.Index);

			WriteLine("tag {0}", tagName);
			WriteLine("from :{0}", commit.Index);
			WriteLine("tagger {0} {1}", WriteUser(m_config.Nobody), UnixTime.FromDateTime(commit.Time));
			WriteData(FileContentData.Empty);
		}

		private string WriteUser(User user)
		{
			return String.Format("{0} <{1}>", user.Name, user.Email);
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
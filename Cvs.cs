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
using System.Threading.Tasks;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// CVS interface
	/// </summary>
	class Cvs
	{
		private readonly ICvsRepository m_repository;
		private readonly int m_cvsProcessCount;

		public const int MaxProcessCount = 128;

		public Cvs(ICvsRepository repository, uint cvsProcessCount)
		{
			if (cvsProcessCount < 1 || cvsProcessCount > MaxProcessCount)
				throw new ArgumentOutOfRangeException("cvsProcessCount");

			m_repository = repository;
			m_cvsProcessCount = (int)cvsProcessCount;
		}

		/// <summary>
		/// Get the files in a commit from the repository.
		/// </summary>
		public IEnumerable<FileContent> GetCommit(Commit commit)
		{
			var liveFiles = new Queue<FileRevision>(commit.Where(f => !f.IsDead));

			var taskCount = Math.Min(liveFiles.Count, m_cvsProcessCount);
			var tasks = new List<Task<FileContent>>(taskCount);

			// start async tasks off
			for (int i = 0; i < taskCount; i++)
			{
				tasks.Add(StartNextFile(liveFiles));
			}

			// now return all dead files
			foreach (var f in commit.Where(f => f.IsDead))
			{
				yield return FileContent.CreateDeadFile(f.File.Name);
			}

			// wait for tasks to complete and start new ones as they do
			while (tasks.Count > 0)
			{
				int taskIndex = Task.WaitAny(tasks.ToArray());
				var completedTask = tasks[taskIndex];
				tasks.RemoveAt(taskIndex);

				if (liveFiles.Any())
					tasks.Add(StartNextFile(liveFiles));

				yield return completedTask.Result;
			}
		}

		private Task<FileContent> StartNextFile(Queue<FileRevision> q)
		{
			var f = q.Dequeue();
			return Task<FileContent>.Factory.StartNew(() => m_repository.GetCvsRevision(f));
		}


		/// <summary>
		/// Download the log for an entire repository.
		/// </summary>
		public static void DownloadCvsLog(string cvsLogFile, string sandbox)
		{
			var module = ReadModuleName(sandbox);

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "cmd.exe",
					Arguments = String.Format("/C cvs rlog \"{0}\" > \"{1}\"", module, cvsLogFile),
					UseShellExecute = false,
					RedirectStandardError = true,
					StandardErrorEncoding = Encoding.Default,
					CreateNoWindow = true,
				},
			};

			var error = new StringBuilder();
			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					error.Append(e.Data);
			};

			process.Start();

			process.BeginErrorReadLine();
			process.WaitForExit();

			if (error.Length > 0)
				throw new ImportFailedException(String.Format("Failed to get CVS log: {0}", error));
			else if (process.ExitCode != 0)
				throw new ImportFailedException(String.Format("Failed to get CVS log: cvs exited with exit code {0}", process.ExitCode));
		}

		private static string ReadModuleName(string sandbox)
		{
			var repoPath = Path.Combine(sandbox, @"CVS\Repository");
			return File.ReadAllText(repoPath).Trim();
		}
	}
}
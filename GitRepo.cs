/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Represents a Git repository.
	/// </summary>
	class GitRepo
	{
		private readonly ILogger m_log;
		private readonly string m_dir;
		private readonly StringBuilder m_stderr = new StringBuilder();
		private Process m_importProcess;

		public GitRepo(ILogger log, string directory)
		{
			m_log = log;
			m_dir = directory;
		}

		/// <summary>
		/// Initialise the repository.
		/// </summary>
		/// <exception cref="IOException">there was an error creating the repository</exception>
		public void Init(IEnumerable<GitConfigOption> options)
		{
			try
			{
				if (!Directory.Exists(m_dir))
					Directory.CreateDirectory(m_dir);

				RunGitProcess("Git init", "init --bare");

				foreach (var option in options)
				{
					var name = EscapeConfigValue(option.Name);
					var value = EscapeConfigValue(option.Value);
					var addString = option.Add ? " --add" : "";

					RunGitProcess("Git config", String.Format("config{0} \"{1}\" \"{2}\"", addString, name, value));
				}
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new IOException(uae.Message, uae);
			}
			catch (Win32Exception w32e)
			{
				throw new IOException(w32e.Message, w32e);
			}
		}

		/// <summary>
		/// Start git fast-import.
		/// </summary>
		/// <returns>a stream which import data can be written to</returns>
		/// <exception cref="IOException">there was an error starting the process</exception>
		public Stream StartImport()
		{
			if (m_importProcess != null)
				throw new InvalidOperationException("Import already underway");

			try
			{
				var git = CreateGitProcess("fast-import");
				git.StartInfo.RedirectStandardInput = true;

				git.ErrorDataReceived += (_, e) =>
				{
					if (e.Data != null)
					{
						m_stderr.Append(e.Data);
						m_stderr.AppendLine();
					}
				};

				git.Start();
				git.BeginErrorReadLine();

				m_importProcess = git;
				return git.StandardInput.BaseStream;
			}
			catch (Win32Exception w32e)
			{
				throw new IOException(w32e.Message, w32e);
			}
		}

		/// <summary>
		/// End an import, waiting for the git process to exit.
		/// </summary>
		/// <exception cref="IOException">there was an error doing the import</exception>
		public void EndImport()
		{
			var process = m_importProcess;
			if (process == null)
				return;

			try
			{
				process.StandardInput.Close();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					if (m_stderr.Length == 0)
						throw new IOException(String.Format("Git import failed with exit code {0}", process.ExitCode));
					else
						throw new IOException(String.Format("Git import failed: {0}", m_stderr));
				}
			}
			finally
			{
				m_importProcess = null;
				m_stderr.Clear();
			}
		}

		/// <summary>
		/// Repack a repository.
		/// </summary>
		public void Repack()
		{
			RunGitProcess("Repack", "repack -f -a -d --depth=250 --window=250");
		}


		private Process CreateGitProcess(string arguments)
		{
			return new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "git.exe",
					Arguments = arguments,
					WorkingDirectory = m_dir,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};
		}

		private void RunGitProcess(string description, string arguments)
		{
			m_log.WriteLine("{0}: Command line: git {1}", description, arguments);
			var errorOutput = new StringBuilder();

			var git = CreateGitProcess(arguments);
			git.OutputDataReceived += (_, e) =>
			{
				if (e.Data != null)
					m_log.WriteLine(e.Data.TrimEnd());
			};
			git.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					errorOutput.Append(e.Data);
			};

			git.Start();
			git.BeginOutputReadLine();
			git.BeginErrorReadLine();
			git.WaitForExit();

			if (git.ExitCode != 0)
			{
				if (errorOutput.Length > 0)
					throw new IOException(String.Format("{0} failed: {1}", description, errorOutput));
				else
					throw new IOException(String.Format("{0} failed with exit code {1}", description, git.ExitCode));
			}
		}

		private static string EscapeConfigValue(string x)
		{
			return x.Replace("\\", @"\\").Replace("\"", @"\""");
		}
	}
}
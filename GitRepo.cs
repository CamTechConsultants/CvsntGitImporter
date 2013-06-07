/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
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
		private readonly string m_dir;
		private readonly StringBuilder m_stderr = new StringBuilder();
		private Process m_importProcess;

		public GitRepo(string directory)
		{
			m_dir = directory;
		}

		/// <summary>
		/// Initialise the repository.
		/// </summary>
		/// <exception cref="IOException">there was an error creating the repository</exception>
		public void Init()
		{
			try
			{
				if (!Directory.Exists(m_dir))
					Directory.CreateDirectory(m_dir);

				var startInfo = new ProcessStartInfo()
				{
					FileName = "git.exe",
					Arguments = "init --bare",
					WorkingDirectory = m_dir,
					UseShellExecute = false,
					RedirectStandardError = true,
				};

				var git = Process.Start(startInfo);
				var errorOutput = git.StandardError.ReadToEnd();
				git.WaitForExit();

				if (git.ExitCode != 0)
				{
					if (errorOutput.Length > 0)
						throw new IOException(String.Format("Git init failed: {0}", errorOutput));
					else
						throw new IOException(String.Format("Git init failed with exit code {0}", git.ExitCode));
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
				var git = new Process();
				git.StartInfo = new ProcessStartInfo()
				{
					FileName = "git.exe",
					Arguments = "fast-import",
					WorkingDirectory = m_dir,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardError = true,
				};

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
	}
}
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

namespace CvsGitConverter
{
	/// <summary>
	/// CVS interface
	/// </summary>
	class CvsRepository : ICvsRepository
	{
		private readonly string m_sandboxPath;
		private readonly Task m_ensureAllDirectories;

		public CvsRepository(string sandboxPath)
		{
			m_sandboxPath = sandboxPath;
			
			// start the CVS update command that ensures that all empty directories are created
			m_ensureAllDirectories = EnsureAllDirectories();
		}

		public FileContent GetCvsRevision(FileRevision f)
		{
			m_ensureAllDirectories.Wait();

			var data = InvokeCvs("-f", "-Q", "update", "-p", "-r" + f.Revision.ToString(), f.File.Name);
			return new FileContent(f.File.Name, data);
		}

		/// <summary>
		/// Create all directories, including empty ones.
		/// </summary>
		private async Task EnsureAllDirectories()
		{
			await Task.Factory.StartNew(() => InvokeCvs("-f", "-Q", "update", "-d"));
		}

		private FileContentData InvokeCvs(params string[] args)
		{
			var quotedArguments = String.Join(" ", args.Select(a => a.Contains(' ') ? String.Format("\"{0}\"", a) : a));

			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "cvs.exe",
					Arguments = quotedArguments,
					WorkingDirectory = m_sandboxPath,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					StandardOutputEncoding = Encoding.Default,
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

			using (var buf = new MemoryStream())
			{
				process.StandardOutput.BaseStream.CopyTo(buf);

				process.BeginErrorReadLine();
				process.WaitForExit();

				if (error.Length > 0)
					throw new CvsException(String.Format("CVS call failed: {0}", error));
				else if (process.ExitCode != 0)
					throw new CvsException(String.Format("CVS exited with exit code {0}", process.ExitCode));
				else
					return new FileContentData(buf.GetBuffer(), buf.Length);
			}
		}
	}
}
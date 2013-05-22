/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Diagnostics;
using System.IO;
using IOPath = System.IO.Path;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Create and delete a temporary directory using RAII.
	/// </summary>
	public class TempDir : IDisposable
	{
		/// <summary>
		/// Creates a new temporary directory.
		/// </summary>
		/// <exception cref="IOException">if there is an error creating the directory</exception>
		public TempDir() : this(String.Empty)
		{
		}

		/// <summary>
		/// Creates a new temporary directory.
		/// </summary>
		/// <param name="prefix">a prefix to give to the directory's name</param>
		/// <exception cref="IOException">if there is an error creating the directory</exception>
		public TempDir(string prefix)
		{
			try
			{
				do
				{
					string filename = (String.IsNullOrEmpty(prefix)) ? "" : prefix + ".";
					filename += IOPath.GetRandomFileName();
					Info = new DirectoryInfo(IOPath.Combine(IOPath.GetTempPath(), filename));
				}
				while (Info.Exists);

				Info.Create();
				Info.Refresh();
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new IOException(uae.Message, uae);
			}
			catch (System.Security.SecurityException se)
			{
				throw new IOException(se.Message, se);
			}
		}


		#region Public methods

		/// <summary>
		/// Gets the path to the directory.
		/// </summary>
		public string Path
		{
			[DebuggerStepThrough]
			get { return Info.FullName; }
		}
		
		/// <summary>
		/// Gets the DirectoryInfo that represents the directory.
		/// </summary>
		public DirectoryInfo Info { get; private set; }

		/// <summary>
		/// Gets the absolute path for a file inside this directory.
		/// </summary>
		/// <param name="filename">the filename relative to this TempDir</param>
		public string GetPath(string filename)
		{
			return IOPath.Combine(this.Path, filename);
		}

		public override string ToString()
		{
			return "%TEMP%\\" + Info.Name;
		}

		#endregion


		#region Disposal

		private bool m_isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_isDisposed && disposing)
			{
				try
				{
					Info.Delete(true);
					m_isDisposed = true;
				}
				catch (IOException)
				{
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (System.Security.SecurityException)
				{
				}
			}
		}

		#endregion
	}
}

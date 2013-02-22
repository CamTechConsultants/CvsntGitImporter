/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Represents a commit to a single file.
	/// </summary>
	class FileRevision
	{
		private StringBuilder m_messageBuf = new StringBuilder();

		public readonly string File;
		public readonly string Revision;
		public readonly string Mergepoint;
		public readonly DateTime Time;
		public readonly string Author;
		public readonly string CommitId;

		public string Message
		{
			get { return m_messageBuf.ToString(); }
		}

		public FileRevision(string file, string revision, string mergepoint, DateTime time, string author, string commitId)
		{
			this.File = file;
			this.Revision = revision;
			this.Mergepoint = mergepoint;
			this.Time = time;
			this.Author = author;
			this.CommitId = commitId;
		}

		public void AddMessage(string line)
		{
			if (m_messageBuf.Length > 0)
				m_messageBuf.Append(Environment.NewLine);
			m_messageBuf.Append(line);
		}
	}
}

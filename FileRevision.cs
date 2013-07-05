/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Represents a commit to a single file.
	/// </summary>
	class FileRevision
	{
		private StringBuilder m_messageBuf = new StringBuilder();

		public readonly FileInfo File;
		public readonly Revision Revision;
		public readonly Revision Mergepoint;
		public readonly DateTime Time;
		public readonly string Author;
		public readonly string CommitId;
		public readonly bool IsDead;

		public string Message
		{
			get { return m_messageBuf.ToString(); }
		}

		/// <summary>
		/// Gets the branch this commit was made on.
		/// </summary>
		public string Branch
		{
			get { return this.File.GetBranch(this.Revision); }
		}

		/// <summary>
		/// Is this file revision merely recording on the trunk that the file was actually added on another
		/// branch?
		/// </summary>
		public bool IsAddedOnAnotherBranch
		{
			get
			{
				return Revision == Revision.First && IsDead && Regex.IsMatch(Message, @"file .* was initially added on branch ");
			}
		}

		public FileRevision(FileInfo file, Revision revision, Revision mergepoint, DateTime time, string author,
				string commitId, bool isDead = false)
		{
			this.File = file;
			this.Revision = revision;
			this.Mergepoint = mergepoint;
			this.Time = time;
			this.Author = author;
			this.CommitId = commitId;
			this.IsDead = isDead;
		}

		public void AddMessage(string line)
		{
			if (m_messageBuf.Length > 0)
				m_messageBuf.Append(Environment.NewLine);
			m_messageBuf.Append(line);
		}

		public override string ToString()
		{
			return String.Format("{0} r{1}", File.Name, Revision);
		}
	}
}

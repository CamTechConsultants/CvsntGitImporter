/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Linq;
using System.Text;

namespace CvsGitConverter
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

		public string Message
		{
			get { return m_messageBuf.ToString(); }
		}

		/// <summary>
		/// Gets the branch this commit was made on.
		/// </summary>
		public string Branch
		{
			get
			{
				if (Revision.Parts.Count() == 2)
				{
					return "MAIN";
				}
				else
				{
					var branchStem = Revision.BranchStem;
					string branchTag;
					if (!File.Branches.TryGetValue(branchStem, out branchTag))
						throw new Exception(String.Format("Branch with stem {0} not found", branchStem));

					return branchTag;
				}
			}
		}

		public FileRevision(FileInfo file, Revision revision, Revision mergepoint, DateTime time, string author, string commitId)
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

		public override string ToString()
		{
			return String.Format("{0} r{1}", File.Name, Revision);
		}
	}
}

/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CvsGitConverter
{
	/// <summary>
	/// Represents a set of changes to files committed in one go.
	/// </summary>
	class Commit : IEnumerable<FileRevision>
	{
		private readonly List<FileRevision> m_commits = new List<FileRevision>();
		private DateTime? m_time;

		public readonly string CommitId;

		public DateTime Time
		{
			get
			{
				if (m_time.HasValue)
					return m_time.Value;

				var time = m_commits.Select(c => c.Time).Min();
				m_time = time;
				return time;
			}
		}

		public Commit(string commitId)
		{
			CommitId = commitId;
		}

		public void Add(FileRevision commit)
		{
			m_time = null;
			m_commits.Add(commit);
		}

		
		public IEnumerator<FileRevision> GetEnumerator()
		{
			return m_commits.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_commits.GetEnumerator();
		}
	}
}
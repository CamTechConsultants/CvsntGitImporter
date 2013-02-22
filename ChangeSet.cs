/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	///
	/// </summary>
	class ChangeSet : IEnumerable<Commit>
	{
		private readonly List<Commit> m_commits = new List<Commit>();
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

		public ChangeSet(string commitId)
		{
			CommitId = commitId;
		}

		public void Add(Commit commit)
		{
			m_time = null;
			m_commits.Add(commit);
		}

		
		public IEnumerator<Commit> GetEnumerator()
		{
			return m_commits.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_commits.GetEnumerator();
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Split commits that span multiple branches into separate commits.
	/// </summary>
	class SplitMultiBranchCommits : IEnumerable<Commit>
	{
		private readonly IEnumerable<Commit> m_commits;

		public SplitMultiBranchCommits(IEnumerable<Commit> commits)
		{
			m_commits = commits;
		}

		private IEnumerable<Commit> Filter()
		{
			foreach (var commit in m_commits)
			{
				var branches = commit.Select(f => f.Branch).Distinct();
				if (branches.Count() > 1)
				{
					var groups = from f in commit
								 group f by f.Branch into b
								 select b;
					foreach (var group in groups)
					{
						var subCommit = new Commit(String.Format("{0}-{1}", commit.CommitId, group.Key));
						foreach (var file in group)
							subCommit.Add(file);
						yield return subCommit;
					}
				}
				else
				{
					yield return commit;
				}
			}
		}

		public IEnumerator<Commit> GetEnumerator()
		{
			return Filter().GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

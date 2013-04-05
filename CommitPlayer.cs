/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CvsGitConverter
{
	/// <summary>
	/// Playback commits in an appropriate order for importing them.
	/// Does a depth first search, returning branches in the entirety as they're encountered.
	/// </summary>
	class CommitPlayer
	{
		private readonly ILogger m_log;
		private readonly IList<Commit> m_commits;
		private readonly Dictionary<string, string> m_branchpoints;

		public CommitPlayer(ILogger log, IEnumerable<Commit> commits, IDictionary<string, Commit> branches)
		{
			m_log = log;
			m_commits = commits.ToListIfNeeded();

			var firstCommit = commits.FirstOrDefault();
			if (firstCommit != null && firstCommit.Branch != "MAIN")
				throw new ImportFailedException("First commit in the sequence is not on the trunk");

			m_branchpoints = new Dictionary<string, string>(branches.Count);
			foreach (var x in branches)
				m_branchpoints[x.Value.CommitId] = x.Key;
		}

		/// <summary>
		/// Get the commits in an order in which they can be imported.
		/// </summary>
		public IEnumerable<Commit> Play()
		{
			return EnumerateBranch(m_commits, 0, "MAIN");
		}

		private IEnumerable<Commit> EnumerateBranch(IList<Commit> commits, int start, string branch)
		{
			for (int i = start; i < commits.Count; i++)
			{
				var commit = commits[i];
				if (commit.Branch == branch)
				{
					yield return commit;

					if (m_branchpoints.ContainsKey(commit.CommitId))
					{
						foreach (var branchCommit in EnumerateBranch(commits, i + 1, m_branchpoints[commit.CommitId]))
							yield return branchCommit;
					}
				}
			}
		}
	}
}

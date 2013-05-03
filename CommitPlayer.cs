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
	/// Does a depth first search, returning branches in their entirety as they're encountered.
	/// </summary>
	class CommitPlayer
	{
		private readonly ILogger m_log;
		private readonly BranchStreamCollection m_branches;

		public CommitPlayer(ILogger log, BranchStreamCollection branches)
		{
			m_log = log;
			m_branches = branches;
		}

		/// <summary>
		/// Get the commits in an order in which they can be imported.
		/// </summary>
		public IEnumerable<Commit> Play()
		{
			return EnumerateBranch("MAIN");
		}

		private IEnumerable<Commit> EnumerateBranch(string branch)
		{
			var root = m_branches[branch];

			for (var commit = root; commit != null; commit = commit.Successor)
			{
				yield return commit;

				if (commit.IsBranchpoint)
				{
					var branchCommits = from branchpoint in commit.Branches
										from c in EnumerateBranch(branchpoint.Branch)
										select c;

					foreach (var branchCommit in branchCommits)
						yield return branchCommit;
				}
			}
		}
	}
}

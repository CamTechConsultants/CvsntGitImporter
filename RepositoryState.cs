/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Tracks the state of the repository allowing commits to be replayed.
	/// </summary>
	class RepositoryState
	{
		private readonly Dictionary<string, RepositoryBranchState> m_branches = new Dictionary<string, RepositoryBranchState>();
		private readonly FileCollection m_allFiles;
		private readonly bool m_setupInitialBranchState;

		private RepositoryState(FileCollection allFiles, bool setupInitialBranchState)
		{
			m_allFiles = allFiles;
			m_setupInitialBranchState = setupInitialBranchState;
		}

		/// <summary>
		/// Create an instance of RepositoryState that tracks the full state of each branch, i.e. each
		/// branch inherits all live files from its parent.
		/// </summary>
		public static RepositoryState CreateWithFullBranchState(FileCollection allFiles)
		{
			return new RepositoryState(allFiles, true);
		}

		/// <summary>
		/// Create an instance of RepositoryState that tracks only new files added on a branch.
		/// </summary>
		public static RepositoryState CreateWithBranchChangesOnly()
		{
			return new RepositoryState(null, false);
		}

		/// <summary>
		/// Gets the state for a branch.
		/// </summary>
		public RepositoryBranchState this[string branch]
		{
			get
			{
				RepositoryBranchState state;
				if (m_branches.TryGetValue(branch, out state))
					return state;

				state = CreateBranchState(branch);
				m_branches[branch] = state;
				return state;
			}
		}

		/// <summary>
		/// Apply a commit.
		/// </summary>
		public void Apply(Commit commit)
		{
			var state = this[commit.Branch];
			state.Apply(commit);

			// find any file revisions that are branchpoints for branches and update the state of those branches
			var branches = commit
					.SelectMany(f => f.File.GetBranchesAtRevision(f.Revision))
					.Distinct()
					.Where(b => m_branches.ContainsKey(b));

			foreach (var branch in branches)
			{
				var tempCommit = new Commit("");
				foreach (var fr in commit.Where(f => f.File.GetBranchesAtRevision(f.Revision).Contains(branch)))
					tempCommit.Add(fr);
				this[branch].Apply(tempCommit);
			}
		}

		private RepositoryBranchState CreateBranchState(string branch)
		{
			var state = new RepositoryBranchState(branch);

			if (m_setupInitialBranchState)
			{
				foreach (var file in m_allFiles)
				{
					var branchpointRevision = file.GetBranchpointForBranch(branch);
					if (branchpointRevision == Revision.Empty)
						continue;

					var sourceBranch = file.GetBranch(branchpointRevision);
					if (sourceBranch != null)
					{
						var sourceBranchRevision = this[sourceBranch][file.Name];

						if (sourceBranchRevision != Revision.Empty)
							state.SetUnsafe(file.Name, branchpointRevision);
					}
				}
			}

			return state;
		}
	}
}
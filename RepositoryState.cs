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
		private readonly Dictionary<string, FileInfo> m_allFiles;
		private readonly bool m_setupInitialBranchState;

		private RepositoryState(Dictionary<string, FileInfo> allFiles, bool setupInitialBranchState)
		{
			m_allFiles = allFiles;
			m_setupInitialBranchState = setupInitialBranchState;
		}

		/// <summary>
		/// Create an instance of RepositoryState that tracks the full state of each branch, i.e. each
		/// branch inherits all live files from its parent.
		/// </summary>
		public static RepositoryState CreateWithFullBranchState(Dictionary<string, FileInfo> allFiles)
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
		}

		private RepositoryBranchState CreateBranchState(string branch)
		{
			var state = new RepositoryBranchState(branch);

			if (m_setupInitialBranchState)
			{
				foreach (var file in m_allFiles.Values)
				{
					var branchRevision = file.GetBranchpointForBranch(branch);
					if (branchRevision == Revision.Empty)
						continue;

					state.SetUnsafe(file.Name, branchRevision);
				}
			}

			return state;
		}
	}
}
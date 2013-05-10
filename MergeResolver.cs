/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Resolve merges back to individual commits on other branches.
	/// </summary>
	class MergeResolver
	{
		private readonly ILogger m_log;
		private readonly BranchStreamCollection m_streams;
		private readonly HashSet<string> m_includedBranches;

		public MergeResolver(ILogger log, BranchStreamCollection streams, IEnumerable<string> includedBranches)
		{
			m_log = log;
			m_streams = streams;
			m_includedBranches = new HashSet<string>(includedBranches);
		}

		public void Resolve()
		{
			m_log.DoubleRuleOff();
			m_log.WriteLine("Resolving merges...");

			using (m_log.Indent())
			{
				ResolveMerges();
			}
		}

		private void ResolveMerges()
		{
			int failures = 0;
			foreach (var branch in m_streams.Branches)
			{
				var branchRoot = m_streams[branch];
				failures += ProcessBranch(branchRoot);
			}

			if (failures > 0)
				throw new ImportFailedException("Failed to resolve all merges");
		}

		/// <summary>
		/// Process merges to a single branch.
		/// </summary>
		/// <returns>Number of failures</returns>
		private int ProcessBranch(Commit branchDestRoot)
		{
			int failures = 0;
			var lastMerges = new Dictionary<string, Commit>();
			Func<string, Commit> getLastMerge = branchFrom =>
			{
				Commit result;
				return lastMerges.TryGetValue(branchFrom, out result) ? result : null;
			};

			for (Commit commitDest = branchDestRoot; commitDest != null; commitDest = commitDest.Successor)
			{
				if (!commitDest.MergedFiles.Any())
					continue;

				// get the last commit on the source branch for all the merged files
				var commitSource = commitDest.MergedFiles
						.Select(f => f.File.GetCommit(f.Mergepoint))
						.OrderByDescending(c => c.Index)
						.First();

				// ignore excluded branches
				if (!m_includedBranches.Contains(commitSource.Branch))
					continue;

				var commitBranchRoot = m_streams[commitSource.Branch];
				if (commitBranchRoot.Predecessor == null || commitBranchRoot.Predecessor.Branch != commitDest.Branch)
				{
					using (m_log.Indent())
					{
						m_log.WriteLine("Warning: ignoring merge to commit {0} on {1} - merged commit {2} is on {3} which is not branched off from {4}",
								commitDest.CommitId, commitDest.Branch, commitSource.CommitId, commitSource.Branch, commitDest.Branch);
					}
					continue;
				}

				var lastMergeSource = getLastMerge(commitSource.Branch);
				if (lastMergeSource != null && commitSource.Index < lastMergeSource.Index)
				{
					m_log.WriteLine("Merges from {0} to {1} are crossed ({2}->{3})",
							commitSource.Branch, commitDest.Branch, commitSource.CommitId, commitDest.CommitId);

					using (m_log.Indent())
					{
						m_streams.MoveCommit(commitSource, lastMergeSource);

						// don't update last merge as it has not changed
					}
				}
				else
				{
					lastMerges[commitSource.Branch] = commitSource;
				}

				// fill in the resolved merge
				commitDest.MergeFrom = commitSource;
			}

			return failures;
		}
	}
}
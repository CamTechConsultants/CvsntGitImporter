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

		public MergeResolver(ILogger log, BranchStreamCollection streams)
		{
			m_log = log;
			m_streams = streams;
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
			var lastMerges = new Dictionary<string, int>();
			Func<string, int> getLastMerge = branchFrom =>
			{
				int result;
				return lastMerges.TryGetValue(branchFrom, out result) ? result : 0;
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

				int lastMerge = getLastMerge(commitSource.Branch);
				if (commitSource.Index < lastMerge)
				{
					m_log.WriteLine("Merges from {0} to {1} are crossed ({2}->{3})",
							commitSource.Branch, commitDest.Branch, commitSource.CommitId, commitDest.CommitId);

					using (m_log.Indent())
					{
						// go back and find the previous merge
						var commitMoveDestination = FindCommitToReorder(lastMerge, commitSource);
						if (commitMoveDestination == null)
						{
							failures++;
							continue;
						}
						else
						{
							m_streams.MoveCommit(commitSource, commitMoveDestination);
						}

						// don't update last merge as it has not changed
					}
				}
				else
				{
					lastMerges[commitSource.Branch] = commitSource.Index;
				}

				commitDest.MergeFrom = commitSource;
			}

			return failures;
		}

		private Commit FindCommitToReorder(int indexToFind, Commit commitSource)
		{
			for (Commit commit = m_streams.Head(commitSource.Branch); commit != null; commit = commit.Predecessor)
			{
				if (commit.Branch == commitSource.Branch && commit.IsBranchpoint)
				{
					m_log.WriteLine("Commit {0} is a branchpoint for branch {1} so can't reorder commits on branch {1}",
							commit.CommitId, commitSource.Branch, commit.Branches.First());
					return null;
				}

				if (commit.Index == indexToFind)
					return commit;
			}


			m_log.WriteLine("Failed to find commit with index {0} while re-ordering commit {1} (index {2}) on branch {3}",
					indexToFind, commitSource.CommitId, commitSource.Index, commitSource.Branch);
			return null;
		}
	}
}
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
				var branchCommits = m_streams[branch];
				failures += ProcessBranch(branchCommits);
			}

			if (failures > 0)
				throw new ImportFailedException("Failed to resolve all merges");
		}

		/// <summary>
		/// Process merges to a single branch.
		/// </summary>
		/// <returns>Number of failures</returns>
		private int ProcessBranch(IList<Commit> branchToCommits)
		{
			int failures = 0;
			var lastMerges = new Dictionary<string, int>();
			Func<string, int> getLastMerge = branchFrom =>
			{
				int result;
				return lastMerges.TryGetValue(branchFrom, out result) ? result : 0;
			};

			for (int i = 0; i < branchToCommits.Count; i++)
			{
				var commitTo = branchToCommits[i];
				if (!commitTo.MergedFiles.Any())
					continue;

				// get the last commit on the source branch for all the merged files
				var commitFrom = commitTo.MergedFiles
						.Select(f => f.File.GetCommit(f.Mergepoint))
						.OrderByDescending(c => c.Index)
						.First();

				int lastMerge = getLastMerge(commitFrom.Branch);
				if (commitFrom.Index < lastMerge)
				{
					m_log.WriteLine("Merges from {0} to {1} are crossed ({2}->{3})",
							commitFrom.Branch, commitTo.Branch, commitFrom.CommitId, commitTo.CommitId);

					using (m_log.Indent())
					{
						// go back and find the previous merge
						var branchFromCommits = m_streams[commitFrom.Branch];
						var commitFromPosition = branchFromCommits.IndexOfFromEnd(commitFrom);
						if (commitFromPosition < 0)
						{
							m_log.WriteLine("Failed to find commit {0} in commit list - perhaps the merged commit appears in the list after the commit to which it is merged to ({1})?",
									commitFrom.CommitId, commitTo.CommitId);
							failures++;
							continue;
						}

						int lastMergePosition = FindCommitToReorder(branchFromCommits, lastMerge, commitFrom);
						if (lastMergePosition >= 0)
						{
							branchFromCommits.Move(commitFromPosition, lastMergePosition);
						}
						else
						{
							failures++;
							continue;
						}

						// don't update last merge as it has not changed
					}
				}
				else
				{
					lastMerges[commitFrom.Branch] = commitFrom.Index;
				}

				commitTo.MergeFrom = commitFrom;
			}

			return failures;
		}

		private int FindCommitToReorder(IList<Commit> branchFromCommits, int indexToFind, Commit commitFrom)
		{
			int lastMergePosition;
			for (lastMergePosition = branchFromCommits.Count - 1; lastMergePosition >= 0; lastMergePosition--)
			{
				var commit = branchFromCommits[lastMergePosition];
				if (commit.Branch == commitFrom.Branch && commit.IsBranchpoint)
				{
					m_log.WriteLine("Commit {0} is a branchpoint for branch {1} so can't reorder commits on branch {1}",
							commit.CommitId, commitFrom.Branch, commit.Branches.First());
					return -1;
				}

				if (branchFromCommits[lastMergePosition].Index == indexToFind)
					break;
			}


			if (lastMergePosition < 0)
			{
				m_log.WriteLine("Failed to find commit with index {0} while re-ordering commit {1} (index {2}) on branch {3}",
						indexToFind, commitFrom.CommitId, commitFrom.Index, commitFrom.Branch);
			}

			return lastMergePosition;
		}
	}
}
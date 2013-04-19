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
		private readonly IList<Commit> m_commits;
		private readonly IDictionary<string, Commit> m_branchpoints;
		private HashSet<string> m_branchpointCommits;

		public MergeResolver(ILogger log, IEnumerable<Commit> commits, IDictionary<string, Commit> branchpoints)
		{
			m_log = log;
			m_commits = commits.ToListIfNeeded();
			m_branchpoints = branchpoints;
		}

		/// <summary>
		/// Gets the list of commits after merge resolution.
		/// </summary>
		public IEnumerable<Commit> Commits
		{
			get { return m_commits; }
		}

		/// <summary>
		/// Generate a set of commits that are branchpoints on demand.
		/// </summary>
		private HashSet<string> BranchpointCommits
		{
			get
			{
				if (m_branchpointCommits == null)
					m_branchpointCommits = new HashSet<string>(m_branchpoints.Values.Select(c => c.CommitId));
				return m_branchpointCommits;
			}
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
			var lastMerges = new Dictionary<string, int>();
			Func<string, string, string> makeKey = (branchFrom, branchTo) => String.Format("{0}->{1}", branchFrom, branchTo);
			Func<string, string, int> getLastMerge = (branchFrom, branchTo) =>
			{
				int result;
				return lastMerges.TryGetValue(makeKey(branchFrom, branchTo), out result) ? result : 0;
			};
			Action<string, string, int> setLastMerge = (branchFrom, branchTo, index) => lastMerges[makeKey(branchFrom, branchTo)] = index;

			int failures = 0;
			for (int i = 0; i < m_commits.Count; i++)
			{
				var commitTo = m_commits[i];
				if (!commitTo.MergedFiles.Any())
					continue;

				// get the last commit on the source branch for all the merged files
				var commitFrom = commitTo.MergedFiles
						.Select(f => f.File.GetCommit(f.Mergepoint))
						.OrderByDescending(c => c.Index)
						.First();

				int lastMerge = getLastMerge(commitFrom.Branch, commitTo.Branch);
				if (commitFrom.Index < lastMerge)
				{
					m_log.WriteLine("Merges from {0} to {1} are crossed ({2}->{3})",
							commitFrom.Branch, commitTo.Branch, commitFrom.CommitId, commitTo.CommitId);

					using (m_log.Indent())
					{
						// go back and find the previous merge
						var commitFromPosition = m_commits.IndexOfFromEnd(commitFrom, i - 1);
						if (commitFromPosition < 0)
						{
							m_log.WriteLine("Failed to find commit {0} in commit list - perhaps the merged commit appears in the list after the commit to which it is merged to ({1})?",
									commitFrom.CommitId, commitTo.CommitId);
							failures++;
							continue;
						}

						int lastMergePosition = FindCommitToReorder(i, lastMerge, commitFrom);
						if (lastMergePosition >= 0)
						{
							m_commits.Move(commitFromPosition, lastMergePosition);
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
					setLastMerge(commitFrom.Branch, commitTo.Branch, commitFrom.Index);
				}
			}

			if (failures > 0)
				throw new ImportFailedException("Failed to resolve all merges");
		}

		private int FindCommitToReorder(int startPosition, int indexToFind, Commit commitFrom)
		{
			int lastMergePosition;
			for (lastMergePosition = startPosition - 1; lastMergePosition >= 0; lastMergePosition--)
			{
				var commit = m_commits[lastMergePosition];
				if (commit.Branch == commitFrom.Branch && BranchpointCommits.Contains(commit.CommitId))
				{
					m_log.WriteLine("Commit {0} is a branchpoint for branch {1} so can't reorder commits on branch {1}",
							commit.CommitId, commitFrom.Branch, m_branchpoints.Where(kvp => kvp.Value == commit).Select(kvp => kvp.Key));
					return -1;
				}

				if (m_commits[lastMergePosition].Index == indexToFind)
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
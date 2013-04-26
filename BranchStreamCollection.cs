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
	/// Collection of streams of commits for each branch.
	/// </summary>
	class BranchStreamCollection
	{
		private readonly Dictionary<string, IList<Commit>> m_branches = new Dictionary<string, IList<Commit>>();

		private string m_lastBranch;
		private IList<Commit> m_lastBranchList;
		private Commit m_lastBranchCommit;

		public BranchStreamCollection(IEnumerable<Commit> commits, IDictionary<string, Commit> branchpoints)
		{
			foreach (var commit in commits)
			{
				if (branchpoints.ContainsKey(commit.Branch))
					AddCommit(commit);
			}

			// join branches to their branchpoints
			foreach (var kvp in m_branches)
			{
				if (kvp.Key == "MAIN")
					continue;

				var branchpoint = branchpoints[kvp.Key];
				branchpoint.AddBranch(kvp.Value.First());
			}
		}

		/// <summary>
		/// Get the stream of commits for a branch.
		/// </summary>
		/// <returns>the stream of commits, or an empty list if the branch does not exist</returns>
		public IList<Commit> this[string branch]
		{
			get
			{
				IList<Commit> list;
				return m_branches.TryGetValue(branch, out list) ? list : new List<Commit>(0);
			}
		}

		/// <summary>
		/// Get a list of the names of the branches contained in this collection.
		/// </summary>
		public IEnumerable<string> Branches
		{
			get { return m_branches.Keys; }
		}

		private void AddCommit(Commit commit)
		{
			IList<Commit> list;
			var branch = commit.Branch;

			if (branch == m_lastBranch)
			{
				// optimisation - assume last commit was on the same branch
				list = m_lastBranchList;
				list.Add(commit);

				if (m_lastBranchCommit != null)
				{
					m_lastBranchCommit.Successor = commit;
					commit.Predecessor = m_lastBranchCommit;
				}
			}
			else
			{
				if (m_branches.TryGetValue(branch, out list))
				{
					var prevCommit = list.Last();
					list.Add(commit);
					prevCommit.Successor = commit;
					commit.Predecessor = prevCommit;
				}
				else
				{
					list = new List<Commit>() { commit };
					m_branches.Add(branch, list);
				}

				m_lastBranch = commit.Branch;
				m_lastBranchList = list;
			}

			commit.Index = list.Count;
			m_lastBranchCommit = commit;
		}
	}
}

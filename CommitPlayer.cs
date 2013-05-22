/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Playback commits in an appropriate order for importing them.
	/// </summary>
	class CommitPlayer
	{
		private readonly ILogger m_log;
		private readonly BranchStreamCollection m_streams;
		private readonly Dictionary<string, Commit> m_branchHeads = new Dictionary<string, Commit>();
		private static readonly Commit EndMarker = new Commit("ENDMARKER") { Index = int.MaxValue };

		public CommitPlayer(ILogger log, BranchStreamCollection streams)
		{
			m_log = log;
			m_streams = streams;
		}

		/// <summary>
		/// Get the total number of commits.
		/// </summary>
		public int Count
		{
			get
			{
				return m_streams.Branches.Select(b => CountCommits(m_streams[b])).Sum();
			}
		}

		/// <summary>
		/// Get the commits in an order in which they can be imported.
		/// </summary>
		public IEnumerable<Commit> Play()
		{
			foreach (var branch in m_streams.Branches)
				m_branchHeads[branch] = m_streams[branch];

			// ensure first commit is the first commit from MAIN
			var mainHead = m_streams["MAIN"];
			yield return mainHead;
			UpdateHead("MAIN", mainHead.Successor);

			Commit nextCommit;
			while ((nextCommit = GetNextCommit()) != null)
			{
				// ensure that any branch we merge from is far enough along
				if (nextCommit.MergeFrom != null)
				{
					foreach (var branchCommit in FastForwardBranch(nextCommit.MergeFrom))
						yield return branchCommit;
				}

				yield return nextCommit;
				UpdateHead(nextCommit.Branch, nextCommit.Successor);
			}
		}

		private IEnumerable<Commit> FastForwardBranch(Commit commit)
		{
			var branch = commit.Branch;
			Commit nextCommit;
			while ((nextCommit = m_branchHeads[branch]).Index <= commit.Index)
			{
				// may need to recursively fast forward to handle stacked branches
				if (nextCommit.MergeFrom != null)
				{
					foreach (var branchCommit in FastForwardBranch(nextCommit.MergeFrom))
						yield return branchCommit;
				}

				yield return nextCommit;
				UpdateHead(branch, nextCommit.Successor);
			}
		}

		private Commit GetNextCommit()
		{
			Commit earliest = null;

			foreach (var c in m_branchHeads.Values.Where(c => c != EndMarker))
			{
				if (earliest == null || c.Time < earliest.Time)
					earliest = c;
			}

			return earliest;
		}

		private void UpdateHead(string branch, Commit commit)
		{
			m_branchHeads[branch] = commit ?? EndMarker;
		}

		private int CountCommits(Commit root)
		{
			int count = 0;
			for (Commit c = root; c != null; c = c.Successor)
				count++;
			return count;
		}
	}
}

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
	/// Apply any file exclusions to a stream of commits, and also track the state of any files that are
	/// "head-only", i.e. are excluded but we want to apply their latest version to relevant branches at the end.
	/// </summary>
	class ExclusionFilter
	{
		private readonly ILogger m_log;
		private readonly IConfig m_config;
		private readonly Renamer m_branchRenamer;
		private readonly RepositoryState m_headOnlyState;

		public ExclusionFilter(ILogger log, IConfig config)
		{
			m_log = log;
			m_config = config;
			m_branchRenamer = config.BranchRename;
			m_headOnlyState = RepositoryState.CreateWithBranchChangesOnly();
		}

		public RepositoryState HeadOnlyState
		{
			get { return m_headOnlyState; }
		} 

		/// <summary>
		/// Filter a stream of commits, removing files from commits or entire commits where files have
		/// been excluded from the import.
		/// </summary>
		public IEnumerable<Commit> Filter(IEnumerable<Commit> commits)
		{
			foreach (var commit in commits)
			{
				if (commit.All(f => m_config.IncludeFile(f.File.Name)))
				{
					yield return commit;
				}
				else
				{
					var replacement = SplitCommit(commit, f => m_config.IncludeFile(f.Name));
					if (replacement != null)
						yield return replacement;

					var headOnly = SplitCommit(commit, f => m_config.IsHeadOnly(f.Name));
					if (headOnly != null)
						m_headOnlyState.Apply(headOnly);
				}
			}
		}

		/// <summary>
		/// Create extra commits for files that have been marked as "head-only".
		/// </summary>
		public void CreateHeadOnlyCommits(IEnumerable<string> headOnlyBranches, BranchStreamCollection streams,
				FileCollection allFiles)
		{
			var branches = SortBranches(headOnlyBranches, streams);
			var branchMerges = new Dictionary<string, string>();

			if (branches.Any())
			{
				m_log.DoubleRuleOff();
				m_log.WriteLine("Creating head-only commits");
			}

			using (m_log.Indent())
			{
				foreach (var branch in branches)
				{
					// record where this branch will be merged to
					if (streams[branch].Predecessor != null)
						branchMerges[streams[branch].Predecessor.Branch] = branch;

					string branchMergeFrom;
					branchMerges.TryGetValue(branch, out branchMergeFrom);

					CreateHeadOnlyCommit(branch, streams, allFiles, branchMergeFrom);
				}
			}
		}

		private void CreateHeadOnlyCommit(string branch, BranchStreamCollection streams, FileCollection allFiles, string branchMergeFrom)
		{
			var headOnlyState = m_headOnlyState[branch];
			var commitId = String.Format("headonly-{0}", branch);
			var commit = new Commit(commitId);

			var message = String.Format("Adding head-only files to {0}", m_branchRenamer.Process(branch));

			foreach (var file in headOnlyState.LiveFiles.OrderBy(i => i, StringComparer.OrdinalIgnoreCase))
			{
				var fileRevision = new FileRevision(allFiles[file], headOnlyState[file],
						mergepoint: Revision.Empty, time: DateTime.Now, author: "", commitId: commitId);
				fileRevision.AddMessage(message);
				commit.Add(fileRevision);
			}

			// check for a merge
			if (branchMergeFrom != null)
			{
				Commit mergeSource = streams.Head(branchMergeFrom);
				if (mergeSource.CommitId.StartsWith("headonly-"))
				{
					commit.MergeFrom = mergeSource;

					// check for deleted files
					var fileLookup = new HashSet<string>(commit.Select(r => r.File.Name));
					foreach (var r in mergeSource)
					{
						if (!fileLookup.Contains(r.File.Name))
						{
							var fileRevision = new FileRevision(
									file: r.File,
									revision: Revision.Empty,   // a bit lazy, but I think we can get away with it
									mergepoint: Revision.Empty,
									time: DateTime.Now,
									author: "",
									commitId: commitId,
									isDead: true);
							fileRevision.AddMessage(message);
							commit.Add(fileRevision);
						}
					}
				}
			}

			if (commit.Any())
			{
				m_log.WriteLine("Added commit {0}", commitId);
				streams.AppendCommit(commit);
			}
		}

		private Commit SplitCommit(Commit commit, Predicate<FileInfo> filter)
		{
			Commit newCommit = null;

			foreach (var f in commit)
			{
				if (filter(f.File))
				{
					if (newCommit == null)
						newCommit = new Commit(commit.CommitId);
					newCommit.Add(f);
				}
			}

			return newCommit;
		}

		private List<string> SortBranches(IEnumerable<string> headOnlyBranches, BranchStreamCollection streams)
		{
			// build a set of branches that we care about
			var branches = new HashSet<string> { "MAIN" };
			foreach (var branch in headOnlyBranches)
				branches.Add(branch);

			// get a list of all branches in order and filter it
			var list = streams.OrderedBranches.Where(b => branches.Contains(b)).ToList();
			list.Reverse();
			return list;
		}
	}
}
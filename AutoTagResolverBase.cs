/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CTC.CvsntGitImporter.Utils;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Abstract base class for TagResolver and BranchResolver.
	/// </summary>
	abstract class AutoTagResolverBase : CTC.CvsntGitImporter.ITagResolver
	{
		private readonly ILogger m_log;
		private readonly IList<Commit> m_commits;
		private readonly Dictionary<string, FileInfo> m_allFiles;
		private readonly bool m_branches;

		protected AutoTagResolverBase(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles,
				bool branches = false)
		{
			m_log = log;
			m_commits = commits.ToListIfNeeded();
			m_allFiles = allFiles;
			m_branches = branches;
			this.PartialTagThreshold = 30;
		}

		/// <summary>
		/// The number of untagged files that are encountered before a tag/branch is declared as partial
		/// and is abandoned.
		/// </summary>
		public int PartialTagThreshold { get; set; }

		/// <summary>
		/// Gets a lookup that returns the resolved commits for each tag.
		/// </summary>
		public IDictionary<string, Commit> ResolvedTags
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets a list of any unresolved tags.
		/// </summary>
		public IEnumerable<string> UnresolvedTags
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the (possibly re-ordered) list of commits.
		/// </summary>
		public IEnumerable<Commit> Commits
		{
			get { return m_commits; }
		}

		/// <summary>
		/// Get the tags or branches for a specific revision of a file.
		/// </summary>
		protected abstract IEnumerable<string> GetTagsForFileRevision(FileInfo file, Revision revision);

		/// <summary>
		/// Get the revision for a specific tag or branch.
		/// </summary>
		protected abstract Revision GetRevisionForTag(FileInfo file, string tag);

		/// <summary>
		/// Resolve tags and try and fix those that don't immediately resolve.
		/// </summary>
		/// <returns>true if all tags were resolved (potentially after being fixed), or false
		/// if fixing tags failed</returns>
		public virtual bool Resolve(IEnumerable<string> tags)
		{
			this.UnresolvedTags = Enumerable.Empty<string>();
			this.ResolvedTags = new Dictionary<string, Commit>();
			var problematicTags = new Dictionary<string, Commit>();

			foreach (var tag in tags.Where(t => MatchTag(t)))
			{
				Commit commit;
				if (ResolveTag(tag, out commit))
					ResolvedTags[tag] = commit;
				else
					problematicTags.Add(tag, commit);
			}

			// attempt fix
			if (problematicTags.Count > 0)
			{
				m_log.WriteLine("Problematic:");
				using (m_log.Indent())
				{
					foreach (var kvp in problematicTags.OrderBy(i => i.Key, StringComparer.CurrentCultureIgnoreCase))
						m_log.WriteLine("{0}: {1}", kvp.Key, kvp.Value.ConciseFormat);
				}

				return AttemptFix(problematicTags);
			}
			else
			{
				m_log.WriteLine("All tags resolved");
				return true;
			}
		}

		private bool ResolveTag(string tag, out Commit candidate)
		{
			var state = RepositoryState.CreateWithFullBranchState(m_allFiles);
			candidate = null;

			foreach (var commit in m_commits)
			{
				state.Apply(commit);
				if (IsCandidate(tag, commit))
				{
					candidate = commit;

					if (IsCommitForTag(state, tag, commit))
						return true;
				}
			}

			return false;
		}

		private bool IsCandidate(string tag, Commit commit)
		{
				return commit.Any(r =>
						GetTagsForFileRevision(r.File, r.Revision).Contains(tag) ||
						r.IsDead && GetRevisionForTag(r.File, tag) == Revision.Empty);
			}

		private FileInfo m_lastMatchFailure;

		private bool IsCommitForTag(RepositoryState state, string tag, Commit candidate)
		{
			var branchState = state[candidate.Branch];

			// optimisation - check the last file that failed first
			if (m_lastMatchFailure != null && !IsFileAtTag(branchState, m_lastMatchFailure, tag))
				return false;

			foreach (var file in m_allFiles.Values)
			{
				if (!IsFileAtTag(branchState, file, tag))
				{
					m_lastMatchFailure = file;
					return false;
				}
			}

			return true;
		}

		protected virtual bool IsFileAtTag(RepositoryBranchState state, FileInfo file, string tag)
		{
			return state[file.Name] == GetRevisionForTag(file, tag);
		}

		private bool AttemptFix(Dictionary<string, Commit> tags)
		{
			AnalyseProblematicTags(tags);

			var failedTags = new List<string>();
			foreach (var tag in tags.Keys.Where(t => MatchTag(t)))
			{
				Commit commit;
				if (ResolveTag(tag, out commit))
					ResolvedTags[tag] = commit;
				else
					failedTags.Add(tag);
			}

			this.UnresolvedTags = failedTags;
			return failedTags.Count == 0;
		}

		/// <summary>
		/// Should a tag be processed or not?
		/// </summary>
		protected virtual bool MatchTag(string tag)
		{
			return true;
		}

		private void AnalyseProblematicTags(Dictionary<string, Commit> tags)
		{
			var moveRecords = new List<CommitMoveRecord>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Analysing problematic {0}...", m_branches ? "branches" : "tags");

			using (m_log.Indent())
			{
				foreach (var tag in tags.Keys)
				{
					m_log.WriteLine("Tag {0}:", tag);
					moveRecords.AddRange(AnalyseProblematicTag(tag, tags));
					m_log.RuleOff();
				}

				if (moveRecords.Count > 0)
				{
					m_log.WriteLine("Moving commits...");
					foreach (var record in moveRecords)
						record.Apply(m_commits);
				}
			}
		}

		private IEnumerable<CommitMoveRecord> AnalyseProblematicTag(string tag, Dictionary<string, Commit> finalCommits)
		{
			var moveRecords = new List<CommitMoveRecord>();
			CommitMoveRecord moveRecord = null;
			var state = RepositoryState.CreateWithFullBranchState(m_allFiles);
			var filesAtTagRevision = new Dictionary<string, Commit>();
			var finalCommit = finalCommits[tag];
			var branch = finalCommit.Branch;
			var commitsToMove = new List<Commit>();

			var untaggedFiles = FindUntaggedFiles(finalCommit, tag).ToHashSet();

			foreach (var commit in m_commits)
			{
				state.Apply(commit);

				List<FileRevision> filesToMove = null;
				foreach (var fileRevision in commit)
				{
					var file = fileRevision.File;
					if (file.IsRevisionOnBranch(fileRevision.Revision, branch))
					{
						if (fileRevision.Revision == GetRevisionForTag(file, tag))
						{
							filesAtTagRevision[file.Name] = commit;
						}
						else if (filesAtTagRevision.ContainsKey(file.Name))
						{
							m_log.WriteLine("  File {0} updated to r{1} in commit {2} but tagged in commit {3}",
									file.Name, fileRevision.Revision, commit.ConciseFormat, filesAtTagRevision[file.Name].ConciseFormat);

							AddAndCreateList(ref filesToMove, fileRevision);
						}
						else if (state[branch][file.Name] != Revision.Empty && untaggedFiles.Contains(file.Name))
						{
							m_log.WriteLine("  File {0} not tagged with {1}, assuming added after tag was made",
									file.Name, tag);

							AddAndCreateList(ref filesToMove, fileRevision);
						}
					}
				}

				if (filesToMove != null)
				{
					if (moveRecord == null)
					{
						moveRecord = new CommitMoveRecord(tag, finalCommit, m_log);
						moveRecords.Add(moveRecord);
					}
					moveRecord.AddCommit(commit, filesToMove);
				}

				if (commit == finalCommit)
					break;
			}

			return moveRecords;
		}

		private IEnumerable<string> FindUntaggedFiles(Commit finalCommit, string tag)
		{
			var state = RepositoryState.CreateWithFullBranchState(m_allFiles);

			foreach (var commit in m_commits)
			{
				state.Apply(commit);
				if (commit == finalCommit)
					break;
			}

			foreach (var fileName in state[finalCommit.Branch].LiveFiles)
			{
				if (GetRevisionForTag(m_allFiles[fileName], tag) == Revision.Empty)
					yield return fileName;
			}
		}

		private bool IsAddedFile(FileRevision fileRevision, string tag)
		{
			var file = fileRevision.File;
			return (GetRevisionForTag(file, tag) == Revision.Empty /*&& m_missingFiles[tag].Contains(file.Name)*/);
		}

		private static void AddAndCreateList<T>(ref List<T> list, T item)
		{
			if (list == null)
				list = new List<T>() { item };
			else
				list.Add(item);
		}
	}
}

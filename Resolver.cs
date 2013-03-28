/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Abstract base class for TagResolver and BranchResolver.
	/// </summary>
	abstract class Resolver
	{
		private readonly Logger m_log;
		private readonly IList<Commit> m_commits;
		private readonly Dictionary<string, FileInfo> m_allFiles;
		private readonly InclusionMatcher m_tagMatcher;
		private readonly bool m_branches;

		private List<string> m_errors;
		private IEnumerable<string> m_allTags;

		private Dictionary<string, Commit> m_finalCommits;
		private IEnumerable<string> m_problematicTags;

		protected Resolver(Logger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles,
				InclusionMatcher tagMatcher, bool branches = false)
		{
			m_log = log;
			m_commits = commits.ToListIfNeeded();
			m_allFiles = allFiles;
			m_tagMatcher = tagMatcher;
			m_branches = branches;
		}

		/// <summary>
		/// Gets any errors encountered resolving tags.
		/// </summary>
		public IEnumerable<string> Errors
		{
			get { return (m_errors == null) ? Enumerable.Empty<string>() : m_errors; }
		}

		/// <summary>
		/// Gets a list of all tags being considered.
		/// </summary>
		public IEnumerable<string> AllTags
		{
			get
			{
				if (m_allTags == null)
					throw new InvalidOperationException("Resolve not yet called");
				return m_allTags.OrderBy(t => t);
			}
		}

		/// <summary>
		/// Resolve tags. Find what tags each commit contributes to and build a stack for each tag.
		/// The last commit that contributes to a tag should be the one that we tag.
		/// </summary>
		/// <returns>true if all tags are resolvable, otherwise false</returns>
		public bool Resolve()
		{
			m_errors = null;

			m_finalCommits = FindCommitsPerTag();
			m_allTags = m_finalCommits.Keys;

			var candidateCommits = FindCandidateCommits(m_finalCommits);
			m_problematicTags = FindProblematicTags(candidateCommits);
			return !m_problematicTags.Any();
		}

		/// <summary>
		/// Resolve tags and try and fix those that don't immediately resolve.
		/// </summary>
		/// <returns>true if all tags were resolved (potentially after being fixed), or false
		/// if fixing tags failed</returns>
		public bool ResolveAndFix()
		{
			if (Resolve())
				return true;

			// analyse and hopefully fix
			AnalyseProblematicTags(m_problematicTags, m_finalCommits);

			var newFinalCommits = FindCommitsPerTag();
			var newCandidateCommits = FindCandidateCommits(newFinalCommits);
			var newProblematicTags = FindProblematicTags(newCandidateCommits);
			return !newProblematicTags.Any();
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
		/// Find the last commit for each tag.
		/// </summary>
		private Dictionary<string, Commit> FindCommitsPerTag()
		{
			var tags = new Dictionary<string, Commit>();

			foreach (var commit in m_commits)
			{
				foreach (var file in commit)
				{
					foreach (var tag in GetTagsForFileRevision(file.File, file.Revision))
					{
						if (m_tagMatcher.Match(tag))
							tags[tag] = commit;
					}
				}
			}

			return tags;
		}

		/// <summary>
		/// Build the inverse of CommitsPerTag - a lookup of commits to tags that it is supposed to be the commit for.
		/// </summary>
		private static Dictionary<Commit, List<string>> FindCandidateCommits(Dictionary<string, Commit> tags)
		{
			var candidateCommits = new Dictionary<Commit, List<string>>(CommitComparer.ById);

			foreach (var kvp in tags)
			{
				var commit = kvp.Value;

				List<string> tagsForCommit;
				if (candidateCommits.TryGetValue(commit, out tagsForCommit))
					tagsForCommit.Add(kvp.Key);
				else
					candidateCommits[commit] = new List<string>(1) { kvp.Key };
			}

			return candidateCommits;
		}

		/// <summary>
		/// Replay commits and find any candidate commits where any files are not in the correct state, i.e. they
		/// do not have the tag applied at the point the candidate commit for the tag is applied.
		/// </summary>
		/// <returns>a list of tags that do not have a single commit that represents them</returns>
		private IEnumerable<string> FindProblematicTags(Dictionary<Commit, List<string>> candidateCommits)
		{
			// now replay commits and check that all files are in the correct state for each tag
			var state = new RepositoryState();
			var problematicTags = new HashSet<string>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Finding problematic {0}...", m_branches ? "branches" : "tags");
			m_log.Indent();

			foreach (var commit in m_commits)
			{
				state.Apply(commit);

				// if the commit is a candidate for being tagged, then check that all files are at the correct version
				if (candidateCommits.ContainsKey(commit))
				{
					foreach (var tag in candidateCommits[commit])
					{
						var branchState = state[commit.Branch];
						foreach (var filename in branchState.LiveFiles)
						{
							var file = m_allFiles[filename];
							if (!GetTagsForFileRevision(file, branchState[filename]).Contains(tag))
							{
								m_log.WriteLine("No commit found for tag {0}  Commit: {1}  File: {2},r{3}",
										tag, commit.CommitId, filename, branchState[filename]);
								problematicTags.Add(tag);
							}
						}
					}
				}
			}

			if (!problematicTags.Any())
				m_log.WriteLine("None found");
			m_log.Outdent();

			return problematicTags;
		}

		private void AnalyseProblematicTags(IEnumerable<string> tags, Dictionary<string, Commit> finalCommits)
		{
			var moveRecords = new List<CommitMoveRecord>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Analysing problematic {0}...", m_branches ? "branches" : "tags");
			m_log.Indent();

			foreach (var tag in tags)
			{
				m_log.WriteLine("Tag {0}:", tag);
				var state = new RepositoryState();
				var filesAtTagRevision = new Dictionary<string, Commit>();
				var finalCommit = finalCommits[tag];
				CommitMoveRecord moveRecord = null;
				var branch = finalCommit.Branch;
				var commitsToMove = new List<Commit>();

				foreach (var commit in m_commits)
				{
					state.Apply(commit);
					if (commit == finalCommit)
						break;

					bool moveCommit = false;
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
										file.Name, fileRevision.Revision, commit.CommitId, filesAtTagRevision[file.Name].CommitId);
								moveCommit = true;
							}
						}
					}

					if (moveCommit)
					{
						if (moveRecord == null)
						{
							moveRecord = new CommitMoveRecord(finalCommit);
							moveRecords.Add(moveRecord);
						}
						moveRecord.Commits.Add(commit);
					}
				}

				m_log.RuleOff();
			}

			if (moveRecords.Count > 0)
			{
				m_log.WriteLine("Moving commits...");
				foreach (var record in moveRecords)
					MoveCommits(record);
			}

			m_log.Outdent();
		}

		private void MoveCommits(CommitMoveRecord moveRecord)
		{
			int destLocation = m_commits.IndexOf(moveRecord.FinalCommit);
			int searchStart = destLocation;

			m_log.WriteLine("Final commit: {0}", moveRecord.FinalCommit.CommitId);
			m_log.Indent();

			// handle in reverse order
			for (int i = moveRecord.Commits.Count - 1; i >= 0; i--)
			{
				int location = m_commits.IndexOfFromEnd(moveRecord.Commits[i], searchStart);
				if (location < 0)
				{
					// assume already moved
					m_log.WriteLine("Skip moving {0} after {1}", moveRecord.Commits[i].CommitId, moveRecord.FinalCommit.CommitId);
					continue;
				}

				m_log.WriteLine("Move {0}({1}) after {2}({3})", moveRecord.Commits[i].CommitId, location,
							moveRecord.FinalCommit.CommitId, destLocation);
				m_commits.Move(location, destLocation);
				destLocation--;
			}

			m_log.Outdent();
		}

		private class CommitMoveRecord
		{
			public readonly Commit FinalCommit;

			public readonly List<Commit> Commits = new List<Commit>();

			public CommitMoveRecord(Commit finalCommit)
			{
				this.FinalCommit = finalCommit;
			}
		}

		private void AddError(string format, params object[] args)
		{
			if (m_errors == null)
				m_errors = new List<string>();
			m_errors.Add(String.Format(format, args));
		}
	}
}

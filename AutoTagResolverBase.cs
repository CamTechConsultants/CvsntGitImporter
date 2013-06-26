/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

		private HashSet<string> m_allTags;
		private Dictionary<string, Commit> m_finalCommits;

		private OneToManyDictionary<string, string> m_missingFiles;
		private IEnumerable<string> m_problematicTags;

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
		public IDictionary<string, Commit> ResolvedCommits
		{
			get { return m_finalCommits; }
		}

		/// <summary>
		/// Gets a list of any unresolved tags.
		/// </summary>
		public IEnumerable<string> UnresolvedTags
		{
			get { return m_problematicTags ?? Enumerable.Empty<string>(); }
		}

		/// <summary>
		/// Gets the (possibly re-ordered) list of commits.
		/// </summary>
		public IEnumerable<Commit> Commits
		{
			get { return m_commits; }
		}

		/// <summary>
		/// Resolve tags and try and fix those that don't immediately resolve.
		/// </summary>
		/// <returns>true if all tags were resolved (potentially after being fixed), or false
		/// if fixing tags failed</returns>
		public bool Resolve(IEnumerable<string> tags)
		{
			m_allTags = new HashSet<string>(tags);
			m_finalCommits = FindCommitsPerTag();

			var candidateCommits = FindCandidateCommits(m_finalCommits);
			m_problematicTags = FindProblematicTags(candidateCommits);

			if (m_problematicTags.Any())
				return AttemptFix();
			else
				return true;
		}

		private bool AttemptFix()
		{
			// analyse and hopefully fix
			AnalyseProblematicTags(m_problematicTags, m_finalCommits);

			var newFinalCommits = FindCommitsPerTag();
			var newCandidateCommits = FindCandidateCommits(newFinalCommits);
			var newProblematicTags = FindProblematicTags(newCandidateCommits);
			m_problematicTags = newProblematicTags;
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
		/// Should a tag be processed or not?
		/// </summary>
		protected virtual bool MatchTag(string tag)
		{
			return m_allTags.Contains(tag);
		}

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
						if (MatchTag(tag))
							tags[tag] = commit;
					}
				}
			}

			return tags;
		}

		/// <summary>
		/// Build the inverse of CommitsPerTag - a lookup of commits to tags that it is supposed to be the commit for.
		/// </summary>
		private static OneToManyDictionary<Commit, string> FindCandidateCommits(Dictionary<string, Commit> tags)
		{
			var candidateCommits = new OneToManyDictionary<Commit, string>(CommitComparer.ById);

			foreach (var kvp in tags)
				candidateCommits.Add(kvp.Value, kvp.Key);

			return candidateCommits;
		}

		/// <summary>
		/// Replay commits and find any candidate commits where any files are not in the correct state, i.e. they
		/// do not have the tag applied at the point the candidate commit for the tag is applied.
		/// </summary>
		/// <returns>a list of tags that do not have a single commit that represents them</returns>
		private IEnumerable<string> FindProblematicTags(OneToManyDictionary<Commit, string> candidateCommits)
		{
			// now replay commits and check that all files are in the correct state for each tag
			var state = new RepositoryState();
			var problematicTags = new HashSet<string>();
			List<string> partialTags = null;
			m_missingFiles = new OneToManyDictionary<string, string>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Finding problematic {0}...", m_branches ? "branches" : "tags");

			using (m_log.Indent())
			{
				foreach (var commit in m_commits)
				{
					state.Apply(commit);

					// if the commit is a candidate for being tagged, then check that all files are at the correct version
					foreach (var tag in candidateCommits[commit])
					{
						var branchState = state[commit.Branch];
						foreach (var filename in branchState.LiveFiles)
						{
							var file = m_allFiles[filename];
							if (!GetTagsForFileRevision(file, branchState[filename]).Contains(tag))
							{
								if (GetRevisionForTag(file, tag) == Revision.Empty)
								{
									m_log.WriteLine("File {0} not tagged with tag {1}!", filename, tag);
									m_missingFiles.Add(tag, filename);
									if (PartialTagThreshold > 0 && m_missingFiles[tag].Count() >= PartialTagThreshold)
									{
										m_log.WriteLine("Partial: {0}", tag);
										AddAndCreateList(ref partialTags, tag);
										break;
									}
								}

								m_log.WriteLine("No commit found for tag {0}  Commit: {1}  File: {2},r{3}",
										tag, commit.CommitId, filename, branchState[filename]);
								problematicTags.Add(tag);
							}
						}
					}
				}

				if (partialTags != null)
				{
					throw new ImportFailedException(String.Format(
							"Partial branches/tags found: {0}",
							String.Join(", ", partialTags)));
				}

				if (!problematicTags.Any())
					m_log.WriteLine("None found");
			}

			return problematicTags;
		}

		private void AnalyseProblematicTags(IEnumerable<string> tags, Dictionary<string, Commit> finalCommits)
		{
			var moveRecords = new List<CommitMoveRecord>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Analysing problematic {0}...", m_branches ? "branches" : "tags");

			using (m_log.Indent())
			{
				foreach (var tag in tags)
				{
					m_log.WriteLine("Tag {0}:", tag);
					moveRecords.AddRange(AnalyseProblematicTag(tag, finalCommits));
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

		private IEnumerable<CommitMoveRecord> AnalyseProblematicTag(string tag,Dictionary<string,Commit> finalCommits)
		{
			var moveRecords = new List<CommitMoveRecord>();
			CommitMoveRecord moveRecord = null;
			var state = new RepositoryState();
			var filesAtTagRevision = new Dictionary<string, Commit>();
			var finalCommit = finalCommits[tag];
			var branch = finalCommit.Branch;
			var commitsToMove = new List<Commit>();

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
						else if (state[branch][file.Name] != Revision.Empty && IsAddedFile(fileRevision, tag))
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

		private bool IsAddedFile(FileRevision fileRevision, string tag)
		{
			var file = fileRevision.File;
			return (GetRevisionForTag(file, tag) == Revision.Empty && m_missingFiles[tag].Contains(file.Name));
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

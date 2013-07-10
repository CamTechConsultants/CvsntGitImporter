/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Extension methods on a list of commits.
	/// </summary>
	static class CommitListExtensions
	{
		public static IEnumerable<Commit> SplitMultiBranchCommits(this IEnumerable<Commit> commits)
		{
			return new SplitMultiBranchCommits(commits);
		}

		public static IEnumerable<Commit> FilterCommitsOnExcludedBranches(this IEnumerable<Commit> commits)
		{
			return commits.Where(c => c.Branch != null);
		}

		/// <summary>
		/// Convert a list of items to a concrete IList, or return the existing IList if it already is one.
		/// </summary>
		public static IList<T> ToListIfNeeded<T>(this IEnumerable<T> list)
		{
			return list as IList<T> ?? list.ToList();
		}

		/// <summary>
		/// Verify commits
		/// </summary>
		public static IEnumerable<Commit> Verify(this IEnumerable<Commit> commits, ILogger log)
		{
			bool anyFailed = false;

			foreach (var commit in commits)
			{
				bool passed = commit.Verify();
				yield return commit;

				if (!passed)
				{
					if (!anyFailed)
					{
						log.DoubleRuleOff();
						log.WriteLine("Commit verification");
						anyFailed = true;
					}

					using (log.Indent())
					{
						log.WriteLine("Verification failed: {0} {1}", commit.CommitId, commit.Time);
						foreach (var revision in commit)
							log.WriteLine("  {0} r{1}", revision.File, revision.Revision);

						foreach (var error in commit.Errors)
						{
							log.WriteLine(error);
						}

						log.RuleOff();
					}
				}
			}

			if (anyFailed)
				throw new RepositoryConsistencyException("One or more commits failed verification");
		}

		/// <summary>
		/// Find the index of an item working backwards starting from an index.
		/// </summary>
		public static int IndexOfFromEnd<T>(this IList<T> list, T item)
		{
			return IndexOfFromEnd(list, item, list.Count - 1);
		}

		/// <summary>
		/// Find the index of an item working backwards starting from an index.
		/// </summary>
		public static int IndexOfFromEnd<T>(this IList<T> list, T item, int start)
		{
			for (int i = start; i >= 0; i--)
			{
				if (item.Equals(list[i]))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Move an item in a list forwards, shuffling all the items inbetween backwards.
		/// </summary>
		public static void Move(this IList<Commit> list, int sourceIndex, int destIndex)
		{
			if (sourceIndex < 0 || sourceIndex >= list.Count)
				throw new ArgumentOutOfRangeException("sourceIndex");
			if (destIndex < 0 || destIndex >= list.Count)
				throw new ArgumentOutOfRangeException("destIndex");

			if (destIndex == sourceIndex)
				return;

			var moveItem = list[sourceIndex];
			var saveDestIndex = list[destIndex].Index;

			if (destIndex > sourceIndex)
			{
				for (int i = destIndex; i > sourceIndex; i--)
					list[i].Index = list[i - 1].Index;

				for (int i = sourceIndex; i < destIndex; i++)
					list[i] = list[i + 1];
			}
			else
			{
				for (int i = destIndex; i < sourceIndex; i++)
					list[i].Index = list[i + 1].Index;

				for (int i = sourceIndex; i > destIndex; i--)
					list[i] = list[i - 1];
			}

			moveItem.Index = saveDestIndex;
			list[destIndex] = moveItem;
		}

		/// <summary>
		/// Filter out excluded files from the stream of commits.
		/// </summary>
		public static IEnumerable<Commit> FilterExcludedFiles(this IEnumerable<Commit> commits, ExclusionFilter filter)
		{
			return filter.Filter(commits);
		}

		/// <summary>
		/// Split a flat list of commits into a set of streams per branch.
		/// </summary>
		public static BranchStreamCollection SplitBranchStreams(this IEnumerable<Commit> commits, IDictionary<string, Commit> branchpoints)
		{
			return new BranchStreamCollection(commits, branchpoints);
		}

		/// <summary>
		/// Add commits to files, creating lookups of file revisions to their commits.
		/// </summary>
		public static IEnumerable<Commit> AddCommitsToFiles(this IEnumerable<Commit> commits)
		{
			var list = commits.ToListIfNeeded();

			foreach (var commit in list)
			{
				foreach (var f in commit)
				{
					f.File.AddCommit(commit, f.Revision);
				}
			}

			return list;
		}
	}
}
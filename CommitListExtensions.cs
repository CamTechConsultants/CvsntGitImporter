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
	/// Extension methods on a list of commits.
	/// </summary>
	static class CommitListExtensions
	{
		public static IEnumerable<Commit> SplitMultiBranchCommits(this IEnumerable<Commit> commits)
		{
			return new SplitMultiBranchCommits(commits);
		}

		/// <summary>
		/// Convert a list of items to a concrete IList, or return the existing IList if it already is one.
		/// </summary>
		public static IList<T> ToListIfNeeded<T>(this IEnumerable<T> list)
		{
			return list as IList<T> ?? list.ToList();
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
		public static void Move<T>(this IList<T> list, int sourceIndex, int destIndex)
		{
			if (sourceIndex < 0 || sourceIndex >= list.Count)
				throw new ArgumentOutOfRangeException("sourceIndex");
			if (destIndex < 0 || destIndex >= list.Count)
				throw new ArgumentOutOfRangeException("destIndex");

			if (destIndex == sourceIndex)
				return;

			var moveItem = list[sourceIndex];
			if (destIndex > sourceIndex)
			{
				for (int i = sourceIndex; i < destIndex; i++)
				{
					list[i] = list[i + 1];
				}
			}
			else
			{
				for (int i = sourceIndex; i > destIndex; i--)
				{
					list[i] = list[i - 1];
				}
			}

			list[destIndex] = moveItem;
		}

		/// <summary>
		/// Assign unique incrementing ids to commits. The ids won't necessarily be sequential, but they should
		/// rise monotonically along each branch.
		/// </summary>
		public static IList<Commit> AssignNumbers(this IEnumerable<Commit> commits)
		{
			var list = commits.ToListIfNeeded();
			for (int i = 0; i < list.Count; i++)
			{
				list[i].Index = i + 1;
			}

			return list;
		}

		/// <summary>
		/// Add commits to files, creating lookups of file revisions to their commits.
		/// </summary>
		public static IEnumerable<Commit> AddCommitsToFiles(this IEnumerable<Commit> commits)
		{
			var list = commits.ToListIfNeeded();

			foreach (var commit in commits)
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
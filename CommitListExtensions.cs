/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
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
	}
}
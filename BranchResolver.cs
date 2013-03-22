/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Resolves tags to specific commits.
	/// </summary>
	class BranchResolver : Resolver
	{
		public BranchResolver(IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles, InclusionMatcher tagMatcher) :
				base(commits: commits, allFiles: allFiles, tagMatcher: tagMatcher, problemLog: "problembranches.log")
		{
		}

		protected override IEnumerable<string> GetTagsForFileRevision(FileRevision fileRevision)
		{
			return fileRevision.File.GetBranchesAtRevision(fileRevision.Revision);
		}
	}
}
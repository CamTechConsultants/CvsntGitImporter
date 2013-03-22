/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CvsGitConverter
{
	/// <summary>
	/// Resolves tags to specific commits.
	/// </summary>
	class TagResolver : Resolver
	{
		public TagResolver(IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles, InclusionMatcher tagMatcher) :
				base(commits: commits, allFiles: allFiles, tagMatcher: tagMatcher, problemLog: "problemtags.log")
		{
		}

		protected override IEnumerable<string> GetTagsForFileRevision(FileRevision fileRevision)
		{
			return fileRevision.File.GetTagsForRevision(fileRevision.Revision);
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Resolves tags to specific commits.
	/// </summary>
	class TagResolver : TagResolverBase
	{
		public TagResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles, InclusionMatcher tagMatcher) :
				base(log: log, commits: commits, allFiles: allFiles, tagMatcher: tagMatcher)
		{
		}

		protected override IEnumerable<string> GetTagsForFileRevision(FileInfo file, Revision revision)
		{
			return file.GetTagsForRevision(revision);
		}

		protected override Revision GetRevisionForTag(FileInfo file, string tag)
		{
			return file.GetRevisionForTag(tag);
		}
	}
}
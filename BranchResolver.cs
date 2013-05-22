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
	/// Resolves tags to specific commits.
	/// </summary>
	class BranchResolver : TagResolverBase
	{
		public BranchResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles, InclusionMatcher tagMatcher) :
				base(log: log, commits: commits, allFiles: allFiles, tagMatcher: tagMatcher, branches: true)
		{
		}

		protected override IEnumerable<string> GetTagsForFileRevision(FileInfo file, Revision revision)
		{
			return file.GetBranchesAtRevision(revision);
		}

		protected override Revision GetRevisionForTag(FileInfo file, string tag)
		{
			return file.GetBranchpointForBranch(tag);
		}
	}
}
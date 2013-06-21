/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Resolves branches to specific commits.
	/// </summary>
	class AutoBranchResolver : AutoTagResolverBase
	{
		public AutoBranchResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles) :
				base(log: log, commits: commits, allFiles: allFiles, branches: true)
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
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
		private readonly ILogger m_log;

		public AutoBranchResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles) :
				base(log: log, commits: commits, allFiles: allFiles, branches: true)
		{
			m_log = log;
		}

		public override bool Resolve(IEnumerable<string> tags)
		{
			m_log.DoubleRuleOff();
			m_log.WriteLine("Resolving branches");

			using (m_log.Indent())
			{
				return base.Resolve(tags);
			}
		}

		protected override IEnumerable<string> GetTagsForFileRevision(FileInfo file, Revision revision)
		{
			return file.GetBranchesAtRevision(revision);
		}

		protected override Revision GetRevisionForTag(FileInfo file, string tag)
		{
			return file.GetBranchpointForBranch(tag);
		}

		protected override bool IsFileAtTag(RepositoryBranchState state, FileInfo file, string tag)
		{
			return base.IsFileAtTag(state, file, tag) || file.BranchAddedOn == tag;
		}
	}
}
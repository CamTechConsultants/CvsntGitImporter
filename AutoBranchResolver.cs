/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

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

		protected override void HandleMissingFiles(string tag, List<Commit> commits, IEnumerable<FileInfo> files,
				CommitMoveRecord moveRecord, ref Commit candidate)
		{
			var filteredFiles = files.Where(f => f.BranchAddedOn != tag);
			base.HandleMissingFiles(tag, commits, filteredFiles, moveRecord, ref candidate);
		}
	}
}
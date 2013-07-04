/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Resolves tags to specific commits.
	/// </summary>
	class TagResolver : AutoTagResolverBase
	{
		private readonly ILogger m_log;

		public TagResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles) :
				base(log: log, commits: commits, allFiles: allFiles)
		{
			m_log = log;
		}

		public override bool Resolve(IEnumerable<string> tags)
		{
			m_log.DoubleRuleOff();
			m_log.WriteLine("Resolving tags");

			using (m_log.Indent())
			{
				return base.Resolve(tags);
			}
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
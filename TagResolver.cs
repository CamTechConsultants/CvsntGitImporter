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
		private readonly InclusionMatcher m_tagMatcher;
		private HashSet<string> m_extraTags;

		public IEnumerable<string> ExtraTags
		{
			get { return m_extraTags; }
			set { m_extraTags = new HashSet<string>(value); }
		}

		public TagResolver(ILogger log, IEnumerable<Commit> commits, Dictionary<string, FileInfo> allFiles, InclusionMatcher tagMatcher) :
				base(log: log, commits: commits, allFiles: allFiles)
		{
			m_log = log;
			m_tagMatcher = tagMatcher;
			ExtraTags = Enumerable.Empty<string>();
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

		protected override bool MatchTag(string tag)
		{
			return m_tagMatcher.Match(tag) || m_extraTags.Contains(tag);
		}
	}
}
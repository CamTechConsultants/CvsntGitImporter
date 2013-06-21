/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	class ManualBranchResolver : ITagResolver
	{
		private readonly ILogger m_log;
		private readonly ITagResolver m_fallback;
		private readonly ITagResolver m_tagResolver;
		private readonly RenameRule m_branchpointRule;
		private Dictionary<string, Commit> m_resolvedCommits;

		public ManualBranchResolver(ILogger log, ITagResolver fallbackResolver, ITagResolver tagResolver, RenameRule branchpointRule)
		{
			m_log = log;
			m_fallback = fallbackResolver;
			m_tagResolver = tagResolver;
			m_branchpointRule = branchpointRule;
		}

		public IDictionary<string, Commit> ResolvedCommits
		{
			get { return m_resolvedCommits; }
		}

		public IEnumerable<string> UnresolvedTags
		{
			get { return m_fallback.UnresolvedTags; }
		}

		public IEnumerable<Commit> Commits
		{
			get { return m_fallback.Commits; }
		}

		public bool Resolve(IEnumerable<string> branches)
		{
			var rule = m_branchpointRule;
			m_resolvedCommits = new Dictionary<string, Commit>();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Matching branches to branchpoints");
			using (m_log.Indent())
			{
				foreach (var branch in branches.Where(b => rule.IsMatch(b)))
				{
					var tag = rule.Apply(branch);

					Commit commit;
					if (m_tagResolver.ResolvedCommits.TryGetValue(tag, out commit))
					{
						m_resolvedCommits[branch] = commit;
						m_log.WriteLine("Branch {0} -> Tag {1}", branch, tag);
					}
					else
					{
						m_log.WriteLine("Branch {0}: tag {1} is unresolved", branch, tag);
					}
				}
			}

			var otherBranches = branches.Except(m_resolvedCommits.Keys).ToList();
			if (otherBranches.Any())
			{
				var result = m_fallback.Resolve(otherBranches);
				foreach (var kvp in m_fallback.ResolvedCommits)
					m_resolvedCommits[kvp.Key] = kvp.Value;
				return result;
			}
			else
			{
				return true;
			}
		}
	}
}
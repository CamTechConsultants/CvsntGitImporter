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
		private IList<Commit> m_commits;

		public ManualBranchResolver(ILogger log, ITagResolver fallbackResolver, ITagResolver tagResolver, RenameRule branchpointRule)
		{
			m_log = log;
			m_fallback = fallbackResolver;
			m_tagResolver = tagResolver;
			m_branchpointRule = branchpointRule;
		}

		public IDictionary<string, Commit> ResolvedTags
		{
			get { return m_resolvedCommits; }
		}

		public IEnumerable<string> UnresolvedTags
		{
			get { return m_fallback.UnresolvedTags; }
		}

		public IEnumerable<Commit> Commits
		{
			get { return m_commits; }
		}

		public bool Resolve(IEnumerable<string> branches, IEnumerable<Commit> commits)
		{
			var rule = m_branchpointRule;
			m_resolvedCommits = new Dictionary<string, Commit>();
			m_commits = commits.ToListIfNeeded();

			m_log.DoubleRuleOff();
			m_log.WriteLine("Matching branches to branchpoints");
			using (m_log.Indent())
			{
				foreach (var branch in branches.Where(b => rule.IsMatch(b)))
				{
					var tag = rule.Apply(branch);

					var commit = ResolveBranchpoint(branch, tag);
					if (commit != null)
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
				var result = m_fallback.Resolve(otherBranches, m_commits);

				foreach (var kvp in m_fallback.ResolvedTags)
					m_resolvedCommits[kvp.Key] = kvp.Value;

				m_commits = m_fallback.Commits.ToListIfNeeded();
				return result;
			}
			else
			{
				return true;
			}
		}

		private Commit ResolveBranchpoint(string branch, string tag)
		{
			Commit branchCommit;
			if (!m_tagResolver.ResolvedTags.TryGetValue(tag, out branchCommit))
				return null;

			// check for commits to the branch that occur before the tag
			CommitMoveRecord moveRecord = null;
			foreach (var c in m_commits)
			{
				if (c == branchCommit)
					break;

				if (c.Branch == branch)
				{
					if (moveRecord == null)
						moveRecord = new CommitMoveRecord(branch, m_log) { FinalCommit = branchCommit };
					moveRecord.AddCommit(c, c.Select(r => r.File));
				}
			}

			if (moveRecord != null)
			{
				m_log.WriteLine("Some commits on {0} need moving after branchpoint {1}", branch, tag);
				using (m_log.Indent())
				{
					moveRecord.Apply(m_commits);
				}
			}

			return branchCommit;
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	/// <summary>
	/// Build commits from the CVS log.
	/// </summary>
	class CommitBuilder
	{
		private readonly CvsLogParser m_parser;

		public CommitBuilder(CvsLogParser parser)
		{
			m_parser = parser;
		}

		/// <summary>
		/// Get all the commits in a CVS log ordered by date.
		/// </summary>
		public IEnumerable<Commit> GetCommits()
		{
			var revisions = from r in m_parser.Parse()
							where !(r.Revision == "1.1" && Regex.IsMatch(r.Message, @"file .* was initially added on branch "))
							select r;

			var lookup = new Dictionary<string, Commit>();

			foreach (var revision in revisions)
			{
				Commit commit;
				if (lookup.TryGetValue(revision.CommitId, out commit))
				{
					commit.Add(revision);
				}
				else
				{
					commit = new Commit(revision.CommitId) { revision };
					lookup.Add(commit.CommitId, commit);
				}
			}

			return lookup.Values.OrderBy(c => c.Time).ToList();
		}
	}
}

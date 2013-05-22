/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Build commits from the CVS log.
	/// </summary>
	class CommitBuilder
	{
		private readonly IEnumerable<FileRevision> m_fileRevisions;

		public CommitBuilder(IEnumerable<FileRevision> fileRevisions)
		{
			m_fileRevisions = fileRevisions;
		}

		/// <summary>
		/// Get all the commits in a CVS log ordered by date.
		/// </summary>
		public IEnumerable<Commit> GetCommits()
		{
			var revisions = from r in m_fileRevisions
							where !(r.Revision == "1.1" && Regex.IsMatch(r.Message, @"file .* was initially added on branch "))
							select r;

			var lookup = new Dictionary<string, Commit>();
			var commitsByMessage = new CommitsByMessage();

			foreach (var revision in revisions)
			{
				if (revision.CommitId.Length == 0)
				{
					commitsByMessage.Add(revision);
				}
				else
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
			}

			return lookup.Values.Concat(commitsByMessage.Resolve()).OrderBy(c => c.Time).ToList();
		}


		private class CommitsByMessage
		{
			private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(10);
			private readonly Dictionary<string, List<FileRevision>> m_revisions = new Dictionary<string, List<FileRevision>>();
			private int m_nextCommitId;

			public void Add(FileRevision revision)
			{
				List<FileRevision> list;
				if (m_revisions.TryGetValue(revision.Message, out list))
					list.Add(revision);
				else
					m_revisions.Add(revision.Message, new List<FileRevision> { revision });
			}

			public IEnumerable<Commit> Resolve()
			{
				foreach (var revisionList in m_revisions.Values)
				{
					revisionList.Sort((a, b) => DateTime.Compare(a.Time, b.Time));
					int start = 0;
					var lastTime = revisionList[0].Time;

					for (int i = 1; i < revisionList.Count; i++)
					{
						if (revisionList[i].Time - lastTime > MaxInterval)
						{
							yield return MakeCommit(revisionList, start, i);
							start = i;
						}
					}

					if (start < revisionList.Count)
						yield return MakeCommit(revisionList, start, revisionList.Count);
				}
			}

			private Commit MakeCommit(List<FileRevision> revisions, int start, int end)
			{
				var commit = new Commit(MakeCommitId(revisions[start]));
				for (int i = start; i < end; i++)
					commit.Add(revisions[i]);
				return commit;
			}

			private string MakeCommitId(FileRevision r)
			{
				return String.Format("{0:yyMMdd}-{1}-{2}", r.Time, r.Author, m_nextCommitId++);
			}
		}
	}
}

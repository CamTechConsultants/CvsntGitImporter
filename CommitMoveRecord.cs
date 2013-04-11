﻿/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CvsGitConverter
{
	/// <summary>
	/// Holds information about commits that need to be moved relative to some final commit, to
	/// resolve a tag or a branch point.
	/// </summary>
	class CommitMoveRecord
	{
		private Commit m_finalCommit;
		private readonly ILogger m_log;
		private readonly List<Commit> m_commits = new List<Commit>();
		private readonly Dictionary<string, IEnumerable<FileRevision>> m_files = new Dictionary<string, IEnumerable<FileRevision>>();

		public CommitMoveRecord(Commit finalCommit, ILogger log)
		{
			m_finalCommit = finalCommit;
			m_log = log;
		}

		public void AddCommit(Commit commit, List<FileRevision> filesToMove)
		{
			m_commits.Add(commit);
			m_files[commit.CommitId] = filesToMove;
		}

		public void Apply(IList<Commit> commitStream)
		{
			int destLocation = commitStream.IndexOf(m_finalCommit);
			int searchStart = destLocation;

			m_log.WriteLine("Final commit: {0}", m_finalCommit.CommitId);

			using (m_log.Indent())
			{
				// handle in reverse order
				for (int i = m_commits.Count - 1; i >= 0; i--)
				{
					var commitToMove = m_commits[i];
					int location = commitStream.IndexOfFromEnd(commitToMove, searchStart);
					if (location < 0)
					{
						// assume already moved
						m_log.WriteLine("Skip moving {0} after {1}", commitToMove.CommitId, m_finalCommit.CommitId);
						continue;
					}

					// does the commit need splitting?
					var fileRevisions = m_files[commitToMove.CommitId];
					if (fileRevisions.Count() < commitToMove.Count())
					{
						m_log.WriteLine("Split {0}", commitToMove.CommitId);

						using (m_log.Indent())
						{
							// commit1 is the files that do not need moving
							var commit1 = CreateCommitSubset(commitToMove, "1", commitToMove.Except(fileRevisions));

							// commit2 is the files that do need moving
							var commit2 = CreateCommitSubset(commitToMove, "2", fileRevisions);

							commitStream[location] = commit1;
							commitStream.Insert(++location, commit2);
							destLocation++;

							commitToMove = commit2;
							if (m_finalCommit == commitToMove)
								m_finalCommit = commit2;
						}
					}

					m_log.WriteLine("Move {0}({1}) after {2}({3})", commitToMove.CommitId, location,
								m_finalCommit.CommitId, destLocation);
					commitStream.Move(location, destLocation);
					destLocation--;
				}
			}
		}

		private Commit CreateCommitSubset(Commit parent, string suffix, IEnumerable<FileRevision> revisions)
		{
			var commit = new Commit(parent.CommitId + "-" + suffix);
			m_log.WriteLine("New commit {0}", commit.CommitId);

			using (m_log.Indent())
			{
				foreach (var f in revisions)
				{
					m_log.WriteLine("{0}", f);
					commit.Add(f);
				}
			}

			return commit;
		}
	}
}
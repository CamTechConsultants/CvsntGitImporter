/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Holds information about commits that need to be moved relative to some final commit, to
	/// resolve a tag or a branch point.
	/// </summary>
	class CommitMoveRecord
	{
		private readonly string m_tag;
		private readonly ILogger m_log;
		private readonly OneToManyDictionary<Commit, FileInfo> m_files = new OneToManyDictionary<Commit, FileInfo>(CommitComparer.ById);
		private Commit m_finalCommit;

		public CommitMoveRecord(string tag, ILogger log)
		{
			m_tag = tag;
			m_log = log;
		}

		public Commit FinalCommit
		{
			get
			{
				return m_finalCommit;
			}
			set
			{
				m_finalCommit = value;

				// if the final commit is in the list of ones to be moved and does not need splitting, then remove it
				if (value.Count() == m_files[value].Count())
					m_files.Remove(value);
			}
		}

		public IEnumerable<Commit> Commits
		{
			get { return m_files.Keys; }
		}

		public override string ToString()
		{
			return String.Format("{0}: {1}, {2} commits", m_tag, m_finalCommit.CommitId, m_files.Count);
		}

		public void AddCommit(Commit commit, IEnumerable<FileInfo> filesToMove)
		{
			m_files.AddRange(commit, filesToMove);
		}

		public void AddCommit(Commit commit, FileInfo fileToMove)
		{
			m_files.Add(commit, fileToMove);
		}

		public void Apply(IList<Commit> commitStream)
		{
			int destLocation = commitStream.IndexOf(m_finalCommit);
			int searchStart = destLocation;
			var commits = m_files.Keys.OrderBy(c => c.Index).ToList();

			Dump();
			m_log.WriteLine("Applying:");

			using (m_log.Indent())
			{
				// handle in reverse order
				for (int i = commits.Count - 1; i >= 0; i--)
				{
					var commitToMove = commits[i];
					int location = commitStream.IndexOfFromEnd(commitToMove, searchStart);
					if (location < 0)
					{
						// assume already moved
						m_log.WriteLine("Skip moving {0} after {1}", commitToMove.ConciseFormat, m_finalCommit.ConciseFormat);
						continue;
					}

					// does the commit need splitting?
					var files = m_files[commitToMove];
					if (files.Count() < commitToMove.Count())
					{
						m_log.WriteLine("Split {0}", commitToMove.CommitId);

						using (m_log.Indent())
						{
							int index = commitToMove.Index;
							Commit splitCommitNeedMove;
							Commit splitCommitNoMove;
							SplitCommit(commitToMove, files, out splitCommitNeedMove, out splitCommitNoMove);

							commitStream[location] = splitCommitNoMove;
							commitStream.Insert(location + 1, splitCommitNeedMove);
							destLocation++;

							if (m_finalCommit == commitToMove)
								m_finalCommit = splitCommitNeedMove;
							commitToMove = splitCommitNeedMove;

							// update Commit indices
							for (int j = location; j < commitStream.Count; j++)
								commitStream[j].Index = index++;

							location++;
						}
					}

					m_log.WriteLine("Move {0}({1}) after {2}({3})", commitToMove.ConciseFormat, location,
								m_finalCommit.ConciseFormat, destLocation);
					commitStream.Move(location, destLocation);
					destLocation--;
				}
			}
		}

		private void Dump()
		{
			if (m_log.DebugEnabled)
			{
				m_log.WriteLine("Commits requiring moving");
				using (m_log.Indent())
				{
					foreach (var commit in m_files.Keys.OrderBy(c => c.Index))
					{
						m_log.WriteLine("{0}", commit.ConciseFormat);
						using (m_log.Indent())
						{
							foreach (var f in m_files[commit].OrderBy(f => f.Name))
								m_log.WriteLine("{0} should be at r{1}", f.Name, f.GetRevisionForTag(m_tag));
						}
					}
				}
			}
		}

		private void SplitCommit(Commit parent, IEnumerable<FileInfo> files, out Commit included, out Commit excluded)
		{
			included = new Commit(parent.CommitId + "-1");
			excluded = new Commit(parent.CommitId + "-2");
			m_log.WriteLine("New commit {0}", included.CommitId);
			m_log.WriteLine("New commit {0}", excluded.CommitId);

			using (m_log.Indent())
			{
				foreach (var revision in parent)
				{
					Commit commit = files.Contains(revision.File) ? included : excluded;
					m_log.WriteLine("  {0}: add {1}", commit.CommitId, revision.File.Name);
					commit.Add(revision);
					revision.File.UpdateCommit(commit, revision.Revision);
				}
			}
		}
	}
}
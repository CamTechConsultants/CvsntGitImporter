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
	/// Represents a set of changes to files committed in one go.
	/// </summary>
	class Commit : IEnumerable<FileRevision>
	{
		private readonly List<FileRevision> m_files = new List<FileRevision>();
		private DateTime? m_time;
		private string m_message;
		private string m_author;
		private string m_branch;
		private List<string> m_errors;
		private List<Commit> m_branches;

		/// <summary>
		/// The CVS commit ID.
		/// </summary>
		public readonly string CommitId;

		/// <summary>
		/// A unique numeric id for the commit.
		/// </summary>
		public int Index;

		/// <summary>
		/// Gets a concise string suitable for debug log files.
		/// </summary>
		public string ConciseFormat
		{
			get { return String.Format("{{{0}[{1}] {2}}}", CommitId, Index, Time); }
		}

		/// <summary>
		/// The commit's direct predecessor.
		/// </summary>
		public Commit Predecessor;

		/// <summary>
		/// The commit's direct predecessor.
		/// </summary>
		public Commit Successor;

		/// <summary>
		/// Gets a list of branches that this commit is a branchpoint for.
		/// </summary>
		public IEnumerable<Commit> Branches
		{
			get { return m_branches ?? Enumerable.Empty<Commit>(); }
		}

		/// <summary>
		/// Is this a commit a branchpoint for any other branches?
		/// </summary>
		public bool IsBranchpoint
		{
			get { return m_branches != null && m_branches.Any(); }
		}

		/// <summary>
		/// Gets the date and time of the commit.
		/// </summary>
		public DateTime Time
		{
			get
			{
				if (m_time.HasValue)
					return m_time.Value;

				var time = m_files.Select(c => c.Time).Min();
				m_time = time;
				return time;
			}
		}

		/// <summary>
		/// Gets the commit message for this commit.
		/// </summary>
		public string Message
		{
			get
			{
				if (m_message == null)
					m_message = String.Join(Environment.NewLine + Environment.NewLine, m_files.Select(c => c.Message).Distinct());
				return m_message;
			}
		}

		/// <summary>
		/// Gets the author of the commit.
		/// </summary>
		public string Author
		{
			get
			{
				if (m_author == null)
					m_author = m_files.First().Author;

				return m_author;
			}
		}

		/// <summary>
		/// Gets the name of the branch this commit is on.
		/// </summary>
		public string Branch
		{
			get
			{
				if (m_branch == null)
					m_branch = m_files.First().Branch;

				return m_branch;
			}
		}

		/// <summary>
		/// Gets the files that are merged in this commit.
		/// </summary>
		public IEnumerable<FileRevision> MergedFiles
		{
			get { return m_files.Where(f => f.Mergepoint != Revision.Empty); }
		}

		/// <summary>
		/// A commit that this commit is a merge from.
		/// </summary>
		public Commit MergeFrom;

		/// <summary>
		/// Gets any errors in this commit after verification.
		/// </summary>
		public IEnumerable<string> Errors
		{
			get { return m_errors ?? Enumerable.Empty<string>(); }
		}


		public Commit(string commitId)
		{
			CommitId = commitId;
		}

		public void Add(FileRevision commit)
		{
			m_time = null;
			m_message = null;
			m_files.Add(commit);
		}

		public void AddBranch(Commit commit)
		{
			if (m_branches == null)
				m_branches = new List<Commit>(1) { commit };
			else
				m_branches.Add(commit);
		}

		public void ReplaceBranch(Commit existing, Commit replacement)
		{
			if (existing == null || replacement == null)
				throw new ArgumentNullException();

			int index = -1;
			if (m_branches != null)
				index = m_branches.IndexOf(existing);

			if (index < 0)
				throw new ArgumentException(String.Format("Commit {0} does not exist as a branch from this commit", existing.CommitId));

			m_branches[index] = replacement;
		}

		public bool Verify(bool fussy = false)
		{
			m_errors = null;

			var authors = m_files.Select(c => c.Author).Distinct();
			if (authors.Count() > 1)
				AddError("Multiple authors found: {0}", String.Join(", ", authors));

			if (fussy)
			{
				var times = m_files.Select(c => c.Time).Distinct();
				if (times.Max() - times.Min() >= TimeSpan.FromMinutes(1))
					AddError("Times vary too much: {0}", String.Join(", ", times));

				var branches = m_files.Select(c => c.Branch).Distinct();
				if (branches.Count() > 1)
					AddError("Multiple branches found: {0}", String.Join(", ", branches));
			}

			// check for a commit that merges from multiple branches
			List<string> mergedBranches = null;
			bool first = true;
			foreach (var fr in MergedFiles)
			{
				var thisFileBranches = PossibleMergedBranches(fr);
				if (!thisFileBranches.Any())
					continue;

				if (first)
				{
					mergedBranches = thisFileBranches.ToList();
					first = false;
				}
				else
				{
					var overlap = mergedBranches.Intersect(thisFileBranches).ToList();
					if (overlap.Any())
					{
						mergedBranches = overlap;
					}
					else
					{
						var buf = new StringBuilder();
						var branches = mergedBranches.Concat(thisFileBranches).Distinct();
						buf.AppendFormat("Multiple branches merged from found: {0}\r\n", FormatBranchList(branches));
						m_files.Aggregate(buf, (sb, f) => sb.AppendFormat("    {0}: {1}\r\n", f, FormatBranchList(PossibleMergedBranches(f))));
						AddError(buf.ToString());
						break;
					}
				}
			}

			return !Errors.Any();
		}

		private static IEnumerable<string> PossibleMergedBranches(FileRevision r)
		{
			if (r.Mergepoint.Equals(Revision.Empty))
				yield break;

			var branch = r.File.GetBranch(r.Mergepoint);
			if (branch != null)
				yield return branch;

			foreach (var otherBranch in r.File.GetBranchesAtRevision(r.Mergepoint))
				yield return otherBranch;
		}

		private static string FormatBranchList(IEnumerable<string> branches)
		{
			return String.Join(", ", branches.Select(b => b ?? "<excluded branch>"));
		}

		private void AddError(string format, params object[] args)
		{
			var msg = String.Format(format, args);

			if (m_errors == null)
				m_errors = new List<string>() { msg };
			else
				m_errors.Add(msg);
		}

		public override string ToString()
		{
			return String.Format("{0} time={1} {2}({3})",
					CommitId,
					Time,
					(Index == 0) ? "" : String.Format("index={0} ", Index),
					String.Join(", ", m_files));
		}


		public IEnumerator<FileRevision> GetEnumerator()
		{
			return m_files.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_files.GetEnumerator();
		}
	}
}
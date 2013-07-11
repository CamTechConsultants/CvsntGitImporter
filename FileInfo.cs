/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Information about a file in CVS.
	/// </summary>
	class FileInfo
	{
		private readonly Dictionary<string, Revision> m_revisionForTag = new Dictionary<string, Revision>();
		private readonly OneToManyDictionary<Revision, string> m_tagsForRevision = new OneToManyDictionary<Revision, string>();
		private readonly Dictionary<string, Revision> m_revisionForBranch = new Dictionary<string, Revision>();
		private readonly Dictionary<Revision, string> m_branchForRevision = new Dictionary<Revision, string>();
		private readonly Dictionary<Revision, Commit> m_commits = new Dictionary<Revision, Commit>();


		/// <summary>
		/// The file's name.
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The name of the branch that the file was created on.
		/// </summary>
		public string BranchAddedOn = "MAIN";

		/// <summary>
		/// Gets a list of all a file's tags.
		/// </summary>
		public IEnumerable<string> AllTags
		{
			get { return m_revisionForTag.Keys; }
		}

		/// <summary>
		/// Gets a list of all a file's branches.
		/// </summary>
		public IEnumerable<string> AllBranches
		{
			get { return m_revisionForBranch.Keys; }
		}


		public FileInfo(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Add a tag to the file.
		/// </summary>
		public void AddTag(string name, Revision revision)
		{
			if (revision.IsBranch)
				throw new ArgumentException(String.Format("Invalid tag revision: {0} is a branch tag revision", revision));

			m_revisionForTag[name] = revision;
			m_tagsForRevision.Add(revision, name);
		}

		/// <summary>
		/// Add a branch tag to the file. This is a pseudo revision that marks the revision that the branch
		/// starts at along with the branch "number" (since multiple branches can be made at a specific revision).
		/// E.g. revision 1.5.0.4 is a branch at revision 1.5 and its revisions will be 1.5.4.1, 1.5.4.2, etc.
		/// </summary>
		public void AddBranchTag(string name, Revision revision)
		{
			if (!revision.IsBranch)
				throw new ArgumentException(String.Format("Invalid branch tag revision: {0}", revision));

			m_revisionForBranch[name] = revision;
			m_branchForRevision[revision.BranchStem] = name;
		}

		/// <summary>
		/// Gets the branch that a revision is on.
		/// </summary>
		public string GetBranch(Revision revision)
		{
			if (revision.Parts.Count() == 2)
			{
				return "MAIN";
			}
			else
			{
				var branchStem = revision.BranchStem;
				string branchTag;
				return m_branchForRevision.TryGetValue(branchStem, out branchTag) ? branchTag : null;
			}
		}

		/// <summary>
		/// Gets the branch for a revision.
		/// </summary>
		public IEnumerable<string> GetBranchesAtRevision(Revision revision)
		{
			foreach (var kvp in m_branchForRevision)
			{
				if (kvp.Key.BranchStem.Equals(revision))
					yield return kvp.Value;
			}
		}

		/// <summary>
		/// Get the revision for the branchpoint for a branch.
		/// </summary>
		public Revision GetBranchpointForBranch(string branch)
		{
			Revision branchRevision;
			if (m_revisionForBranch.TryGetValue(branch, out branchRevision))
				return branchRevision.GetBranchpoint();
			else
				return Revision.Empty;
		}

		/// <summary>
		/// Gets a list of tags applied to a revision.
		/// </summary>
		public IEnumerable<string> GetTagsForRevision(Revision revision)
		{
			return m_tagsForRevision[revision];
		}

		/// <summary>
		/// Gets the revision for a tag.
		/// </summary>
		/// <returns>the revision that a tag is applied to, or Revision.Empty if the tag does not exist</returns>
		public Revision GetRevisionForTag(string tag)
		{
			Revision revision;
			if (m_revisionForTag.TryGetValue(tag, out revision))
				return revision;
			else
				return Revision.Empty;
		}

		/// <summary>
		/// Is a revision on a branch (or the branch's parent branch)
		/// </summary>
		public bool IsRevisionOnBranch(Revision revision, string branch)
		{
			if (branch == "MAIN")
				return revision.Parts.Count() == 2;

			Revision branchRevision;
			if (m_revisionForBranch.TryGetValue(branch, out branchRevision))
				return (revision.Parts.Count() > 2 && branchRevision.BranchStem == revision.BranchStem) || revision.Precedes(branchRevision);
			else
				return false;
		}

		/// <summary>
		/// Add a commit that references this file.
		/// </summary>
		public void AddCommit(Commit commit, Revision r)
		{
			m_commits.Add(r, commit);
		}

		/// <summary>
		/// Updates a commit that references this file.
		/// </summary>
		public void UpdateCommit(Commit commit, Revision r)
		{
			m_commits[r] = commit;
		}

		/// <summary>
		/// Get a commit for a specific revision.
		/// </summary>
		/// <returns>the commit that created that revision or null if not found</returns>
		public Commit GetCommit(Revision r)
		{
			Commit commit;
			return m_commits.TryGetValue(r, out commit) ? commit : null;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
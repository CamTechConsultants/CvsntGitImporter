/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CvsGitConverter
{
	/// <summary>
	/// Information about a file in CVS.
	/// </summary>
	class FileInfo
	{
		private readonly Dictionary<string, Revision> m_revisionForTag = new Dictionary<string, Revision>();
		private readonly Dictionary<Revision, List<string>> m_tagsForRevision = new Dictionary<Revision, List<string>>();
		private readonly Dictionary<string, Revision> m_revisionForBranch = new Dictionary<string, Revision>();
		private readonly Dictionary<Revision, string> m_branchForRevision = new Dictionary<Revision, string>();


		/// <summary>
		/// The file's name.
		/// </summary>
		public readonly string Name;


		public FileInfo(string name)
		{
			this.Name = name;
		}

		public void AddTag(string name, Revision revision)
		{
			// work out whether it's a normal tag or a branch tag
			if (revision.IsBranch)
			{
				m_revisionForBranch[name] = revision;
				m_branchForRevision[revision.BranchStem] = name;
			}
			else
			{
				m_revisionForTag[name] = revision;

				List<string> tags;
				if (m_tagsForRevision.TryGetValue(revision, out tags))
					tags.Add(name);
				else
					m_tagsForRevision[revision] = new List<string>(1) { name };
			}
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
				if (!m_branchForRevision.TryGetValue(branchStem, out branchTag))
				{
					throw new RepositoryConsistencyException(String.Format(
							"Branch with stem {0} not found on file {1} when looking for r{2}",
							branchStem, this.Name, revision));
				}

				return branchTag;
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
		/// Gets a list of tags applied to a revision.
		/// </summary>
		public IEnumerable<string> GetTagsForRevision(Revision revision)
		{
			List<string> tags;
			if (m_tagsForRevision.TryGetValue(revision, out tags))
				return tags;
			else
				return Enumerable.Empty<string>();
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
		/// <param name="revision"></param>
		/// <param name="branch"></param>
		/// <returns></returns>
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

		public override string ToString()
		{
			return Name;
		}
	}
}
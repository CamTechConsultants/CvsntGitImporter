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
		private readonly Dictionary<Revision, List<string>> m_tags = new Dictionary<Revision, List<string>>();
		private readonly Dictionary<Revision, string> m_branches = new Dictionary<Revision, string>();


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
				m_branches[revision.BranchStem] = name;
			}
			else
			{
				List<string> tags;
				if (m_tags.TryGetValue(revision, out tags))
					tags.Add(name);
				else
					m_tags[revision] = new List<string>(1) { name };
			}
		}

		/// <summary>
		/// Gets the branch for a revision.
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
				if (!m_branches.TryGetValue(branchStem, out branchTag))
				{
					throw new RepositoryConsistencyException(String.Format(
							"Branch with stem {0} not found on file {1} when looking for r{2}",
							branchStem, this.Name, revision));
				}

				return branchTag;
			}
		}

		/// <summary>
		/// Gets a list of tags applied to a revision.
		/// </summary>
		public IEnumerable<string> GetTags(Revision revision)
		{
			List<string> tags;
			if (m_tags.TryGetValue(revision, out tags))
				return tags;
			else
				return Enumerable.Empty<string>();
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
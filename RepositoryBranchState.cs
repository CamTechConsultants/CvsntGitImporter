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
	/// Tracks the versions of all files in the repository for a specific branch.
	/// </summary>
	class RepositoryBranchState
	{
		private readonly string m_branch;
		private readonly Dictionary<string, Revision> m_files = new Dictionary<string, Revision>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets or sets the current revision of a file.
		/// </summary>
		public Revision this[string filename]
		{
			get
			{
				Revision value;
				if (m_files.TryGetValue(filename, out value))
					return value;
				else
					return Revision.Empty;
			}
			set
			{
				var previousRevision = this[filename];

				if (!previousRevision.DirectlyPrecedes(value))
				{
					throw new RepositoryConsistencyException(String.Format(
							"Revision r{0} in {1} did not directly precede r{2}",
							value, filename, previousRevision));
				}

				SetUnsafe(filename, value);
			}
		}

		/// <summary>
		/// Set the revision for a file without any checks.
		/// </summary>
		public void SetUnsafe(string filename, Revision value)
		{
			m_files[filename] = value;
		}

		/// <summary>
		/// Gets all currently live files.
		/// </summary>
		public IEnumerable<string> LiveFiles
		{
			get { return m_files.Keys; }
		}

		public RepositoryBranchState(string branch)
		{
			m_branch = branch;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		private RepositoryBranchState(string branch, RepositoryBranchState other) : this(branch)
		{
			foreach (var kvp in other.m_files)
				m_files[kvp.Key] = kvp.Value;
		}

		/// <summary>
		/// Apply a commit.
		/// </summary>
		public void Apply(Commit commit)
		{
			foreach (var f in commit)
			{
				if (f.IsDead)
					m_files.Remove(f.File.Name);
				else
					m_files[f.File.Name] = f.Revision;
			}
		}

		/// <summary>
		/// Make a copy of this state.
		/// </summary>
		public RepositoryBranchState Copy(string branch)
		{
			return new RepositoryBranchState(branch, this);
		}
	}
}

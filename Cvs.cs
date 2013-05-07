/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CvsGitConverter
{
	/// <summary>
	/// CVS interface
	/// </summary>
	class Cvs
	{
		private readonly ICvsRepository m_repository;

		public Cvs(ICvsRepository repository)
		{
			m_repository = repository;
		}

		/// <summary>
		/// Get the files in a commit from the repository.
		/// </summary>
		public IEnumerable<FileContent> GetCommit(Commit commit)
		{
			foreach (var f in commit)
			{
				if (f.IsDead)
					yield return FileContent.CreateDeadFile(f.File.Name);
				else
					yield return m_repository.GetCvsRevision(f);
			}
		}
	}
}
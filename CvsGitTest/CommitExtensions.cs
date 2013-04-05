/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using CvsGitConverter;

namespace CvsGitTest
{
	/// <summary>
	/// Extension methods on the Commit class.
	/// </summary>
	static class CommitExtensions
	{
		public static Commit WithRevision(this Commit commit, FileInfo file, string revision, bool isDead = false)
		{
			var fileRevision = new FileRevision(file, Revision.Create(revision), Revision.Empty, DateTime.Now,
					"fred", commit.CommitId);
			commit.Add(fileRevision);
			return commit;
		}
	}
}
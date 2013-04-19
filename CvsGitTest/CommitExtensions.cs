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
		public static Commit WithRevision(this Commit commit, FileInfo file, string revision, string mergepoint = null, bool isDead = false)
		{
			var mergepointRevision = (mergepoint == null) ? Revision.Empty : Revision.Create(mergepoint);

			var fileRevision = new FileRevision(file,
					commitId: commit.CommitId,
					revision: Revision.Create(revision),
					mergepoint: mergepointRevision,
					isDead: isDead,
					time: DateTime.Now,
					author: "fred");

			commit.Add(fileRevision);
			file.AddCommit(commit, fileRevision.Revision);
			return commit;
		}
	}
}
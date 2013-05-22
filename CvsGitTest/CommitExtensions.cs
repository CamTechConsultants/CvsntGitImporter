/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Extension methods on the Commit class.
	/// </summary>
	static class CommitExtensions
	{
		public static Commit WithRevision(this Commit commit, FileInfo file, string revision, DateTime time = default(DateTime),
				string mergepoint = null, bool isDead = false)
		{
			var mergepointRevision = (mergepoint == null) ? Revision.Empty : Revision.Create(mergepoint);

			var fileRevision = file.CreateRevision(revision, commit.CommitId, time: time, mergepoint: mergepoint, isDead: isDead);
			commit.Add(fileRevision);
			file.AddCommit(commit, fileRevision.Revision);

			return commit;
		}

		public static IEnumerable<Commit> ToList(this Commit commit)
		{
			for (var c = commit; c != null; c = c.Successor)
				yield return c;
		}
	}
}
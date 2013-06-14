/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Extension methods on FileInfo class.
	/// </summary>
	static class FileInfoExtensions
	{
		/// <summary>
		/// Add a tag or branch to a FileInfo.
		/// </summary>
		public static FileInfo WithTag(this FileInfo file, string tagName, string revision)
		{
			file.AddTag(tagName, revision);
			return file;
		}

		/// <summary>
		/// Add a branch to a FileInfo.
		/// </summary>
		public static FileInfo WithBranch(this FileInfo file, string branchName, string revision)
		{
			Assert.IsTrue(((Revision)revision).IsBranch);
			file.AddBranchTag(branchName, revision);
			return file;
		}

		/// <summary>
		/// Create a FileRevision for a file.
		/// </summary>
		public static FileRevision CreateRevision(this FileInfo file, string revision, string commitId,
				string author = "fred", string mergepoint = null, bool isDead = false)
		{
			return CreateRevision(file, revision, commitId, DateTime.Now, author, mergepoint, isDead);
		}
		/// <summary>
		/// Create a FileRevision for a file.
		/// </summary>
		public static FileRevision CreateRevision(this FileInfo file, string revision, string commitId, DateTime time,
				string author = "fred", string mergepoint = null, bool isDead = false)
		{
			var mergepointRevision = (mergepoint == null) ? Revision.Empty : Revision.Create(mergepoint);

			return new FileRevision(
					file,
					commitId: commitId,
					revision: Revision.Create(revision),
					mergepoint: mergepointRevision,
					isDead: isDead,
					time: (time == default(DateTime)) ? DateTime.Now : time,
					author: author);
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	[TestClass]
	public class SplitMultiBranchCommitsTest
	{
		[TestMethod]
		public void SplitsCommitWithTwoBranches()
		{
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");

			var id = "id1";
			var commit = new Commit(id)
			{
				CreateFileRevision(file1, "1.1", id),
				CreateFileRevision(file2, "1.1.2.1", id),
			};

			var splitter = new SplitMultiBranchCommits(new[] { commit });
			var splitCommits = splitter.ToList();

			Assert.AreEqual(splitCommits.Count, 2);
			Assert.AreEqual(splitCommits.First().Single().File.Name, "file1");
			Assert.AreEqual(splitCommits.Last().Single().File.Name, "file2");
		}


		private FileRevision CreateFileRevision(FileInfo file, string revision, string commitId)
		{
			return new FileRevision(file, Revision.Create(revision), Revision.Empty, DateTime.Now,
					"fred", commitId);
		}
	}
}
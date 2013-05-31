/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the RepositoryState class.
	/// </summary>
	[TestClass]
	public class RepositoryStateTest
	{
		[TestMethod]
		public void BranchesInheritParentFiles()
		{
			var state = new RepositoryState();

			var f1 = new FileInfo("file1").WithBranch("branch", "1.1.0.2");
			var f2 = new FileInfo("file2");

			var mainCommit = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var branchCommit = new Commit("c2").WithRevision(f1, "1.1.2.1");
			mainCommit.AddBranch(branchCommit);

			state.Apply(mainCommit);
			state.Apply(branchCommit);

			var liveFiles = state["branch"].LiveFiles.OrderBy(i => i);
			Assert.IsTrue(liveFiles.SequenceEqual("file1", "file2"));
			Assert.AreEqual(state["branch"]["file1"].ToString(), "1.1.2.1");
			Assert.AreEqual(state["branch"]["file2"].ToString(), "1.1");
		}
	}
}

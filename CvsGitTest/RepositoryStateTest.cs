/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
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
		public void WithFullBranchState_BranchesInheritParentFiles()
		{
			var f1 = new FileInfo("file1").WithBranch("branch", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2");
			var f3 = new FileInfo("file3").WithBranch("branch", "1.2.0.2");

			var mainCommit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1").WithRevision(f3, "1.1");
			var mainCommit2 = new Commit("c2").WithRevision(f3, "1.2");
			var branchCommit = new Commit("c3").WithRevision(f1, "1.1.2.1");
			mainCommit2.AddBranch(branchCommit);
			var commits = new[] { mainCommit1, mainCommit2, branchCommit };

			var state = RepositoryState.CreateWithFullBranchState(commits.CreateAllFiles());

			foreach (var c in commits)
				state.Apply(c);

			var liveFiles = state["branch"].LiveFiles.OrderBy(i => i);
			Assert.IsTrue(liveFiles.SequenceEqual("file1", "file2", "file3"));
			Assert.AreEqual(state["branch"]["file1"].ToString(), "1.1.2.1");
			Assert.AreEqual(state["branch"]["file2"].ToString(), "1.1");
			Assert.AreEqual(state["branch"]["file3"].ToString(), "1.2");
		}

		[TestMethod]
		public void WithFullBranchState_FileAddedOnBranch()
		{
			var f1 = new FileInfo("file1").WithBranch("branch", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2");

			var mainCommit = new Commit("c1").WithRevision(f1, "1.1");
			var commits = new[] { mainCommit };
			var allFiles = commits.CreateAllFiles(f2);

			var state = RepositoryState.CreateWithFullBranchState(allFiles);
			state.Apply(mainCommit);

			Assert.AreEqual(state["branch"].LiveFiles.Single(), "file1");
			Assert.AreEqual(state["branch"]["file1"].ToString(), "1.1");
			Assert.AreSame(state["branch"]["file2"], Revision.Empty);
		}

		[TestMethod]
		public void WithFullBranchState_FileAddedOnParentAfterBranch()
		{
			var f1 = new FileInfo("file1").WithBranch("branch", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c1").WithRevision(f1, "1.1"),
				new Commit("c2").WithRevision(f1, "1.1.2.1"),
				new Commit("c3").WithRevision(f2, "1.1"),
			};
			var allFiles = commits.CreateAllFiles(f2);

			var state = RepositoryState.CreateWithFullBranchState(allFiles);
			foreach (var commit in commits)
				state.Apply(commit);

			Assert.AreEqual(state["branch"].LiveFiles.Count(), 2);
			Assert.AreEqual(state["branch"]["file1"].ToString(), "1.1.2.1");
			Assert.AreEqual(state["branch"]["file2"].ToString(), "1.1");
		}
	}
}

/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	///
	/// </summary>
	[TestClass]
	public class RepositoryBranchStateTest
	{
		[TestMethod]
		public void ApplyCommit_FilesUpdated()
		{
			var repoState = new RepositoryBranchState("MAIN");

			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var commit1 = new Commit("id1")
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit2 = new Commit("id2")
					.WithRevision(file2, "1.2");

			repoState.Apply(commit1);
			repoState.Apply(commit2);

			Assert.AreEqual(repoState[file1.Name], Revision.Create("1.1"));
			Assert.AreEqual(repoState[file2.Name], Revision.Create("1.2"));
		}

		[TestMethod]
		public void ApplyCommit_FileDeleted()
		{
			var repoState = new RepositoryBranchState("MAIN");

			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var commit1 = new Commit("id1")
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit2 = new Commit("id2")
					.WithRevision(file2, "1.2", isDead: true);

			repoState.Apply(commit1);
			repoState.Apply(commit2);

			Assert.AreEqual(repoState[file1.Name], Revision.Create("1.1"));
			Assert.AreEqual(repoState[file2.Name], Revision.Empty);
		}
	}
}
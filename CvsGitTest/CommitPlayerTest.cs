/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the CommitPlayer class.
	/// </summary>
	[TestClass]
	public class CommitPlayerTest
	{
		[TestMethod]
		public void NoBranches_PlaysInSequence()
		{
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var commit0 = new Commit("id0")
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit1 = new Commit("id1")
					.WithRevision(file1, "1.2");

			var commit2 = new Commit("id2")
					.WithRevision(file2, "1.2");

			var commits = new[] { commit0, commit1, commit2 };

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), commits, new Dictionary<string, Commit>());
			var result = player.Play().ToList();

			Assert.IsTrue(result.SequenceEqual(commits));
		}

		[TestMethod]
		public void SingleBranch_PlaysInSequence()
		{
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2");

			var commit0 = new Commit("id0")
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit1 = new Commit("id1")
					.WithRevision(file1, "1.2");

			var commit2 = new Commit("branch0")
					.WithRevision(file2, "1.1.2.1");

			var commits = new[] { commit0, commit1, commit2 };
			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", commit0 }
			};

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), commits, branchpoints);
			var result = player.Play().Select(c => c.CommitId).ToList();

			Assert.IsTrue(result.SequenceEqual("id0", "branch0", "id1"));
		}

		[TestMethod]
		public void NestedBranches_PlaysInSequence()
		{
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2").WithBranch("branch0", "1.1.0.2").WithBranch("branch1", "1.1.2.1.0.2");

			var commit0 = new Commit("id0")
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit1 = new Commit("id1")
					.WithRevision(file1, "1.2");

			var commit2 = new Commit("branch0_0")
					.WithRevision(file2, "1.1.2.1");

			var commit3 = new Commit("branch0_1")
					.WithRevision(file2, "1.1.2.2");

			var commit4 = new Commit("branch1_0")
					.WithRevision(file2, "1.1.2.1.2.1");

			var commits = new[] { commit0, commit1, commit2, commit3, commit4 };
			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch0", commit0 },
				{ "branch1", commit2 }
			};

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), commits, branchpoints);
			var result = player.Play().Select(c => c.CommitId).ToList();

			Assert.IsTrue(result.SequenceEqual("id0", "branch0_0", "branch1_0", "branch0_1", "id1"));
		}
	}
}
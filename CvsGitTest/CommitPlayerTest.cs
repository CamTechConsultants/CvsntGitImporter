/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
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

			var commit0 = new Commit("id0") { Index = 1 }
					.WithRevision(file1, "1.1")
					.WithRevision(file2, "1.1");

			var commit1 = new Commit("id1") { Index = 2 }
					.WithRevision(file1, "1.2");

			var commit2 = new Commit("id2") { Index = 3 }
					.WithRevision(file2, "1.2");

			var commits = new[] { commit0, commit1, commit2 };
			var branches = new BranchStreamCollection(commits, new Dictionary<string, Commit>());

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), branches);
			var result = player.Play().ToList();

			Assert.IsTrue(result.SequenceEqual(commits));
		}

		[TestMethod]
		public void SingleBranch_PlaysInSequence()
		{
			var now = DateTime.Now;
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2");

			var commit0 = new Commit("id0") { Index = 1 }
					.WithRevision(file1, "1.1", time: now)
					.WithRevision(file2, "1.1", time: now);

			var commit1 = new Commit("id1") { Index = 2 }
					.WithRevision(file1, "1.2", time: now + TimeSpan.FromMinutes(2))
					.WithRevision(file2, "1.2", time: now + TimeSpan.FromMinutes(2), mergepoint: "1.1.2.1");

			var commit2 = new Commit("branch0") { Index = 3 }
					.WithRevision(file2, "1.1.2.1", time: now + TimeSpan.FromMinutes(1));

			var commits = new[] { commit0, commit1, commit2 };
			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", commit0 }
			};
			var branches = new BranchStreamCollection(commits, branchpoints);
			commit1.MergeFrom = commit2;

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), branches);
			var result = player.Play().Select(c => c.CommitId).ToList();

			Assert.IsTrue(result.SequenceEqual("id0", "branch0", "id1"));
		}

		[TestMethod]
		public void NestedBranches_PlaysInSequence()
		{
			var now = DateTime.Now;
			var commits = CreateNestedBranches();
			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch0", commits[0] },
				{ "branch1", commits[2] }
			};
			var branches = new BranchStreamCollection(commits, branchpoints);

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), branches);
			var result = player.Play().Select(c => c.CommitId).ToList();

			Assert.IsTrue(result.SequenceEqual(new[] { "id0", "branch0_0", "branch1_0", "branch0_1", "id1" }));
		}

		[TestMethod]
		public void NestedBranches_Merges_OutOfOrder()
		{
			var now = DateTime.Now;
			var commits = CreateNestedBranches(true);
			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch0", commits[0] },
				{ "branch1", commits[2] }
			};
			var branches = new BranchStreamCollection(commits, branchpoints);

			var player = new CommitPlayer(MockRepository.GenerateStub<ILogger>(), branches);
			var result = player.Play().Select(c => c.CommitId).ToList();

			Assert.IsTrue(result.SequenceEqual(new[] { "id0", "branch0_0", "branch1_0", "branch0_1", "id1" }));
		}


		private static Commit[] CreateNestedBranches(bool outOfOrder = false)
		{
			DateTime now = DateTime.Now;
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2").WithBranch("branch0", "1.1.0.2").WithBranch("branch1", "1.1.2.1.0.2");

			var commit0 = new Commit("id0") { Index = 1 }
					.WithRevision(file1, "1.1", time: now)
					.WithRevision(file2, "1.1", time: now);

			var commit1 = new Commit("id1") { Index = 2 }
					.WithRevision(file1, "1.2", time: now + TimeSpan.FromMinutes(outOfOrder ? 1 : 4))
					.WithRevision(file2, "1.2", time: now + TimeSpan.FromMinutes(outOfOrder ? 1 : 4), mergepoint: "1.1.2.2");

			var commit2 = new Commit("branch0_0") { Index = 3 }
					.WithRevision(file2, "1.1.2.1", time: now + TimeSpan.FromMinutes(1));

			var commit3 = new Commit("branch0_1") { Index = 4 }
					.WithRevision(file2, "1.1.2.2", time: now + TimeSpan.FromMinutes(outOfOrder ? 1 : 3), mergepoint: "1.1.2.1.2.1");

			var commit4 = new Commit("branch1_0") { Index = 5 }
					.WithRevision(file2, "1.1.2.1.2.1", time: now + TimeSpan.FromMinutes(outOfOrder ? 1 : 2));

			commit3.MergeFrom = commit4;
			commit1.MergeFrom = commit3;

			return new[] { commit0, commit1, commit2, commit3, commit4 };
		}
	}
}
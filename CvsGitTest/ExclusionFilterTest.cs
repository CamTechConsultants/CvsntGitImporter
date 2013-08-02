/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the ExclusionFilter class.
	/// </summary>
	[TestClass]
	public class ExclusionFilterTest
	{
		private ILogger m_log;
		private IConfig m_config;

		[TestInitialize]
		public void Setup()
		{
			m_log = MockRepository.GenerateStub<ILogger>();
			m_config = MockRepository.GenerateStub<IConfig>();
		}

		[TestMethod]
		public void Filter_NoMatches_CommitsUnchanged()
		{
			var f1 = new FileInfo("file1");
			var commit = new Commit("c1").WithRevision(f1, "1.1");

			m_config.Stub(c => c.IncludeFile("file1")).Return(true);
			var filter = new ExclusionFilter(m_log, m_config);

			var commits = filter.Filter(new[] { commit });
			Assert.AreSame(commits.Single(), commit);
		}

		[TestMethod]
		public void Filter_PartialMatch_NewCommitReturned()
		{
			var f1 = new FileInfo("file1");
			var f2 = new FileInfo("file2");
			var commit = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");

			m_config.Stub(c => c.IncludeFile("file1")).Return(true);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			var filter = new ExclusionFilter(m_log, m_config);

			var commits = filter.Filter(new[] { commit });
			Assert.AreNotSame(commits.Single(), commit);
			Assert.AreEqual(commits.Single().CommitId, commit.CommitId);
			Assert.AreSame(commits.Single().Single().File, f1);
		}

		[TestMethod]
		public void Filter_AllFilesMatch_CommitExcluded()
		{
			var f1 = new FileInfo("file1");
			var f2 = new FileInfo("file2");
			var commit = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");

			m_config.Stub(c => c.IncludeFile("file1")).Return(false);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			var filter = new ExclusionFilter(m_log, m_config);

			var commits = filter.Filter(new[] { commit });
			Assert.IsFalse(commits.Any());
		}

		[TestMethod]
		public void Filter_HeadOnly_FileTracked()
		{
			var f1 = new FileInfo("file1");
			var f2 = new FileInfo("file2");
			var commit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var commit2 = new Commit("c2").WithRevision(f1, "1.2");

			m_config.Stub(c => c.IncludeFile("file1")).Return(false);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(true);
			var filter = new ExclusionFilter(m_log, m_config);

			var commits = filter.Filter(new[] { commit1, commit2 });
			Assert.IsFalse(commits.Any());

			Assert.AreEqual(filter.HeadOnlyState["MAIN"]["file1"].ToString(), "1.2");
		}

		[TestMethod]
		public void Filter_Exclude_NoHeadOnlyRules()
		{
			var f1 = new FileInfo("file1");
			var f2 = new FileInfo("file2");
			var commit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var commit2 = new Commit("c2").WithRevision(f1, "1.2");

			m_config.Stub(c => c.IncludeFile("file1")).Return(false);
			m_config.Stub(c => c.IncludeFile("file2")).Return(true);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file2")).Return(false);
			var filter = new ExclusionFilter(m_log, m_config);

			var commits = filter.Filter(new[] { commit1, commit2 });
			Assert.AreEqual(commits.Single().CommitId, "c1");

			Assert.IsFalse(filter.HeadOnlyState["MAIN"].LiveFiles.Any(), "No headonly files");
		}

		[TestMethod]
		public void CreateHeadOnlyCommits_OnlyProcessListedBranches()
		{
			var f1 = new FileInfo("file1").WithBranch("branch1", "1.2.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");
			var mainCommit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var mainCommit2 = new Commit("c2").WithRevision(f1, "1.2");
			var branchCommit1 = new Commit("c3").WithRevision(f1, "1.2.2.1").WithRevision(f2, "1.1.2.1");

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch1", mainCommit2 },
			};

			IEnumerable<Commit> commits = new[] { mainCommit1, mainCommit2, branchCommit1 };
			var streams = new BranchStreamCollection(commits, branchpoints);

			m_config.Stub(c => c.IncludeFile("file1")).Return(true);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file2")).Return(true);
			m_config.Stub(c => c.BranchRename).Return(new Renamer());

			var filter = new ExclusionFilter(m_log, m_config);
			commits = filter.Filter(commits).ToListIfNeeded();

			filter.CreateHeadOnlyCommits(new[] { "MAIN" }, streams, AllFiles(f1, f2));
			Assert.AreEqual(streams.Head("MAIN").CommitId, "headonly-MAIN", "Commit created on HEAD");
			Assert.AreEqual(streams.Head("branch1").CommitId, "c3", "No commit created on branch1");
		}

		[TestMethod]
		public void CreateHeadOnlyCommits_MergeFromBranch()
		{
			var f1 = new FileInfo("file1").WithBranch("branch1", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");
			var mainCommit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var branchCommit1 = new Commit("c2").WithRevision(f1, "1.1.2.1").WithRevision(f2, "1.1.2.1");
			var mainCommit2 = new Commit("c3").WithRevision(f1, "1.2", mergepoint: "1.1.2.1").WithRevision(f2, "1.2", mergepoint: "1.1.2.1");

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch1", mainCommit2 },
			};

			IEnumerable<Commit> commits = new[] { mainCommit1, branchCommit1, mainCommit2 };
			var streams = new BranchStreamCollection(commits, branchpoints);

			m_config.Stub(c => c.IncludeFile("file1")).Return(true);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file2")).Return(true);
			m_config.Stub(c => c.BranchRename).Return(new Renamer());

			var filter = new ExclusionFilter(m_log, m_config);
			commits = filter.Filter(commits).ToListIfNeeded();

			filter.CreateHeadOnlyCommits(new[] { "MAIN", "branch1" }, streams, AllFiles(f1, f2));
			Assert.AreEqual(streams.Head("MAIN").CommitId, "headonly-MAIN", "Commit created on HEAD");
			Assert.AreEqual(streams.Head("branch1").CommitId, "headonly-branch1", "Commit created on branch1");
			Assert.AreSame(streams.Head("MAIN").MergeFrom, streams.Head("branch1"), "Merge from branch1 to HEAD");
		}

		[TestMethod]
		public void CreateHeadOnlyCommits_FileDeletedInMerge()
		{
			var f1 = new FileInfo("file1").WithBranch("branch1", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");
			var mainCommit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var branchCommit1 = new Commit("c2").WithRevision(f1, "1.1.2.1").WithRevision(f2, "1.1.2.1");
			var mainCommit2 = new Commit("c3").WithRevision(f1, "1.2", mergepoint: "1.1.2.1").WithRevision(f2, "1.2", mergepoint: "1.1.2.1");
			var mainCommit3 = new Commit("c4").WithRevision(f2, "1.3", isDead: true);

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch1", mainCommit1 },
			};

			IEnumerable<Commit> commits = new[] { mainCommit1, branchCommit1, mainCommit2, mainCommit3 };
			var streams = new BranchStreamCollection(commits, branchpoints);

			m_config.Stub(c => c.IncludeFile("file1")).Return(false);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(true);
			m_config.Stub(c => c.IsHeadOnly("file2")).Return(true);
			m_config.Stub(c => c.BranchRename).Return(new Renamer());

			var filter = new ExclusionFilter(m_log, m_config);
			commits = filter.Filter(commits).ToListIfNeeded();

			filter.CreateHeadOnlyCommits(new[] { "MAIN", "branch1" }, streams, AllFiles(f1, f2));

			var mainHead = streams.Head("MAIN");
			Assert.IsTrue(mainHead.CommitId == "headonly-MAIN");
			Assert.IsTrue(mainHead.Where(r => !r.IsDead).Single().File.Name == "file1");
			Assert.IsTrue(mainHead.Where(r => r.IsDead).Single().File.Name == "file2");
		}

		[TestMethod]
		public void CreateHeadOnlyCommits_MessageSet()
		{
			var f1 = new FileInfo("file1").WithBranch("branch1", "1.1.0.2");
			var f2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");
			var mainCommit1 = new Commit("c1").WithRevision(f1, "1.1").WithRevision(f2, "1.1");
			var branchCommit1 = new Commit("c2").WithRevision(f1, "1.1.2.1").WithRevision(f2, "1.1.2.1");

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch1", mainCommit1 },
			};

			IEnumerable<Commit> commits = new[] { mainCommit1, branchCommit1 };
			var streams = new BranchStreamCollection(commits, branchpoints);

			// rename branch1
			var renamer = new Renamer();
			renamer.AddRule(new RenameRule(@"^MAIN$", "master"));
			renamer.AddRule(new RenameRule(@"^branch(\d)", "BRANCH#$1"));
			m_config.Stub(c => c.BranchRename).Return(renamer);

			m_config.Stub(c => c.IncludeFile("file1")).Return(true);
			m_config.Stub(c => c.IncludeFile("file2")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file1")).Return(false);
			m_config.Stub(c => c.IsHeadOnly("file2")).Return(true);

			var filter = new ExclusionFilter(m_log, m_config);
			commits = filter.Filter(commits).ToListIfNeeded();

			filter.CreateHeadOnlyCommits(new[] { "MAIN", "branch1" }, streams, AllFiles(f1, f2));
			Assert.AreEqual(streams.Head("MAIN").Message, "Adding head-only files to master");
			Assert.AreEqual(streams.Head("branch1").Message, "Adding head-only files to BRANCH#1");
		}


		private static FileCollection AllFiles(params FileInfo[] files)
		{
			return new FileCollection(files);
		}
	}
}
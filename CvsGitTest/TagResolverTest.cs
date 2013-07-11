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
	/// Unit tests for the TagResolver class.
	/// </summary>
	[TestClass]
	public class TagResolverTest
	{
		private ILogger m_logger;

		[TestInitialize]
		public void Setup()
		{
			m_logger = MockRepository.GenerateStub<ILogger>();
		}


		[TestMethod]
		public void Resolve_NonExistentTag()
		{
			var file1 = new FileInfo("file1");
			var file2 = new FileInfo("file2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
			};
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsFalse(result, "Failed");
			Assert.IsFalse(resolver.ResolvedTags.ContainsKey("tag"));
			Assert.AreEqual(resolver.UnresolvedTags.Single(), "tag");
		}

		[TestMethod]
		public void Resolve_NoReorderingNeeded()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1");
			var file3 = new FileInfo("file3").WithTag("tag", "1.1");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file3, "1.1"),
				new Commit("c3").WithRevision(file2, "1.2"),
			};
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3"), "Commits not reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"], commits[2]);
		}

		[TestMethod]
		public void Resolve_ReorderCommits()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file2, "1.2"),
			};
			var target = commits.ElementAt(2);
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1"), "Commits reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
		}

		[TestMethod]
		public void Resolve_ReorderCommits_TwoFilesThatDontMove()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file2, "1.3"),

			};

			var allFiles = commits.CreateAllFiles();
			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c3", "c1"), "Commits reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c3");
		}

		[TestMethod]
		public void Resolve_SplitCommit()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file1, "1.3").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file2, "1.3"),
			};
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			var newCommits = resolver.Commits.ToList();
			Assert.AreEqual(newCommits.Count, 5);
			Assert.AreEqual(newCommits[0].CommitId, "c0");
			Assert.AreEqual(newCommits[1].CommitId, "c1");
			Assert.IsTrue(newCommits[2].Single().File.Name == "file2" && newCommits[2].Single().Revision.Equals(Revision.Create("1.2")));
			Assert.AreEqual(newCommits[3].CommitId, "c3");
			Assert.IsTrue(newCommits[4].Single().File.Name == "file1" && newCommits[3].Single().Revision.Equals(Revision.Create("1.3")));
			Assert.AreEqual(resolver.ResolvedTags["tag"].CommitId, "c3");
		}

		[TestMethod]
		public void Resolve_SplitCommit_FileInfoCommitsUpdated()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file1, "1.3").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file2, "1.3"),
			};
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");

			// check that the FileInfo Commit lookup gets updated
			Assert.IsTrue(file1.GetCommit("1.3").CommitId.StartsWith("c2-"));
			Assert.IsTrue(file2.GetCommit("1.2").CommitId.StartsWith("c2-"));
		}

		[TestMethod]
		public void Resolve_SplitCommit_SplitCandidate()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file1, "1.3").WithRevision(file2, "1.2"),
			};
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			var newCommits = resolver.Commits.ToList();
			Assert.AreEqual(newCommits.Count, 4);
			Assert.AreEqual(newCommits[0].CommitId, "c0");
			Assert.AreEqual(newCommits[1].CommitId, "c1");
			Assert.IsTrue(newCommits[2].Single().File.Name == "file2" && newCommits[2].Single().Revision.Equals(Revision.Create("1.2")));
			Assert.IsTrue(newCommits[3].Single().File.Name == "file1" && newCommits[3].Single().Revision.Equals(Revision.Create("1.3")));
		}

		[TestMethod]
		public void Resolve_ReorderWithCreatedFileInTheMiddle()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2").WithRevision(file3, "1.1"),
				new Commit("c2").WithRevision(file2, "1.2"),
			};

			var target = commits.ElementAt(2);
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			Assert.AreEqual(resolver.Commits.Count(), 3, "No split");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1"), "Commits reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
		}

		[TestMethod]
		public void Resolve_ReorderWithCreatedAndModifiedFileInTheMiddle()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file3, "1.1"),  // file3 added
				new Commit("c2").WithRevision(file3, "1.2"),  // file3 modified
				new Commit("c3").WithRevision(file2, "1.2"),  // this is the target commit for "tag"
			};
			var target = commits.ElementAt(3);
			var allFiles = commits.CreateAllFiles();

			var resolver = new TagResolver(m_logger, allFiles);
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Succeeded");
			Assert.AreEqual(resolver.Commits.Count(), 4, "No split");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c3", "c1", "c2"), "Commits reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
		}

		[TestMethod]
		public void Resolve_TaggedFileDeletedBeforeTag()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true),
				new Commit("c2").WithRevision(file1, "1.2"),
			};
			var target = commits.ElementAt(2);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1"), "Commits reordered");
			Assert.AreEqual(resolver.ResolvedTags["tag"].CommitId, "c2");
		}

		[TestMethod]
		public void Resolve_TaggedFileDeletedAtTag()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2").WithRevision(file2, "1.2", isDead: true),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			var resolvedCommits = resolver.Commits.ToList();
			Assert.AreEqual(resolvedCommits.Count, 3, "Commit is split");

			Assert.AreEqual(resolvedCommits[0].CommitId, "c0");
			Assert.AreEqual(resolvedCommits[1].Single().File.Name, "file1");
			Assert.AreEqual(resolvedCommits[1].Single().Revision.ToString(), "1.2");
			Assert.AreEqual(resolvedCommits[2].Single().File.Name, "file2");
			Assert.IsTrue(resolvedCommits[2].Single().IsDead);
		}

		[TestMethod]
		public void Resolve_UntaggedFileDeletedBeforeTag()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true),
				new Commit("c2").WithRevision(file1, "1.2"),
			};
			var target = commits.ElementAt(2);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2"), "Commits not reordered");
			Assert.AreSame(resolver.ResolvedTags["tag"], commits[2]);
		}

		[TestMethod]
		public void Resolve_UntaggedFileDeletedBeforeTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2");
			var file3 = new FileInfo("file3").WithTag("tag", "1.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1").WithRevision(file3, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true),
				new Commit("c2").WithRevision(file1, "1.2"),
				new Commit("c3").WithRevision(file3, "1.2"),
			};
			var target = commits.ElementAt(3);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c3", "c2"));
		}

		[TestMethod]
		public void Resolve_UntaggedFileDeletedOnBranchBeforeTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1").WithRevision(file3, "1.1"),
				new Commit("c1").WithRevision(file3, "1.1.2.1"),
				new Commit("c2").WithRevision(file3, "1.1.2.2", isDead: true),                    // delete on branch
				new Commit("c3").WithRevision(file3, "1.2", isDead: true, mergepoint: "1.1.2.2"), // merge deletion
				new Commit("c4").WithRevision(file1, "1.2"),
				new Commit("c5").WithRevision(file2, "1.2"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c5");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3", "c5", "c4"));
		}

		[TestMethod]
		public void Resolve_FileDeletedOnBranchBeforeTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1").WithRevision(file3, "1.1"),
				new Commit("c1").WithRevision(file3, "1.1.2.1"),
				new Commit("c2").WithRevision(file3, "1.1.2.2", isDead: true),                    // delete on branch
				new Commit("c3").WithRevision(file3, "1.2", isDead: true, mergepoint: "1.1.2.2"), // merge deletion
				new Commit("c4").WithRevision(file1, "1.2"),
				new Commit("c5").WithRevision(file2, "1.2"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c5");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3", "c5", "c4"));
		}

		[TestMethod]
		public void Resolve_FileDeletedAfterTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3").WithTag("tag", "1.1");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1").WithRevision(file3, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file3, "1.2", isDead: true),
			};
			var target = commits.ElementAt(2);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1", "c3"));
		}

		[TestMethod]
		public void Resolve_FileDeletedAtTag()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true)
			};
			var target = commits.ElementAt(1);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1"));
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
		}

		[TestMethod]
		public void Resolve_FileCreatedAfterTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file3, "1.1"),
			};
			var target = commits.ElementAt(2);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1", "c3"));
		}

		[TestMethod]
		public void Resolve_FileCreatedAndDeletedAfterTag_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1");
			var file2 = new FileInfo("file2").WithTag("tag", "1.2");
			var file3 = new FileInfo("file3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file2, "1.2"),
				new Commit("c3").WithRevision(file3, "1.1"),
				new Commit("c4").WithRevision(file3, "1.2", isDead: true),
			};
			var target = commits.ElementAt(2);

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"], target);
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1", "c3", "c4"));
		}

		[TestMethod]
		public void Resolve_FileModifiedOnTrunkAfterBranchContainingTag()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.1.2.1"),
				new Commit("c2").WithRevision(file1, "1.2"),
				new Commit("c3").WithRevision(file2, "1.1.2.1"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c3");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3"));
		}

		[TestMethod]
		public void Resolve_FileModifiedOnTrunkBeforeAnyCommitsOnBranch()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.2"),
				new Commit("c2").WithRevision(file1, "1.1.2.1"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c2");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2"));
		}

		[TestMethod]
		public void Resolve_FileCreatedAndDeletedOnDifferentBranch_ReorderingRequired()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1.2.2").WithBranch("branch", "1.1.0.2");
			var file3 = new FileInfo("file3");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file1, "1.1.2.1").WithRevision(file2, "1.1.2.1"),
				new Commit("c2").WithRevision(file1, "1.1.2.2"),
				new Commit("c3").WithRevision(file2, "1.1.2.2"),
				new Commit("c4").WithRevision(file3, "1.1"),
				new Commit("c5").WithRevision(file3, "1.2", isDead: true),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["tag"].CommitId, "c3");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c3", "c2", "c4", "c5"));
		}

		[TestMethod]
		public void Resolve_FileCreatedOnTrunkAndBackported()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1").WithBranch("branch", "1.1.0.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");
			var file3 = new FileInfo("file3").WithTag("tag", "1.1.2.1").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.1.2.1"),
				new Commit("c2").WithRevision(file3, "1.1").WithRevision(file1, "1.2"),   // add file3 on trunk, but also take file1 to r1.2
				new Commit("c3").WithRevision(file3, "1.1.2.1"),                          // backport file3
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreEqual(resolver.ResolvedTags["tag"].CommitId, "c3");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3"), "No reordering");
		}

		[TestMethod]
		public void Resolve_FileDeletedOnTrunkAfterBranchMadeButBeforeAnyCommitsOnBranch()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.1.2.2").WithBranch("branch", "1.1.0.2");
			var file2 = new FileInfo("file2").WithTag("tag", "1.1").WithBranch("branch", "1.1.0.2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true),
				new Commit("c2").WithRevision(file1, "1.1.2.2"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreEqual(resolver.ResolvedTags["tag"].CommitId, "c2");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2"), "No reordering");
		}

		[TestMethod]
		public void Resolve_PartialBranchDetection()
		{
			var file1 = new FileInfo("file1").WithTag("tag", "1.2");
			var file2 = new FileInfo("file2");
			var file3 = new FileInfo("file3");
			var file4 = new FileInfo("file4");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file3, "1.1").WithRevision(file4, "1.1"),
				new Commit("c2").WithRevision(file1, "1.2"),
			};

			var resolver = new TagResolver(m_logger, commits.CreateAllFiles()) { PartialTagThreshold = 2 };
			var result = resolver.Resolve(new[] { "tag" }, commits);

			Assert.IsFalse(result, "Resolve failed");
			Assert.IsFalse(resolver.ResolvedTags.ContainsKey("tag"));
			Assert.AreEqual(resolver.UnresolvedTags.Single(), "tag");
		}
	}
}
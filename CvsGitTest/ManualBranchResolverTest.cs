/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the ManualBranchResolver class
	/// </summary>
	[TestClass]
	public class ManualBranchResolverTest
	{
		private ILogger m_log;
		private ITagResolver m_tagResolver;
		private RenameRule m_rule;

		public ManualBranchResolverTest()
		{
			m_log = MockRepository.GenerateStub<ILogger>();
			m_tagResolver = MockRepository.GenerateStub<ITagResolver>();
			m_rule = new RenameRule(@"^(.*)", "$1-branchpoint");
		}

		[TestMethod]
		public void Resolve_BranchpointTagExists()
		{
			var fallback = MockRepository.GenerateMock<ITagResolver>();

			var commit1 = new Commit("c1");
			var resolvedTags = new Dictionary<string, Commit>()
			{
				{ "branch-branchpoint", commit1 },
			};
			m_tagResolver.Stub(tr => tr.ResolvedTags).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" }, new[] { commit1 });

			Assert.IsTrue(result, "Resolved");
			Assert.AreSame(resolver.ResolvedTags["branch"], commit1);
			fallback.AssertWasNotCalled(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything, Arg<IEnumerable<Commit>>.Is.Anything));
		}

		[TestMethod]
		public void Resolve_BranchpointTagDoesNotExist_FallBackToAuto()
		{
			var commit1 = new Commit("c1");
			var resolvedCommits = new Dictionary<string, Commit>()
			{
				{ "branch", commit1 }
			};

			var fallback = MockRepository.GenerateMock<ITagResolver>();
			fallback.Stub(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything, Arg<IEnumerable<Commit>>.Is.Anything)).Return(true);
			fallback.Stub(f => f.ResolvedTags).Return(resolvedCommits);
			fallback.Stub(f => f.Commits).Return(new[] { commit1 });

			var resolvedTags = new Dictionary<string, Commit>();
			m_tagResolver.Stub(tr => tr.ResolvedTags).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" }, new[] { commit1 });

			Assert.IsTrue(result, "Resolved");
			Assert.AreSame(resolver.ResolvedTags["branch"], commit1);
		}

		[TestMethod]
		public void Resolve_ResolveFails()
		{
			var resolvedCommits = new Dictionary<string, Commit>();

			var fallback = MockRepository.GenerateMock<ITagResolver>();
			fallback.Stub(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything, Arg<IEnumerable<Commit>>.Is.Anything)).Return(false);
			fallback.Stub(f => f.ResolvedTags).Return(resolvedCommits);
			fallback.Stub(f => f.UnresolvedTags).Return(new[] { "branch" });
			fallback.Stub(f => f.Commits).Return(Enumerable.Empty<Commit>());

			var resolvedTags = new Dictionary<string, Commit>();
			m_tagResolver.Stub(tr => tr.ResolvedTags).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" }, Enumerable.Empty<Commit>());

			Assert.IsFalse(result, "Resolved");
			Assert.IsTrue(resolver.UnresolvedTags.SequenceEqual("branch"));
		}

		[TestMethod]
		public void Resolve_CommitOnBranchBeforeBranchpointTag()
		{
			var f1 = new FileInfo("file1").WithBranch("branch", "1.2.0.2").WithTag("branch-branchpoint", "1.2");
			var f2 = new FileInfo("file2").WithBranch("branch", "1.1.0.2").WithTag("branch-branchpoint", "1.1");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(f1, "1.1").WithRevision(f2, "1.1"),
				new Commit("c1").WithRevision(f2, "1.1.2.1"),
				new Commit("c2").WithRevision(f1, "1.2"),
			};

			var fallback = MockRepository.GenerateMock<ITagResolver>();

			var resolvedTags = new Dictionary<string, Commit>()
			{
				{ "branch-branchpoint", commits[2] },
			};
			m_tagResolver.Stub(tr => tr.ResolvedTags).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" }, commits);

			Assert.IsTrue(result, "Resolved");
			Assert.AreEqual(resolver.ResolvedTags["branch"].CommitId, "c2");
			Assert.IsTrue(resolver.Commits.Select(c => c.CommitId).SequenceEqual("c0", "c2", "c1"), "Commits reordered");
		}
	}
}
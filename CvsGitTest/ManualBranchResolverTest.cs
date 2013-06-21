/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
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
			m_tagResolver.Stub(tr => tr.ResolvedCommits).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" });

			Assert.IsTrue(result, "Resolved");
			Assert.AreSame(resolver.ResolvedCommits["branch"], commit1);
			fallback.AssertWasNotCalled(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything));
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
			fallback.Stub(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything)).Return(true);
			fallback.Stub(f => f.ResolvedCommits).Return(resolvedCommits);

			var resolvedTags = new Dictionary<string, Commit>();
			m_tagResolver.Stub(tr => tr.ResolvedCommits).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" });

			Assert.IsTrue(result, "Resolved");
			Assert.AreSame(resolver.ResolvedCommits["branch"], commit1);
		}

		[TestMethod]
		public void Resolve_ResolveFails()
		{
			var resolvedCommits = new Dictionary<string, Commit>();

			var fallback = MockRepository.GenerateMock<ITagResolver>();
			fallback.Stub(f => f.Resolve(Arg<IEnumerable<string>>.Is.Anything)).Return(false);
			fallback.Stub(f => f.ResolvedCommits).Return(resolvedCommits);
			fallback.Stub(f => f.UnresolvedTags).Return(new[] { "branch" });

			var resolvedTags = new Dictionary<string, Commit>();
			m_tagResolver.Stub(tr => tr.ResolvedCommits).Return(resolvedTags);
 
			var resolver = new ManualBranchResolver(m_log, fallback, m_tagResolver, m_rule);
			bool result = resolver.Resolve(new[] { "branch" });

			Assert.IsFalse(result, "Resolved");
			Assert.IsTrue(resolver.UnresolvedTags.SequenceEqual("branch"));
		}
	}
}
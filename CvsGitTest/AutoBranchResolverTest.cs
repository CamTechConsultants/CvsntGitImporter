/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the AutoBranchResolver class.
	/// </summary>
	[TestClass]
	public class AutoBranchResolverTest
	{
		private ILogger m_log;

		public AutoBranchResolverTest()
		{
			m_log = MockRepository.GenerateStub<ILogger>();
		}

		[TestMethod]
		public void Resolve_FileAddedOnBranch()
		{
			var file1 = new FileInfo("file1").WithBranch("branch1", "1.1.0.2");
			var file2 = new FileInfo("file2").WithBranch("branch1", "1.1.0.2");
			file2.BranchAddedOn = "branch1";

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1"),
				new Commit("c1").WithRevision(file1, "1.1.2.1"),
				new Commit("c2").WithRevision(file1, "1.2"),
				new Commit("c3").WithRevision(file2, "1.1.2.1"),
			};

			var resolver = new AutoBranchResolver(m_log, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "branch1" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["branch1"], commits[0]);
			Assert.IsTrue(resolver.Commits.SequenceEqual(commits), "Commits not reordered");
		}

		[TestMethod]
		public void Resolve_FileDeletedBeforeBranch()
		{
			var file1 = new FileInfo("file1").WithBranch("branch1", "1.2.0.2");
			var file2 = new FileInfo("file2");

			var commits = new List<Commit>()
			{
				new Commit("c0").WithRevision(file1, "1.1").WithRevision(file2, "1.1"),
				new Commit("c1").WithRevision(file2, "1.2", isDead: true),
				new Commit("c2").WithRevision(file1, "1.2"),
				new Commit("c3").WithRevision(file1, "1.2.2.1"),
			};

			var resolver = new AutoBranchResolver(m_log, commits.CreateAllFiles());
			var result = resolver.Resolve(new[] { "branch1" }, commits);

			Assert.IsTrue(result, "Resolve succeeded");
			Assert.AreSame(resolver.ResolvedTags["branch1"], commits[2]);
			Assert.IsTrue(resolver.Commits.SequenceEqual(commits), "Commits not reordered");
		}
	}
}
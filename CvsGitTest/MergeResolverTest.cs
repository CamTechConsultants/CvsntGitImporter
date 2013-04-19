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
	/// Unit tests for the MergeResolver class.
	/// </summary>
	[TestClass]
	public class MergeResolverTest
	{
		private ILogger m_logger;
		private FileInfo m_file;

		[TestInitialize]
		public void Setup()
		{
			m_logger = MockRepository.GenerateStub<ILogger>();
			m_file = new FileInfo("file0");
		}

		[TestMethod]
		public void SingleMerge()
		{
			var commits = new List<Commit>()
			{
				new Commit("initial").WithRevision(m_file, "1.1"),
				new Commit("branch").WithRevision(m_file, "1.1.2.1"),
				new Commit("merge").WithRevision(m_file, "1.2", mergepoint: "1.1.2.1"),
			};
			m_file.AddTag("branch", Revision.Create("1.1.0.2"));

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", commits[0] }
			};

			var originalCommits = commits.ToList();
			var resolver = new MergeResolver(m_logger, commits.AssignNumbers(), branchpoints);
			resolver.Resolve();

			Assert.IsTrue(resolver.Commits.SequenceEqual(originalCommits));
		}

		[TestMethod]
		public void MultipleMerges()
		{
			var commits = new List<Commit>()
			{
				new Commit("initial").WithRevision(m_file, "1.1"),
				new Commit("branch1").WithRevision(m_file, "1.1.2.1"),
				new Commit("branch2").WithRevision(m_file, "1.1.2.2"),
				new Commit("merge1").WithRevision(m_file, "1.2", mergepoint: "1.1.2.1"),
				new Commit("merge2").WithRevision(m_file, "1.3", mergepoint: "1.1.2.2"),
			};
			m_file.AddTag("branch", Revision.Create("1.1.0.2"));

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", commits[0] }
			};

			var originalCommits = commits.ToList();
			var resolver = new MergeResolver(m_logger, commits.AssignNumbers(), branchpoints);
			resolver.Resolve();

			Assert.IsTrue(resolver.Commits.SequenceEqual(originalCommits));
		}

		[TestMethod]
		public void CrossedMerge()
		{
			var commits = new List<Commit>()
			{
				new Commit("initial").WithRevision(m_file, "1.1"),
				new Commit("branch1").WithRevision(m_file, "1.1.2.1"),
				new Commit("branch2").WithRevision(m_file, "1.1.2.2"),
				new Commit("merge1").WithRevision(m_file, "1.2", mergepoint: "1.1.2.2"),
				new Commit("merge2").WithRevision(m_file, "1.3", mergepoint: "1.1.2.1"),
			};
			m_file.AddTag("branch", Revision.Create("1.1.0.2"));

			var branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", commits[0] }
			};

			var originalCommits = commits.ToList();
			var resolver = new MergeResolver(m_logger, commits.AssignNumbers(), branchpoints);
			resolver.Resolve();

			Assert.IsTrue(resolver.Commits.SequenceEqual(new[] {
					originalCommits[0], originalCommits[2], originalCommits[1], originalCommits[3], originalCommits[4]
			}));
		}
	}
}

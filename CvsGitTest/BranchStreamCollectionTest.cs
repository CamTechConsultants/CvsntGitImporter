/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Linq;
using System.Collections.Generic;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CvsGitTest
{
	[TestClass]
	public class BranchStreamCollectionTest
	{
		private List<Commit> m_commits;
		private Dictionary<string, Commit> m_branchpoints;

		[TestInitialize]
		public void TestSetup()
		{
			var f1 = new FileInfo("f1").WithBranch("branch", "1.1.0.2");

			m_commits = new List<Commit>()
			{
				new Commit("1").WithRevision(f1, "1.1"),
				new Commit("2").WithRevision(f1, "1.1.2.1"),
				new Commit("3").WithRevision(f1, "1.2", mergepoint: "1.1.2.1")
			};

			m_branchpoints = new Dictionary<string, Commit>()
			{
				{ "branch", m_commits[0] },
			};
		}

		[TestMethod]
		public void Construct()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["MAIN"].Select(c => c.CommitId).SequenceEqual("1", "3"));
			Assert.IsTrue(streams["branch"].Single().CommitId == "2");
		}

		[TestMethod]
		public void Construct_SetIndexValues()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["MAIN"].Select(c => c.Index).SequenceEqual(1, 2));
			Assert.IsTrue(streams["branch"].Single().Index == 1);
		}

		[TestMethod]
		public void Branches()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams.Branches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).SequenceEqual("branch", "MAIN"));
		}
	}
}
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
		private FileInfo m_f1;
		private List<Commit> m_commits;
		private Dictionary<string, Commit> m_branchpoints;

		[TestInitialize]
		public void TestSetup()
		{
			m_f1 = new FileInfo("f1").WithBranch("branch", "1.1.0.2");

			m_commits = new List<Commit>()
			{
				new Commit("1").WithRevision(m_f1, "1.1"),
				new Commit("2").WithRevision(m_f1, "1.1.2.1"),
				new Commit("3").WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
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

			Assert.IsTrue(streams["MAIN"].ToList().Select(c => c.CommitId).SequenceEqual("1", "3"));
			Assert.IsTrue(streams["branch"].ToList().Single().CommitId == "2");
		}

		[TestMethod]
		public void Construct_SetIndexValues()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["MAIN"].ToList().Select(c => c.Index).SequenceEqual(1, 2));
			Assert.IsTrue(streams["branch"].ToList().Single().Index == 1);
		}

		[TestMethod]
		public void Construct_IgnoredBranch()
		{
			// remove 'branch' from the list of branchpoints, simulating an ignored branch
			m_branchpoints.Remove("branch");
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsFalse(streams["branch"].ToList().Any());
		}

		[TestMethod]
		public void Construct_BranchPredecessorSet()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["branch"].ToList().Single().Predecessor == m_commits[0]);
		}

		[TestMethod]
		public void Roots()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["MAIN"] == m_commits[0]);
			Assert.IsTrue(streams["branch"] == m_commits[1]);
		}

		[TestMethod]
		public void Heads()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams.Head("MAIN") == m_commits[2]);
			Assert.IsTrue(streams.Head("branch") == m_commits[1]);
		}

		[TestMethod]
		public void Branches()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams.Branches.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).SequenceEqual("branch", "MAIN"));
		}

		[TestMethod]
		public void MoveCommit_ToItself()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[2], m_commits[2]);

			Assert.IsTrue(streams["MAIN"].ToList().SequenceEqual(m_commits[0], m_commits[2]));
		}

		[TestMethod]
		[ExpectedException(typeof(NotSupportedException))]
		public void MoveCommit_Backwards()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[2], m_commits[1]);
		}

		[TestMethod]
		public void MoveCommit_Forwards()
		{
			m_commits.Add(new Commit("4").WithRevision(m_f1, "1.3"));
			m_commits.Add(new Commit("5").WithRevision(m_f1, "1.4"));
			m_commits.Add(new Commit("6").WithRevision(m_f1, "1.5"));
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[2], m_commits[4]);

			Assert.IsTrue(streams["MAIN"].ToList().Select(c => c.CommitId).SequenceEqual("1", "4", "5", "3", "6"));
		}

		[TestMethod]
		public void MoveCommit_ToEnd()
		{
			m_commits.Add(new Commit("4").WithRevision(m_f1, "1.3"));
			m_commits.Add(new Commit("5").WithRevision(m_f1, "1.4"));
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[3], m_commits[4]);

			Assert.IsTrue(streams["MAIN"].ToList().Select(c => c.CommitId).SequenceEqual("1", "3", "5", "4"));
			Assert.IsTrue(streams.Head("MAIN").CommitId == "4");
		}

		[TestMethod]
		public void MoveCommit_FromStart()
		{
			m_commits.Add(new Commit("4").WithRevision(m_f1, "1.1.2.2"));
			m_commits.Add(new Commit("5").WithRevision(m_f1, "1.1.2.3"));
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[1], m_commits[4]);

			Assert.IsTrue(streams["branch"].CommitId == "4");
			Assert.IsTrue(streams["branch"].ToList().Select(c => c.CommitId).SequenceEqual("4", "5", "2"));
		}
	}
}
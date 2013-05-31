/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Linq;
using System.Collections.Generic;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CTC.CvsntGitImporter.TestCode
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

		#region Construct

		[TestMethod]
		public void Construct()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			Assert.IsTrue(streams["MAIN"].ToList().Select(c => c.CommitId).SequenceEqual("1", "3"));
			Assert.IsTrue(streams["branch"].ToList().Single().CommitId == "2");
			Assert.IsTrue(streams.Verify());
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

		#endregion Construct


		#region Properties

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

		#endregion Properties


		#region AppendCommit

		[TestMethod]
		public void AppendCommit_ToMain()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			var commit = new Commit("new").WithRevision(m_f1, "1.3");
			streams.AppendCommit(commit);

			Assert.AreSame(streams.Head("MAIN"), commit);
			Assert.AreSame(streams["MAIN"].Successor.Successor, commit);
			Assert.IsTrue(commit.Index > streams["MAIN"].Successor.Index, "Index set");
		}

		[TestMethod]
		public void AppendCommit_ToBranch()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			var commit = new Commit("new").WithRevision(m_f1, "1.1.2.2");
			streams.AppendCommit(commit);

			Assert.AreSame(streams.Head("branch"), commit);
			Assert.AreSame(streams["branch"].Successor, commit);
			Assert.IsTrue(commit.Index > streams["MAIN"].Index, "Index set");
		}

		#endregion AppendCommit


		#region MoveCommit

		[TestMethod]
		public void MoveCommit_ToItself()
		{
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			streams.MoveCommit(m_commits[2], m_commits[2]);

			Assert.IsTrue(streams["MAIN"].ToList().SequenceEqual(m_commits[0], m_commits[2]));
			Assert.IsTrue(streams.Verify());
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
			Assert.IsTrue(streams.Verify());
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
			Assert.IsTrue(streams.Verify());
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
			Assert.IsTrue(streams.Verify());
		}

		#endregion MoveCommit


		#region OrderedBranches

		[TestMethod]
		public void OrderedBranches()
		{
			Commit subBranchPoint;
			m_commits.AddRange(new[]
			{
				new Commit("4").WithRevision(m_f1, "1.3"),
				new Commit("5").WithRevision(m_f1, "1.1.2.2"),
				subBranchPoint = new Commit("6").WithRevision(m_f1, "1.1.2.3"),
				new Commit("7").WithRevision(m_f1, "1.1.2.3.2.1"),
			});

			m_f1.WithBranch("subbranch", "1.1.2.3.0.2");
			m_branchpoints["subbranch"] = subBranchPoint;
			var streams = new BranchStreamCollection(m_commits, m_branchpoints);

			var orderedBranches = streams.OrderedBranches.ToList();
			Assert.IsTrue(orderedBranches.SequenceEqual("MAIN", "branch", "subbranch"));
		}

		#endregion OrderedBranches
	}
}
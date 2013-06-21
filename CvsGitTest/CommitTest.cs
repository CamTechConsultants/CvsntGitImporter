/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for Commit class.
	/// </summary>
	[TestClass]
	public class CommitTest
	{
		private FileInfo m_f1;
		private FileInfo m_f2;
		private FileInfo m_f3;

		[TestInitialize]
		public void Setup()
		{
			m_f1 = new FileInfo("f1");
			m_f2 = new FileInfo("f2");
			m_f3 = new FileInfo("f3");
		}


		#region MergedFiles

		[TestMethod]
		public void MergedFiles_None()
		{
			var commit = new Commit("abc")
					.WithRevision(m_f1, "1.2");
			var result = commit.MergedFiles;

			Assert.IsFalse(result.Any());
		}

		[TestMethod]
		public void MergedFiles_All()
		{
			var commit = new Commit("abc")
					.WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
					.WithRevision(m_f2, "1.3", mergepoint: "1.1.4.2");
			var result = commit.MergedFiles;

			Assert.AreEqual(result.Count(), 2);
		}

		[TestMethod]
		public void MergedFiles_Mixture()
		{
			var commit = new Commit("abc")
					.WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
					.WithRevision(m_f2, "1.3");
			var result = commit.MergedFiles;

			Assert.AreEqual(result.Single().File.Name, "f1");
		}

		#endregion MergedFiles


		#region Verify

		[TestMethod]
		public void Verify_MergeFromTwoBranches()
		{
			m_f1.WithBranch("branch1", "1.1.0.2");
			m_f2.WithBranch("branch2", "1.1.0.2");

			var commit = new Commit("abc")
				.WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
				.WithRevision(m_f2, "1.2", mergepoint: "1.1.2.1");
			commit.Verify();

			Assert.IsTrue(commit.Errors.Single().Contains("Multiple branches merged from"));
		}

		[TestMethod]
		public void Verify_MergeFromTwoBranches_OneIsExcluded()
		{
			m_f1.WithBranch("branch1", "1.1.0.2");

			var commit = new Commit("abc")
				.WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
				.WithRevision(m_f2, "1.2", mergepoint: "1.1.2.1");
			var result = commit.Verify();

			Assert.IsTrue(result, "Verification succeeded");
		}

		[TestMethod]
		public void Verify_MergeFromTwoBranchesAndNonMerge()
		{
			m_f1.WithBranch("branch1", "1.1.0.2");
			m_f2.WithBranch("branch2", "1.1.0.2");

			var commit = new Commit("abc")
				.WithRevision(m_f3, "1.1")
				.WithRevision(m_f1, "1.2", mergepoint: "1.1.2.1")
				.WithRevision(m_f2, "1.2", mergepoint: "1.1.2.1");
			commit.Verify();

			Assert.IsTrue(commit.Errors.Single().Contains("Multiple branches merged from"));
		}

		[TestMethod]
		public void Verify_MergeFromParallelBranch_WithUnmodifiedFileOnSourceBranch()
		{
			m_f1.WithBranch("branch1", "1.1.0.2").WithBranch("branch2", "1.2.0.2");
			m_f2.WithBranch("branch1", "1.1.0.2").WithBranch("branch2", "1.2.0.2");

			var commit = new Commit("abc")
				.WithRevision(m_f1, "1.2.2.1", mergepoint: "1.1.2.1") // file was modified on branch1
				.WithRevision(m_f2, "1.2.2.1", mergepoint: "1.1");    // file was not modified on branch1
			bool result = commit.Verify();

			Assert.IsTrue(result, "Verification succeeded");
		}

		#endregion Verify


		#region IsBranchpoint

		[TestMethod]
		public void IsBranchpoint_NoBranches()
		{
			var commit = new Commit("abc").WithRevision(m_f1, "1.1");

			Assert.IsFalse(commit.IsBranchpoint);
		}

		[TestMethod]
		public void IsBranchpoint_WithBranches()
		{
			m_f1.WithBranch("branch", "1.1.0.2");
			var commit = new Commit("main1").WithRevision(m_f1, "1.1");
			var branchCommit = new Commit("branch1").WithRevision(m_f1, "1.1.2.1");
			commit.AddBranch(branchCommit);

			Assert.IsTrue(commit.IsBranchpoint);
		}

		#endregion IsBranchpoint


		#region ReplaceBranch

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ReplaceBranch_NonExistent()
		{
			var commit = new Commit("main1").WithRevision(m_f1, "1.1");
			var branchCommit1 = new Commit("branch1").WithRevision(m_f1, "1.1.2.1");
			var branchCommit2 = new Commit("branch2").WithRevision(m_f1, "1.1.2.2");

			commit.ReplaceBranch(branchCommit1, branchCommit2);
		}

		[TestMethod]
		public void ReplaceBranch()
		{
			m_f1.WithBranch("branch", "1.1.0.2");
			var commit = new Commit("main1").WithRevision(m_f1, "1.1");
			var branchCommit1 = new Commit("branch1").WithRevision(m_f1, "1.1.2.1");
			var branchCommit2 = new Commit("branch2").WithRevision(m_f1, "1.1.2.2");
			commit.AddBranch(branchCommit1);

			commit.ReplaceBranch(branchCommit1, branchCommit2);

			Assert.IsTrue(commit.IsBranchpoint);
			Assert.IsTrue(commit.Branches.Single() == branchCommit2);
		}

		#endregion ReplaceBranch
	}
}
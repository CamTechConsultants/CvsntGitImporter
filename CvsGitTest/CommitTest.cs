/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
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
	}
}

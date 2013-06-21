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
	/// Unit tests for the CommitBuilder class.
	/// </summary>
	[TestClass]
	public class CommitBuilderTest
	{
		[TestMethod]
		public void FileAddedOnBranch()
		{
			var file = new FileInfo("file.txt");
			var revisions = new[]
			{
				file.CreateRevision("1.1.2.1", "branch").WithMessage("created"),
				file.CreateRevision("1.1", "main", isDead: true).WithMessage("file file.txt was initially added on branch branch"),
			};

			var builder = new CommitBuilder(revisions);
			var commits = builder.GetCommits().ToList();

			Assert.IsTrue(commits.Single().Single().Revision.ToString() == "1.1.2.1");
		}

		[TestMethod]
		public void FilesWithSameCommitId_MergedIntoSingleCommit()
		{
			var f1 = new FileInfo("file1.txt");
			var f2 = new FileInfo("file2.txt");
			var revisions = new[]
			{
				f1.CreateRevision("1.1", "commit1"),
				f2.CreateRevision("1.1", "commit1"),
			};

			var builder = new CommitBuilder(revisions);
			var commits = builder.GetCommits().ToList();

			Assert.IsTrue(commits.Single().Select(f => f.File.Name).SequenceEqual("file1.txt", "file2.txt"));
		}

		[TestMethod]
		public void FilesWithoutCommitId_SameMessage_MergedTogether()
		{
			var now = DateTime.Now;
			var f1 = new FileInfo("file1.txt");
			var f2 = new FileInfo("file2.txt");
			var revisions = new[]
			{
				f1.CreateRevision("1.1", "", now - TimeSpan.FromSeconds(1)).WithMessage("message"),
				f2.CreateRevision("1.1", "", now).WithMessage("message"),
			};

			var builder = new CommitBuilder(revisions);
			var commits = builder.GetCommits().ToList();

			Assert.IsTrue(commits.Single().Select(f => f.File.Name).SequenceEqual("file1.txt", "file2.txt"));
		}

		[TestMethod]
		public void FilesWithoutCommitId_DifferentMessage_NotMergedTogether()
		{
			var now = DateTime.Now;
			var f1 = new FileInfo("file1.txt");
			var f2 = new FileInfo("file2.txt");
			var revisions = new[]
			{
				f1.CreateRevision("1.1", "", now).WithMessage("message #1"),
				f2.CreateRevision("1.1", "", now).WithMessage("message #2"),
			};

			var builder = new CommitBuilder(revisions);
			var commits = builder.GetCommits().ToList();

			Assert.AreEqual(commits.Count, 2);
		}

		[TestMethod]
		public void FilesWithoutCommitId_SameMessage_TimeGap()
		{
			var now = DateTime.Now;
			var f1 = new FileInfo("file1.txt");
			var f2 = new FileInfo("file2.txt");
			var f3 = new FileInfo("file3.txt");
			var revisions = new[]
			{
				f1.CreateRevision("1.1", "", now).WithMessage("message"),
				f2.CreateRevision("1.1", "", now).WithMessage("message"),
				f3.CreateRevision("1.1", "", now + TimeSpan.FromMinutes(5)).WithMessage("message"),
			};

			var builder = new CommitBuilder(revisions);
			var commits = builder.GetCommits().ToList();

			Assert.AreEqual(commits.Count, 2);
			Assert.IsTrue(commits[0].Select(f => f.File.Name).SequenceEqual("file1.txt", "file2.txt"));
			Assert.IsTrue(commits[1].Select(f => f.File.Name).Single() == "file3.txt");
		}
	}
}

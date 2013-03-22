/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CvsGitConverter;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the FileInfo class.
	/// </summary>
	[TestClass]
	public class FileInfoTest
	{
		#region GetTagsForRevision

		[TestMethod]
		public void GetTagsForRevision_RevisionTagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			var tags = file.GetTagsForRevision(Revision.Create("1.1"));
			Assert.AreEqual(tags.Single(), "tag");
		}

		[TestMethod]
		public void GetTagsForRevision_RevisionUntagged()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			Assert.IsFalse(file.GetTagsForRevision(Revision.Create("1.2")).Any());
		}

		#endregion GetTagsForRevision


		#region GetRevisionForTag

		[TestMethod]
		public void GetRevisionForTag_TagExists()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("tag", Revision.Create("1.1"));

			var r = file.GetRevisionForTag("tag");
			Assert.AreEqual(r, Revision.Create("1.1"));
		}

		[TestMethod]
		public void GetRevisionForTag_TagDoesNotExist()
		{
			var file = new FileInfo("file.txt");

			var r = file.GetRevisionForTag("tag");
			Assert.AreEqual(r, Revision.Empty);
		}

		#endregion GetRevisionForTag


		#region IsRevisionOnBranch

		[TestMethod]
		public void IsRevisionOnBranch_RevisionOnMain()
		{
			var file = new FileInfo("file.txt");

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.1"), "MAIN"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_BranchRevisionIsNotOnMain()
		{
			var file = new FileInfo("file.txt");

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.1.2.1"), "MAIN"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsBranchpoint()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsDirectlyOnTheBranch()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4.2.5"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsPredecessorOnMain()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.3"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsSuccessorToBranchpoint()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.5"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_ManyBranches()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch1", Revision.Create("1.4.0.2"));
			file.AddTag("branch2", Revision.Create("1.4.2.3.0.2"));
			file.AddTag("branch3", Revision.Create("1.4.2.3.2.1.0.6"));

			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.3"), "branch3"));
			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.4"), "branch3"));
			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.4.2.2"), "branch3"));
			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.4.2.3"), "branch3"));
			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.4.2.4"), "branch3"));
			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.4.4.1"), "branch3"));
			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.4.2.3.2.1"), "branch3"));
			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.4.2.3.2.2"), "branch3"));
			Assert.IsTrue(file.IsRevisionOnBranch( Revision.Create("1.4.2.3.2.1.6.1"), "branch3"));
			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.4.2.3.2.1.2.1"), "branch3"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_BranchNotPresent()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch1", Revision.Create("1.4.0.2"));

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.1"), "branch2"));
		}

		#endregion


		#region GetBranchesAtRevision

		[TestMethod]
		public void GetBranchesAtRevision_Branchpoint_ReturnsBranch()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.AreEqual(file.GetBranchesAtRevision(Revision.Create("1.4")).Single(), "branch");
		}

		[TestMethod]
		public void GetBranchesAtRevision_Branchpoint_ReturnsMultipleBranch()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch1", Revision.Create("1.4.0.2"));
			file.AddTag("branch2", Revision.Create("1.4.0.4"));

			Assert.IsTrue(file.GetBranchesAtRevision(Revision.Create("1.4")).OrderBy(i => i).SequenceEqual(new[] { "branch1", "branch2" }));
		}

		[TestMethod]
		public void GetBranchesAtRevision_PredecessorToBranchpoint_NoBranches()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsFalse(file.GetBranchesAtRevision(Revision.Create("1.3")).Any());
		}

		[TestMethod]
		public void GetBranchesAtRevision_RevisionOnTheBranch_NoBranches()
		{
			var file = new FileInfo("file.txt");
			file.AddTag("branch", Revision.Create("1.4.0.2"));

			Assert.IsFalse(file.GetBranchesAtRevision(Revision.Create("1.4.2.1")).Any());
		}

		#endregion GetBranchesAtRevision
	}
}
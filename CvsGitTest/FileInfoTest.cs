/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the FileInfo class.
	/// </summary>
	[TestClass]
	public class FileInfoTest
	{
		#region AllBranches

		[TestMethod]
		public void AllBranches_NoBranchesDefined()
		{
			var file = new FileInfo("file.txt");

			Assert.AreEqual(file.AllBranches.Count(), 0);
		}

		[TestMethod]
		public void AllBranches_BranchesDefined()
		{
			var file = new FileInfo("file.txt").WithBranch("branch2", "1.1.0.2").WithBranch("branch1", "1.1.0.4"); ;

			Assert.IsTrue(file.AllBranches.OrderBy(i => i).SequenceEqual("branch1", "branch2"));
		}

		#endregion AllBranches


		#region AllTags

		[TestMethod]
		public void AllTags_NoTagsDefined()
		{
			var file = new FileInfo("file.txt");

			Assert.AreEqual(file.AllTags.Count(), 0);
		}

		[TestMethod]
		public void AllTags_TagsDefined()
		{
			var file = new FileInfo("file.txt").WithTag("tag2", "1.1").WithTag("tag1", "1.2"); ;

			Assert.IsTrue(file.AllTags.OrderBy(i => i).SequenceEqual("tag1", "tag2"));
		}

		#endregion AllTags


		#region BranchAddedOn

		[TestMethod]
		public void BranchCreatedOn_DefaultsToMain()
		{
			var file = new FileInfo("file");
			Assert.AreEqual(file.BranchAddedOn, "MAIN");
		}

		#endregion BranchAddedOn


		#region GetTagsForRevision

		[TestMethod]
		public void GetTagsForRevision_RevisionTagged()
		{
			var file = new FileInfo("file.txt").WithTag("tag", "1.1");

			var tags = file.GetTagsForRevision(Revision.Create("1.1"));
			Assert.AreEqual(tags.Single(), "tag");
		}

		[TestMethod]
		public void GetTagsForRevision_RevisionUntagged()
		{
			var file = new FileInfo("file.txt").WithTag("tag", "1.1");

			Assert.IsFalse(file.GetTagsForRevision(Revision.Create("1.2")).Any());
		}

		#endregion GetTagsForRevision


		#region GetRevisionForTag

		[TestMethod]
		public void GetRevisionForTag_TagExists()
		{
			var file = new FileInfo("file.txt").WithTag("tag", "1.1");

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
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsDirectlyOnTheBranch()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4.2.5"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsPredecessorOnMain()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.3"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_RevisionIsSuccessorToBranchpoint()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.5"), "branch"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_ManyBranches()
		{
			var file = new FileInfo("file.txt")
					.WithBranch("branch1", "1.4.0.2")
					.WithBranch("branch2", "1.4.2.3.0.2")
					.WithBranch("branch3", "1.4.2.3.2.1.0.6");

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
			var file = new FileInfo("file.txt").WithBranch("branch1", "1.4.0.2");

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.1"), "branch2"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_BranchFromBranch_OnParentBranch()
		{
			var file = new FileInfo("file.txt")
					.WithBranch("branch1", "1.1.0.2")
					.WithBranch("branch2", "1.4.0.2")
					.WithBranch("branch3", "1.4.2.1.0.2"); // branch from branch2

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4.2.1"), "branch3"));
			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.4.2.2"), "branch3"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_BranchFromBranch_OnBranch()
		{
			var file = new FileInfo("file.txt")
					.WithBranch("branch1", "1.1.0.2")
					.WithBranch("branch2", "1.4.0.2")
					.WithBranch("branch3", "1.4.2.1.0.2"); // branch from branch2

			Assert.IsTrue(file.IsRevisionOnBranch(Revision.Create("1.4.2.1.2.1"), "branch3"));
		}

		[TestMethod]
		public void IsRevisionOnBranch_BranchFromBranch_NotOnBranch()
		{
			var file = new FileInfo("file.txt")
					.WithBranch("branch1", "1.1.0.2")
					.WithBranch("branch2", "1.4.0.2")
					.WithBranch("branch3", "1.4.2.1.0.2"); // branch from branch2

			Assert.IsFalse(file.IsRevisionOnBranch(Revision.Create("1.1.2.1"), "branch3"));
		}

		#endregion


		#region GetBranch

		[TestMethod]
		public void GetBranch_Unknown()
		{
			var file = new FileInfo("file.txt");

			Assert.IsNull(file.GetBranch(Revision.Create("1.1.2.1")));
		}

		[TestMethod]
		public void GetBranch_Known()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.1.0.2");

			Assert.AreEqual(file.GetBranch(Revision.Create("1.1.2.3")), "branch");
		}

		#endregion


		#region GetBranchesAtRevision

		[TestMethod]
		public void GetBranchesAtRevision_Branchpoint_ReturnsBranch()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.AreEqual(file.GetBranchesAtRevision(Revision.Create("1.4")).Single(), "branch");
		}

		[TestMethod]
		public void GetBranchesAtRevision_Branchpoint_ReturnsMultipleBranch()
		{
			var file = new FileInfo("file.txt")
					.WithBranch("branch1", "1.4.0.2")
					.WithBranch("branch2", "1.4.0.4");

			Assert.IsTrue(file.GetBranchesAtRevision(Revision.Create("1.4")).OrderBy(i => i).SequenceEqual("branch1", "branch2"));
		}

		[TestMethod]
		public void GetBranchesAtRevision_PredecessorToBranchpoint_NoBranches()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsFalse(file.GetBranchesAtRevision(Revision.Create("1.3")).Any());
		}

		[TestMethod]
		public void GetBranchesAtRevision_RevisionOnTheBranch_NoBranches()
		{
			var file = new FileInfo("file.txt").WithBranch("branch", "1.4.0.2");

			Assert.IsFalse(file.GetBranchesAtRevision(Revision.Create("1.4.2.1")).Any());
		}

		#endregion GetBranchesAtRevision


		#region GetCommit

		[TestMethod]
		public void GetCommit_RevisionExists()
		{
			var file = new FileInfo("file.txt");
			var commit = new Commit("abc").WithRevision(file, "1.1");

			var result = file.GetCommit(Revision.Create("1.1"));
			Assert.AreSame(result, commit);
		}

		[TestMethod]
		public void GetCommit_RevisionDoesNotExist()
		{
			var file = new FileInfo("file.txt");
			var result = file.GetCommit(Revision.Create("1.5"));
			Assert.AreSame(result, null);
		}

		#endregion
	}
}
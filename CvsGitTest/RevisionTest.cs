/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
{
	[TestClass]
	public class RevisionTest
	{
		#region Validate

		[TestMethod]
		public void Validate_TrunkRevision()
		{
			Revision.Create("1.1");
		}

		[TestMethod]
		public void Validate_BranchRevision()
		{
			Revision.Create("1.1.0.2");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Validate_Invalid_ZeroPart()
		{
			Revision.Create("1.0");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Validate_Invalid_OddBranchIndex()
		{
			Revision.Create("1.1.1.2");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Validate_Invalid_OddBranchIndex_Branchpoint()
		{
			Revision.Create("1.1.0.1");
		}

		#endregion Validate


		#region IsBranch

		[TestMethod]
		public void IsBranch_TrunkRevision()
		{
			var r = Revision.Create("1.1");
			Assert.IsFalse(r.IsBranch);
		}

		[TestMethod]
		public void IsBranch_BranchRevision()
		{
			var r = Revision.Create("1.1.0.2");
			Assert.IsTrue(r.IsBranch);
		}

		#endregion IsBranch


		#region BranchStem
		
		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void BranchStem_MainRevision()
		{
			var r = Revision.Create("1.1");
			var stem = r.BranchStem;
		}
		
		[TestMethod]
		public void BranchStem_BranchRevision()
		{
			var r = Revision.Create("1.1.2.5");
			var expected = Revision.Create("1.1.2");
			Assert.AreEqual(r.BranchStem, expected);
		}
		
		[TestMethod]
		public void BranchStem_Branchpoint()
		{
			var r = Revision.Create("1.1.0.6");
			var expected = Revision.Create("1.1.6");
			Assert.AreEqual(r.BranchStem, expected);
		}

		#endregion BranchStem


		#region DirectlyPrecedes
		
		[TestMethod]
		public void DirectlyPrecedes_ConsecutiveTrunkRevisions()
		{
			var r1 = Revision.Create("1.1");
			var r2 = Revision.Create("1.2");
			Assert.IsTrue(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_ConsecutiveBranchRevisions()
		{
			var r1 = Revision.Create("1.1.2.5");
			var r2 = Revision.Create("1.1.2.6");
			Assert.IsTrue(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_NonConsecutiveTrunkRevisions()
		{
			var r1 = Revision.Create("1.1");
			var r2 = Revision.Create("1.3");
			Assert.IsFalse(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_NonConsecutiveBranchRevisions()
		{
			var r1 = Revision.Create("1.3.4.5");
			var r2 = Revision.Create("1.3.4.7");
			Assert.IsFalse(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_DifferentBranches()
		{
			var r1 = Revision.Create("1.3.4.5");
			var r2 = Revision.Create("1.3.6.6");
			Assert.IsFalse(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_Branchpoint()
		{
			var r1 = Revision.Create("1.3");
			var r2 = Revision.Create("1.3.4.1");
			Assert.IsTrue(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}
		
		[TestMethod]
		public void DirectlyPrecedes_OtherIsFirstRevision()
		{
			var r1 = Revision.Create("1.3");
			var r2 = Revision.Create("1.1");
			Assert.IsFalse(r1.DirectlyPrecedes(r2));
			Assert.IsFalse(r2.DirectlyPrecedes(r1));
		}

		#endregion DirectlyPrecedes


		#region Precedes

		[TestMethod]
		public void Precedes_Self()
		{
			var r1 = Revision.Create("1.2");
			var r2 = Revision.Create("1.2");

			Assert.IsTrue(r1.Precedes(r2));
			Assert.IsTrue(r2.Precedes(r1));
		}

		[TestMethod]
		public void Precedes_RevisionsOnMain()
		{
			var r1 = Revision.Create("1.2");
			var r2 = Revision.Create("1.4");

			Assert.IsTrue(r1.Precedes(r2));
			Assert.IsFalse(r2.Precedes(r1));
		}

		[TestMethod]
		public void Precedes_BranchpointPrecedesBranchRevision()
		{
			var r1 = Revision.Create("1.2");
			var r2 = Revision.Create("1.2.4.2");

			Assert.IsTrue(r1.Precedes(r2));
			Assert.IsFalse(r2.Precedes(r1));
		}

		[TestMethod]
		public void Precedes_RevisionOnParentBranchPrecedesRevisionOnBranch()
		{
			var r1 = Revision.Create("1.2");
			var r2 = Revision.Create("1.4.4.2");

			Assert.IsTrue(r1.Precedes(r2));
			Assert.IsFalse(r2.Precedes(r1));
		}

		[TestMethod]
		public void Precedes_RevisionOnGrandParentBranchPrecedesRevisionOnBranch()
		{
			var r1 = Revision.Create("1.2");
			var r2 = Revision.Create("1.4.4.2.6.2");

			Assert.IsTrue(r1.Precedes(r2));
			Assert.IsFalse(r2.Precedes(r1));
		}

		#endregion Precedes
	}
}
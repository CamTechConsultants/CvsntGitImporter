/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using CvsGitConverter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
{
	[TestClass]
	public class RevisionTest
	{
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
	}
}

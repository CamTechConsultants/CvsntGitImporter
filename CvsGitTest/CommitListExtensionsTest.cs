/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CvsGitConverter;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the CommitListExtensions class.
	/// </summary>
	[TestClass]
	public class CommitListExtensionsTest
	{
		#region Move

		[TestMethod]
		public void Move_FirstItem()
		{
			var list = new List<int>() { 1, 2, 3, 4, 5 };
			list.Move(0, 3);

			Assert.IsTrue(list.SequenceEqual(new[] { 2, 3, 4, 1, 5 }));
		}

		[TestMethod]
		public void Move_ToEnd()
		{
			var list = new List<int>() { 1, 2, 3, 4, 5 };
			list.Move(2, 4);

			Assert.IsTrue(list.SequenceEqual(new[] { 1, 2, 4, 5, 3 }));
		}

		[TestMethod]
		public void Move_ToSelf()
		{
			var list = new List<int>() { 1, 2, 3, 4, 5 };
			list.Move(2, 2);

			Assert.IsTrue(list.SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
		}

		#endregion Move


		#region IndexOfFromEnd

		[TestMethod]
		public void IndexOfFromEnd_NotFound()
		{
			var list = new List<int>() { 1, 2, 3 };
			var result = list.IndexOfFromEnd(3, 1);
			Assert.AreEqual(result, -1);
		}

		[TestMethod]
		public void IndexOfFromEnd_Found()
		{
			var list = new List<int>() { 1, 2, 3, 4 };
			var result = list.IndexOfFromEnd(2, 3);
			Assert.AreEqual(result, 1);
		}

		#endregion IndexOfFromEnd


		#region ToListIfNeeded

		[TestMethod]
		public void ToListIfNeeded_NotAList()
		{
			var list = Enumerable.Repeat(1, 5);
			var ilist = list.ToListIfNeeded();
			Assert.AreNotSame(list, ilist);
		}

		[TestMethod]
		public void ToListIfNeeded_IsAList()
		{
			var list = Enumerable.Repeat(1, 5).ToList();
			var ilist = list.ToListIfNeeded();
			Assert.AreSame(list, ilist);
		}

		#endregion ToListIfNeeded
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the CommitListExtensions class.
	/// </summary>
	[TestClass]
	public class CommitListExtensionsTest
	{
		#region Move

		private static List<Commit> MakeListOfCommits()
		{
			return new List<Commit>()
			{
				new Commit("c0") { Index = 10 },
				new Commit("c1") { Index = 11 },
				new Commit("c2") { Index = 12 },
				new Commit("c3") { Index = 13 },
				new Commit("c4") { Index = 14 },
			};
		}

		[TestMethod]
		public void Move_Forwards_FirstItem()
		{
			var list = MakeListOfCommits();
			list.Move(0, 3);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c1", "c2", "c3", "c0", "c4"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		public void Move_Forwards_ToEnd()
		{
			var list = MakeListOfCommits();
			list.Move(2, 4);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c3", "c4", "c2"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		public void Move_Forwards_ToSelf()
		{
			var list = MakeListOfCommits();
			list.Move(2, 2);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3", "c4"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		public void Move_Backwards_LastItem()
		{
			var list = MakeListOfCommits();
			list.Move(4, 2);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c4", "c2", "c3"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		public void Move_Backwards_ToStart()
		{
			var list = MakeListOfCommits();
			list.Move(2, 0);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c2", "c0", "c1", "c3", "c4"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		public void Move_Backwards_ToSelf()
		{
			var list = MakeListOfCommits();
			list.Move(2, 2);

			Assert.IsTrue(list.Select(c => c.CommitId).SequenceEqual("c0", "c1", "c2", "c3", "c4"));
			Assert.IsTrue(list.Select(c => c.Index).SequenceEqual(Enumerable.Range(10, 5)));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Move_SourceOutOfRange()
		{
			var list = MakeListOfCommits();
			list.Move(6, 2);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Move_DestOutOfRange()
		{
			var list = MakeListOfCommits();
			list.Move(3, 6);
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
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CTC.CvsntGitImporter;

namespace CTC.CvsntGitImporter.TestCode
{
	[TestClass]
	public class FuncExtensionsTest
	{
		[TestMethod]
		public void Memoize_IsTransparent()
		{
			int counter = 0;
			Func<string, int> f = x => counter++;

			var g = f.Memoize();

			Assert.AreEqual(g("foo"), 0);
			Assert.AreEqual(g("bar"), 1);
		}

		[TestMethod]
		public void Memoize_CachesValues()
		{
			int counter = 0;
			Func<string, int> f = x => counter++;

			var g = f.Memoize();

			Assert.AreEqual(g("foo"), 0);
			Assert.AreEqual(g("bar"), 1);
			Assert.AreEqual(g("bar"), 1);
			Assert.AreEqual(g("foo"), 0);
		}
	}
}
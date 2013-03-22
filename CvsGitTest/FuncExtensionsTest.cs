﻿/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CvsGitConverter;

namespace CvsGitTest
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
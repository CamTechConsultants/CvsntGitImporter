/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Text.RegularExpressions;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	[TestClass]
	public class RenamerTest
	{
		[TestMethod]
		public void Rename_NoMatch()
		{
			var renamer = new Renamer();
			var renamed = renamer.Process("blah");
			Assert.AreEqual(renamed, "blah");
		}

		[TestMethod]
		public void Rename_Match()
		{
			var renamer = new Renamer();
			renamer.AddRule(new RenameRule("a(.)", "b$1"));
			var renamed = renamer.Process("blah");
			Assert.AreEqual(renamed, "blbh");
		}

		[TestMethod]
		public void Rename_MultipleMatches_FirstWins()
		{
			var renamer = new Renamer();
			renamer.AddRule(new RenameRule("a", "b"));
			renamer.AddRule(new RenameRule("h", "x"));

			var renamed = renamer.Process("blah");
			Assert.AreEqual(renamed, "blbh");
		}
	}
}
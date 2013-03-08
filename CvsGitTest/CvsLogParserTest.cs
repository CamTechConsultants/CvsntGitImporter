/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.IO;
using System.Linq;
using CvsGitConverter;
using CvsGitTest.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
{
	/// <summary>
	/// Unit tests for the CvsLogParser class.
	/// </summary>
	[TestClass]
	public class CvsLogParserTest
	{
		[TestMethod]
		public void StandardFormat()
		{
			var parser = new CvsLogParser(new StringReader(CvsLogParserResources.StandardFormat), DateTime.MinValue);
			var revisions = parser.Parse().ToList();

			Assert.AreEqual(revisions.Count(), 2);

			var r = revisions.First();
			Assert.AreEqual(r.Author, "johnb");
			Assert.AreEqual(r.CommitId, "c0449ae6c023bd1");
			Assert.AreEqual(r.File.Name, ".cvsignore");
			Assert.AreEqual(r.IsDead, false);
			Assert.AreEqual(r.Mergepoint, Revision.Empty);
			Assert.AreEqual(r.Revision, Revision.Create("1.2"));
			Assert.AreEqual(r.Time, new DateTime(2009, 3, 4, 11, 54, 43));
		}

		[TestMethod]
		public void Mergepoint()
		{
			var parser = new CvsLogParser(new StringReader(CvsLogParserResources.Mergepoint), DateTime.MinValue);
			var revisions = parser.Parse().ToList();

			var rev = revisions.First(r => r.Revision == Revision.Create("1.2"));
			Assert.AreEqual(rev.Mergepoint, Revision.Create("1.1.2.1"));
		}
		
		[TestMethod]
		[ExpectedException(typeof(ParseException))]
		public void MissingCommitId_AfterStartDate()
		{
			var parser = new CvsLogParser(new StringReader(CvsLogParserResources.MissingCommitId), DateTime.MinValue);
			parser.Parse().ToList();
		}

		[TestMethod]
		public void MissingCommitId_BeforeStartDate()
		{
			var parser = new CvsLogParser(new StringReader(CvsLogParserResources.MissingCommitId), new DateTime(2012, 1, 1));
			var revisions = parser.Parse().ToList();

			Assert.IsFalse(revisions.Any(), "No revisions");
		}

		[TestMethod]
		public void StateDead()
		{
			var parser = new CvsLogParser(new StringReader(CvsLogParserResources.StateDead), DateTime.MinValue);
			var revisions = parser.Parse().ToList();

			var r = revisions.First();
			Assert.IsTrue(r.IsDead);
		}
	}
}
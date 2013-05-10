/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using System.Linq;
using CvsGitConverter;
using CvsGitConverter.Utils;
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
		private TempDir m_temp;
		private string m_sandbox;

		[TestInitialize]
		public void Setup()
		{
			m_temp = new TempDir();
			Directory.CreateDirectory(m_temp.GetPath("CVS"));
			File.WriteAllText(m_temp.GetPath(@"CVS\Repository"), "xjtag/dev/src/Project/test");
			m_sandbox = m_temp.Path;
		}

		[TestCleanup]
		public void Clearup()
		{
			m_temp.Dispose();
		}

		[TestMethod]
		public void StandardFormat()
		{
			var parser = new CvsLogParser(m_sandbox, new StringReader(CvsLogParserResources.StandardFormat));
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
			var parser = new CvsLogParser(m_sandbox, new StringReader(CvsLogParserResources.Mergepoint));
			var revisions = parser.Parse().ToList();

			var rev = revisions.First(r => r.Revision == Revision.Create("1.2"));
			Assert.AreEqual(rev.Mergepoint, Revision.Create("1.1.2.1"));
		}
		
		[TestMethod]
		public void StateDead()
		{
			var parser = new CvsLogParser(m_sandbox, new StringReader(CvsLogParserResources.StateDead));
			var revisions = parser.Parse().ToList();

			var r = revisions.First();
			Assert.IsTrue(r.IsDead);
		}
		
		[TestMethod]
		public void NoCommitId()
		{
			var parser = new CvsLogParser(m_sandbox, new StringReader(CvsLogParserResources.MissingCommitId));
			var revisions = parser.Parse().ToList();

			Assert.AreEqual(revisions.Count, 2);
			Assert.IsTrue(revisions.All(r => r.CommitId == ""));
		}
	}
}
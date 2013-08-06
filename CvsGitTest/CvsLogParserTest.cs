/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CTC.CvsntGitImporter.TestCode.Properties;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the CvsLogParser class.
	/// </summary>
	[TestClass]
	public class CvsLogParserTest
	{
		private TempDir m_temp;
		private string m_sandbox;
		private InclusionMatcher m_branchMatcher;

		[TestInitialize]
		public void Setup()
		{
			m_temp = new TempDir();
			Directory.CreateDirectory(m_temp.GetPath("CVS"));
			File.WriteAllText(m_temp.GetPath(@"CVS\Repository"), "module");
			m_sandbox = m_temp.Path;
			m_branchMatcher = new InclusionMatcher();
		}

		[TestCleanup]
		public void Clearup()
		{
			m_temp.Dispose();
		}

		[TestMethod]
		public void StandardFormat()
		{
			var parser = CreateParser(CvsLogParserResources.StandardFormat);
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

			Assert.IsFalse(parser.ExcludedTags.Any(), "No tags excluded");
			Assert.IsFalse(parser.ExcludedBranches.Any(), "No branches excluded");
		}

		[TestMethod]
		public void Mergepoint()
		{
			var parser = CreateParser(CvsLogParserResources.Mergepoint);
			var revisions = parser.Parse().ToList();

			var rev = revisions.First(r => r.Revision == Revision.Create("1.2"));
			Assert.AreEqual(rev.Mergepoint, Revision.Create("1.1.2.1"));
		}
		
		[TestMethod]
		public void StateDead()
		{
			var parser = CreateParser(CvsLogParserResources.StateDead);
			var revisions = parser.Parse().ToList();

			var r = revisions.First();
			Assert.IsTrue(r.IsDead);
		}
		
		[TestMethod]
		public void FileAddedOnBranch()
		{
			var parser = CreateParser(CvsLogParserResources.FileAddedOnBranch);
			var revisions = parser.Parse().ToList();

			Assert.AreEqual(revisions[1].Revision.ToString(), "1.1");
			Assert.IsTrue(revisions[1].IsDead);
		}

		[TestMethod]
		public void NoCommitId()
		{
			var parser = CreateParser(CvsLogParserResources.MissingCommitId);
			var revisions = parser.Parse().ToList();

			Assert.AreEqual(revisions.Count, 2);
			Assert.IsTrue(revisions.All(r => r.CommitId == ""));
		}

		[TestMethod]
		public void ExcludeBranches()
		{
			m_branchMatcher.AddExcludeRule(@"^branch2");
			m_branchMatcher.AddIncludeRule(@"^branch1");

			var parser = CreateParser(CvsLogParserResources.Branches);
			parser.Parse().ToList();
			var file = parser.Files.Single();

			Assert.AreEqual(file.GetBranchpointForBranch("branch1"), Revision.Create("1.1"));
			Assert.AreEqual(file.GetBranchpointForBranch("branch2"), Revision.Empty);
			Assert.AreEqual(parser.ExcludedBranches.Single(), "branch2");
		}

		[TestMethod]
		public void NonAsciiFile()
		{
			using (var temp = new TempDir())
			{
				// write the log file in the default encoding, which is what the CVS log will typically be in
				var cvsLog = temp.GetPath("cvs.log");
				File.WriteAllText(cvsLog, CvsLogParserResources.NonAscii, Encoding.Default);

				var parser = new CvsLogParser(m_sandbox, cvsLog, m_branchMatcher);
				parser.Parse().ToList();
				var file = parser.Files.Single();

				Assert.AreEqual(file.Name, "demo©.xje");
			}
		}


		private CvsLogParser CreateParser(string log)
		{
			return new CvsLogParser(m_sandbox, new StringReader(log), m_branchMatcher);
		}
	}
}
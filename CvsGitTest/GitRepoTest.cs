/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the GitRepo class.
	/// </summary>
	[TestClass]
	public class GitRepoTest
	{
		private ILogger m_log;

		public GitRepoTest()
		{
			m_log = MockRepository.GenerateStub<ILogger>();
		}


		[TestMethod]
		public void Init_CreatesGitFiles()
		{
			using (var temp = new TempDir())
			{
				var git = new GitRepo(m_log, temp.Path);
				git.Init(Enumerable.Empty<GitConfigOption>());

				Assert.IsTrue(Directory.Exists(temp.GetPath("refs")));
				Assert.IsTrue(File.Exists(temp.GetPath("HEAD")));
			}
		}

		[TestMethod]
		public void Init_CreatesDirectoryIfItDoesNotExist()
		{
			using (var temp = new TempDir())
			{
				var gitdir = temp.GetPath(@"dir1\dir2");
				var git = new GitRepo(m_log, gitdir);
				git.Init(Enumerable.Empty<GitConfigOption>());

				Assert.IsTrue(Directory.Exists(gitdir));
				Assert.IsTrue(Directory.Exists(temp.GetPath(@"dir1\dir2\refs")));
			}
		}

		[TestMethod]
		public void Init_Option_Set()
		{
			using (var temp = new TempDir())
			{
				var git = new GitRepo(m_log, temp.Path);
				git.Init(new[] { new GitConfigOption("foo.bar", "blah", add: false) });

				var configFile = temp.GetPath("config");
				var configContents = File.ReadAllLines(configFile);

				var sectionLineNumber = FindSectionHeader(configContents, "foo");
				Assert.IsTrue(sectionLineNumber >= 0);

				Assert.IsTrue(Regex.IsMatch(configContents[sectionLineNumber + 1].Trim(), @"bar\s*=\s*blah"));
			}
		}

		[TestMethod]
		public void Init_Option_Add()
		{
			using (var temp = new TempDir())
			{
				var git = new GitRepo(m_log, temp.Path);
				git.Init(new[]
				{
					new GitConfigOption("foo.bar", "blah1", add: true),
					new GitConfigOption("foo.bar", "blah2", add: true),
				});

				var configFile = temp.GetPath("config");
				var configContents = File.ReadAllLines(configFile);

				var sectionLineNumber = FindSectionHeader(configContents, "foo");
				Assert.IsTrue(sectionLineNumber >= 0);

				Assert.IsTrue(Regex.IsMatch(configContents[sectionLineNumber + 1].Trim(), @"bar\s*=\s*blah1"));
				Assert.IsTrue(Regex.IsMatch(configContents[sectionLineNumber + 2].Trim(), @"bar\s*=\s*blah2"));
			}
		}


		private static int FindSectionHeader(IEnumerable<string> lines, string sectionName)
		{
			var sectionHeader = String.Format("[{0}]", sectionName);
			int lineNumber = 0;

			foreach (var line in lines)
			{
				if (line.Trim() == sectionHeader)
					return lineNumber;
				lineNumber++;
			}

			return -1;
		}
	}
}
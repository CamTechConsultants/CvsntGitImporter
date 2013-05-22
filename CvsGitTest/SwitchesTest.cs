/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using CTC.CvsntGitImporter;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the Switches class.
	/// </summary>
	[TestClass]
	public class SwitchesTest
	{
		[TestMethod]
		public void CvsProcesses_DefaultValue()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath());

			Assert.AreEqual(switches.CvsProcesses, Environment.ProcessorCount);
		}

		[TestMethod]
		public void CvsProcesses_ValueProvided()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--cvs-processes", "42");

			Assert.AreEqual(switches.CvsProcesses, 42);
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void CvsProcesses_Zero()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--cvs-processes", "0");
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void CvsProcesses_InvalidInt()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--cvs-processes", "blah");
		}

		[TestMethod]
		public void RenameTag()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--rename-tag", "foo/bar");

			Assert.AreEqual(switches.TagRename.Process("foobar"), "barbar");
		}

		[TestMethod]
		public void RenameBranch()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--rename-branch", "foo/bar");

			Assert.AreEqual(switches.BranchRename.Process("foobar"), "barbar");
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void Rename_RuleMissingSlash()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--rename-tag", "blah");
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void Rename_InvalidRegex()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--rename-tag", "**/foo");
		}

		[TestMethod]
		public void Tagger_DefaultEmail()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--default-domain", "example.com", "--tagger-name", "Joe Bloggs");

			Assert.AreEqual(switches.Tagger.Email, "Joe@example.com");
		}

		[TestMethod]
		public void Tagger_NoDefaultEmailIfSetExplicitly()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--default-domain", "example.com", "--tagger-email", "blah@example.com");

			Assert.AreEqual(switches.Tagger.Email, "blah@example.com");
		}
	}
}
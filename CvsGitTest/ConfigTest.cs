/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using CTC.CvsntGitImporter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the Config class.
	/// </summary>
	[TestClass]
	public class ConfigTest
	{
		#region BranchpointRule

		[TestMethod]
		public void BranchpointRule_Unspecified()
		{
			var switches = new Switches();
			var config = new Config(switches);

			Assert.IsNull(config.BranchpointRule);
		}

		[TestMethod]
		public void BranchpointRule_Valid()
		{
			var config = new Config(new Switches());
			config.ParseCommandLineSwitches("--sandbox", Path.GetTempPath(), "--branchpoint-rule", @"^(.*)$/$1-branchpoint");

			Assert.IsNotNull(config.BranchpointRule);
			Assert.AreEqual("x-branchpoint", config.BranchpointRule.Apply("x"));
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void BranchpointRule_Invalid()
		{
			var config = new Config(new Switches());
			config.ParseCommandLineSwitches("--sandbox", Path.GetTempPath(), "--branchpoint-rule", @"xxx");
		}

		#endregion BranchpointRule


		#region CvsLogFileName

		[TestMethod]
		public void CvsLogFileName_NotSpecified_DefaultValue()
		{
			var switches = new Switches();
			var config = new Config(switches);

			Assert.AreEqual(Path.GetFullPath(@"DebugLogs\cvs.log"), config.CvsLogFileName);
			Assert.IsTrue(config.CreateCvsLog);
		}

		[TestMethod]
		public void CvsLogFileName_Specified_Exists()
		{
			using (var temp = new TempDir())
			{
				var logFilename = temp.GetPath("blah.log");
				File.WriteAllText(logFilename, "blah");
				var switches = new Switches()
				{
					CvsLog = logFilename,
				};
				var config = new Config(switches);

				Assert.AreEqual(logFilename, config.CvsLogFileName);
				Assert.IsFalse(config.CreateCvsLog);
			}
		}

		[TestMethod]
		public void CvsLogFileName_Specified_DoesNotExist()
		{
			using (var temp = new TempDir())
			{
				var logFilename = temp.GetPath("blah.log");
				var switches = new Switches()
				{
					CvsLog = logFilename,
				};
				var config = new Config(switches);

				Assert.AreEqual(logFilename, config.CvsLogFileName);
				Assert.IsTrue(config.CreateCvsLog);
			}
		}

		#endregion


		#region CvsProcesses

		[TestMethod]
		public void CvsProcesses_DefaultValue()
		{
			var switches = new Switches();
			var config = new Config(switches);

			Assert.AreEqual(config.CvsProcesses, (uint)Environment.ProcessorCount);
		}

		[TestMethod]
		public void CvsProcesses_ValueProvided()
		{
			var switches = new Switches()
			{
				CvsProcesses = 42,
			};
			var config = new Config(switches);

			Assert.AreEqual(config.CvsProcesses, 42u);
		}

		#endregion CvsProcesses


		#region RenameTag/RenameBranch

		[TestMethod]
		public void RenameTag()
		{
			var switches = new Switches();
			var config = new Config(switches);
			
			switches.RenameTag.Add("foo/bar");

			Assert.AreEqual(config.TagRename.Process("foobar"), "barbar");
		}

		[TestMethod]
		public void RenameTag_WhitespaceIsTrimmed()
		{
			var switches = new Switches();
			var config = new Config(switches);

			switches.RenameTag.Add("  foo /  \tbar");

			Assert.AreEqual(config.TagRename.Process("foobar"), "barbar");
		}

		[TestMethod]
		public void RenameBranch()
		{
			var switches = new Switches();
			var config = new Config(switches);

			switches.RenameBranch.Add("foo/bar");

			Assert.AreEqual(config.BranchRename.Process("foobar"), "barbar");
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void Rename_RuleMissingSlash()
		{
			var switches = new Switches();
			var config = new Config(switches);

			switches.RenameTag.Add("blah");
		}

		[TestMethod]
		[ExpectedException(typeof(CommandLineArgsException))]
		public void Rename_InvalidRegex()
		{
			var switches = new Switches();
			var config = new Config(switches);

			switches.RenameTag.Add("**/foo");
		}

		#endregion RenameTag/RenameBranch


		#region Nobody

		[TestMethod]
		public void Nobody_DefaultEmail()
		{
			var switches = new Switches()
			{
				DefaultDomain = "example.com",
				NobodyName = "Joe Bloggs",
			};
			var config = new Config(switches);

			Assert.AreEqual(config.Nobody.Email, "Joe@example.com");
		}

		[TestMethod]
		public void Nobody_NoDefaultEmailIfSetExplicitly()
		{
			var switches = new Switches()
			{
				DefaultDomain = "example.com",
				NobodyEmail = "blah@example.com",

			};
			var config = new Config(switches);

			Assert.AreEqual(config.Nobody.Email, "blah@example.com");
		}

		#endregion Nobody


		#region Users

		[TestMethod]
		public void Users_EmptyUserCreated()
		{
			var switches = new Switches()
			{
				NobodyName = "fred",
				NobodyEmail = "fred@example.com",
			};
			var config = new Config(switches);

			var nobody = config.Users.GetUser("");
			Assert.AreEqual(nobody.Name, "fred");
		}

		#endregion
	}
}
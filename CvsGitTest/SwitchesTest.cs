/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.IO;
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
		[ExpectedException(typeof(CommandLineArgsException))]
		public void GitDir_NotEmpty()
		{
			using (var temp = new TempDir())
			{
				File.WriteAllText(temp.GetPath("file.txt"), "blah");

				var switches = new Switches();
				switches.Parse("--sandbox", Path.GetTempPath(), "--gitdir", temp.Path);
			}
		}

		[TestMethod]
		public void GitDir_NotEmpty_NoImportSpecified()
		{
			using (var temp = new TempDir())
			{
				File.WriteAllText(temp.GetPath("file.txt"), "blah");

				var switches = new Switches();
				switches.Parse("--sandbox", Path.GetTempPath(), "--gitdir", temp.Path, "--noimport");
			}
		}

		[TestMethod]
		public void GitDir_InvalidChars()
		{
			var switches = new Switches();
			switches.Parse("--sandbox", Path.GetTempPath(), "--gitdir", "blah:blah");
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
		public void ConfFile_VerifyNotCalledUntilAllSwitchesProcessed()
		{
			using (var temp = new TempDir())
			{
				var confFileName = temp.GetPath("test.conf");
				File.WriteAllText(confFileName, "noimport\r\n");
				var sandbox = temp.GetPath("sandbox");
				Directory.CreateDirectory(sandbox);

				// specify sandbox directory after the conf file has been processed
				var switches = new Switches();
				switches.Parse("-C", confFileName, "--sandbox", sandbox);

				Assert.IsTrue(switches.NoImport);
				Assert.AreEqual(sandbox, switches.Sandbox);
			}
		}

		[TestMethod]
		public void MarkerTag_LeftBlank()
		{
			using (var temp = new TempDir())
			{
				var confFileName = temp.GetPath("test.conf");
				File.WriteAllText(confFileName, "import-marker-tag  \"\"\r\n");

				var sandbox = temp.GetPath("sandbox");
				Directory.CreateDirectory(sandbox);

				var switches = new Switches();
				switches.Parse("-C", confFileName, "--sandbox", sandbox);

				Assert.AreEqual(switches.MarkerTag, "");
			}
		}
	}
}
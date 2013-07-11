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
	}
}
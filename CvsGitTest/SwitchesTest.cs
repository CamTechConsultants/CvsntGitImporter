/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.IO;
using CvsGitConverter;
using CvsGitConverter.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CvsGitTest
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
	}
}
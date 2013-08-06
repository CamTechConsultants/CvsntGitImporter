/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the GitConfigOption class.
	/// </summary>
	[TestClass]
	public class GitConfigOptionTest
	{
		[TestMethod]
		public void Parse_ValidSetArgument()
		{
			var option = GitConfigOption.Parse(" section.name = 42 ");

			Assert.AreEqual(option.Name, "section.name");
			Assert.AreEqual(option.Value, "42");
			Assert.IsFalse(option.Add);
		}

		[TestMethod]
		public void Parse_ValidAddArgument()
		{
			var option = GitConfigOption.Parse(" section.name = 42 ", true);

			Assert.AreEqual(option.Name, "section.name");
			Assert.AreEqual(option.Value, "42");
			Assert.IsTrue(option.Add);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Parse_InvalidArgument_EmptyString_ThrowsException()
		{
			var option = GitConfigOption.Parse("");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Parse_InvalidArgument_NoEquals_ThrowsException()
		{
			var option = GitConfigOption.Parse("section.name ");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Parse_InvalidArgument_NoName_ThrowsException()
		{
			var option = GitConfigOption.Parse(" =   blah");
		}
	}
}

/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for the RenameRule class.
	/// </summary>
	[TestClass]
	public class RenameRuleTest
	{
		#region Parse

		[TestMethod]
		public void Parse_ValidString()
		{
			var ruleString = @"^(x+) / $1x";
			var rule = RenameRule.Parse(ruleString);

			Assert.IsTrue(rule.IsMatch("xx_"));
			Assert.AreEqual("xxx_", rule.Apply("xx_"));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Parse_InvalidString()
		{
			var ruleString = @"(.+) | $1x";
			var rule = RenameRule.Parse(ruleString);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Parse_InvalidRegex()
		{
			var ruleString = @"(.**) / $1x";
			var rule = RenameRule.Parse(ruleString);
		}

		#endregion Parse
	}
}
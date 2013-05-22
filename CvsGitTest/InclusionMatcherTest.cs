/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Text.RegularExpressions;
using CTC.CvsntGitImporter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTC.CvsntGitImporter.TestCode
{
	/// <summary>
	/// Unit tests for InclusionMatcher class.
	/// </summary>
	[TestClass]
	public class InclusionMatcherTest
	{
		[TestMethod]
		public void AddIncludeRuleFirst_ExcludesByDefault()
		{
			var matcher = new InclusionMatcher();
			matcher.AddIncludeRule(new Regex(@"xx"));

			var result = matcher.Match("blah");
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void AddExcludeRuleFirst_IncludesByDefault()
		{
			var matcher = new InclusionMatcher();
			matcher.AddExcludeRule(new Regex(@"xx"));

			var result = matcher.Match("blah");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void AddExcludeThenInclude_Matches()
		{
			var matcher = new InclusionMatcher();
			matcher.AddExcludeRule(new Regex(@"xx"));
			matcher.AddIncludeRule(new Regex(@"yy"));

			var result = matcher.Match("aaxx");
			Assert.IsFalse(result);

			result = matcher.Match("xxyy");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void AddIncludeThenExclude_Matches()
		{
			var matcher = new InclusionMatcher();
			matcher.AddIncludeRule(new Regex(@"xx"));
			matcher.AddExcludeRule(new Regex(@"yy"));

			var result = matcher.Match("aaxx");
			Assert.IsTrue(result);

			result = matcher.Match("xxyy");
			Assert.IsFalse(result);
		}
	}
}

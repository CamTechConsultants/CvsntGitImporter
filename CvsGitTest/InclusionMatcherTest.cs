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
			matcher.AddIncludeRule(@"xx");

			var result = matcher.Match("blah");
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void AddExcludeRuleFirst_IncludesByDefault()
		{
			var matcher = new InclusionMatcher();
			matcher.AddExcludeRule(@"xx");

			var result = matcher.Match("blah");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void AddExcludeThenInclude_Matches()
		{
			var matcher = new InclusionMatcher();
			matcher.AddExcludeRule(@"xx");
			matcher.AddIncludeRule(@"yy");

			var result = matcher.Match("aaxx");
			Assert.IsFalse(result);

			result = matcher.Match("xxyy");
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void AddIncludeThenExclude_Matches()
		{
			var matcher = new InclusionMatcher();
			matcher.AddIncludeRule(@"xx");
			matcher.AddExcludeRule(@"yy");

			var result = matcher.Match("aaxx");
			Assert.IsTrue(result);

			result = matcher.Match("xxyy");
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void CaseSensitive_MatchesCase()
		{
			var matcher = new InclusionMatcher(ignoreCase: false);
			matcher.AddIncludeRule(@"xx");

			var result = matcher.Match("XX");
			Assert.IsFalse(result);
		}

		[TestMethod]
		public void CaseInsensitive_IgnoresCase()
		{
			var matcher = new InclusionMatcher(ignoreCase: true);
			matcher.AddIncludeRule(@"xx");

			var result = matcher.Match("XX");
			Assert.IsTrue(result);
		}
	}
}

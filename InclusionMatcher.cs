/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Manages a list of include/exclude rules.
	/// </summary>
	class InclusionMatcher
	{
		private readonly List<Rule> m_rules = new List<Rule>();
		private readonly RegexOptions m_regexOptions;

		/// <summary>
		/// The default match value if no rules are added. The default is true.
		/// </summary>
		public bool Default = true;

		/// <summary>
		/// Should this matcher ignore case?
		/// </summary>
		public readonly bool IgnoreCase;

		public InclusionMatcher(bool ignoreCase = false)
		{
			IgnoreCase = ignoreCase;
			m_regexOptions = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
		}

		/// <summary>
		/// Add a rule that includes items if it matches.
		/// </summary>
		/// <exception cref="ArgumentException">the regex pattern is invalid</exception>
		public void AddIncludeRule(string regex)
		{
			if (m_rules.Count == 0)
				m_rules.Add(MakeRule(".", false));

			m_rules.Add(MakeRule(regex, true));
		}

		/// <summary>
		/// Add a rule that excludes items if it matches.
		/// </summary>
		/// <exception cref="ArgumentException">the regex pattern is invalid</exception>
		public void AddExcludeRule(string regex)
		{
			if (m_rules.Count == 0)
				m_rules.Add(MakeRule(".", true));

			m_rules.Add(MakeRule(regex, false));
		}

		/// <summary>
		/// Matches an item.
		/// </summary>
		public bool Match(string item)
		{
			return m_rules.Aggregate(Default, (isMatched, rule) => rule.Match(item, isMatched));
		}

		private Rule MakeRule(string regex, bool include)
		{
			return new Rule(new Regex(regex, m_regexOptions), include);
		}

		private class Rule
		{
			private readonly Regex m_regex;
			private readonly bool m_include;

			public Rule(Regex regex, bool include)
			{
				m_regex = regex;
				m_include = include;
			}

			public bool Match(string item, bool isMatched)
			{
				if (m_regex.IsMatch(item))
					return m_include;
				else
					return isMatched;
			}

			public override string ToString()
			{
				return String.Format("{0} ({1})", m_regex, m_include ? "include" : "exclude");
			}
		}
	}
}
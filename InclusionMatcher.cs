/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	/// <summary>
	/// Manages a list of include/exclude rules.
	/// </summary>
	class InclusionMatcher
	{
		private readonly List<Rule> m_rules = new List<Rule>();


		/// <summary>
		/// Add a rule that includes items if it matches.
		/// </summary>
		public void AddIncludeRule(string regex)
		{
			if (m_rules.Count == 0)
				m_rules.Add(new Rule(new Regex("."), false));

			m_rules.Add(new Rule(new Regex(regex), true));
		}

		/// <summary>
		/// Add a rule that excludes items if it matches.
		/// </summary>
		public void AddExcludeRule(string regex)
		{
			if (m_rules.Count == 0)
				m_rules.Add(new Rule(new Regex("."), true));

			m_rules.Add(new Rule(new Regex(regex), false));
		}

		/// <summary>
		/// Matches an item.
		/// </summary>
		public bool Match(string item)
		{
			return m_rules.Aggregate(false, (isMatched, rule) => rule.Match(item, isMatched));
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
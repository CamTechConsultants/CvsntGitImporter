/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CvsGitConverter
{
	/// <summary>
	/// Collection of rename rules to rename tags and branches. The rules are processed in the order in which
	/// they were added and processing stops as soon as a rule matches.
	/// </summary>
	class Renamer
	{
		private readonly List<Rule> m_rules = new List<Rule>();

		/// <summary>
		/// Adds a renaming rule.
		/// </summary>
		public void AddRule(Regex pattern, string replacement)
		{
			m_rules.Add(new Rule(pattern, replacement));
		}

		/// <summary>
		/// Process a name, renaming it if it matches a rule.
		/// </summary>
		public string Process(string name)
		{
			var match = m_rules.FirstOrDefault(r => r.Pattern.IsMatch(name));
			if (match == null)
				return name;
			else
				return match.Pattern.Replace(name, match.Replacement);
		}


		private class Rule
		{
			public readonly Regex Pattern;
			public readonly string Replacement;

			public Rule(Regex pattern, string replacement)
			{
				this.Pattern = pattern;
				this.Replacement = replacement;
			}
		}
	}
}
/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// A rename rule - a regular expression and a replacement pattern.
	/// </summary>
	class RenameRule
	{
		private readonly Regex m_pattern;
		private readonly string m_replacement;

		public RenameRule(Regex pattern, string replacement)
		{
			this.m_pattern = pattern;
			this.m_replacement = replacement;
		}

		public RenameRule(string pattern, string replacement) : this(new Regex(pattern), replacement)
		{
		}

		/// <summary>
		/// Parse a rename rule, where the pattern and replacement are separated by a slash.
		/// This is the form it is passed in on the command-line.
		/// </summary>
		/// <exception cref="ArgumentException">the format of the rule is invalid</exception>
		public static RenameRule Parse(string ruleString)
		{
			var parts = ruleString.Split('/');
			if (parts.Length != 2)
				throw new ArgumentException(String.Format("The string is not in the expected format: {0}", ruleString));

			var regex = new Regex(parts[0].Trim());
			return new RenameRule(regex, parts[1].Trim());
		}

		/// <summary>
		/// Does this rule match an input string>
		/// </summary>
		public bool IsMatch(string input)
		{
			return m_pattern.IsMatch(input);
		}

		/// <summary>
		/// Apply the rule.
		/// </summary>
		public string Apply(string input)
		{
			return m_pattern.Replace(input, m_replacement);
		}

		public override string ToString()
		{
			return string.Format("{0} -> {1}", m_pattern.ToString(), m_replacement);
		}
	}
}
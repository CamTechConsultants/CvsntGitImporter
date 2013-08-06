/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// A git configuration option.
	/// </summary>
	class GitConfigOption
	{
		public readonly string Name;

		public readonly string Value;

		public readonly bool Add;

		public GitConfigOption()
		{
		}

		public GitConfigOption(string name, string value, bool add = false) : this()
		{
			Name = name;
			Value = value;
			Add = add;
		}

		public static GitConfigOption Parse(string item, bool add = false)
		{
			var equals = item.IndexOf('=');
			if (equals < 0)
				throw new ArgumentException("No value found for the option: " + item);

			var name = item.Remove(equals).Trim();
			if (name.Length == 0)
				throw new ArgumentException("Empty option name: " + item);

			var value = item.Substring(equals + 1).Trim();
			return new GitConfigOption(name, value, add);
		}
	}
}
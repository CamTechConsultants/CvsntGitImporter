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
	/// Represents a CVS revision number.
	/// </summary>
	class Revision
	{
		private static Dictionary<string, Revision> m_cache = new Dictionary<string, Revision>();

		private string m_value;

		public static Revision Empty = new Revision("");

		private Revision(string value)
		{
			if (value.Length > 0 && !Regex.IsMatch(value, @"\d+(\.\d+){1,}"))
				throw new ArgumentException(String.Format("Invalid revision format: '{0}'", value));

			m_value = value;
		}

		/// <summary>
		/// Returns an instance of the <see cref="Revision"/> class.
		/// </summary>
		/// <exception cref="ArgumentException">if the revision string is invalid</exception>
		public static Revision Create(string value)
		{
			Revision r;
			if (m_cache.TryGetValue(value, out r))
				return r;

			r = new Revision(value);
			m_cache.Add(value, r);
			return r;
		}

		/// <summary>
		/// Split the revision up into parts.
		/// </summary>
		public IEnumerable<int> Parts
		{
			get { return m_value.Split('.').Select(p => int.Parse(p)); }
		}

		/// <summary>
		/// Is this revision actually the start of a branch?
		/// </summary>
		public bool IsBranch
		{
			get
			{
				var parts = this.Parts.ToArray();
				return parts.Length > 3 && parts[parts.Length - 2] == 0;
			}
		}

		/// <summary>
		/// If the revision is actually a branch, get the stem for all revisions on the branch.
		/// </summary>
		/// <remarks>Effectively converts a.b.0.x into a.b.x</remarks>
		public Revision BranchStem
		{
			get
			{
				var parts = this.Parts.ToArray();
				return Revision.Create(String.Format("{0}.{1}", String.Join(".", parts.Take(parts.Length - 2)), parts[parts.Length - 1]));
			}
		}

		public override string ToString()
		{
			return m_value;
		}

		public static bool operator==(Revision a, string b)
		{
			return a.m_value == b;
		}

		public static bool operator !=(Revision a, string b)
		{
			return a.m_value != b;
		}

		public override bool Equals(object obj)
		{
			if (obj is string)
				return m_value == (string)obj;
			else if (obj is Revision)
				return m_value == ((Revision)obj).m_value;
			else
				return false;
		}

		public override int GetHashCode()
		{
			return m_value.GetHashCode();
		}
	}
}
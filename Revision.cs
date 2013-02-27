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

		private int[] m_parts;

		public static Revision Empty = new Revision("");

		private Revision(string value)
		{
			if (value.Length > 0 && !Regex.IsMatch(value, @"\d+(\.\d+){1,}"))
				throw new ArgumentException(String.Format("Invalid revision format: '{0}'", value));

			m_parts = value.Split('.').Select(p => int.Parse(p)).ToArray();
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
			get { return m_parts; }
		}

		/// <summary>
		/// Is this revision actually the start of a branch?
		/// </summary>
		public bool IsBranch
		{
			get
			{
				return m_parts.Length > 3 && m_parts[m_parts.Length - 2] == 0;
			}
		}

		/// <summary>
		/// Get the branch stem for a revision.
		/// </summary>
		/// <remarks>If the revision is a branch point, then effectively converts a.b.0.x into a.b.x,
		/// otherwise it just removes the last item.</remarks>
		/// <exception cref="InvalidOperationException">if the revision is on MAIN</exception>
		public Revision BranchStem
		{
			get
			{
				if (IsBranch)
					return Revision.Create(String.Format("{0}.{1}", String.Join(".", m_parts.Take(m_parts.Length - 2)), m_parts[m_parts.Length - 1]));
				else
					return Revision.Create(String.Join(".", m_parts.Take(m_parts.Length - 1)));
			}
		}

		public override string ToString()
		{
			return String.Join(".", m_parts);
		}

		public static bool operator==(Revision a, string b)
		{
			return a.ToString() == b;
		}

		public static bool operator !=(Revision a, string b)
		{
			return a.ToString() != b;
		}

		public override bool Equals(object obj)
		{
			if (obj is string)
			{
				return this.ToString() == (string)obj;
			}
			else
			{
				var other = obj as Revision;
				if (other == null)
					return false;

				if (this.m_parts.Length != other.m_parts.Length)
					return false;

				for (int i = 0; i < m_parts.Length; i++)
				{
					if (this.m_parts[i] != other.m_parts[i])
						return false;
				}

				return true;
			}
		}

		public override int GetHashCode()
		{
			return m_parts.GetHashCode();
		}
	}
}
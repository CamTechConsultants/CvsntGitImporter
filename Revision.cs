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

		public static Revision Empty = new Revision(new int[0]);

		private Revision(string value)
		{
			if (value.Length > 0 && !Regex.IsMatch(value, @"\d+(\.\d+){1,}"))
				throw new ArgumentException(String.Format("Invalid revision format: '{0}'", value));

			m_parts = value.Split('.').Select(p => int.Parse(p)).ToArray();
			Validate(m_parts);
		}

		private Revision(int[] parts)
		{
			m_parts = parts;
			Validate(m_parts);
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
				if (m_parts.Length <= 2)
					throw new InvalidOperationException("Cannot get branch stem for revisions on MAIN");

				var branchParts = new int[m_parts.Length - 1];
				if (IsBranch)
				{
					Array.Copy(m_parts, branchParts, m_parts.Length - 2);
					branchParts[branchParts.Length - 1] = m_parts[m_parts.Length - 1];
				}
				else
				{
					Array.Copy(m_parts, branchParts, m_parts.Length - 1);
				}

				return Revision.Create(String.Join(".", branchParts));
			}
		}

		/// <summary>
		/// Does this revision directly precede another?
		/// </summary>
		public bool DirectlyPrecedes(Revision other)
		{
			var precedingParts = new int[other.m_parts.Length];
			Array.Copy(other.m_parts, precedingParts, other.m_parts.Length);

			precedingParts[precedingParts.Length - 1]--;
			if (precedingParts[precedingParts.Length - 1] == 0 && precedingParts.Length > 2)
			{
				// we've reached the start of a branch - trim the last two elements
				var tmp = precedingParts;
				precedingParts = new int[precedingParts.Length - 2];
				Array.Copy(tmp, precedingParts, precedingParts.Length);
			}

			return PartsEqual(m_parts, precedingParts);
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

		public bool Equals(Revision other)
		{
			if (other == null)
				return false;

			return PartsEqual(this.m_parts, other.m_parts);
		}

		public override bool Equals(object obj)
		{
			if (obj is string)
				return this.ToString() == (string)obj;
			else
				return this.Equals(obj as Revision);
		}

		public override int GetHashCode()
		{
			return m_parts.GetHashCode();
		}


		private static bool PartsEqual(int[] a, int[] b)
		{
			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
					return false;
			}

			return true;
		}

		private static void Validate(int[] parts)
		{
			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i] < 1 && (parts.Length <= 2 || i != parts.Length - 2))
				{
					throw new ArgumentException(String.Format("Invalid revision: '{0}' - a part is 0 or negative",
							String.Join(".", parts)));
				}
			}

			// check branch number is even
			if (parts.Length > 2)
			{
				int branchIndex = (parts.Length % 2 == 1) ? parts.Length - 1 : parts.Length - 2;
				if (parts[branchIndex] % 2 == 1)
				{
					throw new ArgumentException(String.Format("Invalid revision: '{0}' - the branch index must be even",
							String.Join(".", parts)));
				}

				// check that a branchpoint (a.b.0.X) is correct - X should be even
				if (parts.Length % 2 == 0 && parts[parts.Length - 2] == 0 && parts[parts.Length - 1] % 2 == 1)
				{
					throw new ArgumentException(String.Format("Invalid revision: '{0}' - the branch index must be even",
							String.Join(".", parts)));
				}
			}
		}
	}
}
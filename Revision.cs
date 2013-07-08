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
	/// Represents a CVS revision number.
	/// </summary>
	class Revision : IEquatable<Revision>
	{
		private static Dictionary<string, Revision> m_cache = new Dictionary<string, Revision>();

		private int[] m_parts;

		/// <summary>
		/// The empty revision.
		/// </summary>
		public static Revision Empty = new Revision(new int[0]);

		/// <summary>
		/// The first revision of any file.
		/// </summary>
		public static Revision First = Revision.Create("1.1");

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
		/// Implicit string conversion operator.
		/// </summary>
		/// <exception cref="ArgumentException">string is not a valid CVS revision number</exception>
		public static implicit operator Revision(string revision)
		{
			return Revision.Create(revision);
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
		/// Get the version that the branch containing the current version branches from.
		/// </summary>
		public Revision GetBranchpoint()
		{
			if (m_parts.Length <= 2)
				throw new InvalidOperationException("Cannot get branch stem for revisions on MAIN");

			// round the number of parts down to the next multiple of 2
			var newPartsLength = (m_parts.Length - 1) / 2 * 2;
			var newParts = new int[newPartsLength];
			Array.Copy(m_parts, newParts, newPartsLength);
			return Revision.Create(String.Join(".", newParts));
		}

		/// <summary>
		/// Does this revision directly precede another?
		/// </summary>
		public bool DirectlyPrecedes(Revision other)
		{
			if (other == Revision.Empty)
				return false;
			else if (other == Revision.First)
				return this == Revision.Empty;

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

		/// <summary>
		/// Is this revision a predecessor of another?
		/// </summary>
		public bool Precedes(Revision other)
		{
			if (this.m_parts.Length > other.m_parts.Length)
				return false;

			var length = this.m_parts.Length;
			var truncatedParts = new int[length];
			Array.Copy(other.m_parts, truncatedParts, length);

			for (int i = 0; i < length - 1; i++)
			{
				if (this.m_parts[i] != other.m_parts[i])
					return false;
			}

			return this.m_parts[length - 1] <= other.m_parts[length - 1];
		}

		public override string ToString()
		{
			if (m_parts.Length == 0)
				return "<none>";
			else
				return String.Join(".", m_parts);
		}

		public static bool operator ==(Revision a, string b)
		{
			return a.ToString() == b;
		}

		public static bool operator !=(Revision a, string b)
		{
			return a.ToString() != b;
		}

		public bool Equals(Revision other)
		{
			return Object.ReferenceEquals(this, other);
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
/*
 * John Hall <john.hall@xjtag.com>
 * Copyright (c) Midas Yellow Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace CvsGitConverter
{
	/// <summary>
	/// Information about a file in CVS.
	/// </summary>
	class FileInfo
	{
		public readonly string Name;

		/// <summary>
		/// The tags defined on the file.
		/// </summary>
		public readonly Dictionary<string, Revision> Tags = new Dictionary<string, Revision>();

		/// <summary>
		/// The branches defined on the file.
		/// </summary>
		public readonly Dictionary<string, Revision> Branches = new Dictionary<string, Revision>();

		public FileInfo(string name)
		{
			this.Name = name;
		}

		public void AddTag(string name, Revision revision)
		{
			// work out whether it's a normal tag or a branch tag
			var parts = revision.Parts.ToArray();
			if (IsBranchTag(parts))
				Branches[name] = GetBranchStem(parts);
			else
				Tags[name] = revision;
		}

		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Is the revision a branch tag?
		/// </summary>
		/// <remarks>Branch tags have a revision of the form X.0.a, where revisions on the branch will have the form X.a.1, X.a.2, etc</remarks>
		private static bool IsBranchTag(int[] parts)
		{
			return parts.Length > 3 && parts[parts.Length - 2] == 0;
		}

		/// <summary>
		/// Get the stem revision of a branch.
		/// </summary>
		private static Revision GetBranchStem(int[] parts)
		{
			return Revision.Create(String.Format("{0}.{1}", String.Join(".", parts.Take(parts.Length - 2)), parts[parts.Length - 1]));
		}
	}
}

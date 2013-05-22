/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Commit comparison.
	/// </summary>
	abstract class CommitComparer : IEqualityComparer<Commit>
	{
		/// <summary>
		/// A CommitComparer that compares by CommitId.
		/// </summary>
		public static readonly CommitComparer ById = new IdCommitComparer();


		public abstract bool Equals(Commit x, Commit y);

		public abstract int GetHashCode(Commit obj);

		/// <summary>
		/// CommitComparer that compares by id.
		/// </summary>
		private class IdCommitComparer : CommitComparer
		{
			public override bool Equals(Commit x, Commit y)
			{
				return x.CommitId == y.CommitId;
			}

			public override int GetHashCode(Commit obj)
			{
				return obj.CommitId.GetHashCode();
			}
		}
	}
}

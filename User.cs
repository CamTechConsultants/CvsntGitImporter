/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// A git user.
	/// </summary>
	class User
	{
		public readonly string Name;

		public readonly string Email;

		/// <summary>
		/// Was this user generated automatically from the CVS user name (i.e. it was not found in the
		/// supplied user file)?
		/// </summary>
		public readonly bool Generated;

		public User(string name, string email, bool generated = false)
		{
			this.Name = name;
			this.Email = email;
			this.Generated = generated;
		}

		public override string ToString()
		{
			return string.Format("{0} <{1}>", Name, Email);
		}
	}
}

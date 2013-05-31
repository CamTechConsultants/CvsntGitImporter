/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Maps CVS users to proper names and e-mail addresses.
	/// </summary>
	/// <remarks>The file is a tab separated file with three columns: CVS user name, real name and e-mail address</remarks>
	class UserMap
	{
		private readonly Dictionary<string, User> m_map = new Dictionary<string, User>();
		private readonly string m_defaultDomain;

		public UserMap(string defaultDomain)
		{
			m_defaultDomain = defaultDomain;
		}

		public User GetUser(string cvsName)
		{
			User result;
			if (m_map.TryGetValue(cvsName, out result))
				return result;

			result = CreateDefaultUser(cvsName);
			m_map[cvsName] = result;
			return result;
		}

		/// <summary>
		/// Add a single entry.
		/// </summary>
		public void AddEntry(string cvsName, User user)
		{
			m_map[cvsName] = user;
		}

		/// <summary>
		/// Parse a user file.
		/// </summary>
		/// <exception cref="IOException">if an error occurs reading the file</exception>
		public void ParseUserFile(string filename)
		{
			try
			{
				using (var reader = new StreamReader(filename, Encoding.UTF8))
				{
					ParseUserFile(reader, filename);
				}
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new IOException(uae.Message, uae);
			}
			catch (System.Security.SecurityException se)
			{
				throw new IOException(se.Message, se);
			}
		}

		/// <summary>
		/// Parse a user file.
		/// </summary>
		/// <exception cref="IOException">if an error occurs reading the file</exception>
		/// <remarks>This overload exists primarily for the user by testcode, where we're reading a test file
		/// from resources.</remarks>
		public void ParseUserFile(TextReader reader, string filename)
		{
			try
			{
				int lineNumber = 0;
				string line;

				while ((line = reader.ReadLine()) != null)
				{
					lineNumber++;
					line = line.Trim();
					if (line.Length == 0)
						continue;

					var parts = line.Split('\t');
					if (parts.Length != 3)
						throw new IOException(String.Format("{0}({1}): Invalid format in user file", filename, lineNumber));

					var cvsName = parts[0].Trim();
					if (m_map.ContainsKey(cvsName))
						throw new IOException(String.Format("{0}({1}): User {2} appears twice", filename, lineNumber, cvsName));

					var user = new User(parts[1].Trim(), parts[2].Trim());
					m_map[cvsName] = user;
				}
			}
			catch (UnauthorizedAccessException uae)
			{
				throw new IOException(uae.Message, uae);
			}
			catch (System.Security.SecurityException se)
			{
				throw new IOException(se.Message, se);
			}
		}


		private User CreateDefaultUser(string cvsName)
		{
			return new User(cvsName, String.Format("{0}@{1}", cvsName.Replace(' ', '_'), m_defaultDomain), generated: true);
		}
	}
}

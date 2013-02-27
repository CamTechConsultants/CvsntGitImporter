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
	/// Represents a set of changes to files committed in one go.
	/// </summary>
	class Commit : IEnumerable<FileRevision>
	{
		private readonly List<FileRevision> m_files = new List<FileRevision>();
		private DateTime? m_time;
		private string m_message;
		private string m_branch;
		private List<string> m_errors;

		public readonly string CommitId;

		public DateTime Time
		{
			get
			{
				if (m_time.HasValue)
					return m_time.Value;

				var time = m_files.Select(c => c.Time).Min();
				m_time = time;
				return time;
			}
		}

		public string Message
		{
			get
			{
				if (m_message == null)
					m_message = String.Join(Environment.NewLine + Environment.NewLine, m_files.Select(c => c.Message).Distinct());
				return m_message;
			}
		}

		public string Branch
		{
			get
			{
				if (m_branch == null)
					m_branch = m_files.First().Branch;

				return m_branch;
			}
		}

		public IEnumerable<string> Errors
		{
			get { return m_errors ?? Enumerable.Empty<string>(); }
		}

		public Commit(string commitId)
		{
			CommitId = commitId;
		}

		public void Add(FileRevision commit)
		{
			m_time = null;
			m_message = null;
			m_files.Add(commit);
		}

		public bool Verify()
		{
			m_errors = null;

			var authors = m_files.Select(c => c.Author).Distinct();
			if (authors.Count() > 1)
				AddError("Multiple authors found: {0}", String.Join(", ", authors));

			var times = m_files.Select(c => c.Time).Distinct();
			if (times.Max() - times.Min() >= TimeSpan.FromMinutes(1))
				AddError("Times vary too much: {0}", String.Join(", ", times));

			var branches = m_files.Select(c => c.Branch).Distinct();
			if (branches.Count() > 1)
				AddError("Multiple branches found: {0}", String.Join(", ", branches));

			return !Errors.Any();
		}

		public override string ToString()
		{
			return String.Join(", ", m_files);
		}

		public IEnumerator<FileRevision> GetEnumerator()
		{
			return m_files.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_files.GetEnumerator();
		}

		private void AddError(string format, params object[] args)
		{
			var msg = String.Format(format, args);

			if (m_errors == null)
				m_errors = new List<string>() { msg };
			else
				m_errors.Add(msg);
		}
	}
}
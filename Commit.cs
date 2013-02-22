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
		private readonly List<FileRevision> m_commits = new List<FileRevision>();
		private DateTime? m_time;
		private string m_message;
		private List<string> m_errors;

		public readonly string CommitId;

		public DateTime Time
		{
			get
			{
				if (m_time.HasValue)
					return m_time.Value;

				var time = m_commits.Select(c => c.Time).Min();
				m_time = time;
				return time;
			}
		}

		public string Message
		{
			get
			{
				if (m_message == null)
					m_message = String.Join(Environment.NewLine + Environment.NewLine, m_commits.Select(c => c.Message).Distinct());
				return m_message;
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
			m_commits.Add(commit);
		}

		public bool Verify()
		{
			m_errors = null;

			var authors = m_commits.Select(c => c.Author).Distinct();
			if (authors.Count() > 1)
				AddError("Multiple authors found: {0}", String.Join(", ", authors));

			var times = m_commits.Select(c => c.Time).Distinct();
			if (times.Max() - times.Min() >= TimeSpan.FromMinutes(1))
				AddError("Times vary too much: {0}", String.Join(", ", times));

			return !Errors.Any();
		}

		public IEnumerator<FileRevision> GetEnumerator()
		{
			return m_commits.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return m_commits.GetEnumerator();
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
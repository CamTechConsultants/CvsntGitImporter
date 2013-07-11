/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace CTC.CvsntGitImporter
{
	class ImportProgress
	{
		private const int WindowSize = 100;
		private readonly int m_totalCount;
		private readonly LinkedList<TimeSpan> m_windowTimes = new LinkedList<TimeSpan>();

		private int m_lastEtaLength = 0;

		public ImportProgress(int totalCount)
		{
			m_totalCount = totalCount;
			m_windowTimes.AddFirst(TimeSpan.Zero);
		}

		public void Update(TimeSpan elapsed, int count)
		{
			m_windowTimes.AddLast(elapsed);
			if (m_windowTimes.Count > WindowSize)
				m_windowTimes.RemoveFirst();

			var progress = new StringBuilder();
			progress.AppendFormat("({0}%", count * 100 / m_totalCount);

			if (count < m_totalCount)
			{
				var remaining = CalculateRemaining(count);
				progress.AppendFormat(", {0} remaining", remaining.ToFriendlyDisplay(1));
			}

			progress.Append(")");

			var etaLength = progress.Length;
			if (etaLength < m_lastEtaLength)
				progress.Append(new String(' ', m_lastEtaLength - etaLength));
			m_lastEtaLength = etaLength;

			Console.Out.Write("\rProcessed {0} of {1} commits {2}", count, m_totalCount, progress);
		}

		private TimeSpan CalculateRemaining(int count)
		{
			int windowSize = m_windowTimes.Count;

			double msTaken = m_windowTimes.Last.Value.TotalMilliseconds - m_windowTimes.First.Value.TotalMilliseconds;
			double msRemaining = (msTaken / windowSize) * (m_totalCount - count);

			return TimeSpan.FromMilliseconds(msRemaining);
		}
	}
}
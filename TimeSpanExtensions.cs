/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Extension methods on TimeSpan.
	/// </summary>
	public static class TimeSpanExtensions
	{
		/// <summary>
		/// Display a TimeSpan in friendly manner. Adapted from http://stackoverflow.com/a/7204071/214776.
		/// </summary>
		public static string ToFriendlyDisplay(this TimeSpan timeSpan, int maxNrOfElements = 2)
		{
			maxNrOfElements = Math.Max(Math.Min(maxNrOfElements, 5), 1);

			var parts = new[]
						{
							Tuple.Create("day", timeSpan.Days),
							Tuple.Create("hour", timeSpan.Hours),
							Tuple.Create("min", timeSpan.Minutes),
							Tuple.Create("s", timeSpan.Seconds),
							Tuple.Create("ms", timeSpan.Milliseconds)
						}
						.SkipWhile(i => i.Item2 <= 0)
						.Take(maxNrOfElements);

			return String.Join(", ", parts.Select(p => String.Format("{0} {1}", p.Item2, Pluralise(p.Item1, p.Item2))));
		}

		private static string Pluralise(string unit, int value)
		{
			if (value != 1 && !unit.EndsWith("s"))
				return unit + "s";
			else
				return unit;
		}
	}
}
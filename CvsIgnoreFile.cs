﻿/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// Class to handle .cvsignore files.
	/// </summary>
	static class CvsIgnoreFile
	{
		public static bool IsIgnoreFile(FileContent file)
		{
			return String.Equals(Path.GetFileName(file.Name), ".cvsignore", StringComparison.OrdinalIgnoreCase);
		}

		public static FileContent Rewrite(FileContent file)
		{
			string newFilename;
			var lastSlash = file.Name.LastIndexOf('/');
			if (lastSlash >= 0)
				newFilename = file.Name.Remove(lastSlash + 1) + ".gitignore";
			else
				newFilename = ".gitignore";

			if (file.IsDead)
			{
				return new FileContent(newFilename, FileContentData.Empty);
			}
			else
			{
				var lines = from i in ReadCvsIgnoreFile(file)
							where i.Length > 0
							select TranslateEntry(i);

				var newContent = lines.Aggregate(
						new StringBuilder(),
						(buf, line) => buf.AppendFormat("{0}{1}", line, Environment.NewLine),
						buf => buf.ToString());

				var bytes = Encoding.UTF8.GetBytes(newContent);
				return new FileContent(newFilename, new FileContentData(bytes));
			}
		}

		private static string TranslateEntry(string entry)
		{
			if (entry.StartsWith("!"))
				return "!/" + entry.Substring(1);
			else
				return "/" + entry;
		}

		private static IEnumerable<string> ReadCvsIgnoreFile(FileContent file)
		{
			var content = file.Data.ToString();
			return Regex.Split(content, @"\s+", RegexOptions.Singleline);
		}
	}
}

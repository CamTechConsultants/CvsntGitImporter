/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Abstract class that command line switch definitions must derive from. Such a derived class would
	/// typically define all the possible switches available as properties decorated with the
	/// <seealso cref="SwitchDefAttribute"/> attribute.
	/// </summary>
	abstract class SwitchesDefBase
	{
		private string[] m_extra = new string[0];

		/// <summary>
		/// Any extra arguments found following the switches.
		/// </summary>
		public IList<string> ExtraArguments
		{
			get { return Array.AsReadOnly<string>(m_extra); }
		}

		/// <summary>
		/// Collection of arguments defined in this class.
		/// </summary>
		internal SwitchCollection Args
		{
			get;
			private set;
		}

		protected SwitchesDefBase()
		{
			this.Args = new SwitchCollection(this);

			foreach (PropertyInfo prop in this.GetType().GetProperties())
			{
				// ignore ExtraArguments and properties without setters
				if (prop.Name == "ExtraArguments" || prop.GetSetMethod() == null)
					continue;

				// check for SwitchDef attribute
				var attr = prop.GetAttribute<SwitchDefAttribute>();
				if (attr != null)
				{
					if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(string) ||
							prop.PropertyType == typeof(uint?) || prop.PropertyType.Implements<IList<string>>())
					{
						var arg = new SwitchInfo(prop);
						if (attr.ShortSwitch != null)
							arg.ShortSwitch = attr.ShortSwitch;
						if (attr.LongSwitch != null)
							arg.LongSwitch = attr.LongSwitch;
						arg.Description = attr.Description;
						arg.ValueDescription = attr.ValueDescription;

						if (prop.HasAttribute<SwitchHiddenAttribute>())
							arg.Hidden = true;

						Args.AddSwitch(arg);
					}
					else
					{
						throw new ArgumentException(String.Format("Argument {0} is marked as a commandline arg, but is not a supported type", prop.Name));
					}
				}
			}
		}

		/// <summary>
		/// Parse command line arguments.
		/// </summary>
		/// <param name="args">The arguments passed to the application</param>
		/// <exception cref="CommandLineArgsException">Thrown if there is an error in the arguments passed into the application</exception>
		/// <exception cref="ArgumentException">Thrown if there is an error in the <see cref="SwitchesDefBase"/> instance</exception>
		public virtual void Parse(params string[] args)
		{
			SwitchesParser.Parse(this, args);
			Verify();
		}

		/// <summary>
		/// Called once all argument parsing is done - override if there is any verification of the
		/// arguments required.
		/// </summary>
		/// <exception cref="CommandLineArgsException">thrown if any of the switches are invalid</exception>
		public virtual void Verify()
		{
		}

		/// <summary>
		/// Get a string that displays help for all the options.
		/// </summary>
		/// <returns>a string of text that documents the switches sized to match the current console width</returns>
		public string GetHelpText()
		{
			return GetHelpText(GetConsoleWidth() - 1);
		}

		/// <summary>
		/// Get a string that displays help for all the options.
		/// </summary>
		/// <param name="maxWidth">The maximum width of text to display; set to 0 for unlimited width</param>
		/// <returns>a string of text that documents the switches</returns>
		public virtual string GetHelpText(int maxWidth)
		{
			int maxSwitchWidth = 0;

			// first determine the max width needed to display the switches and build a list
			var displaySwitches = new List<SwitchInfo>();
			foreach (SwitchInfo sw in Args.Items)
			{
				if (sw.Hidden)
					continue;

				int w = FormatSwitchForHelp(sw).Length;
				if (w > maxSwitchWidth)
					maxSwitchWidth = w;

				displaySwitches.Add(sw);
			}

			// sort (order by short switch if present, then by long switch)
			displaySwitches.Sort((SwitchInfo a, SwitchInfo b) =>
			{
				int result;
				if (String.IsNullOrEmpty(a.ShortSwitch) && String.IsNullOrEmpty(b.ShortSwitch))
				{
					result = String.Compare(a.LongSwitch, b.LongSwitch, StringComparison.OrdinalIgnoreCase);
				}
				else if (String.IsNullOrEmpty(a.ShortSwitch))
				{
					result = 1;
				}
				else if (String.IsNullOrEmpty(b.ShortSwitch))
				{
					result = -1;
				}
				else
				{
					result = String.Compare(a.ShortSwitch, b.ShortSwitch, StringComparison.OrdinalIgnoreCase);
					// if the short switches are the same, assume they differ in case and so try again, making lower
					// case come before upper case
					if (result == 0)
						result = -String.Compare(a.ShortSwitch, b.ShortSwitch, StringComparison.Ordinal);
				}

				return result;
			});

			// now lay it all out
			var buf = new StringBuilder();

			foreach (SwitchInfo sw in displaySwitches)
			{
				if (sw.Hidden)
					continue;

				string switches = FormatSwitchForHelp(sw);
				buf.AppendFormat("  {0}{1} - ", switches, new String(' ', maxSwitchWidth - switches.Length));

				string description = sw.Description ?? "";
				if (sw.Type == typeof(List<string>))
					description += " (may be specified more than once)";

				int left = maxWidth - maxSwitchWidth - 6;
				if (maxWidth == 0 || left < 2)
				{
					buf.AppendFormat("{0}\n", description);
				}
				else
				{
					List<string> lines = Wrap(description, left);
					bool first = true;
					foreach (string s in lines)
					{
						if (first)
							first = false;
						else
							buf.Append(new String(' ', maxWidth - left - 1));
						buf.AppendFormat("{0}\n", s);
					}
				}
			}

			return buf.ToString();
		}

		/// <summary>
		/// Get version information for the current application.
		/// </summary>
		public string GetAppVersion()
		{
			var a = Assembly.GetEntryAssembly();
			var attrVersion = a.GetAttribute<AssemblyFileVersionAttribute>();
			return (attrVersion == null) ? String.Empty : attrVersion.Version;
		}

		/// <summary>
		/// Get the name of the current application.
		/// </summary>
		public string GetAppName()
		{
			var a = Assembly.GetEntryAssembly();
			var attrDesc = a.GetAttribute<AssemblyTitleAttribute>();
			return (attrDesc == null) ? String.Empty : attrDesc.Title;
		}

		/// <summary>
		/// Get the description for the current application.
		/// </summary>
		public string GetAppDescription()
		{
			var a = Assembly.GetEntryAssembly();
			var attrDesc = a.GetAttribute<AssemblyDescriptionAttribute>();
			return (attrDesc == null) ? String.Empty : attrDesc.Description;
		}


		/// <summary>
		/// Get the width of the console.
		/// </summary>
		/// <returns></returns>
		protected int GetConsoleWidth()
		{
			int width = 80;

			try
			{
				width = Console.WindowWidth;
			}
			catch (Exception)
			{
			}

			return width;
		}


		#region Internal methods

		/// <summary>
		/// Set the array of extra arguments.
		/// </summary>
		internal void SetExtraArguments(string[] args)
		{
			m_extra = args;
		}

		#endregion


		private static string FormatSwitchForHelp(SwitchInfo arg)
		{
			StringBuilder buf = new StringBuilder();

			// work out the value placeholder
			string valueDesc = null;
			if (arg.Type == typeof(uint?))
			{
				if (arg.ValueDescription == null)
					valueDesc = "int";
				else
					valueDesc = arg.ValueDescription;
			}
			else if (arg.Type != typeof(bool))
			{
				if (arg.ValueDescription == null)
					valueDesc = "string";
				else
					valueDesc = arg.ValueDescription;
			}

			if (!String.IsNullOrEmpty(arg.ShortSwitch))
			{
				buf.Append(arg.ShortSwitch);
				if (valueDesc != null)
					buf.AppendFormat(" <{0}>", valueDesc);
			}

			if (!String.IsNullOrEmpty(arg.LongSwitch))
			{
				if (buf.Length > 0)
					buf.Append(',');
				buf.Append(arg.LongSwitch);
				if (valueDesc != null)
					buf.AppendFormat(" <{0}>", valueDesc);
			}

			return buf.ToString();
		}

		/// <summary>
		/// Returns a list of strings no larger than the max length sent in.
		/// </summary>
		/// <param name="text">Text to be wrapped into of List of Strings</param>
		/// <param name="maxLength">Max length you want each line to be.</param>
		/// <returns>List of Strings</returns>
		protected static List<String> Wrap(string text, int maxLength)
		{
			// Return empty list of strings if the text was empty
			if (text.Length == 0)
				return new List<string>() { "" };

			var words = text.Split(' ');
			var lines = new List<string>();
			var currentLine = "";

			foreach (var currentWord in words)
			{
				if ((currentLine.Length >= maxLength) ||
					((currentLine.Length + currentWord.Length) >= maxLength))
				{
					lines.Add(currentLine);
					currentLine = "";
				}

				if (currentLine.Length > 0)
					currentLine += " " + currentWord;
				else
					currentLine += currentWord;
			}

			if (currentLine.Length > 0)
				lines.Add(currentLine);

			return lines;
		}
	}
}
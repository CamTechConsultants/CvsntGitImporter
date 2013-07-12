/*
 * John.Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Class for parsing command line arguments.
	/// A client simply creates a class deriving from <see cref="SwitchesDefBase"/> with properties
	/// representing the available switches. Each property should be decorated with
	/// <see cref="SwitchDefAttribute"/>.
	/// </summary>
	class SwitchesParser
	{
		private SwitchesDefBase m_def;

		/// <summary>
		/// Ctor is private because users should use the static Parse method.
		/// </summary>
		/// <param name="def"></param>
		private SwitchesParser(SwitchesDefBase def)
		{
			m_def = def;
		}


		#region Public methods

		/// <summary>
		/// Parse command line arguments.
		/// </summary>
		/// <param name="def">The command line definition</param>
		/// <param name="args">The arguments passed to the application</param>
		/// <exception cref="CommandLineArgsException">Thrown if there is an error in the arguments passed into the application</exception>
		/// <exception cref="ArgumentException">Thrown if there is an error in the <see cref="SwitchesDefBase"/> instance</exception>
		public static void Parse(SwitchesDefBase def, params string[] args)
		{
			SwitchesParser parser = new SwitchesParser(def);
			int nextArg = parser.Parse(args);
		
			// copy any extra arguments
			if (nextArg < args.Length)
			{
				string[] extraArgs = new string[args.Length - nextArg];
				Array.Copy(args, nextArg, extraArgs, 0, args.Length - nextArg);
				def.SetExtraArguments(extraArgs);
			}
		}

		#endregion


		#region Private methods

		private int Parse(string[] args)
		{
			int i = 0;
			while (i < args.Length)
			{
				string arg = args[i];
				if (!arg.StartsWith("-"))
					break;

				i++;

				if (m_def.Args.GetSwitchType(arg) == typeof(bool))
				{
					m_def.Args.Set(arg, true);
				}
				else
				{
					if (i >= args.Length)
						throw new CommandLineArgsException("Missing argument to the {0} switch", arg);

					if (m_def.Args.GetSwitchType(arg) == typeof(string))
						m_def.Args.Set(arg, args[i++]);
					else if (m_def.Args.GetSwitchType(arg) == typeof(uint?))
						m_def.Args.Set(arg, ParseIntValue(args[i++]));
				}
			}

			return i;
		}

		private static uint ParseIntValue(string value)
		{
			uint result;
			if (!TryParsePossibleHex(value, out result))
				throw new CommandLineArgsException("Invalid numeric argument: {0}", value);
			return result;
		}

		/// <summary>
		/// Converts the string representation of a number to its 32-bit unsigned integer equivalent.
		/// The number is assumed to be in hexadecimal if it has a 0x prefix or in decimal otherwise.
		/// </summary>
		/// <param name="value">a string containing the number to convert</param>
		/// <param name="result">the parsed number if succeded, otherwise 0 if the conversion failed</param>
		/// <returns>true if the conversion succeeded, otherwise false</returns>
		private static bool TryParsePossibleHex(string value, out uint result)
		{
			NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
			string parseValue = value;

			Match m = Regex.Match(parseValue, @"^\s*0x\s*(.*)", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				parseValue = m.Groups[1].Value;
				styles |= NumberStyles.AllowHexSpecifier;
			}

			return UInt32.TryParse(parseValue, styles, CultureInfo.CurrentCulture, out result);
		}

		#endregion
	}
}

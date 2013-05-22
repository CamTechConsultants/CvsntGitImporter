/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Collection of switch properties.
	/// </summary>
	class SwitchCollection
	{
		#region Private data
		private SwitchesDefBase m_def;
		private Dictionary<string, SwitchInfo> m_dict = new Dictionary<string, SwitchInfo>();
		private List<SwitchInfo> m_args = new List<SwitchInfo>();
		#endregion


		/// <summary>
		/// Get a collection of all the switches.
		/// </summary>
		public ReadOnlyCollection<SwitchInfo> Items
		{
			get { return m_args.AsReadOnly(); }
		}


		#region Ctor

		public SwitchCollection(SwitchesDefBase def)
		{
			m_def = def;
		}

		#endregion


		#region Public methods

		public void AddSwitch(SwitchInfo arg)
		{
			bool valid = false;

			// short switch
			if (!String.IsNullOrEmpty(arg.ShortSwitch))
			{
				string s = arg.ShortSwitch;
				if (!s.StartsWith("-"))
					s = "-" + s;
				AddSwitch(s, arg);
				valid = true;
			}

			// long switch
			if (!String.IsNullOrEmpty(arg.LongSwitch))
			{
				string s = arg.LongSwitch;
				if (!s.StartsWith("--"))
					s = "--" + s;
				AddSwitch(s, arg);
				valid = true;
			}

			if (valid)
				m_args.Add(arg);
		}

		public bool Contains(string s)
		{
			return m_dict.ContainsKey(s);
		}

		public Type GetSwitchType(string s)
		{
			if (!m_dict.ContainsKey(s))
				throw new CommandLineArgsException("Unrecognised switch: " + s);

			Type propType = m_dict[s].Type;
			if (propType == typeof(string) || propType.Implements<IList<string>>())
				return typeof(string);
			else if (propType == typeof(uint?))
				return typeof(uint?);
			else
				return typeof(bool);
		}

		public void Set(string s, object value)
		{
			SwitchInfo arg = null;
			if (!m_dict.TryGetValue(s, out arg))
				throw new CommandLineArgsException("Unrecognised switch: " + s);

			try
			{
				if (arg.Property.PropertyType.Implements<IList<string>>())
				{
					IList<string> list = (IList<string>)arg.Property.GetValue(m_def, null);
					if (list == null)
					{
						list = (IList<string>)Activator.CreateInstance(arg.Property.PropertyType);
						arg.Property.SetValue(m_def, list, null);
					}
					list.Add((string)value);
				}
				else
				{
					arg.Property.SetValue(m_def, value, null);
				}
			}
			catch (TargetInvocationException tie)
			{
				if (tie.InnerException == null)
					throw;
				else
					throw tie.InnerException;
			}
		}

		#endregion


		private void AddSwitch(string s, SwitchInfo arg)
		{
			if (m_dict.ContainsKey(s))
				throw new ArgumentException("Duplicate switch: " + s);
			m_dict.Add(s, arg);
		}
	}
}

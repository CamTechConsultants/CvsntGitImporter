/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Description of a command line switch.
	/// </summary>
	class SwitchInfo
	{
		#region Private fields
		#endregion


		#region Public properties

		/// <summary>
		/// The property that represents this switch.
		/// </summary>
		public PropertyInfo Property { get; private set; }

		/// <summary>
		/// The long form of the switch.
		/// </summary>
		public string LongSwitch { get; set; }

		/// <summary>
		/// The short form of the switch.
		/// </summary>
		public string ShortSwitch { get; set; }

		/// <summary>
		/// The type of the argument.
		/// </summary>
		public Type Type
		{
			get { return Property.PropertyType; }
		}

		/// <summary>
		/// Help description.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Description for the value placeholder in help.
		/// </summary>
		public string ValueDescription { get; set; }

		/// <summary>
		/// Is this switch hidden?
		/// </summary>
		public bool Hidden { get; set; }

		#endregion


		#region Constructors

		public SwitchInfo(PropertyInfo prop)
		{
			this.Property = prop;
		}

		#endregion


		#region Public methods
		#endregion
	}
}

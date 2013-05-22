/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Extension methods to query attributes.
	/// </summary>
	static class AttributeExtensions
	{
		/// <summary>
		/// Get the first custom attribute of a specified type attached to a member. The member's inheritance chain is searched.
		/// </summary>
		/// <typeparam name="T">the type of the attribute to get</typeparam>
		/// <param name="info">the member to examine</param>
		/// <returns>the attribute instance or null if it was not found</returns>
		/// <exception cref="ArgumentNullException">info is null</exception>
		public static T GetAttribute<T>(this MemberInfo info) where T: Attribute
		{
			return GetAttribute<T>(info, true);
		}

		/// <summary>
		/// Get the first custom attribute of a specified type attached to a member.
		/// </summary>
		/// <typeparam name="T">the type of the attribute to get</typeparam>
		/// <param name="info">the member to examine</param>
		/// <param name="inherit">specifies whether to search this member's inheritance chain to find the attribute</param>
		/// <returns>the attribute instance or null if it was not found</returns>
		/// <exception cref="ArgumentNullException">info is null</exception>
		public static T GetAttribute<T>(this MemberInfo info, bool inherit) where T: Attribute
		{
			return GetAttributes<T>(info, inherit).FirstOrDefault();
		}

		/// <summary>
		/// Get all the custom attributes of a specified type attached to a member.
		/// </summary>
		/// <typeparam name="T">the type of the attribute to get</typeparam>
		/// <param name="info">the member to examine</param>
		/// <param name="inherit">specifies whether to search this member's inheritance chain to find the attribute</param>
		/// <returns>a list of all the attribute instances</returns>
		/// <exception cref="ArgumentNullException">info is null</exception>
		public static IEnumerable<T> GetAttributes<T>(this MemberInfo info, bool inherit) where T: Attribute
		{
			if (info == null)
				throw new ArgumentNullException("info");
			return info.GetCustomAttributes(typeof(T), inherit).OfType<T>();
		}

		/// <summary>
		/// Does a member have a specific attribute applied to it? The member's inheritance chain is searched.
		/// </summary>
		/// <typeparam name="T">the type of the attribute</typeparam>
		/// <param name="info">the member to examine</param>
		/// <returns>true if the attribute is present, otherwise false</returns>
		/// <exception cref="ArgumentNullException">info is null</exception>
		public static bool HasAttribute<T>(this MemberInfo info) where T: Attribute
		{
			return GetAttribute<T>(info, true) != null;
		}

		/// <summary>
		/// Does a member have a specific attribute applied to it?
		/// </summary>
		/// <typeparam name="T">the type of the attribute</typeparam>
		/// <param name="info">the member to examine</param>
		/// <param name="inherit">specifies whether to search this member's inheritance chain to find the attribute</param>
		/// <returns>true if the attribute is present, otherwise false</returns>
		/// <exception cref="ArgumentNullException">info is null</exception>
		public static bool HasAttribute<T>(this MemberInfo info, bool inherit) where T: Attribute
		{
			return GetAttribute<T>(info, inherit) != null;
		}

		/// <summary>
		/// Get the first custom attribute of a specified type attached to an assembly.
		/// </summary>
		/// <typeparam name="T">the type of the attribute to get</typeparam>
		/// <param name="assembly">the assembly to examine</param>
		/// <returns>the attribute instance or null if it was not found</returns>
		/// <exception cref="ArgumentNullException">assembly is null</exception>
		public static T GetAttribute<T>(this Assembly assembly) where T: Attribute
		{
			if (assembly == null)
				throw new ArgumentNullException("info");
			return assembly.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
		}

		/// <summary>
		/// Does an assembly have a specific attribute applied to it?
		/// </summary>
		/// <typeparam name="T">the type of the attribute</typeparam>
		/// <param name="assembly">the assembly to examine</param>
		/// <returns>true if the attribute is present, otherwise false</returns>
		/// <exception cref="ArgumentNullException">assembly is null</exception>
		public static bool HasAttribute<T>(this Assembly assembly) where T: Attribute
		{
			return assembly.GetAttribute<T>() != null;
		}
	}
}

/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System;
using System.Linq;

namespace CTC.CvsntGitImporter.Utils
{
	/// <summary>
	/// Extension methods for Type.
	/// </summary>
	static class TypeExtensions
	{
		/// <summary>
		/// Is a given type derived from a specific open generic type?
		/// </summary>
		/// <param name="targetType">the type to check</param>
		/// <param name="baseType">the open generic type to determine whether <paramref name="targetType">targetType</paramref>
		/// is derived from</param>
		/// <exception cref="ArgumentException">generic is not a generic type definition</exception>
		public static bool IsSubclassOfOpenGeneric(this Type targetType, Type baseType)
		{
			var result = GetClosedGenericBaseClass(targetType, baseType);
			return result != null;
		}

		/// <summary>
		/// Gets the concrete type of an open generic type that a type inherits from.
		/// </summary>
		/// <param name="targetType">the type whose closed generic base type is to be found</param>
		/// <param name="baseType">the open generic base type to find</param>
		/// <returns>a closed generic type if found, or null if none was found</returns>
		public static Type GetClosedGenericBaseClass(this Type targetType, Type baseType)
		{
			if (!baseType.IsGenericTypeDefinition)
				throw new ArgumentException("baseType");

			var t = targetType;
			while (t != null && t != typeof(object))
			{
				var cur = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
				if (baseType == cur)
					return t;

				t = t.BaseType;
			}

			return null;
		}

		/// <summary>
		/// Gets the default value for this type.
		/// </summary>
		public static object DefaultValue(this Type type)
		{
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		/// <summary>
		/// Does a type implement an interface?
		/// </summary>
		/// <typeparam name="T">the type of the interface</typeparam>
		/// <param name="type">the type to query</param>
		public static bool Implements<T>(this Type type)
		{
			var interfaceType = typeof(T);
			var x = type.GetInterfaces();
			return type.GetInterfaces().Any(i => i == interfaceType);
		}

		/// <summary>
		/// Is a type in a namespace?
		/// </summary>
		public static bool InNamespace(this Type type, string ns)
		{
			if (ns == null)
				ns = "";

			if (type.Namespace == null)
				return ns.Length == 0;
			else
				return type.Namespace == ns || type.Namespace.StartsWith(ns + ".", StringComparison.Ordinal);
		}
	}
}

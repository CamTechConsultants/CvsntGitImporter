/*
 * John Hall <john.hall@camtechconsultants.com>
 * Copyright (c) Cambridge Technology Consultants Ltd. All rights reserved.
 */

using System.Collections.Generic;
using System.Linq;

namespace CTC.CvsntGitImporter
{
	/// <summary>
	/// A dictionary that maps a key to a list of values.
	/// </summary>
	/// <remarks>This implementation differs from the standard dictionary in that it is much more forgiving of keys
	/// that do not exist. For example, the indexer returns an empty list if a value does not exist.</remarks>
	class OneToManyDictionary<TKey, TValue>
	{
		private readonly Dictionary<TKey, List<TValue>> m_dict;

		public OneToManyDictionary() : this(EqualityComparer<TKey>.Default)
		{
		}

		public OneToManyDictionary(IEqualityComparer<TKey> comparer)
		{
			m_dict = new Dictionary<TKey, List<TValue>>(comparer);
		}

		/// <summary>
		/// Gets or sets the list of items for a key. When setting, any existing values are
		/// replaced rather than appended to.
		/// </summary>
		public IEnumerable<TValue> this[TKey key]
		{
			get
			{
				List<TValue> list;
				if (m_dict.TryGetValue(key, out list))
					return list.AsReadOnly();
				else
					return Enumerable.Empty<TValue>();
			}
			set
			{
				m_dict[key] = new List<TValue>(value);
			}
		}

		/// <summary>
		/// Gets the list of keys in the dictionary.
		/// </summary>
		public IEnumerable<TKey> Keys
		{
			get { return m_dict.Keys; }
		}

		/// <summary>
		/// Add a value for a key.
		/// </summary>
		public void Add(TKey key, TValue value)
		{
			List<TValue> list;
			if (m_dict.TryGetValue(key, out list))
				list.Add(value);
			else
				m_dict[key] = new List<TValue>(1) { value };
		}

		/// <summary>
		/// Does the collection contain a key?
		/// </summary>
		public bool ContainsKey(TKey key)
		{
			return m_dict.ContainsKey(key);
		}
	}
}
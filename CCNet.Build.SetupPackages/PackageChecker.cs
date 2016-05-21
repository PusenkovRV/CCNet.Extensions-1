﻿using System;
using System.Collections.Generic;
using System.Linq;
using CCNet.Build.Common;

namespace CCNet.Build.SetupPackages
{
	public class PackageChecker
	{
		private readonly NuGetDb m_db;

		private Dictionary<string, NuGetPackage> m_localPackages;
		private Dictionary<string, Version> m_customVersions;

		public PackageChecker(string nugetDbConnection, string customVersions)
		{
			m_db = new NuGetDb(nugetDbConnection);

			m_localPackages = new Dictionary<string, NuGetPackage>();
			m_customVersions = new Dictionary<string, Version>();

			if (!String.IsNullOrEmpty(customVersions))
			{
				ParseCustomVersions(customVersions);
			}
		}

		public int PackageCount
		{
			get { return m_localPackages.Count; }
		}

		private void ParseCustomVersions(string customVersions)
		{
			m_customVersions = new Dictionary<string, Version>();

			foreach (var item in customVersions.Split('|'))
			{
				if (!item.Contains('+'))
				{
					// empty version behavior depends on the package type
					// for local packages, it means they should be pinned to its current version
					// for remote packages, it means they should be updated to their latest version
					m_customVersions.Add(item, null);
					continue;
				}

				var parts = item.Split('+');
				var name = parts[0];
				var version = parts[1];
				m_customVersions.Add(name, new Version(version));
			}
		}

		public void Load()
		{
			m_localPackages = m_db.GetLatestVersions().ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
		}

		public bool IsLocal(string id)
		{
			return m_localPackages.ContainsKey(id);
		}

		public bool IsStatic(string id)
		{
			if (!IsLocal(id))
				return false;

			var tags = m_localPackages[id].Tags;
			if (String.IsNullOrEmpty(tags))
				return false;

			if (tags.Contains("static"))
				return true;

			return false;
		}

		public TargetFramework TargetFramework(string id)
		{
			if (!IsLocal(id))
				throw new InvalidOperationException("Target framework versions are available for local packages only.");

			return m_localPackages[id].Framework;
		}

		public string ProjectName(string id)
		{
			if (!IsLocal(id))
				return id;

			var title = m_localPackages[id].Title;
			if (String.IsNullOrEmpty(title))
				return id;

			return title;
		}

		public bool IsPinnedToCurrentVersion(string id)
		{
			bool custom = m_customVersions.ContainsKey(id);

			if (IsLocal(id))
			{
				if (!custom)
					return false;

				var version = m_customVersions[id];
				if (version != null)
					return false;

				return true;
			}
			else
			{
				if (!custom)
					return true;

				var version = m_customVersions[id];
				if (version != null)
					throw new InvalidOperationException("Only local packages can be pinned to specific version.");

				return false;
			}
		}

		public Version IsPinnedToSpecificVersion(string id)
		{
			if (!m_customVersions.ContainsKey(id))
				return null;

			var version = m_customVersions[id];
			if (version == null)
				return null;

			if (!IsLocal(id))
				throw new InvalidOperationException("Only local packages can be pinned to specific version.");

			return version;
		}

		public Version VersionToUse(string id)
		{
			if (!IsLocal(id))
				throw new InvalidOperationException("Only local packages can be updated to a specifc version.");

			if (IsPinnedToCurrentVersion(id))
				throw new InvalidOperationException("This package is pinned to its current version.");

			var custom = IsPinnedToSpecificVersion(id);
			if (custom != null)
				return custom;

			return m_localPackages[id].Version;
		}
	}
}

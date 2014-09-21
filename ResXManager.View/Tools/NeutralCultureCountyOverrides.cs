﻿namespace tomenglertde.ResXManager.View.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Linq;
    using tomenglertde.ResXManager.View.Properties;

    public class NeutralCultureCountyOverrides
    {
        private static readonly CultureInfo[] AllSpecificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
        private readonly Dictionary<CultureInfo, CultureInfo> _overrides;

        public static readonly NeutralCultureCountyOverrides Default = new NeutralCultureCountyOverrides();

        private NeutralCultureCountyOverrides()
        {
            _overrides = new Dictionary<CultureInfo, CultureInfo>(ReadSettings().ToDictionary(item => item.Key, item => item.Value));
        }

        public event EventHandler<CultureOverrideEventArgs> OverrideChanged;

        public CultureInfo this[CultureInfo neutralCulture]
        {
            get
            {
                Contract.Requires(neutralCulture != null);

                CultureInfo specificCulture;

                if (!_overrides.TryGetValue(neutralCulture, out specificCulture))
                {
                    specificCulture = GetDefaultSpecificCulture(neutralCulture);
                }

                return specificCulture;
            }
            set
            {
                Contract.Requires(neutralCulture != null);
                Contract.Requires(value != null);

                if (value.Equals(GetDefaultSpecificCulture(neutralCulture)))
                {
                    _overrides.Remove(neutralCulture);
                }
                else
                {
                    _overrides[neutralCulture] = value;
                }

                OnOverrideChanged(new CultureOverrideEventArgs(neutralCulture, value));
                WriteSettings();
            }
        }

        private void OnOverrideChanged(CultureOverrideEventArgs e)
        {
            var handler = OverrideChanged;
            if (handler != null)
                handler(this, e);
        }

        private static CultureInfo GetDefaultSpecificCulture(CultureInfo neutralCulture)
        {
            var cultureName = neutralCulture.Name;
            var specificCultures = AllSpecificCultures.Where(c => (c != null) && ((c.Parent.Name == cultureName) || (c.Parent.IetfLanguageTag == cultureName))).ToArray();

            var preferredSpecificCultureName = cultureName + @"-" + cultureName.ToUpperInvariant();

            var specificCulture =
                // If a specific culture exists with "subtag == primary tag" (e.g. de-DE), use this
                specificCultures.FirstOrDefault(c => c.Name.Equals(preferredSpecificCultureName, StringComparison.OrdinalIgnoreCase))
                    // else it's more likely that the default one starts with the same letter as the neutral culture name (sv-SE, not sv-FI)
                ?? specificCultures.FirstOrDefault(c => c.Name.Split('-').Last().StartsWith(cultureName.Substring(0, 1), StringComparison.OrdinalIgnoreCase))
                    // If nothing else matches, use the first.
                ?? specificCultures.FirstOrDefault();
            return specificCulture;
        }

        private static IEnumerable<KeyValuePair<CultureInfo, CultureInfo>> ReadSettings()
        {
            var neutralCultureCountryOverrides = (Settings.Default.NeutralCultureCountyOverrides ?? string.Empty).Split(',');

            foreach (var item in neutralCultureCountryOverrides)
            {
                CultureInfo neutralCulture;
                CultureInfo specificCulture;

                try
                {
                    var parts = item.Split('=').Select(i => i.Trim()).ToArray();
                    if (parts.Length != 2)
                        continue;

                    neutralCulture = CultureInfo.GetCultureInfo(parts[0]);
                    specificCulture = CultureInfo.GetCultureInfo(parts[1]);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                yield return new KeyValuePair<CultureInfo, CultureInfo>(neutralCulture, specificCulture);
            }
        }

        private void WriteSettings()
        {
            var items = _overrides.Select(item => string.Join("=", item.Key, item.Value));
            var settings = string.Join(",", items);

            Settings.Default.NeutralCultureCountyOverrides = settings;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(AllSpecificCultures != null);
            Contract.Invariant(_overrides != null);
        }
    }
}
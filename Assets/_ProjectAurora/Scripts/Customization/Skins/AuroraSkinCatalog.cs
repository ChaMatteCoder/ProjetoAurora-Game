using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAurora.Customization.Skins
{
    [CreateAssetMenu(fileName = "AuroraSkinCatalog", menuName = "Projeto Aurora/Skins/Skin Catalog")]
    public sealed class AuroraSkinCatalog : ScriptableObject
    {
        [SerializeField] private List<AuroraSkinDefinition> skins = new List<AuroraSkinDefinition>();

        public IReadOnlyList<AuroraSkinDefinition> Skins => skins;
        public int Count => skins.Count;

        public AuroraSkinDefinition GetById(string skinId)
        {
            if (string.IsNullOrWhiteSpace(skinId))
            {
                return null;
            }

            for (int i = 0; i < skins.Count; i++)
            {
                AuroraSkinDefinition skin = skins[i];
                if (skin != null && string.Equals(skin.Id, skinId, StringComparison.Ordinal))
                {
                    return skin;
                }
            }

            return null;
        }

        public AuroraSkinDefinition GetDefaultSkin()
        {
            for (int i = 0; i < skins.Count; i++)
            {
                if (skins[i] != null && skins[i].IsDefaultSkin)
                {
                    return skins[i];
                }
            }

            AuroraSkinDefinition conventional = GetById("default");
            return conventional != null ? conventional : (skins.Count > 0 ? skins[0] : null);
        }

        public List<string> CollectValidationIssues()
        {
            var issues = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int defaultCount = 0;

            for (int i = 0; i < skins.Count; i++)
            {
                AuroraSkinDefinition skin = skins[i];
                if (skin == null)
                {
                    issues.Add("Entrada nula no indice " + i + ".");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(skin.Id))
                {
                    issues.Add("Skin sem ID no indice " + i + ".");
                }
                else if (!ids.Add(skin.Id))
                {
                    issues.Add("ID duplicado: " + skin.Id + ".");
                }

                if (skin.SplashArt == null)
                {
                    issues.Add(skin.Id + " sem Splash Art.");
                }

                if (skin.IsDefaultSkin)
                {
                    defaultCount++;
                }
            }

            if (defaultCount != 1)
            {
                issues.Add("O catalogo deve possuir exatamente uma skin default; atual=" + defaultCount + ".");
            }

            return issues;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(IEnumerable<AuroraSkinDefinition> orderedSkins)
        {
            skins.Clear();
            if (orderedSkins != null)
            {
                skins.AddRange(orderedSkins);
            }
        }
#endif
    }
}

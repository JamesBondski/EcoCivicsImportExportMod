﻿using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

namespace Eco.Mods.CivicsImpExp.Migrations._0973
{
    using Shared.Utils;

    public class LegalActionNamespace : ICivicsImpExpMigratorV1
    {
        public bool ShouldMigrate(JObject obj)
            => GetVanillaLegalActions(obj)
                .Any();

        private bool IsVanillaLegalAction(string typeName)
            => !string.IsNullOrEmpty(typeName) && ((typeName.StartsWith("Eco.Gameplay.Civics.") && typeName.EndsWith("_LegalAction")) || typeName == "Eco.Gameplay.Civics.SendNotice");

        private IEnumerable<JObject> GetVanillaLegalActions(JObject obj)
            => GetNestedObjects(obj).Where(x => IsVanillaLegalAction(x.Value<string>("type")));

        private IEnumerable<JObject> GetNestedObjects(JObject obj)
        {
            foreach (var pair in obj)
            {
                if (pair.Value is JObject innerObj)
                {
                    yield return innerObj;
                    foreach (var x in GetNestedObjects(innerObj)) { yield return x; }
                }
                else if (pair.Value is JArray innerArr)
                {
                    foreach (var element in innerArr)
                    {
                        if (element is JObject elementObj)
                        {
                            yield return elementObj;
                            foreach (var x in GetNestedObjects(elementObj)) { yield return x; }
                        }
                    }
                }
            }
        }

        public bool ApplyMigration(JObject obj, IList<string> outMigrationReport)
        {
            bool didMigrate = false;
            foreach (var legalActionObj in GetVanillaLegalActions(obj))
            {
                var oldType = legalActionObj.Value<string>("type");
                if (!oldType.StartsWith("Eco.Gameplay.Civics.")) { Logger.Debug($"Skipping (not a legal action)"); continue; }
                if (oldType.StartsWith("Eco.Gameplay.Civics.LegalActions.")) { Logger.Debug($"Skipping (already migrated)"); continue; }
                var fixedType = $"Eco.Gameplay.Civics.LegalActions.{oldType["Eco.Gameplay.Civics.".Length..]}";
                legalActionObj["type"] = fixedType;
                outMigrationReport.Add($"Changed '{oldType}' to '{fixedType}'");
                didMigrate = true;
            }
            return didMigrate;
        }
    }
}

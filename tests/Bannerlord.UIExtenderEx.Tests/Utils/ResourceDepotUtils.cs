﻿using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System.Linq;
using System.Reflection;

using TaleWorlds.Library;

namespace Bannerlord.UIExtenderEx.Tests.Utils
{
    public static class ResourceDepotUtils
    {
        private delegate ResourceDepot V1Delegate();
        private delegate ResourceDepot V2Delegate(string str);

        private static readonly V1Delegate? V1;
        private static readonly V2Delegate? V2;

        static ResourceDepotUtils()
        {
            foreach (var constructorInfo in AccessTools.GetDeclaredConstructors(typeof(ResourceDepot), false) ?? Enumerable.Empty<ConstructorInfo>())
            {
                var @params = constructorInfo.GetParameters();
                if (@params.Length == 0)
                    V1 = AccessTools2.GetDelegate<V1Delegate>(constructorInfo);
                if (@params.Length == 1 && @params[0].ParameterType == typeof(string))
                    V2 = AccessTools2.GetDelegate<V2Delegate>(constructorInfo);
            }
        }

        public static ResourceDepot? Create()
        {
            if (V1 is not null)
                return V1();
            if (V2 is not null)
                return V2(string.Empty);
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using Eshopworld.DevOps;

namespace EShopWorld.Tools.Common
{
    internal static class RegionHelper
    {
        internal static IList<DeploymentRegion> DeploymentRegionsToList(bool ciMode = false)
        {
            if (ciMode)
            {
                return new List<DeploymentRegion>(new[] {DeploymentRegion.WestEurope});
            }

            var list = new List<DeploymentRegion>();

            foreach (var item in Enum.GetValues(typeof(DeploymentRegion)))
            {
                list.Add((DeploymentRegion)item);
            }

            return list;
        }
    }
}

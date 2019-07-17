using System.Collections.Generic;
using Eshopworld.DevOps;

namespace EShopWorld.Tools.Common
{
    internal static class RegionHelper
    {
        internal static IEnumerable<DeploymentRegion> DeploymentRegionsToList(DeploymentEnvironment env = DeploymentEnvironment.Prod)
        {
            return EswDevOpsSdk.GetRegionSequence(env, DeploymentRegion.WestEurope);
        }
    }
}

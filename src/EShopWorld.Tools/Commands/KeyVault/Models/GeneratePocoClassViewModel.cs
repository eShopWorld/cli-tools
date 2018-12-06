using System;
using System.Collections.Generic;

namespace EShopWorld.Tools.Commands.KeyVault.Models
{
    public class GeneratePocoClassViewModel
    {
        public string Namespace { get; set; }
        public IEnumerable<Tuple<string, bool>> Fields { get; set; } //name + obsolete flag
    }
}

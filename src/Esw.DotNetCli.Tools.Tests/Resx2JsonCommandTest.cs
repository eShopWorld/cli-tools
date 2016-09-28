using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Esw.DotNetCli.Tools;
using Esw.DotNetCli.Tools.Tests.data;
using Newtonsoft.Json;
using Xunit;

// ReSharper disable once CheckNamespace
public class Resx2JsonCommandTest
{
    public class ConvertResx2Json
    {
        [Fact, Trait("Category", "Integration")]
        public void Test_WithEmbebeddedResxResource()
        {
            var resx = File.ReadAllText(@".\data\test.resx");

            var json = new Resx2JsonCommand(string.Empty, string.Empty).ConvertResx2Json(resx);

            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            foreach (var key in jsonDict.Keys)
            {
                var resourceValue = typeof(test).GetMethod($"get_{key}").Invoke(null, null);
                Assert.Equal(resourceValue, jsonDict[key]);
            }
        }
    }

    public class Run
    {
        [Fact, Trait("Category", "Integration")]
        public void TheOneTestToRuleThemAll()
        {
            // #1 clean up before the test

            // #2 run the command to export the json

            // #3 ensure folder structure

            // #4 inspect all the json and compare it to the resx
            // #4.1 use roslyn to emit the assembly with the resources on the integration project
        }
    }
}

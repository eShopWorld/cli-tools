using System.Globalization;
using System.Xml.Linq;

namespace EShopWorld.Tools.Common
{
    /// <summary>
    /// fluent API to generate csproj structure and render it into XML
    /// </summary>
    public class ProjectFileBuilder
    {
        private XElement _scope = new XElement(XName.Get("Project"), new XAttribute(XName.Get("Sdk"), "Microsoft.NET.Sdk"));

        /// <summary>
        /// enter into property group definition level
        /// </summary>
        /// <returns>property group fluent API level</returns>
        public IPropertyGroupBuilder WithPropertyGroup()
        {
            return new PropertyGroupBuilder(this);
        }

        /// <summary>
        /// create default - empty - builder instance
        /// </summary>
        /// <returns>builder api instance</returns>
        public static ProjectFileBuilder Default()
        {
            return new ProjectFileBuilder();
        }

        /// <summary>
        /// presets the builder for the expected nuget properties
        /// </summary>
        /// <param name="appName">name of the app</param>
        /// <param name="version">version of the package</param>
        /// <returns>fluent api</returns>
        public static ProjectFileBuilder CreateEswNetStandard20NuGet(string appName, string version)
        {
            return new ProjectFileBuilder()
                .WithPropertyGroup()
                    .WithTargetFramework("netstandard2.0")
                    .WithCompany("eShopWorld")
                    .GeneratePackageOnBuild(true)
                    .RequirePackageLicenseAcceptance(false)
                    .WithPackageId($"eShopWorld.{appName}.Configuration")
                    .WithVersion(version)
                    .WithAuthors("eShopWorld")
                    .WithCompany("eShopWorld")
                    .WithProduct(appName)
                    // ReSharper disable once StringLiteralTypo
                    .WithDescription($"c# poco representation of the {appName} configuration Azure KeyVault")
                    .WithCopyright("eShopWorld")
                    .WithAssemblyVersion(version)
                    .Attach();
        }

        private void AddPropertyGroup(XElement pg)
        {
            _scope.Add(pg);
        }

        /// <summary>
        /// termination call of the fluent api, get the built up XML representation as a string
        /// </summary>
        /// <returns>string representing the product of the builder</returns>
        public string GetContent()
        {
            return _scope.ToString();
        }

        private class PropertyGroupBuilder : IPropertyGroupBuilder
        {
            private readonly ProjectFileBuilder _parent;
            private readonly XElement _scope;

            protected internal PropertyGroupBuilder(ProjectFileBuilder parent)
            {
                _parent = parent;
                _scope = new XElement(XName.Get("PropertyGroup"));
            }

            public ProjectFileBuilder Attach()
            {
                _parent.AddPropertyGroup(_scope);

                return _parent;
            }

            public IPropertyGroupBuilder WithTargetFramework(string tfm)
            {
                return WithProperty("TargetFramework", tfm);
            }

            public IPropertyGroupBuilder WithPackageId(string id)
            {
                return WithProperty("PackageId", id);
            }

            public IPropertyGroupBuilder WithAuthors(params string[] authors)
            {
                return WithProperty("Authors", string.Join(',', authors));
            }

            public IPropertyGroupBuilder WithCompany(string company)
            {
                return WithProperty("Company", company);
            }

            public IPropertyGroupBuilder WithProduct(string product)
            {
                return WithProperty("Product", product);
            }

            public IPropertyGroupBuilder WithDescription(string description)
            {
                return WithProperty("Description", description);
            }

            public IPropertyGroupBuilder WithAssemblyVersion(string version)
            {
                return WithProperty("AssemblyVersion", version);
            }

            public IPropertyGroupBuilder WithCopyright(string copyright)
            {
                return WithProperty("Copyright", copyright);
            }

            public IPropertyGroupBuilder WithProperty(string name, string value)
            {

                var el = _scope.Element(XName.Get(name));
                if (el == null)
                {
                    el = new XElement(XName.Get(name));
                    _scope.Add(el);
                }

                el.Value = value;

                return this;
            }

            public IPropertyGroupBuilder GeneratePackageOnBuild(bool flag)
            {
                return WithProperty("GeneratePackageOnBuild", flag.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            public IPropertyGroupBuilder RequirePackageLicenseAcceptance(bool flag)
            {
                return WithProperty("PackageRequireLicenseAcceptance", flag.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
            }

            public IPropertyGroupBuilder WithVersion(string version)
            {
                return WithProperty("Version", version);
            }
        }
    }

    /// <summary>
    /// fluent api options for property group level
    /// </summary>
    public interface IPropertyGroupBuilder
    {
        /// <summary>
        /// Attach property group to the project level and return caller back to the project fluent level
        /// </summary>
        /// <returns>project fluent scope</returns>
        ProjectFileBuilder Attach();

        /// <summary>
        /// set "TargetFramework" property
        /// </summary>
        /// <param name="tfm">target framework moniker to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithTargetFramework(string tfm);

        /// <summary>
        /// set "PackageId" property
        /// </summary>
        /// <param name="id">package id to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithPackageId(string id);

        /// <summary>
        /// set "Authors" property
        /// </summary>
        /// <param name="authors">list of authors to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithAuthors(params string[] authors);

        /// <summary>
        /// set "Company" property
        /// </summary>
        /// <param name="company">company to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithCompany(string company);

        /// <summary>
        /// set "Product" property
        /// </summary>
        /// <param name="product">product to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithProduct(string product);

        /// <summary>
        /// set "Description" property
        /// </summary>
        /// <param name="description">description to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithDescription(string description);

        /// <summary>
        /// set "AssemblyVersion" property
        /// </summary>
        /// <param name="version">assembly version to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithAssemblyVersion(string version);

        /// <summary>
        /// set "Copyright" property
        /// </summary>
        /// <param name="copyright">copyright to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithCopyright(string copyright);

        /// <summary>
        /// set generic property
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">value to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithProperty(string name, string value);

        /// <summary>
        /// set "GeneratePackageOnBuild" property
        /// </summary>
        /// <param name="flag">flag to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder GeneratePackageOnBuild(bool flag);


        /// <summary>
        /// set "PackageRequireLicenseAcceptance" property
        /// </summary>
        /// <param name="flag">flag to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder RequirePackageLicenseAcceptance(bool flag);

        /// <summary>
        /// set "Version" property
        /// </summary>
        /// <param name="version">flag to set</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithVersion(string version);
    }
}

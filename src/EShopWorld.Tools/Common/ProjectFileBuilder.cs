using System;
using System.Globalization;
using System.Xml.Linq;

namespace EShopWorld.Tools.Common
{
    /// <summary>
    /// fluent API to generate csproj structure and render it into XML
    /// </summary>
    public class ProjectFileBuilder : IProjectBuilder
    {
        private readonly XElement _scope = new XElement(XName.Get("Project"), new XAttribute(XName.Get("Sdk"), "Microsoft.NET.Sdk"));

        /// <summary>
        /// enter into property group definition level
        /// </summary>
        /// <returns>property group fluent API level</returns>
        public IPropertyGroupBuilder WithPropertyGroup()
        {
            return new PropertyGroupBuilder(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IItemGroupBuilder WithItemGroup()
        {
            return new ItemGroupBuilder(this);
        }

        /// <summary>
        /// create default - empty - builder instance
        /// </summary>
        /// <returns>builder api instance</returns>
        public static IProjectBuilder Default()
        {
            return new ProjectFileBuilder();
        }

        /// <summary>
        /// presets the builder for the expected nuget properties
        /// </summary>
        /// <param name="appName">name of the app</param>
        /// <param name="version">version of the package</param>
        /// <param name="description">description of the package</param>
        /// <param name="tfms">tfm(s) to cover</param>
        /// <param name="packageDependencies">list of package dependencies expressed as package name/version tupple</param>
        /// <returns>fluent api</returns>
        // ReSharper disable once IdentifierTypo
        public static IProjectBuilder CreateEswNetStandard20NuGet(string appName, string version, string description, string tfms = "netstandard2.0", params (string name, string version)[] packageDependencies)
        {
            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("invalid value", nameof(appName));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("invalid value", nameof(version));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("invalid value", nameof(description));
            }

            if (string.IsNullOrWhiteSpace(tfms))
            {
                throw new ArgumentException("invalid value", nameof(tfms));
            }

            var builder = new ProjectFileBuilder();

            var pg = builder.WithPropertyGroup();
            if (!tfms.Contains(';')) //single or multiple frameworks detection
            {
                pg.WithTargetFramework(tfms);
            }
            else
            {
                pg.WithTargetFrameworks(tfms);
            }

            pg
            .WithCompany("eShopWorld")
            .GeneratePackageOnBuild(true)
            .RequirePackageLicenseAcceptance(false)
            .WithPackageId($"eShopWorld.{appName}.Configuration")
            .WithVersion(version)
            .WithAuthors("eShopWorld")
            .WithCompany("eShopWorld")
            .WithProduct(appName)
            .WithDescription(description)
            .WithCopyright("eShopWorld")
            .WithAssemblyVersion(version)
            .Attach();

            if (packageDependencies==null || packageDependencies.Length == 0) return builder;

            var itemGroup = builder.WithItemGroup();

            foreach (var (packageName, packageVersion) in packageDependencies)
            {
                itemGroup.WithPackageReference(packageName, packageVersion);
            }

            itemGroup.Attach();

            return builder;
        }

        private void AddChild(XElement pg)
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

        private class ItemGroupBuilder : IItemGroupBuilder
        {
            private readonly ProjectFileBuilder _parent;
            private readonly XElement _scope;

            public ItemGroupBuilder(ProjectFileBuilder parent)
            {
                _parent = parent;
                _scope = new XElement(XName.Get("ItemGroup"));
            }

            public IItemGroupBuilder WithReference(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("invalid value", nameof(name));
                }

                _scope.Add(new XElement(XName.Get("Reference"), new XAttribute(XName.Get("Include"), name)));
                return this;
            }

            public IItemGroupBuilder WithPackageReference(string name, string version)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("invalid value", nameof(name));
                }

                if (string.IsNullOrWhiteSpace(version))
                {
                    throw new ArgumentException("invalid value", nameof(version));
                }

                _scope.Add(new XElement(XName.Get("PackageReference"), new XAttribute(XName.Get("Include"), name), new XAttribute(XName.Get("Version"), version)));
                return this;
            }

            public IProjectBuilder Attach()
            {
                _parent.AddChild(_scope);

                return _parent;
            }
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
                _parent.AddChild(_scope);

                return _parent;
            }

            public IPropertyGroupBuilder WithTargetFramework(string tfm)
            {
                return WithProperty("TargetFramework", tfm);
            }

            public IPropertyGroupBuilder WithTargetFrameworks(string tfm)
            {
                return WithProperty("TargetFrameworks", tfm);
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
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException("invalid value", nameof(name));
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("invalid value", nameof(name));
                }

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
    /// builder definition for the project level
    /// </summary>
    public interface IProjectBuilder
    {
        /// <summary>
        /// allows adding property group level
        /// </summary>
        /// <returns>property group builder</returns>
        IPropertyGroupBuilder WithPropertyGroup();

        /// <summary>
        /// allows adding item group level
        /// </summary>
        /// <returns>item group level</returns>
        IItemGroupBuilder WithItemGroup();

        /// <summary>
        /// terminate the chain and return product of the builder
        /// </summary>
        /// <returns>project file content</returns>
        string GetContent();
    }

    /// <summary>
    /// interface to build structure defining item group in project file
    /// </summary>
    public interface IItemGroupBuilder
    {
        /// <summary>
        /// with assembly reference
        /// </summary>
        /// <param name="name">name of the assembly</param>
        /// <returns>builder reference for chaining</returns>
        IItemGroupBuilder WithReference(string name);

        /// <summary>
        /// with package (nuget) reference
        /// </summary>
        /// <param name="name">id of the package</param>
        /// <param name="version">version id</param>
        /// <returns>builder reference for chaining</returns>
        IItemGroupBuilder WithPackageReference(string name, string version);

        /// <summary>
        /// attach the item group and return builder back to project level
        /// </summary>
        /// <returns>project level builder</returns>
        IProjectBuilder Attach();
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
        /// set "TargetFrameworks" property
        /// </summary>
        /// <param name="tfmList">comma-separated list of tfms</param>
        /// <returns>fluent API scope</returns>
        IPropertyGroupBuilder WithTargetFrameworks(string tfmList);
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

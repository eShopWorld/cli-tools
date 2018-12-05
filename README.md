# eShopWorld CLI tools

Includes both dotnet CLI and azure CLI tool chains.

**MASTER CI build :**&nbsp;&nbsp;&nbsp;
![](https://eshopworld.visualstudio.com/_apis/public/build/definitions/310eec01-7d3c-402e-b179-74a206e8d4e3/13/badge)

## dotnet CLI tools

This tool contains the dotnet cli commands/subcommands produced by the eShopWorld Tooling Team.

* autoRest - top level 
  * generateClient - generates client for the given swagger manifest including the project file with appropriate NuGet metadata
* transform - aggregates data transformation tool set
  * resx2json - transform resx file(s) into json file (e.g. for Angular use)
* keyvault - aggregates keyvault tool set
  * generatesPOCOs - generates POCO classes based on keyvault content


## Usage Examples

see command with --help option to get full option list

#### AutoRest Generate Client Command

Usage

```console
dotnet-esw autorest generateClient -s <JSON target> -o <output>

```

#### Transform Resx to JSON Command

Usage 

```console
dotnet-esw transform  resx2json -s <source project> -o <output>
```

#### KeyVault Generate POCOs Command

Usage 

```console
dotnet-esw keyvault generatePOCOs -aid 123 -as secret -t esw -k maxmara -an maxmara -n Esw.MaxMara -v 1.2.3 -o .
```

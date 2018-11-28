# eShopWorld CLI tools

Includes both dotnet CLI and azure CLI tool chains.

**MASTER CI build :**&nbsp;&nbsp;&nbsp;
![](https://eshopworld.visualstudio.com/_apis/public/build/definitions/310eec01-7d3c-402e-b179-74a206e8d4e3/13/badge)

## dotnet CLI tools

This tool contains the dotnet cli tools produced by the eShopWorld Tooling Team.

* AutoRest Wrapper and Helper
* Resx to Json Transformer


## Usage Examples

#### AutoRest Commands

Usage

```console
dotnet-esw autorest run -s <JSON target> -o <output>

```


#### ResxtoJson Commands

Usage 

```console
dotnet-esw transfrom -s <source project> -o <output>
```

###
Local Debug Stuff

Application Arguement

transform

```console
transform run -s "..\..\..\..\..\Debug\Debug.Source" -o "..\..\..\..\..\Debug\Debug.Target"
```


autorest

````console

autorest run -s https://tahoe-api.ci.eshopworld.net/swagger/v1/swagger.json --output test/

```
# eShopWorld CLI tools

Includes both dotnet CLI and azure CLI tool chains.

**MASTER CI build :** ![](https://eshopworld.visualstudio.com/Github%20build/_apis/build/status/cli-tools?branchName=master)

## dotnet CLI tools

This tool contains the dotnet cli commands/subcommands produced by the eShopWorld Tooling Team.

* autoRest - top level 
  * generateClient - generates client for the given swagger manifest including the project file with appropriate NuGet metadata
  * transform - aggregates data transformation tool set
    * resx2json - transform resx file(s) into json file (e.g. for Angular use)
  * keyvault - aggregates keyvault tool set
    * generatesPOCOs - generates POCO classes based on keyvault content
  * azscan - scans Azure resources and creates Key Vault secrets with configuration details to streamline development of consuming application 
    * ai - scans Azure Monitor/App Insights resources
    * cosmosDb - scans Cosmos instances
    * dns - scans DNS definition and provides URL entries for entries coresponding to Traffic Manager, App Gateway and Load balancers - with the planned migration to FrontDoor when the infrastructure makes the transition
    * redis - scans Redis caches
    * serviceBus - scans service bus namespaces
    * SQL - scans SQL databases
    * all - runs scans for all above mentioned Azure resources
    


## Usage Examples

see command with --help option to get full option list

### AutoRest Generate Client Command

Usage

```console
dotnet esw autorest generateClient -s <JSON target> -o <output>

```

### Transform Resx to JSON Command

Usage 

```console
dotnet esw transform  resx2json -s <source project> -o <output>
```

### KeyVault Generate POCOs Command

Usage 

```console
dotnet esw keyvault generatePOCOs -aid 123 -as secret -t esw -k maxmara -an maxmara -n Esw.MaxMara -v 1.2.3 -o .
```

### Scanning Azure resources - AzScan commands

The typical flow envisions tool to be invoked from the release pipeline against a combination of environment/domain/region where the build is being pushed to. Key vault known to the application will be supplied as the tool target. 

The environment is represented through the subscription link.

The domain - resource group - scopes the set of resources that will be scanned so that proper segration of applications to each other can be achieved.

With higher environments - where multiple regions are utilised - region is passed so that each regional deployment) considers its instances of the resources. However the region is not considered for resources which implement their own regional resolution e.g. Cosmos DB.

Key vault must exist prior to the tool invoked and must have corresponding access policy (to read/list/write) for the scope that the tool is invoked under (typically the MSI identity configured for the build rig).

An example of tool invoked to scan App Insights instances for West Europe region of evo-ci subscription for "checkout api" domain (ResourceGroup). The relevant configuration secrets will be written to evo-checkout-api-we-kv key vault.

```console

dotnet esw azscan ai -s evo-ci -r we -g checkoutApi -k evo-checkout-api-we-kv
```

#### AzScan parameters

Following table captures recognized required and optional parameters for the *azscan* command family

long names use -- notation e.g. --keyVault
short names use - notation e.g. -k

|Short name|Long Name|Required|Description|Example value|
|----------|---------|--------|-----------|-------------|
| k | keyVault | Y | target keyvault name | esw-tooling-ci |
| g | resourceGroup | N | resource group filter | checkout-api-rg |
| s | subscription | Y | subscription filter | evo-ci |
| r | region | Y | region filter | we |

The region filter recognizes value from the [Devops package - DeploymentRegion enum](https://github.com/eShopWorld/devops/blob/master/src/Eshopworld.DevOps/DeploymentRegion.cs), examples being 'we' and 'eus'.

It is possible to either invoke individual commands e.g. dns or scan **all** supported resource types by invoking

```console
dotnet esw azscan all ....
```

##### Secrets naming strategy

Secrets will be naming using following formula - {resource prefix}--{resource name}--{configuration item suffix}. This is in line with the EswDevopsSdk configuration items separator(--) used to designate individual levels of configuration. 

Each resource type has its reserved prefix for all secrets generated. The name of the resources is transformed into camel case with special characters removed as to meet key vault secret naming rules (a-z and 0-9 and dash allowed). Environmental (e.g. -prep) and other suffixes (-lb) are removed from the name (as they are represented by keyvault itself).

As an example, __esw-checkout-prep__ resource will be named __eswCheckout__.

##### App insights scan - AI prefix

Scans applicable App Insights resources and projects following keyvault secrets

```
AI--name--InstrumentationKey
```

##### CosmosDB scan - CosmosDb prefix

Scans Cosmos DB and projects following secrets

```
CosmosDB--name--PrimaryConnectionString
```

##### DNS scan - Platform prefix

Scans A and CNAME records in dns definition(s) and projects them using following rules

* CNAME is considered *global* url (~ Traffic manager)
* A name when ending in -lb is considered to be a "load balancer" and interpreted as *HTTP* url
* A name when not ending in -lb is considered "Application gateway" entry and interpreted as * HTTPS* url, **however** when there is no pairing -lb suffixed entry, such entry is considered load balancer entry (for *internal* APIs)

Please note that these rules are driven by pre-existing devops automation scripting/ARM templates.

Key vault secrets are then 

```
Platform--name--Global
Platform--name--HTTPS
Platform--name--HTTP
```

Please note that this will be reviewed when Azure FrontDoor will be adopted.

##### Redis scanning - Redis prefix

Scans Redis instances and projects following secrets

```
Redis--name--PrimaryConnectionString
```

##### SQL scan - SQL prefix

Scans MS SQL databases (except master) and projects following secrets

```
SQL--name--ConnectionString
```

##### Service bus namespaces scan - SB prefix

Scans Service bus namespaces and projects following secrets

```
SB--name--PrimaryConnectionString
```

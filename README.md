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
    * Kusto - scans Kusto engines
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

This set of commands serves the purpose of automation of resource oversight and extracting configuration data into secure storage -KeyVaults. The tool is invoked within dedicated release pipeline against a combination of environment/domain. 

The tool runs across all supported resources and regions known within the Evolution platform for the given environment. The tool understands DevOps setup of the Resource groups -both platform and domain level -  and scans them to ingest the configuration data. When populated, a dedicated keyvault per region contains the necessary configurations and connection strings to connect to and use the provisioned Azure resources. This manifests as, West Europe (WE) configurations will reside in the WE KeyVault. Conversely, the East US (EUS) configurations will reside in the EUS KeyVault. Examples of configurations include ServiceBus Connection strings and Subscription Ids, or Cosmos DB Connection Strings.

Key vaults are already supported in ESW DevOps SDK configuration builder so the developer's experience is of very minimal code and this feature has been used by several in-house projects in several generations. The other advantage of Key Vaults is that they support tailored access policies and store versions of the secrets for auditing purposes.

The environment is represented through the subscription link.

The domain scopes the set of resources that will be scanned so that proper segration of applications to each other can be achieved.

The targeted Key Vault must exist prior to the CLI tool attempting to populate it. Additionally the appropriate access identity and rights must be allocated. Typically this will be the MSI Identity for the Build Rig. There is a cmdlet in devopsflex-automation module present that ensures these policies are in place.

An example of tool invoked to scan App Insights instances of evo-ci subscription for "checkout" domain. The relevant configuration secrets will be written to evo-checkout-we and evo-checkout-eus Key vaults.

```console

dotnet esw azscan ai -s evo-ci -d checkout
```

##### Configuration strongly typed objects - POCOs

It is envisioned - WIP - that the tooling will also support generating strongly typed C# objects representing the Key Vault content. This will simplify configuration ingestion to the developer even more and will make entire process very transparent.


#### AzScan parameters

Following table captures recognized required parameters for the *azscan* command family

long names use -- notation e.g. --subscription
short names use - notation e.g. -s

|Short name|Long Name|Required|Description|Example value|
|----------|---------|--------|-----------|-------------|
| s | subscription | Y | subscription filter | evo-ci |
| d | domain | Y | domain filter | tooling |


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
AI--{resourceName}--InstrumentationKey
```

##### CosmosDB scan - CosmosDb prefix

Scans Cosmos DB and projects following secrets

```
CosmosDB--{resourceName}--PrimaryConnectionString
```

##### DNS scan - Platform prefix

Scans A and CNAME records in dns definition(s) and projects them using following rules

* CNAME is considered *global* url (~ Traffic manager)
* A name when ending in -lb is considered to be a "load balancer" and interpreted as *HTTP* url
* A name when not ending in -lb is considered "Application gateway" entry and interpreted as * HTTPS* url, **however** when there is no pairing -lb suffixed entry, such entry is considered load balancer entry (for *internal* APIs)

Please note that these rules are driven by pre-existing devops automation scripting/ARM templates.

Key vault secrets are then 

```
Platform--{resourceName}--Global
Platform--{resourceName}--HTTPS
Platform--{resourceName}--HTTP
```

Please note that this will be reviewed when Azure FrontDoor will be adopted.

##### Redis scanning - Redis prefix

Scans Redis instances and projects following secrets

```
Redis--{resourceName}--PrimaryConnectionString
```

##### SQL scan - SQL prefix

Scans MS SQL databases (except master) and projects following secrets

```
SQL--{resourceName}--ConnectionString
```

##### Service bus namespaces scan - SB prefix

Scans Service bus namespaces and projects following secrets

```
SB--{resourceName}--PrimaryConnectionString
```

##### Kusto scan - Kusto prefix

Scans Kusto engines and projects following secrets

```
Kusto--{domain}--ClusterUri
Kusto--{domain}--ClusterIngestionUri
Kusto--{domain}--TenantId
Kusto--{domain}--DBName
```

Note that due to financial implications, there will not be a kusto cluster per subscription. Instead, single cluster is reserved for non-PROD databases and another cluster reserved for PROD. Non-PROD cluster will host multiple databases per domain and environment e.g. tooling-ci, tooling-sand. 

#### Testing strategy

This repo uses layered test approach. There are level 0, 1 and 2 tests. Layer 0 runs tests for several components with mocked dependencies. Layer 1 runs tests call external dependencies but do not require deployment. Level 2 runs tests calling external dependencies and requires the tool to be deployed (installed). 

As this is CLI tool, most tests are present in layer 2. These tests use dedicated test fixture to set up test resources mimicking supported setup in the Evolution platform. The tool is then invoked (as actual dotnet tool) and Key Vault then inspected to match secrets and their values to expected state. After the test, test resources are deprovisioned.

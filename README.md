Nap
====

![Unreleased](https://img.shields.io/badge/Status-Unreleased-red.svg?style=flat-square)
![Unstable](https://img.shields.io/badge/Status-Unstable-red.svg?style=flat-square)

<!-- ![NuGet: Nap](https://img.shields.io/nuget/v/Nap?style=flat-square&label=NuGet&logo=nuget) -->

Quick REST &mdash; nap &mdash; API Generation from JSON.

Usage:

```ps1
.\Nap.exe build --source-file ".\input.json" --destination-directory ".\out-gen"
```

Sample `.\input.json`:

```json
{
	"cat": {
		"name": "string",
		"purr power": "float",
		"is grumpy": "bool"
	},
	"bird": {
		"name": "string",
		"species": "string",
		"can fly": "bool"
	}
}
```

# Amg.Build

Lean, C# based build automation framework. Alternative to [Cake](https://cakebuild.net/).

## Getting Started

Download the `amgbuild` tool 
Create a new program `build` 
Run the `build` program 

````[cmd]
dotnet tool install -g amgbuild
amgbuild new build
build
````

Edit the build program 
````
$ build\build.csproj
````

## Features

* Pure C#. Write your build tasks as you write any other C# class. 
* Supports async tasks and automatically achieves maximum possible parallelization of build tasks
* Fluent interface for the handling of file system paths.

## Concepts

### Once

All virtual methods decorated with `[Once]` are only executed once and the results are cached in memory.

This helps you to build up and acyclic graph of your build dependencies.

### Automatic Command Line Interface

Properties of the Targets container are available as build commands on the command line if
* they are public
* they have a System.Component.Description attribute

Properties of the Targets container are available as build properties on the command line if
* they have a public setter
* they have a System.Component.Description attribute

## Todo

* file system watch
* Adapter to use Cake extensions

## Done

* Print return value of Commands
* Prepend class name to once methods in log
* Terminate on the first failed [Once] call.
* show result as acii art.
* clear all Sonar Analyzer findings
* creator tool "amgbuild.exe" creates and fixes Amg.Build scripts
* nullable references
* netcoreapp3.0
* Improve error logging
* option to fix source files
* check source files
* output options for ITool
* improved logging of process start failures
* better logging of failures
* more file system extensions
* Display exit codes in help message
* Exit code for command line error
* -vquiet does not show result summary
* build assembly is forced to rebuild after 60 minutes
* rebuild check can be disabled
* Logging shows current target
* timeline: shorten long invocation names
* GetOpt compliant target names for command line
* Make rebuild decisions based on the git hash of source files: GitExtensions.IfChanged
* Progress information for IEnumerable 
* CopyTree
* Remove old "DefineTarget" syntax
* Make Build classes callable from code, not only via Runner.Run => Runner.Once
* error summary
* nice diagnostic message for IsOutOfDate (... are out of date because of ...)
* Improve Target syntax
* Less clumsy target class syntax with [Once]
* Target logging
* ReduceLines, SplitLines
* Improve ITool error loggging
* ITool supports environment variables
* Glob
* Subtargets

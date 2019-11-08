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
````[cmd]
amgbuild open
````

## Features

* Pure C#. Write your build tasks as you write any other C# class.
* Fast startup
* Supports async tasks and automatically achieves maximum possible parallelization of build tasks.
* Fluent interface for the handling of file system paths.
* Contains an adapter to run [Cake](https://cakebuild.net/) [addins](https://cakebuild.net/addins/).

## Concepts

### [Once]

`Once.Create` creates proxy types for your classes where all virtual methods decorated with the `[Once]` attribute are only executed once. The results are cached in memory.

This helps you to build up and acyclic graph of your build dependencies.

If you want to use [Once], you must not use mutable fields or properties in your class. Exceptions are command line properties (`[Description]` attribute). See below.

### Automatic Command Line Interface

Properties of the Targets container are available as build commands on the command line if
* they are public
* they have a System.Component.Description attribute

Properties of the Targets container are available as build properties on the command line if
* they have a public setter
* they have a System.Component.Description attribute

### `amgbuild`

`amgbuild` is the dotnet tool that supports you while working with your Amg.Build scripts.

Install it with 
````[cmd]
dotnet tool install -g amgbuild
````

amgbuild can 
* create new scripts (`new`)
* fix the script files of an existing Amg.Build script (`fix`)
* open the script in Visual Studio (`open`)

## Todo

* complete Amg.FileSystem (copying)
* How-to documentation: create script, the concept of [Once], Cake

## Done

* remove --ascii option
* amgbuild now without --script option

## 0.31.0

* amgbuild add scripts to user's PATH
* amgbuild packs scripts and installs them as global dotnet tool
* gitignore
* complete Amg.FileSystem (RelativeTo, IsDescendantOrSelf)
* remove warning when removing old build results

### 0.29

* Starting without command line arguments shows help if no default command is given.
* Namespace refactoring

### 0.28

* [Once] properties with setters can only be set once
* command line parser can handle arrays
* command line parser can handle default method parameters
* allow more than one command on the command line (e.g. build.cmd clean build)

### 0.27

* Adapter to use Cake extensions

### 0.26

* file system watcher

### 0.25

* enforce that [Once] class does not have mutable fields
* --debug
* --edit
* improved Rebuild

### 0.24

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

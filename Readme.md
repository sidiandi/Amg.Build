# Amg.Build

Lean, C# based build automation framework. Alternative to [Cake](https://cakebuild.net/).

## Getting Started

## Features

* Pure C#. Write your build tasks as you write any other C# class. 
* Supports async tasks and achieves maximum possible parallelization of build tasks
* Fluent interface for the handling of file system paths.

## Concepts

### Target

A Target is a delegate that contains a piece of work to be done during a build.

A target has one or zero results and one or zero inputs.

A Targets container ensures that every target is only executed once, no matter how often it is called from other targets.

Targets are created with Targets.DefineTarget and they are made accessible as properties of a Targets subclass.

If a Target property is public and if it has a System.Component.Description attribute, then it is accessible on the command line.

### Subtargets

You can add classes with subtargets to your target container like so:

````
Git Git => DefineTargets(() => new Git());
````

The targets in the `Git` instance can then called as normal function. The executed jobs of the subtargets will appear in your build summary.

### Build Properties

Properties of the Targets container can be accessed as build properties on the command line if
* they have a public setter
* they have a System.Component.Description attribute

## Todo

* nice diagnostic message for IsOutOfDate (... are out of date because of ...)
* Adapter to use Cake extensions
* Fail on the first failed target

## Done

* Improve Target syntax
* Less clumsy target class syntax with [Once]
* Target logging
* ReduceLines, SplitLines
* Improve ITool error loggging
* ITool supports environment variables
* Glob
* Subtargets

# Amg.Build

Lean, C# based build automation framework. Alternative to [Cake](https://cakebuild.net/).

## Getting Started

See example $/examples/hello

````
public class BuildTargets
{
	static int Main(string[] args) => Runner.Run<BuildTargets>(args);

	[Once]
	public virtual async Task Greet(string name)
	{
		await Task.Delay(TimeSpan.FromSeconds(1));
		Console.WriteLine($"Hello, {name}");
	}
	
	[Once]
	public virtual async Task GreetAll()
	{
		await Task.WhenAll(Enumerable.Range(0,10).Select(_ => Greet($"Alice {_}")));
	}
	
	[Once]
	public virtual async Task Default()
	{
		await GreetAll();
	}
}
````

## Features

* Pure C#. Write your build tasks as you write any other C# class. 
* Supports async tasks and achieves maximum possible parallelization of build tasks
* Fluent interface for the handling of file system paths.

## Concepts

### Once

All virtual methods decorated with [Once] are only executed once and the results are cached in memory.

This helps you to build up acyclic dependency trees of your build activities.

### Subtargets

You can add classes with subtargets to your target container like so:

````
[Once]
protected virtual Git Git => new Git();
````

The targets in the `Git` instance can then called as normal function. 

If `Git` contains `[Once]` attributes, these methods will also be only executed once.

### Build Properties

Properties of the Targets container are available as build properties on the command line if
* they have a public setter
* they have a System.Component.Description attribute

## Todo

* Adapter to use Cake extensions
* Fail on the first failed target

## Done

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

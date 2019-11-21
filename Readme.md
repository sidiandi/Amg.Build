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

With the `[Once]` attribut, you build a [directed acyclic dependency graph](https://en.wikipedia.org/wiki/Directed_acyclic_graph) of your build steps.

All methods and properties in your code marked with the `[Once]` attribute will only be executed once and the return value will be cached. The second time the method or property getter is called, the code will not be executed, but the cached return value will be returned. 

Let's hava a look at an example:

````
    internal class OnceExample
    {
        [Once]
        public virtual async Task Compile()
        {
            await Task.CompletedTask;
        }

        [Once]
        public virtual async Task Test()
        {
            await Compile();

            // ... testing done here ...
        }

        [Once]
        public virtual async Task Package()
        {
            await Compile();
            // ... packaging the compiled binaries here ...
        }

        [Once]
        public virtual async Task Release()
        {
            await Task.WhenAll(Test(), Package());
        }
    }
```` 

	When you call `Release` here, it will run `Test` and `Package` in parallel. Although `Compile` is called in both methods, the code in `Compile` will only executed once and `Package()` and `Test()` can start their own activities as soon as the `Compile()` results are available.

To activate the `[Once]` attributes in your classes, you create a derived proxy class from your class with
````
	var example = Once.Create<OnceExample>();
	example.Release();
````

If you want to use `Once.Create` on a class, it must
* not contain mutable fields
* not contain mutable properties without `[Once]` attribute
* declare all methods and properties marked with `[Once]` as `virtual`
* be either `public` or `internal`. If your class is `internal`, you will need to make it visible to the underlying code injection framework with `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]`.

### Automatic Command Line Interface

Properties of the Targets container are available as build commands on the command line if
* they are public
* they have a `[System.Component.Description]` attribute

Properties of the Targets container are available as build properties on the command line if
* they have a public setter
* they have a `[System.Component.Description]` attribute

### Support of Cake Build Addins

[Cake Build](https://cakebuild.net/) comes with a huge collection of [addins](https://cakebuild.net/addins/) for all kinds of build use cases.

You can use all Cake addins directly in Amg.Build by adding the `Amg.Build.Cake` package and creating a Cake context:

````
[Once]
protected virtual Cake.Core.ICakeContext Cake => Amg.Build.Cake.Cake.CreateContext();
````

See a [full example here](src/hello/CakeAddinExample.cs).

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


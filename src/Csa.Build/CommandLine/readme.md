# Chp.Common.ConsoleBootstrapper

GetOpt compatible command line option parser

Complies to the getopt [Program Argument Syntax Conventions](https://www.gnu.org/software/libc/manual/html_node/Argument-Syntax.html):

* Arguments are options if they begin with a hyphen delimiter (‘-’).
* Multiple options may follow a hyphen delimiter in a single token if the options do not take arguments. Thus, ‘-abc’ is equivalent to ‘-a -b -c’.
* Option names are single alphanumeric characters (as for isalnum; see Classification of Characters).
* Certain options require an argument. For example, the ‘-o’ command of the ld command requires an argument—an output file name.
* An option and its argument may or may not appear as separate tokens. (In other words, the whitespace separating them is optional.) Thus, ‘-o foo’ and ‘-ofoo’ are equivalent.
* Options typically precede other non-option arguments.
* The implementations of getopt and argp_parse in the GNU C Library normally make it appear as if all the option arguments were specified before all the non-option arguments for the purposes of parsing, even if the user of your program intermixed option and non-option arguments. They do this by reordering the elements of the argv array. This behavior is nonstandard; if you want to suppress it, define the _POSIX_OPTION_ORDER environment variable. See Standard Environment.
* The argument ‘--’ terminates all options; any following arguments are treated as non-option arguments, even if they begin with a hyphen.
* A token consisting of a single hyphen character is interpreted as an ordinary non-option argument. By convention, it is used to specify input from or output to the standard input and output streams.
* Options may be supplied in any order, or appear multiple times. The interpretation is left up to the particular application program.
* GNU adds long options to these conventions. Long options consist of ‘--’ followed by a name made of alphanumeric characters and dashes. Option names are typically one to three words long, with hyphens to separate words. Users can abbreviate the option names as long as the abbreviations are unique.
* To specify an argument for a long option, write ‘--name=value’. This syntax enables a long option to accept an argument that is itself optional.

## Usage

In your project file, include the CommandLineTool.props file:

```
  <Import Project="..\CommandLineTool.props" />
```

In your Program class, replace the standard

```
static int Main(string[] args)
```

entry method with

```
static void Main(Options options)
```

and implement the `Options` class:

See [the unit test](../Chp.Common.Tests/ConsoleBootstrapper/GetOpt.cs) for an example of an [options class](../Chp.Common.Tests/ConsoleBootstrapper/Options.cs)

Implementation hints for the `Options` class:
* Add a public set/get property for every option. Allowed types for options are String, Int32, Double, and enums.
* Use `System.ComponentModel.DescriptionAttribute` to add an usage text to the option.
* Use `Chp.Common.ConsoleBootstrapper.ShortAttribute` to specify a one-character option for the property.
* The long option name will be determined from the property name. Example: property name `OutputDirectory` becomes option name `--output-directory`.
* Option classes can be nested.

### Verbs

Multiple *verbs*, like for example in `git` are also supported. Therefore, implement static methods with exactly one option class parameter and a Description attribute. [Example here](/src/Chp.Common.Tests/ConsoleBootstrapper/ProgramWithVerbs.cs).

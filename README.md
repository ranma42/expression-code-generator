# A code generator to quote expressions

This is a PoC for a code generator that can be used to quote expressions in C#.
It is based on the idea proposed in
https://github.com/dotnet/csharplang/discussions/158#discussioncomment-11779518
and
https://github.com/dotnet/csharplang/discussions/158#discussioncomment-11789999

It is mostly a hack (very incomplete, missing input validation etc etc), but its
purpose is to understand the main issues with such an approach.

# Implementations

The code generator implements 3 different translation strategies:

- `QuoteRoslyn` rewrites C# into C# (currently it rewrites switch expressions
  into a chain of conditionals); it emits the new expression in the interceptor
  and leaves the task of actually quoting/translating into an expression tree
  to Roslyn
- `QuoteExpr` rewrites C# into C# code that constructs an expression tree (i.e.
  a series of calls to `System.Linq.Expressions.Expression.*`)
- `QuoteMyExpr` rewrites C# into C# code that constructs an tree of custom
  expressions for a very limited expression language `MyExpr`

Each of these implementations server a few different purposes, detailed below.

## `QuoteRoslyn`
`QuoteRoslyn` shows that using code generators and interceptors it is possible
to extend the constructs of C# code that can be quoted. It does this by
rewriting expressions into other expressions that Roslyn is already capable of
quoting.

Assuming that the consumers of the quoted expressions are capable of handling
all of the expressions that Roslyn currently emits, this might be an interesting
direction to add support for modern syntax to existing ET consumers.

On the other hand, this approach is obviously more limited than the other two as
it does not make it possible to translate syntax nodes in custom ways.

## `QuoteExpr`
This approach still quotes C# code into ETs, but it makes it possible to use
expression nodes that Roslyn would not normally use, such as
`System.Linq.Expressions.Expression.Switch`.

It would also be possible to sub-class `System.Linq.Expressions.Expression` and
use the new node types to translate the expressions, as long as they can be
`Reduce`d and/or are supported by the ET consumer.

This approach could leverage the existing ET consumers and open an avenue
towards supporting new node types by extending them, possibly in a way that is
specialized differently for each consumer.

One disadvantage of this approach is that it does not re-use the machinery that
Roslyn provides for translating expressions; OTOH it might be possible to
implement a `CSharpSyntaxRewriter` that provides a baseline (independent of the
specific ET consumer) for this and that is further specialized as needed.

## `QuoteMyExpr`
This approach basically implements the same thing as `QuoteExpr`, but it uses
a custom expression tree unrelated to ETs.

It shows that the approach is not limited to ETs and can actually be used to
perform quoting in a generic way.

Additionally, the implementation has been extended to support captures, which
is an interesting part to study.

# Issues

## Captures

The sample shows that the `QuoteMyExpr` quoting system can correctly capture
variables, but looking at the internals of the implementation (i.e.
`ClosureEnvironment`) shows that it relies on somewhat arbitrary assumptions on
how to access the captured variables by extracting the target of a delegate and
doing some reflection on it.

While it currently works, I was unable to find any kind of "contract" the
compiler is supposed to implement when capturing variables; AFAICT it might
change depending on compiler version, target environment etc.

## Typing/DX

The sample shows quotations in isolation. A very common use case is to use ETs
on `IQueriable`s, such as when using EFCore to access a database. In this case,
the implementation from this project would regress DX significantly:

```csharp
var query = dbContext.Users
    .Where(QuoteRoslyn(u => u.Verified))
    .Select(QuoteRoslyn(u => u.Name));
```

would not build because the type of `u` cannot be determined.

The following code would work instead:
```csharp
var query = dbContext.Users
    .Where(QuoteRoslyn((User u) => u.Verified))
    .Select(QuoteRoslyn((User u) => u.Name));
```
as the typing is explicit.

An alternative that "fixes" that might be as follows:
```csharp
var query = dbContext.Users
    .WhereQuoted(u => u.Verified)
    .SelectQuoted(u => u.Name);
```
in which the source generator runs on each of the `Quoted` invocations.

This would still not work for LINQ-style queries, such as:
```csharp
var query = from u in dbContext.Users
    where u.Verified
    select u.Name;
```
This could be handled as follows:
```csharp
var query = dbContext.Users
    .Quoted(q => from u in q
        where u.Verified
        select u.Name
    );
// OR
var query = dbContext.Users
    .Quoted(q => q
        .Where(u => u.Verified)
        .Select(u => u.Name)
    );
```
in which `q` has an appropriate type (something like `IEnumerable` might work).

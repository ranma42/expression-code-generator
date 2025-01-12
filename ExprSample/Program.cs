using System;
using static ExprBuilder;

// An expression tree may not contain a switch expression. (CS8514)
// System.Linq.Expressions.Expression myExpr = (int x) => x switch
// {
//     0 => 0 + 1,
//     _ => 123 - 2,
// };

var roslynExpr = QuoteRoslyn((int x) => x switch
{
    0 => 0 + 1,
    _ => 123 - 2,
});

Console.WriteLine(roslynExpr);
Console.WriteLine(roslynExpr.Compile().Invoke(9));

var expr = QuoteExpr((int x) => x switch
{
    0 => 0 + 1,
    _ => 123 - 2,
});

Console.WriteLine(expr);
Console.WriteLine(expr.Compile().Invoke(9));

var myExpr = QuoteMyExpr((int x) => x - 2);

Console.WriteLine(myExpr);
Console.WriteLine(myExpr.Evaluate().Invoke(9));

var y = 7;
var myClosure = QuoteMyExpr((int x) => x - y);
y = 5;

Console.WriteLine(myClosure);
Console.WriteLine(myClosure.Evaluate().Invoke(9));

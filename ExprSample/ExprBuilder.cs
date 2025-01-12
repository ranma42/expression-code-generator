using System;
using System.Linq.Expressions;
using MyExpr;

public static class ExprBuilder
{
    public static Expression<T> QuoteExpr<T>(T target)
        => throw new NotImplementedException("This method should not be used at runtime");

    public static Expression<T> QuoteRoslyn<T>(T target)
        => throw new NotImplementedException("This method should not be used at runtime");

    public static IMyBoundExpression QuoteMyExpr<T>(T target)
        => throw new NotImplementedException("This method should not be used at runtime");
}

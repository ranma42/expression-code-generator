using System;

namespace MyExpr;

public interface IMyBoundExpression
{
    abstract dynamic Evaluate();
}

public record MyBoundExpression(IMyExpression Expression, IMyEnvironment Environment) : IMyBoundExpression
{
    public dynamic Evaluate() => Expression.Evaluate(Environment);
}

public interface IMyExpression
{
    abstract dynamic Evaluate(IMyEnvironment environment);
}

public static class MyExpr
{
    public static IMyBoundExpression Bind(IMyExpression expr, IMyEnvironment environment) => new MyBoundExpression(expr, environment);
    public static IMyExpression MyLambda(IMyExpression body, string parameter) => new MyLambda(body, parameter);
    public static IMyExpression MyBinary(BinOp op, IMyExpression left, IMyExpression right) => new MyBinary(op, left, right);
    public static IMyExpression MyConstant(object value) => new MyConstant(value);
    public static IMyExpression MyVariable(string name) => new MyVariable(name);
}

public record MyConstant(object Value) : IMyExpression
{
    public dynamic Evaluate(IMyEnvironment environment) => Value;
}

public record MyVariable(string Name) : IMyExpression
{
    public dynamic Evaluate(IMyEnvironment environment) => environment.Get(Name);
}

public enum BinOp { Add, Subtract, Multiply, Divide }

public record MyBinary(BinOp op, IMyExpression Left, IMyExpression Right) : IMyExpression
{
    public dynamic Evaluate(IMyEnvironment environment)
    {
        var left = Left.Evaluate(environment);
        var right = Right.Evaluate(environment);
        return op switch
        {
            BinOp.Add => left + right,
            BinOp.Subtract => left - right,
            BinOp.Multiply => left * right,
            BinOp.Divide => left / right,
            _ => throw new NotSupportedException(),
        };
    }
}

public record MyLambda(IMyExpression Body, string Parameter) : IMyExpression
{
    public Func<object, dynamic> InnerEvaluate(IMyEnvironment environment) =>
        x => Body.Evaluate(environment.Bind(Parameter, x));

    dynamic IMyExpression.Evaluate(IMyEnvironment environment) =>
        InnerEvaluate(environment);
}

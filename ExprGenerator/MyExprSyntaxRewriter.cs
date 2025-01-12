using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ExprGenerator;

public class MyExprSyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node) =>
        InvocationExpression(
            ParseName("global::MyExpr.MyExpr.MyConstant"))
        .WithArgumentList(
            ArgumentList(
                SingletonSeparatedList(
                    Argument(node))));

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
        InvocationExpression(
            ParseName("global::MyExpr.MyExpr.MyVariable"))
        .WithArgumentList(
            ArgumentList(
                SingletonSeparatedList(
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(node.Identifier.Text))))));

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) =>
        VisitParenthesizedLambdaExpression(
            ParenthesizedLambdaExpression()
                .WithBody(node.Body)
                .WithParameterList(
                    ParameterList(SingletonSeparatedList(node.Parameter))));

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        if (node.ParameterList.Parameters.Count != 1)
        {
            throw new NotSupportedException("Only single parameter lambdas are supported");
        }

        return InvocationExpression(
            ParseName("global::MyExpr.MyExpr.MyLambda"))
        .WithArgumentList(
            ArgumentList(
                SeparatedList([
                    Argument((ExpressionSyntax)Visit(node.Body)),
                        ..node.ParameterList.Parameters.Select(parameter => Argument(
                            LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(parameter.Identifier.Text)))),
                ])));
    }

    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var op = node.Kind() switch
        {
            SyntaxKind.AddExpression => "Add",
            SyntaxKind.SubtractExpression => "Subtract",
            SyntaxKind.MultiplyExpression => "Multiply",
            SyntaxKind.DivideExpression => "Divide",
            _ => throw new NotSupportedException(node.OperatorToken.Kind().ToString()),
        };

        return InvocationExpression(
            ParseName("global::MyExpr.MyExpr.MyBinary"))
        .WithArgumentList(
            ArgumentList(
                SeparatedList([
                    Argument(
                        ParseName("global::MyExpr.BinOp." + op)
                    ),
                    Argument((ExpressionSyntax)Visit(node.Left)),
                    Argument((ExpressionSyntax)Visit(node.Right)),
                ])));
    }
}
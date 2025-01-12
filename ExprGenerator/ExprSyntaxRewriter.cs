using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ExprGenerator;

public class ExprSyntaxRewriter : CSharpSyntaxRewriter
{
    private List<KeyValuePair<string, string>> decls = [];
    private ImmutableDictionary<string, string> _environment = ImmutableDictionary.Create<string, string>();

    private KeyValuePair<string, string> GenId(string exprName)
    {
        var id = $"_{decls.Count}";
        var pair = new KeyValuePair<string, string>(exprName, id);
        decls.Add(pair);
        return pair;
    }

    private IdentifierNameSyntax Foo(string originalId)
    {
        if (_environment.TryGetValue(originalId, out var internalId))
        {
            return IdentifierName(Identifier(internalId));
        }

        throw new Exception($"Unknown identifier {originalId}");
    }

    public IEnumerable<StatementSyntax> GetDeclarations() =>
        decls.Select(
            (entry) => LocalDeclarationStatement(
                VariableDeclaration(
                    ParseTypeName("global::System.Linq.Expressions.ParameterExpression")
                )
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                            Identifier(entry.Value))
                        .WithInitializer(
                            EqualsValueClause(
                                InvocationExpression(
                                    ParseName("global::System.Linq.Expressions.Expression.Parameter"))
                                .WithArgumentList(
                                    ArgumentList(
                                        SeparatedList([
                                            Argument(
                                                TypeOfExpression(PredefinedType(
                                                    Token(SyntaxKind.IntKeyword)))),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(entry.Key))),
                                        ])))))))));

    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node) =>
        InvocationExpression(
            ParseName("global::System.Linq.Expressions.Expression.Constant"))
        .WithArgumentList(
            ArgumentList(
                SingletonSeparatedList(
                    Argument(node))));

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
        Foo(node.Identifier.Text);

    public override SyntaxNode? VisitParameter(ParameterSyntax node) =>
        Foo(node.Identifier.Text);

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) =>
        VisitParenthesizedLambdaExpression(
            ParenthesizedLambdaExpression()
                .WithBody(node.Body)
                .WithParameterList(
                    ParameterList(SingletonSeparatedList(node.Parameter))));

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        var outerEnv = _environment;
        try
        {
            _environment = _environment.SetItems(
                node.ParameterList.Parameters.Select(parameter => GenId(parameter.Identifier.Text)));

            return InvocationExpression(
                ParseName("global::System.Linq.Expressions.Expression.Lambda"))
            .WithArgumentList(
                ArgumentList(
                    SeparatedList([
                        Argument((ExpressionSyntax)Visit(node.Body)),
                        ..node.ParameterList.Parameters.Select(parameter => Argument((ExpressionSyntax)Visit(parameter))),
                    ])));
        }
        finally
        {
            _environment = outerEnv;
        }
    }

    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        var op = node.Kind() switch {
            SyntaxKind.AddExpression => "Add",
            SyntaxKind.SubtractExpression => "Subtract",
            SyntaxKind.MultiplyExpression => "Multiply",
            SyntaxKind.DivideExpression => "Divide",
            _ => throw new NotSupportedException(node.OperatorToken.Kind().ToString()),
        };

        return InvocationExpression(
            ParseName("global::System.Linq.Expressions.Expression." + op))
        .WithArgumentList(
            ArgumentList(
                SeparatedList([
                    Argument((ExpressionSyntax)Visit(node.Left)),
                    Argument((ExpressionSyntax)Visit(node.Right)),
                ])));
    }

    public override SyntaxNode? VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        var switchValue = Argument((ExpressionSyntax)Visit(node.GoverningExpression));
        var defaultBody = node.Arms
            .Where(arm => arm.Pattern is DiscardPatternSyntax)
            .Take(1)
            .Select(arm => Argument((ExpressionSyntax)Visit(arm.Expression)));

        var cases = node.Arms
            .TakeWhile(arm => arm.Pattern is ConstantPatternSyntax)
            .Select(arm => Argument(
                InvocationExpression(
                    ParseName("global::System.Linq.Expressions.Expression.SwitchCase"))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList([
                            Argument((ExpressionSyntax)Visit(arm.Expression)),
                            Argument((ExpressionSyntax)Visit(((ConstantPatternSyntax)arm.Pattern).Expression)),
                        ])))));

        return InvocationExpression(ParseName("global::System.Linq.Expressions.Expression.Switch"))
            .WithArgumentList(
                ArgumentList(
                    SeparatedList([
                        switchValue,
                        ..defaultBody,
                        ..cases,
                    ])));
    }
}
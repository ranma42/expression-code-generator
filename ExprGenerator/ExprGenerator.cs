using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ExprGenerator;

[Generator]
public class ExprGenerator : IIncrementalGenerator
{
    private static NameSyntax expressionT = GenericName(
        Identifier("global::System.Linq.Expressions.Expression"),
        TypeArgumentList(
            SingletonSeparatedList<TypeSyntax>(
                IdentifierName("T"))));

    private static NameSyntax expression = ParseName("global::System.Linq.Expressions.Expression");

    private static NameSyntax myBoundExpression = ParseName("global::MyExpr.IMyBoundExpression");

    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Look for invocations of QuoteExpr
        var quoteExprs = context.SyntaxProvider
            .CreateSyntaxProvider((node, _) => node is InvocationExpressionSyntax
            {
                Expression: IdentifierNameSyntax
                {
                    Identifier: { Value: "QuoteExpr" }
                }
            }, (node, _) => node);

        context.RegisterImplementationSourceOutput(quoteExprs, EmitExpression);

        // Look for invocations of QuoteRoslyn
        var quoteRoslyn = context.SyntaxProvider
            .CreateSyntaxProvider((node, _) => node is InvocationExpressionSyntax
            {
                Expression: IdentifierNameSyntax
                {
                    Identifier: { Value: "QuoteRoslyn" }
                }
            }, (node, _) => node);

        context.RegisterImplementationSourceOutput(quoteRoslyn, EmitViaRoslyn);

        // Look for invocations of QuoteMyExpr
        var quoteMyExpr = context.SyntaxProvider
            .CreateSyntaxProvider((node, _) => node is InvocationExpressionSyntax
            {
                Expression: IdentifierNameSyntax
                {
                    Identifier: { Value: "QuoteMyExpr" }
                }
            }, (node, _) => node);

        context.RegisterImplementationSourceOutput(quoteMyExpr, EmitMyExpr);
    }

    static void EmitExpression(SourceProductionContext context, GeneratorSyntaxContext query)
    {
        var expressionSyntaxRewriter = new ExprSyntaxRewriter();

        var querySyntax = (InvocationExpressionSyntax)query.Node;
        var lambdaSyntax = (LambdaExpressionSyntax)querySyntax.ArgumentList.Arguments[0].Expression;

        var returnStatement = ReturnStatement(
            CastExpression(
                expressionT,
                (ExpressionSyntax)expressionSyntaxRewriter.Visit(lambdaSyntax)
            ))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken)
            );

        var body = Block(expressionSyntaxRewriter.GetDeclarations())
            .AddStatements(returnStatement);

        EmitQuotedExpressions(context, query, body);
    }

    static void EmitViaRoslyn(SourceProductionContext context, GeneratorSyntaxContext query)
    {
        var syntaxRewriter = new SyntaxRewriter();

        var querySyntax = (InvocationExpressionSyntax)query.Node;
        var lambdaSyntax = (LambdaExpressionSyntax)querySyntax.ArgumentList.Arguments[0].Expression;

        var body = Block(
            ReturnStatement(
            CastExpression(
            expressionT,
            CastExpression(
                expression,
                ParenthesizedExpression(
                    (ExpressionSyntax)syntaxRewriter.Visit(lambdaSyntax)
                ))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken)
            ));

        EmitQuotedExpressions(context, query, body);
    }

    static void EmitQuotedExpressions(
        SourceProductionContext context,
        GeneratorSyntaxContext query,
        BlockSyntax interceptorBody
    )
    {
        var querySyntax = (InvocationExpressionSyntax)query.Node;
        var lambdaSyntax = (LambdaExpressionSyntax)querySyntax.ArgumentList.Arguments[0].Expression;

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var interceptableLocation = query.SemanticModel
            .GetInterceptableLocation((InvocationExpressionSyntax)query.Node)!;
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var generatedClassName = $"Foobar_{query.Node.GetLocation().GetLineSpan().StartLinePosition.Line}";
        var generatedFileName = $"{generatedClassName}.g.cs";

        var classSyntax = ClassDeclaration(generatedClassName)
            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
            .AddMembers(
                MethodDeclaration(
                    expressionT,
                    Identifier("Query"))
                .AddAttributeLists(
                    AttributeList()
                        .AddAttributes(
                            Attribute(
                                ParseName("global::System.Runtime.CompilerServices.InterceptsLocation"),
                                AttributeArgumentList(
                                    SeparatedList([
                                        AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(interceptableLocation.Version)))
                                        .WithNameColon(
                                            NameColon(
                                                IdentifierName("version"))),
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(interceptableLocation.Data)))
                                    .WithNameColon(
                                        NameColon(
                                            IdentifierName("data")))])
                                )
                            )
                        ))
                .WithModifiers(
                    TokenList([
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)]))
                .WithTypeParameterList(
                    TypeParameterList(
                        SingletonSeparatedList(
                            TypeParameter(
                                Identifier("T")))))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("target"))
                            .WithType(
                                IdentifierName("T")))))
                .WithBody(interceptorBody));

        var usingDirectives = querySyntax.SyntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

        var compilationUnit = CompilationUnit()
            .AddUsings(usingDirectives)
            .AddMembers(
                NamespaceDeclaration(
                    ParseName("ExprGenerator.Generated")
                ).AddMembers(classSyntax)
            )
            .WithLeadingTrivia(
                TriviaList(
                    Comment("// <auto-generated/>")
                )
            );

        context.AddSource(generatedFileName, SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
    }

    static void EmitMyExpr(SourceProductionContext context, GeneratorSyntaxContext query)
    {
        var expressionSyntaxRewriter = new MyExprSyntaxRewriter();

        var querySyntax = (InvocationExpressionSyntax)query.Node;
        var lambdaSyntax = (LambdaExpressionSyntax)querySyntax.ArgumentList.Arguments[0].Expression;

        var interceptorBody = Block(
            ReturnStatement(
                InvocationExpression(
                    ParseName("global::MyExpr.MyExpr.Bind"))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList([
                            Argument((ExpressionSyntax)expressionSyntaxRewriter.Visit(lambdaSyntax)),
                            Argument(
                                InvocationExpression(
                                    ParseName("global::MyExpr.MyEnvironment.FromAnything"))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                IdentifierName(Identifier("target"))))))),
                        ]))))
            .WithSemicolonToken(
                Token(SyntaxKind.SemicolonToken)
            ));

#pragma warning disable RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var interceptableLocation = query.SemanticModel
            .GetInterceptableLocation((InvocationExpressionSyntax)query.Node)!;
#pragma warning restore RSEXPERIMENTAL002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var generatedClassName = $"Foobar_{query.Node.GetLocation().GetLineSpan().StartLinePosition.Line}";
        var generatedFileName = $"{generatedClassName}.g.cs";

        var classSyntax = ClassDeclaration(generatedClassName)
            .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
            .AddMembers(
                MethodDeclaration(
                    myBoundExpression,
                    Identifier("Query"))
                .AddAttributeLists(
                    AttributeList()
                        .AddAttributes(
                            Attribute(
                                ParseName("global::System.Runtime.CompilerServices.InterceptsLocation"),
                                AttributeArgumentList(
                                    SeparatedList([
                                        AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                Literal(interceptableLocation.Version)))
                                        .WithNameColon(
                                            NameColon(
                                                IdentifierName("version"))),
                                    AttributeArgument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal(interceptableLocation.Data)))
                                    .WithNameColon(
                                        NameColon(
                                            IdentifierName("data")))])
                                )
                            )
                        ))
                .WithModifiers(
                    TokenList([
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)]))
                .WithTypeParameterList(
                    TypeParameterList(
                        SingletonSeparatedList(
                            TypeParameter(
                                Identifier("T")))))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(
                                Identifier("target"))
                            .WithType(
                                IdentifierName("T")))))
                .WithBody(interceptorBody));

        var usingDirectives = querySyntax.SyntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

        var compilationUnit = CompilationUnit()
            .AddUsings(usingDirectives)
            .AddMembers(
                NamespaceDeclaration(
                    ParseName("ExprGenerator.Generated")
                ).AddMembers(classSyntax)
            )
            .WithLeadingTrivia(
                TriviaList(
                    Comment("// <auto-generated/>")
                )
            );

        context.AddSource(generatedFileName, SourceText.From(compilationUnit.NormalizeWhitespace().ToFullString(), Encoding.UTF8));
    }
}

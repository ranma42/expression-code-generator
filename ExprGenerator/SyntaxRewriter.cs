// Heavily based on https://github.com/koenbeuk/EntityFrameworkCore.Projectables/
// by Koen Beukelaers

/*
MIT License

Copyright (c) 2020 Koen Beukelaers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace ExprGenerator;

public class SyntaxRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitSwitchExpression(SwitchExpressionSyntax node)
    {
        var governingExpression = (ExpressionSyntax)Visit(node.GoverningExpression);

        ExpressionSyntax? currentExpression = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);

        // Reverse arms order to start from the default value
        foreach (var arm in node.Arms.Reverse())
        {
            var armExpression = (ExpressionSyntax)Visit(arm.Expression);

            if (arm.Pattern is DiscardPatternSyntax)
            {
                // Handle fallback value
                currentExpression = armExpression;
            }
            else if (arm.Pattern is ConstantPatternSyntax constant)
            {
                var expression = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, governingExpression, constant.Expression);

                // Add the when clause as a AND expression
                if (arm.WhenClause != null)
                {
                    expression = SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        expression,
                        (ExpressionSyntax)Visit(arm.WhenClause.Condition)
                    );
                }

                currentExpression = SyntaxFactory.ConditionalExpression(
                    expression,
                    armExpression,
                    currentExpression
                );
            }
            else
            {
                throw new InvalidOperationException("Switch expressions rewriting is only supported with constant values");
            }
        }

        return currentExpression;
    }
}
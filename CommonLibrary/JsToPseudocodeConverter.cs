using DocumentFormat.OpenXml.Presentation;
using Esprima;
using Esprima.Ast;
using System.Text;

public class JsToPseudocodeConverter
{
    public string Convert(string jsCode)
    {
        // Create parser options
        var parserOptions = new ParserOptions
        {
            Tolerant = true // Example option, adjust as needed
        };

        // Parse the JavaScript code into an AST
        var parser = new JavaScriptParser(parserOptions);
        var program = parser.ParseScript(jsCode);

        // Initialize a string builder for pseudocode
        var pseudocodeBuilder = new StringBuilder();

        // Traverse the AST and convert to pseudocode
        foreach (var statement in program.Body)
        {
            TraverseAst(statement, pseudocodeBuilder);
        }

        // Join the pseudocode lines into a single string
        return pseudocodeBuilder.ToString();
    }

    private void TraverseAst(Statement statement, StringBuilder builder)
    {
        switch (statement.Type)
        {
            case Nodes.BlockStatement:
                var blockStmt = (BlockStatement)statement;
                foreach (var node in blockStmt.Body)
                {
                    TraverseAst(node, builder);
                }
                break;

            case Nodes.VariableDeclaration:
                var decl = (VariableDeclaration)statement;
                foreach (var d in decl.Declarations)
                {
                    if (d.Id is Identifier identifier)
                    {
                        var initValue = d.Init is Literal literal ? literal.Value : "undefined";
                        if (d.Init is ArrayExpression arrayExpr)
                        {
                            var elements = string.Join(", ", arrayExpr.Elements.Select(e => ((Literal)e).Value.ToString()));
                            builder.AppendLine($"{identifier.Name}: {elements}");
                        }
                        else if (d.Init is CallExpression callExpr && callExpr.Callee is MemberExpression memberExpr && memberExpr.Property is Identifier prop && prop.Name == "replace")
                        {
                            builder.AppendLine($"Trim whitespace from {identifier.Name}");
                        }
                        else if (d.Init is CallExpression callExpr2 && callExpr2.Callee is MemberExpression memberExpr2 && memberExpr2.Property is Identifier prop2 && prop2.Name == "substring")
                        {
                            builder.AppendLine($"Extract the first {callExpr2.Arguments[1]} characters of {identifier.Name}");
                        }
                        else
                        {
                            //builder.AppendLine($"Set {identifier.Name} to {initValue}");
                        }
                    }
                }
                break;



            case Nodes.IfStatement:
                var ifStmt = (IfStatement)statement;
                var condition = ConvertExpression(ifStmt.Test);

                // Check if the consequent is a single expression statement
                if (ifStmt.Consequent is BlockStatement innerBlock && innerBlock.Body.Count == 1 &&
                    innerBlock.Body[0] is ExpressionStatement innerExprStmt &&
                    innerExprStmt.Expression is AssignmentExpression innerAssignExpr &&
                    innerAssignExpr.Left is MemberExpression innerMemberExpr &&
                    innerMemberExpr.Property is Identifier innerProp &&
                    innerProp.Name == "presence" &&
                    innerAssignExpr.Right is Literal innerLiteral &&
                    innerLiteral.Value?.ToString() == "visible")
                {
                    var target = ConvertExpression(innerMemberExpr.Object);
                    builder.AppendLine($"if {condition}\r\n\tShow {target}");
                }
                else
                {
                    builder.AppendLine($"if {condition}");
                    TraverseAst(ifStmt.Consequent, builder);
                }

                if (ifStmt.Alternate != null)
                {
                    builder.AppendLine("else");
                    TraverseAst(ifStmt.Alternate, builder);
                }
                break;

            case Nodes.ExpressionStatement:
                ConvertExpressionStatement((ExpressionStatement)statement, builder);
                break;
        }
    }

    private string ConvertExpression(Node node)
    {
        if (node is Expression expression)
        {
            switch (expression.Type)
            {
                case Nodes.BinaryExpression:
                    var binaryExpr = (BinaryExpression)expression;
                    var left = ConvertExpression(binaryExpr.Left);
                    var right = ConvertExpression(binaryExpr.Right);
                    var operatorStr = binaryExpr.Operator.ToString();
                    if ( binaryExpr.Operator == BinaryOperator.NotEqual) operatorStr = "Not Equal To";
                    if ( binaryExpr.Operator == BinaryOperator.Equal) operatorStr = "Equal To";
                    if ( binaryExpr.Operator == BinaryOperator.GreaterOrEqual) operatorStr = "Greater or Equal To";
                    if ( binaryExpr.Operator == BinaryOperator.LessOrEqual) operatorStr = "Less than or Equal To";
                    return $"{left} {operatorStr} {right}";

                case Nodes.Identifier:
                    return ((Identifier)expression).Name;

                case Nodes.Literal:
                    return ((Literal)expression).Value.ToString();

                case Nodes.MemberExpression:
                    var memberExpr = (MemberExpression)expression;
                    var obj = ConvertExpression(memberExpr.Object);
                    var prop = ConvertExpression(memberExpr.Property);

                    // Remove .rawValue
                    if (prop == "rawValue")
                        return obj;

                    return $"{obj}.{prop}";
            }
        }
        return node.ToString();
    }

    private void ConvertExpressionStatement(ExpressionStatement expressionStmt, StringBuilder builder)
    {
        var expression = expressionStmt.Expression;
        switch (expression.Type)
        {
            case Nodes.AssignmentExpression:
                var assignExpr = (AssignmentExpression)expression;
                var left = ConvertExpression(assignExpr.Left);
                var right = ConvertExpression(assignExpr.Right);

                // Skip hidden statements.
                if (assignExpr.Left is MemberExpression leftExpr &&
                    leftExpr.Property is Identifier leftProp &&
                    leftProp.Name == "presence" &&
                    assignExpr.Right is Literal rightLiteral &&
                    rightLiteral.Value?.ToString() == "hidden")
                {
                    // Skip this line
                    return;
                }

                builder.AppendLine($"Set {left} to {right}");
                break;

            case Nodes.CallExpression:
                var callExpr = (CallExpression)expression;
                var callee = ConvertExpression(callExpr.Callee);
                var args = string.Join(", ", callExpr.Arguments.Select(arg => ConvertExpression(arg)));
                builder.AppendLine($"Call {callee} with {args}");
                break;

            case Nodes.ConditionalExpression:
                var condExpr = (ConditionalExpression)expression;
                var test = ConvertExpression(condExpr.Test);
                var consequent = ConvertExpression(condExpr.Consequent);
                var alternate = ConvertExpression(condExpr.Alternate);
                builder.AppendLine($"if {test}\r\n\tSet to {consequent}\r\n(else)\r\n\tSet to {alternate}");
                break;
        }
    }
}

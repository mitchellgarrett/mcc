namespace FTG.Studios.MCC
{

	public static partial class SemanticAnalzyer
	{

		static void ResolveVariablesInFunction(VariableMap variable_map, ParseNode.Function function)
		{
			ResolveVariablesInBlock(variable_map, function.Body);
		}
		
		static ParseNode.Block ResolveVariablesInBlock(VariableMap variable_map, ParseNode.Block block)
		{
			variable_map = variable_map.Copy();
			for (int index = 0; index < block.Items.Count; index++)
			{
				if (block.Items[index] is ParseNode.Declaration declaration) block.Items[index] = ResolveVariablesInDeclaration(variable_map, declaration);
				if (block.Items[index] is ParseNode.Statement statement) block.Items[index] = ResolveVariablesInStatement(variable_map, statement);
			}

			return block;
		}

		static ParseNode.Declaration ResolveVariablesInDeclaration(VariableMap variable_map, ParseNode.Declaration declaration)
		{
			string unique_identifier = variable_map.InsertUniqueIdentifier(declaration.Identifier.Value);

			ParseNode.Expression resolved_initialization = null;
			if (declaration.Source != null) resolved_initialization = ResolveVariablesInExpression(variable_map, declaration.Source);

			return new ParseNode.Declaration(new ParseNode.Identifier(unique_identifier), resolved_initialization);
		}

		static ParseNode.Statement ResolveVariablesInStatement(VariableMap variable_map, ParseNode.Statement statement)
		{
			if (statement is ParseNode.Return return_statement)
			{
				return new ParseNode.Return(ResolveVariablesInExpression(variable_map, return_statement.Expression));
			}

			if (statement is ParseNode.If @if)
			{
				ParseNode.Expression condition = ResolveVariablesInExpression(variable_map, @if.Condition);
				ParseNode.Statement then = @if.Then != null ? ResolveVariablesInStatement(variable_map, @if.Then) : null;
				ParseNode.Statement @else = @if.Else != null ? ResolveVariablesInStatement(variable_map, @if.Else) : null;

				return new ParseNode.If(condition, then, @else);
			}

			if (statement is ParseNode.Block block) return ResolveVariablesInBlock(variable_map, block);
			if (statement is ParseNode.While @while) return ResolveVariablesInWhile(variable_map, @while);
			if (statement is ParseNode.DoWhile do_while) return ResolveVariablesInDoWhile(variable_map, do_while);
			if (statement is ParseNode.For @for) return ResolveVariablesInFor(variable_map.Copy(), @for);

			if (statement is ParseNode.Break || statement is ParseNode.Continue) return statement;
			
			if (statement is ParseNode.Expression expression)
			{
				return ResolveVariablesInExpression(variable_map, expression);
			}

			throw new SemanticAnalzyerException($"Unhandled statement type \"{statement}\"", statement.ToString());
		}

		static ParseNode.While ResolveVariablesInWhile(VariableMap variable_map, ParseNode.While @while)
		{
			ParseNode.Expression condition = ResolveVariablesInExpression(variable_map, @while.Condition);
			ParseNode.Statement body = ResolveVariablesInStatement(variable_map, @while.Body);

			return new ParseNode.While(condition, body);
		}
		
		static ParseNode.DoWhile ResolveVariablesInDoWhile(VariableMap variable_map, ParseNode.DoWhile do_while)
		{
			ParseNode.Expression condition = ResolveVariablesInExpression(variable_map, do_while.Condition);
			ParseNode.Statement body = do_while.Body != null ? ResolveVariablesInStatement(variable_map, do_while.Body) : null;

			return new ParseNode.DoWhile(condition, body);
		}
		
		static ParseNode.For ResolveVariablesInFor(VariableMap variable_map, ParseNode.For @for)
		{
			ParseNode.ForInitialization initialization;
			if (@for.Initialization is ParseNode.Declaration init_declaration) initialization = ResolveVariablesInDeclaration(variable_map, init_declaration);
			else if (@for.Initialization is ParseNode.Expression init_expression) initialization = ResolveVariablesInExpression(variable_map, init_expression);
			else if (@for.Initialization == null) initialization = null;
			else throw new SemanticAnalzyerException($"Unhandled for initialization type \"{@for.Initialization}\"", @for.Initialization.ToString());
			
			ParseNode.Expression condition = @for.Condition != null ? ResolveVariablesInExpression(variable_map, @for.Condition) : null;
			ParseNode.Expression post = @for.Post != null ? ResolveVariablesInExpression(variable_map, @for.Post) : null;
			ParseNode.Statement body = ResolveVariablesInStatement(variable_map, @for.Body);

			return new ParseNode.For(initialization, condition, post, body);
		}

		static ParseNode.Expression ResolveVariablesInExpression(VariableMap variable_map, ParseNode.Expression expression)
		{
			if (expression is ParseNode.Assignment assignment)
			{
				// Assignment definition must be a variable to be resolved
				if (!(assignment.Destination is ParseNode.Variable)) throw new SemanticAnalzyerException($"Assignment destination is not a variable ({assignment}).", assignment.ToString());
				return new ParseNode.Assignment(ResolveVariablesInExpression(variable_map, assignment.Destination), ResolveVariablesInExpression(variable_map, assignment.Source));
			}

			if (expression is ParseNode.Conditional conditional)
			{
				ParseNode.Expression condition = ResolveVariablesInExpression(variable_map, conditional.Condition);
				ParseNode.Expression then = ResolveVariablesInExpression(variable_map, conditional.Then);
				ParseNode.Expression @else = ResolveVariablesInExpression(variable_map, conditional.Else);

				return new ParseNode.Conditional(condition, then, @else);
			}

			if (expression is ParseNode.BinaryExpression binary) return new ParseNode.BinaryExpression(binary.Operator, ResolveVariablesInExpression(variable_map, binary.LeftExpression), ResolveVariablesInExpression(variable_map, binary.RightExpression));
			if (expression is ParseNode.Factor factor) return ResolveVariablesInFactor(variable_map, factor);

			throw new SemanticAnalzyerException($"Unhandled expression type \"{expression}\"", expression.ToString());
		}

		static ParseNode.Factor ResolveVariablesInFactor(VariableMap variable_map, ParseNode.Factor factor)
		{
			if (factor is ParseNode.Variable variable)
			{
				// Variable must be defined in order to be resolved
				string unique_identifier = variable_map.GetUniqueIdentifier(variable.Identifier.Value);
				return new ParseNode.Variable(new ParseNode.Identifier(unique_identifier));
			}

			if (factor is ParseNode.UnaryExpression unary) return new ParseNode.UnaryExpression(unary.Operator, ResolveVariablesInExpression(variable_map, unary.Expression));
			if (factor is ParseNode.Constant constant) return constant;

			throw new SemanticAnalzyerException($"Unhandled factor type \"{factor}\"", factor.ToString());
		}
	}
}
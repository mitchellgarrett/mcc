using FTG.Studios.MCC.Parser;

namespace FTG.Studios.MCC.SemanticAnalysis;

public static class TypeChecker
{
	public static SymbolTable CheckTypes(ParseTree tree)
	{
		SymbolTable symbol_table = new();
		CheckTypesInProgram(symbol_table, tree.Program);
		return symbol_table;
	}
	
	static void CheckTypesInProgram(SymbolTable symbol_table, ParseNode.Program program)
	{
		foreach (var declaration in program.Declarations)
		{
			if (declaration is ParseNode.FunctionDeclaration function_declaration) CheckTypesInFunctionDeclaration(symbol_table, function_declaration, false);
			if (declaration is ParseNode.VariableDeclaration variable_declaration) CheckTypesInFileScopeVariableDeclaration(symbol_table, variable_declaration);
		}
	}

	static void CheckTypesInFunctionDeclaration(SymbolTable symbol_table, ParseNode.FunctionDeclaration declaration, bool is_block_scope)
	{
		if (declaration.ParameterIdentifiers.Count != declaration.ParameterTypes.Count)
			throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has invalid parameters.", declaration.Identifier.Value);
		
		if (is_block_scope && declaration.StorageClass == StorageClass.Static)
			throw new SemanticAnalzyerException($"Block scope function \"{declaration.Identifier.Value}\" cannot be static.", declaration.Identifier.Value);
		
		bool is_global = declaration.StorageClass != StorageClass.Static;
		bool is_already_defined = false;
		if (symbol_table.TryGetSymbol(declaration.Identifier.Value, out SymbolTableEntry old_entry))
		{
			// Check if this identifier has previously been defined as a variable
			if (old_entry is not FunctionEntry function_entry)
				throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined as a variable.", declaration.Identifier.Value);
			
			// Check that both definitions have the same return type
			if (function_entry.ReturnType != declaration.ReturnType)
				throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined with return type {function_entry.ReturnType}, attemping to define again with return type {declaration.ReturnType}.", declaration.Identifier.Value);
			
			// Check that both definitions have the same number of parameters
			if (function_entry.ParameterTypes.Count != declaration.ParameterIdentifiers.Count)
				throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined with {function_entry.ParameterTypes.Count} parameters, attemping to define again with {declaration.ParameterTypes.Count} parameters.", declaration.Identifier.Value);
			// Check that both definitioons have the same parameters
			for (int i = 0; i < function_entry.ParameterTypes.Count; i++)
			{
				if (function_entry.ParameterTypes[i] != declaration.ParameterTypes[i])
					throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" has already been defined with different parameters.", declaration.Identifier.Value);
			}
			
			// A function body can only be defined once
			IdentifierAttributes.Function function_attributes = old_entry.Attributes as IdentifierAttributes.Function;
			is_already_defined = function_attributes.IsDefined;
			if (is_already_defined && declaration.Body != null) throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" can only have one body.", declaration.Identifier.Value);

			if (function_attributes.IsGlobal && declaration.StorageClass == StorageClass.Static) throw new SemanticAnalzyerException($"Function \"{declaration.Identifier.Value}\" defined as static and non-static.", declaration.Identifier.Value);
			is_global = function_attributes.IsGlobal;
		}
		
		bool is_defined = is_already_defined || declaration.Body != null;
		IdentifierAttributes.Function attributes = new(is_defined, is_global);
		symbol_table.AddFunction(declaration.Identifier.Value, attributes, declaration.ReturnType, declaration.ParameterTypes);

		if (declaration.Body == null) return;
		for (int i = 0; i < declaration.ParameterIdentifiers.Count; i++)
			symbol_table.AddVariable(declaration.ParameterIdentifiers[i].Value, new IdentifierAttributes.Local(), declaration.ParameterTypes[i]);

		// TODO: Do I need to copy the symbol table for each scope?
		CheckTypesInBlock(symbol_table, declaration.Body, declaration);
	}

	static void CheckTypesInFileScopeVariableDeclaration(SymbolTable symbol_table, ParseNode.VariableDeclaration declaration)
	{
		// Check if the initialization expression is a constant
		InitialValue initial_value;
		if (declaration.Source is ParseNode.Constant constant)
		{
			if (constant.ReturnType == PrimitiveType.Long) {
				// Handle case where variable is declared as an int but value is given as a long
				if (declaration.VariableType == PrimitiveType.Integer) initial_value = new InitialValue.Constant(PrimitiveType.Integer, int.CreateTruncating(constant.Value));
				else initial_value = new InitialValue.Constant(PrimitiveType.Long, constant.Value);
			}
			else
				initial_value = new InitialValue.Constant(constant.ReturnType, constant.Value);
		}
		else if (declaration.Source == null)
		{
			if (declaration.StorageClass == StorageClass.Extern) initial_value = new InitialValue.None();
			else initial_value = new InitialValue.Tentative();
		}
		else throw new SemanticAnalzyerException($"Assignment to variable \"{declaration.Identifier.Value}\" must be constant.", declaration.Identifier.Value);

		bool is_global = declaration.StorageClass != StorageClass.Static;

		if (symbol_table.TryGetSymbol(declaration.Identifier.Value, out SymbolTableEntry old_entry))
		{
			if (old_entry is not VariableEntry variable_entry) throw new SemanticAnalzyerException($"Variable \"{declaration.Identifier.Value}\" already defined as a function.", declaration.Identifier.Value);
			if (variable_entry.ReturnType != declaration.VariableType) throw new SemanticAnalzyerException($"Attempting to define variable \"{declaration.Identifier.Value}\" with type \"{declaration.VariableType}\", already defined with type \"{variable_entry.ReturnType}\".", declaration.Identifier.Value);
			if (declaration.StorageClass == StorageClass.Extern) is_global = (old_entry.Attributes as IdentifierAttributes.Static).IsGlobal;
			else if ((old_entry.Attributes as IdentifierAttributes.Static).IsGlobal != is_global)
				throw new SemanticAnalzyerException($"Variable \"{declaration.Identifier.Value}\" has conflicting linkage.", declaration.Identifier.Value);

			if ((old_entry.Attributes as IdentifierAttributes.Static).InitialValue is InitialValue.Constant)
			{
				if (initial_value is InitialValue.Constant) throw new SemanticAnalzyerException($"Conflicting definitons for variable \"{declaration.Identifier.Value}\".", declaration.Identifier.Value);
				else
				{
					initial_value = (old_entry.Attributes as IdentifierAttributes.Static).InitialValue;
				}
			}
			else if (initial_value is not InitialValue.Constant && (old_entry.Attributes as IdentifierAttributes.Static).InitialValue is InitialValue.Tentative)
			{
				initial_value = new InitialValue.Tentative();
			}
		}

		IdentifierAttributes attributes = new IdentifierAttributes.Static(initial_value, is_global);
		symbol_table.AddVariable(declaration.Identifier.Value, attributes, declaration.VariableType);
		// Do not typecheck static initializers because the source must be a constant value
	}

	static void CheckTypesInBlockScopeVariableDeclaration(SymbolTable symbol_table, ParseNode.VariableDeclaration declaration)
	{
		if (declaration.StorageClass == StorageClass.Extern)
		{
			if (declaration.Source != null) throw new SemanticAnalzyerException($"Extern variable \"{declaration.Identifier.Value}\" cannot have an initializer.", declaration.Identifier.Value);
			if (symbol_table.TryGetSymbol(declaration.Identifier.Value, out SymbolTableEntry old_entry))
			{
				if (old_entry is not VariableEntry variable_entry) throw new SemanticAnalzyerException($"Variable \"{declaration.Identifier.Value}\" already defined as a function.", declaration.Identifier.Value);
				if (variable_entry.ReturnType != declaration.VariableType) throw new SemanticAnalzyerException($"Attempting to define variable \"{declaration.Identifier.Value}\" with type \"{declaration.VariableType}\", already defined with type \"{variable_entry.ReturnType}\".", declaration.Identifier.Value);

			}
			else
			{
				symbol_table.AddVariable(declaration.Identifier.Value, new IdentifierAttributes.Static(new InitialValue.None(), true), declaration.VariableType);
			}
		}
		else if (declaration.StorageClass == StorageClass.Static)
		{
			InitialValue initial_value;
			if (declaration.Source is ParseNode.Constant constant) initial_value = new InitialValue.Constant(constant.ReturnType, constant.Value);
			else if (declaration.Source == null) initial_value = new InitialValue.Constant(PrimitiveType.Integer, 0);
			else throw new SemanticAnalzyerException($"Static variable \"{declaration.Identifier.Value}\" cannot have a non-constant initializer.", declaration.Identifier.Value);
			
			symbol_table.AddVariable(declaration.Identifier.Value, new IdentifierAttributes.Static(initial_value, false), declaration.VariableType);
		}
		else
		{
			// TODO: I don't think ths works
			if (symbol_table.TryGetSymbol(declaration.Identifier.Value, out SymbolTableEntry old_entry))
			{
				if (old_entry.Attributes is IdentifierAttributes.Static) throw new SemanticAnalzyerException($"Static variable \"{declaration.Identifier.Value}\" redefined as a local variable.", declaration.Identifier.Value);
			}
			
			symbol_table.AddVariable(declaration.Identifier.Value, new IdentifierAttributes.Local(), declaration.VariableType);
		}
		if (declaration.Source != null) {
			CheckTypesInExpression(symbol_table, declaration.Source);
			declaration.Source = ConvertExpressionTo(declaration.Source, declaration.VariableType);
		}
	}

	static void CheckTypesInBlock(SymbolTable symbol_table, ParseNode.Block block, ParseNode.FunctionDeclaration parent_function)
	{
		foreach (var item in block.Items)
		{
			if (item is ParseNode.FunctionDeclaration function_declaration)
			{
				// Nested function definitions are not allowed
				if (function_declaration.Body != null) throw new SemanticAnalzyerException($"Nested definition for function \"{function_declaration.Identifier.Value}\" is not allowed.", function_declaration.Identifier.Value);
				CheckTypesInFunctionDeclaration(symbol_table, function_declaration, true);
			}
			if (item is ParseNode.VariableDeclaration variable_declaration) CheckTypesInBlockScopeVariableDeclaration(symbol_table, variable_declaration);
			if (item is ParseNode.Statement statement) CheckTypesInStatement(symbol_table, statement, parent_function);
		}
	}

	static void CheckTypesInStatement(SymbolTable symbol_table, ParseNode.Statement statement, ParseNode.FunctionDeclaration parent_function)
	{
		if (statement is ParseNode.Return @return)
		{
			CheckTypesInExpression(symbol_table, @return.Expression);
			@return.Expression = ConvertExpressionTo(@return.Expression, parent_function.ReturnType);
			return;
		}

		if (statement is ParseNode.If @if)
		{
			CheckTypesInExpression(symbol_table, @if.Condition);
			if (@if.Then != null) CheckTypesInStatement(symbol_table, @if.Then, parent_function);
			if (@if.Else != null) CheckTypesInStatement(symbol_table, @if.Else, parent_function);

			return;
		}

		if (statement is ParseNode.Block block)
		{
			CheckTypesInBlock(symbol_table, block, parent_function);
			return;
		}

		if (statement is ParseNode.While @while)
		{
			CheckTypesInExpression(symbol_table, @while.Condition);
			CheckTypesInStatement(symbol_table, @while.Body, parent_function);
			return;
		}

		if (statement is ParseNode.DoWhile do_while)
		{
			CheckTypesInExpression(symbol_table, do_while.Condition);
			if (do_while.Body != null) CheckTypesInStatement(symbol_table, do_while.Body, parent_function);
			return;
		}

		if (statement is ParseNode.For @for)
		{
			if (@for.Initialization is ParseNode.VariableDeclaration init_declaration) CheckTypesInBlockScopeVariableDeclaration(symbol_table, init_declaration);
			else if (@for.Initialization is ParseNode.Expression init_expression) CheckTypesInExpression(symbol_table, init_expression);
			else if (@for.Initialization != null) throw new SemanticAnalzyerException($"Unhandled for initialization type \"{@for.Initialization}\"", @for.Initialization.ToString());

			if (@for.Condition != null) CheckTypesInExpression(symbol_table, @for.Condition);
			if (@for.Post != null) CheckTypesInExpression(symbol_table, @for.Post);
			CheckTypesInStatement(symbol_table, @for.Body, parent_function);
			return;
		}

		if (statement is ParseNode.Break || statement is ParseNode.Continue) return;

		if (statement is ParseNode.Expression expression)
		{
			CheckTypesInExpression(symbol_table, expression);
			return;
		}

		throw new SemanticAnalzyerException($"Unhandled statement type \"{statement}\"", statement.ToString());
	}

	static void CheckTypesInExpression(SymbolTable symbol_table, ParseNode.Expression expression)
	{
		// TODO: Refactor these into separate functions
		if (expression is ParseNode.Variable variable)
		{
			if (!symbol_table.TryGetSymbol(variable.Identifier.Value, out SymbolTableEntry entry))
				throw new SemanticAnalzyerException($"Variable \"{variable.Identifier.Value}\" does not exist.", variable.Identifier.Value);
			
			if (entry is not VariableEntry) throw new SemanticAnalzyerException($"Identifier \"{variable.Identifier.Value}\" is not a variable.", variable.Identifier.Value);
			
			variable.ReturnType = entry.ReturnType;
			return;
		}
		
		if (expression is ParseNode.Cast cast)
		{
			// Casts cannot be applied to assignments
			if (cast.Expression is ParseNode.Assignment)
				throw new SemanticAnalzyerException($"Cast \"{cast}\" cannot be applied to an assignment.", "");
			
			CheckTypesInExpression(symbol_table, cast.Expression);
			// The return type of the cast itself is set by the parser
			return;
		}

		if (expression is ParseNode.FunctionCall function_call)
		{
			if (!symbol_table.TryGetSymbol(function_call.Identifier.Value, out SymbolTableEntry entry))
				throw new SemanticAnalzyerException($"Function \"{function_call.Identifier.Value}\" does not exist.", function_call.Identifier.Value);

			if (entry is not FunctionEntry function_entry) throw new SemanticAnalzyerException($"Variable \"{function_call.Identifier.Value}\" is being used like a function.", function_call.Identifier.Value);

			// TODO: Eventually this will actually check that the arguments match
			if (function_entry.ParameterTypes.Count != function_call.Arguments.Count) throw new SemanticAnalzyerException($"Function \"{function_call.Identifier.Value}\" expects {function_entry.ParameterTypes.Count} arguments, got {function_call.Arguments.Count}.", function_call.Identifier.Value);
			
			for (int i = 0; i < function_call.Arguments.Count; i++)
			{
				CheckTypesInExpression(symbol_table, function_call.Arguments[i]);
				function_call.Arguments[i] = ConvertExpressionTo(function_call.Arguments[i], function_entry.ParameterTypes[i]);
			}
				
			function_call.ReturnType = entry.ReturnType;
			return;
		}

		if (expression is ParseNode.Assignment assignment)
		{
			CheckTypesInExpression(symbol_table, assignment.Destination);
			CheckTypesInExpression(symbol_table, assignment.Source);
			assignment.Source = ConvertExpressionTo(assignment.Source, assignment.Destination.ReturnType);
			assignment.ReturnType = assignment.Destination.ReturnType;
			return;
		}

		if (expression is ParseNode.Conditional conditional)
		{
			CheckTypesInExpression(symbol_table, conditional.Condition);
			CheckTypesInExpression(symbol_table, conditional.Then);
			CheckTypesInExpression(symbol_table, conditional.Else);
			
			PrimitiveType common_type = GetCommonType(conditional.Then.ReturnType, conditional.Else.ReturnType);
			
			conditional.Then = ConvertExpressionTo(conditional.Then, common_type);
			conditional.Else = ConvertExpressionTo(conditional.Else, common_type);
			conditional.ReturnType = common_type;
			return;
		}

		if (expression is ParseNode.BinaryExpression binary)
		{
			CheckTypesInExpression(symbol_table, binary.LeftExpression);
			CheckTypesInExpression(symbol_table, binary.RightExpression);
			
			switch (binary.Operator)
			{
				case Lexer.Syntax.BinaryOperator.LogicalAnd:
				case Lexer.Syntax.BinaryOperator.LogicalOr:
					binary.ReturnType = PrimitiveType.Integer;
					return;
			}

			PrimitiveType common_type = GetCommonType(binary.LeftExpression.ReturnType, binary.RightExpression.ReturnType);
			
			binary.LeftExpression = ConvertExpressionTo(binary.LeftExpression, common_type);
			binary.RightExpression = ConvertExpressionTo(binary.RightExpression, common_type);
			
			switch (binary.Operator)
			{
				case Lexer.Syntax.BinaryOperator.Addition:
				case Lexer.Syntax.BinaryOperator.Subtraction:
				case Lexer.Syntax.BinaryOperator.Multiplication:
				case Lexer.Syntax.BinaryOperator.Division:
				case Lexer.Syntax.BinaryOperator.Remainder:
					binary.ReturnType = common_type;
					return;
				default:
					binary.ReturnType = PrimitiveType.Integer;
					return;
			}
		}
		
		if (expression is ParseNode.UnaryExpression unary)
		{
			CheckTypesInExpression(symbol_table, unary.Expression);
			switch (unary.Operator)
			{
				case Lexer.Syntax.UnaryOperator.Not:
					unary.ReturnType = PrimitiveType.Integer;
					return;
				default:
					unary.ReturnType = unary.Expression.ReturnType;
					return;
			}
		}
		
		// Return types for constants are set by the parser
		if (expression is ParseNode.Constant constant) return;
		
		throw new SemanticAnalzyerException($"Unhandled expression type \"{expression}\"", expression.ToString());
	}
	
	static PrimitiveType GetCommonType(PrimitiveType a, PrimitiveType b)
	{
		if (a == b) return a;
		
		if (a.GetSize() == b.GetSize())
			if (a.IsSigned()) return b;
		else return a;
		
		if (a.GetSize() > b.GetSize())
			return a;
		else return b;
	}
	
	static ParseNode.Expression ConvertExpressionTo(ParseNode.Expression expression, PrimitiveType target_type)
	{
		if (expression.ReturnType == target_type) return expression;
		return new ParseNode.Cast(target_type, expression);
	}
}
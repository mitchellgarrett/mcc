using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FTG.Studios.MCC.Lexer;

namespace FTG.Studios.MCC.Parser;

public static partial class Parser
{	
	static readonly List<Syntax.Keyword> valid_specifiers = [Syntax.Keyword.Integer, Syntax.Keyword.Long, Syntax.Keyword.Signed, Syntax.Keyword.Unsigned, Syntax.Keyword.Static, Syntax.Keyword.Extern];
	static readonly List<Syntax.Keyword> valid_type_specifiers = [Syntax.Keyword.Integer, Syntax.Keyword.Long, Syntax.Keyword.Signed, Syntax.Keyword.Unsigned];
	
	public static ParseTree Parse(List<Token> tokens) {
		LinkedList<Token> stream = new(tokens);
		ParseNode.Program program = ParseProgram(stream);

		if (stream.Count > 0) throw new ParserException($"Unexpected token at end of file: {stream.First()}", stream.First());
		
		return new ParseTree(program);
	}
	
	/// <summary>
	/// Program ::= { Declaration }
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.Program ParseProgram(LinkedList<Token> tokens)
	{
		List<ParseNode.Declaration> declarations = [];
		while (tokens.Count > 0) declarations.Add(ParseDeclaration(tokens));
		return new ParseNode.Program(declarations);
	}

	/// <summary>
	/// Declaration ::= FunctionDeclaration | VariableDeclaration
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.Declaration ParseDeclaration(LinkedList<Token> tokens)
	{
		// Parse Type and StorageClass to be passed along
		(PrimitiveType type, StorageClass storage_class) = ParseTypeAndStorageClass(tokens);
		
		// Check to see if the identifier is followed by a '('
		Token identifier = tokens.Dequeue();
		Expect(identifier, TokenType.Identifier);

		Token next_token = tokens.Peek();
		tokens.AddFirst(identifier);
		
		// If the next token is an open parethensis, it is a function declaration
		if (Match(next_token, TokenType.OpenParenthesis)) return ParseFunctionDeclaration(tokens, type, storage_class);
		// Otherwise, it is a variable declaration
		return ParseVariableDeclaration(tokens, type, storage_class);
	}
	
	/// <summary>
	/// FunctionDeclaration ::= { Specifier }+ Identifier "(" ParameterList ")" ( Block | ";" )
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.FunctionDeclaration ParseFunctionDeclaration(LinkedList<Token> tokens, PrimitiveType return_type, StorageClass storage_class)
	{
		ParseNode.Identifier identifier = ParseIdentifier(tokens);

		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);
		
		(List<ParseNode.Identifier> parameter_identifiers, List<PrimitiveType> parameter_types) = ParseParameterList(tokens);

		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

		ParseNode.Block body = null;
		if (Match(tokens.Peek(), TokenType.Semicolon)) tokens.Dequeue();
		else body = ParseBlock(tokens);

		return new ParseNode.FunctionDeclaration(identifier, return_type, storage_class, parameter_identifiers, parameter_types, body);
	}

	/// <summary>
	/// VariableDeclaration ::= { Specifier }+ Identifier [ "=" Expression ] ";"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.VariableDeclaration ParseVariableDeclaration(LinkedList<Token> tokens, PrimitiveType type, StorageClass storage_class)
	{
		ParseNode.Identifier identifier = ParseIdentifier(tokens);

		// Optional initialization
		ParseNode.Expression expression = null;
		if (Match(tokens.Peek(), TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment))
		{
			// Remove '=" from LinkedList
			tokens.Dequeue();

			// Parse initialization expression
			expression = ParseExpression(tokens, 0);
		}

		Expect(tokens.Dequeue(), TokenType.Semicolon);
		
		return new ParseNode.VariableDeclaration(identifier, type, storage_class, expression);
	}

	/// <summary>
	/// Specifier ::= TypeSpecifier | "static" | "extern"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static List<Syntax.Keyword> ParseSpecifierList(LinkedList<Token> tokens)
	{
		List<Syntax.Keyword> specifiers = [];
		while (Match(tokens.Peek(), TokenType.Keyword) && valid_specifiers.Contains((Syntax.Keyword)tokens.Peek().Value))
			specifiers.Add((Syntax.Keyword)tokens.Dequeue().Value);
		return specifiers;
	}
	
	/// <summary>
	/// TypeSpecifier ::= "int" | "long"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static PrimitiveType ParseTypeSpecifier(LinkedList<Token> tokens)
	{
		List<Syntax.Keyword> specifiers = [];
		while (Match(tokens.Peek(), TokenType.Keyword) && valid_type_specifiers.Contains((Syntax.Keyword)tokens.Peek().Value))
		{
			Syntax.Keyword specifier = (Syntax.Keyword)tokens.Dequeue().Value;
			specifiers.Add(specifier);
		}
		
		return ParseTypeSpecifierFromList(specifiers);
	}
	
	static PrimitiveType ParseTypeSpecifierFromList(List<Syntax.Keyword> specifiers)
	{
		// The list of specifiers cannot be empty, have both 'signed' and 'unsigned', or contain the same specifier twice
		if (
			specifiers.Count == 0 ||
			(specifiers.Contains(Syntax.Keyword.Signed) && specifiers.Contains(Syntax.Keyword.Unsigned)) ||
			specifiers.Count != new HashSet<Syntax.Keyword>(specifiers).Count
		)
		throw new ParserException($"Invalid specifier \"{string.Join(", ", specifiers)}\".", Token.Invalid(-1));
		
		// If the list contains 'unsigned' and 'long', it's an unsigned long
		if (specifiers.Contains(Syntax.Keyword.Unsigned) && specifiers.Contains(Syntax.Keyword.Long)) return PrimitiveType.UnsignedLong;
		// If the list contains 'unsigned' but not 'long', it's an unsigned int
		if (specifiers.Contains(Syntax.Keyword.Unsigned)) return PrimitiveType.UnsignedInteger;
		// If the list contains 'long' but not 'unsigned', it's a long
		if (specifiers.Contains(Syntax.Keyword.Long)) return PrimitiveType.Long;
		// If the list does not contain 'unsigned' or 'long', it's an int
		return PrimitiveType.Integer;
	}
	
	/// <summary>
	/// Type ::= "int"
	/// StorageClass ::= "static" | "extern"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static (PrimitiveType, StorageClass) ParseTypeAndStorageClass(LinkedList<Token> tokens)
	{
		List<Syntax.Keyword> specifiers = ParseSpecifierList(tokens);

		List<Syntax.Keyword> types = [];
		List<StorageClass> storage_classes = [];

		foreach (var specifier in specifiers)
		{
			// Check if Type
			if (valid_type_specifiers.Contains(specifier)) types.Add(specifier);
			// Check if StorageClass
			else if (Enum.GetNames<StorageClass>().Contains(specifier.ToString())) storage_classes.Add(Enum.Parse<StorageClass>(specifier.ToString()));
			// Otherwise, throw error
			else throw new ParserException($"Invalid specifier \"{specifier}\".", Token.Invalid(-1));
		}

		// There should only be one Type and at most one StorageClass
		// TODO: Make the error message better
		if (storage_classes.Count > 1) throw new ParserException($"Identifier given multiple storage classes.", Token.Invalid(-1));

		PrimitiveType type = ParseTypeSpecifierFromList(types);
		StorageClass storage_class = storage_classes.Count == 1 ? storage_classes[0] : StorageClass.None;

		return (type, storage_class);
	}

	/// <summary>
	/// ParameterList ::= "void" | { TypeSpecifier }+ Identifier { "," { TypeSpecifiier }+ Identifier }
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static (List<ParseNode.Identifier>, List<PrimitiveType>) ParseParameterList(LinkedList<Token> tokens)
	{
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Void))
		{
			tokens.Dequeue();
			return ([], []);
		}
		
		List<ParseNode.Identifier> parameter_identifiers = [];
		List<PrimitiveType> parameter_types = [];
		Token last_token;
		do {
			while (!Match(tokens.Peek(), TokenType.Identifier)) parameter_types.Add(ParseTypeSpecifier(tokens));
			parameter_identifiers.Add(ParseIdentifier(tokens));
		} while (Match(last_token = tokens.Dequeue(), TokenType.Comma));
		tokens.AddFirst(last_token);

		return (parameter_identifiers, parameter_types);
	}
	
	/// <summary>
	/// BlockItem ::= Statement | Declaration
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.BlockItem ParseBlockItem(LinkedList<Token> tokens)
	{
		// Check if the current token is a declaration specifier. If so, it is a function or variable declaration
		if (Match(tokens.Peek(), TokenType.Keyword) && valid_specifiers.Contains((Syntax.Keyword)tokens.Peek().Value)) return ParseDeclaration(tokens);
		// If not, it is a statement
		return ParseStatement(tokens);
	}
	
	/// <summary>
	/// Statement ::= "return" Expression ";" 
	/// | Expression ";" 
	/// | "if" "(" Expression ")" Statement [ "else" Statement ]
	/// | Block
	/// | "break" ";"
	/// | "continue" ";"
	/// | "while" "(" Expression ")" Statement
	/// | "do" Statement "while" "(" Expression ")" ";"
	/// | "for" "(" [ ForInitialization ] ";" [ Expression ] ";" [ Expression ] ")" Statement
	/// | ";"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.Statement ParseStatement(LinkedList<Token> tokens)
	{
		if (Match(tokens.Peek(), TokenType.Semicolon))
		{
			tokens.Dequeue();
			return null;
		}
		
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Return)) return ParseReturnStatement(tokens);
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.If)) return ParseIfStatement(tokens);
		if (Match(tokens.Peek(), TokenType.OpenBrace)) return ParseBlock(tokens);
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Break))
		{
			tokens.Dequeue();
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			return new ParseNode.Break();
		}
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Continue))
		{
			tokens.Dequeue();
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			return new ParseNode.Continue();
		}
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.While)) return ParseWhileStatement(tokens);
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Do)) return ParseDoWhileStatement(tokens);
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.For)) return ParseForStatement(tokens);
		
		ParseNode.Expression expression = ParseExpression(tokens, 0);
		Expect(tokens.Dequeue(), TokenType.Semicolon);
		return expression;
	}
	
	static ParseNode.Return ParseReturnStatement(LinkedList<Token> tokens) {
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Return);
		
		ParseNode.Expression expression = ParseExpression(tokens, 0);
		
		Expect(tokens.Dequeue(), TokenType.Semicolon);
		
		return new ParseNode.Return(expression);
	}
	
	static ParseNode.If ParseIfStatement(LinkedList<Token> tokens) {
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.If);
		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);
		
		ParseNode.Expression condition = ParseExpression(tokens, 0);
		
		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

		ParseNode.Statement then = ParseStatement(tokens);

		ParseNode.Statement @else = null;
		if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Else)) {
			tokens.Dequeue();
			@else = ParseStatement(tokens);
		}

		return new ParseNode.If(condition, then, @else);
	}

	static ParseNode.While ParseWhileStatement(LinkedList<Token> tokens)
	{
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.While);
		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);

		ParseNode.Expression condition = ParseExpression(tokens, 0);

		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

		ParseNode.Statement body = ParseStatement(tokens);

		return new ParseNode.While(condition, body);
	}
	
	static ParseNode.DoWhile ParseDoWhileStatement(LinkedList<Token> tokens)
	{
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Do);
		
		ParseNode.Statement body = ParseStatement(tokens);
		
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.While);
		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);

		ParseNode.Expression condition = ParseExpression(tokens, 0);

		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
		Expect(tokens.Dequeue(), TokenType.Semicolon);

		return new ParseNode.DoWhile(condition, body);
	}
	
	static ParseNode.For ParseForStatement(LinkedList<Token> tokens)
	{
		Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.For);
		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);

		ParseNode.ForInitialization initialization = null;
		if (!Match(tokens.Peek(), TokenType.Semicolon)) initialization = ParseForInitialization(tokens);
		else Expect(tokens.Dequeue(), TokenType.Semicolon);
		
		ParseNode.Expression condition = null;
		if (!Match(tokens.Peek(), TokenType.Semicolon)) condition = ParseExpression(tokens, 0);
		
		Expect(tokens.Dequeue(), TokenType.Semicolon);

		ParseNode.Expression post = null;
		if (!Match(tokens.Peek(), TokenType.CloseParenthesis)) post = ParseExpression(tokens, 0);
		
		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

		ParseNode.Statement body = ParseStatement(tokens);

		return new ParseNode.For(initialization, condition, post, body);
	}

	/// <summary>
	/// ForInitialization ::= VariableDeclaratiion | [ Expression ] ";"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.ForInitialization ParseForInitialization(LinkedList<Token> tokens)
	{
		// If first token is a type then this is a variable declaration
		if (Match(tokens.Peek(), TokenType.Keyword) && valid_type_specifiers.Contains((Syntax.Keyword)tokens.Peek().Value))
		{
			ParseNode.Declaration declaration = ParseDeclaration(tokens);
			if (declaration is ParseNode.VariableDeclaration variable_declaration)
			{
				if (variable_declaration.StorageClass != StorageClass.None) throw new ParserException("For loop initialization cannot have storage class specifiers.", tokens.Peek());
				return variable_declaration;
			}
			// TODO: Make this better
			throw new ParserException("Function declaration given as for loop initialization.", tokens.Peek());
		}
		// Otherwise it is an expression
			ParseNode.Expression expression = ParseExpression(tokens, 0);
		Expect(tokens.Dequeue(), TokenType.Semicolon);
		return expression;
	}
	
	/// <summary>
	/// Block ::= "{" { BlockItem } "}"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.Block ParseBlock(LinkedList<Token> tokens)
	{
		Expect(tokens.Dequeue(), TokenType.OpenBrace);

		List<ParseNode.BlockItem> items = [];
		while (!Match(tokens.Peek(), TokenType.CloseBrace))
		{
			ParseNode.BlockItem item = ParseBlockItem(tokens);
			if (item != null) items.Add(item);
		}

		Expect(tokens.Dequeue(), TokenType.CloseBrace);

		return new ParseNode.Block(items);
	}
	
	/// <summary>
	/// Expression ::= Factor | Expression BinaryOperator Expression | Expression "?" Expression ":" Expression
	/// </summary>
	/// <param name="tokens"></param>
	/// <param name="min_precedence"></param>
	/// <returns></returns>
	static ParseNode.Expression ParseExpression(LinkedList<Token> tokens, int min_precedence)
	{	
		ParseNode.Expression left_expression = ParseFactor(tokens);

		int current_precedence;
		while (Match(tokens.Peek(), TokenType.BinaryOperator) && (current_precedence = ((Syntax.BinaryOperator)tokens.Peek().Value).GetPrecedence()) >= min_precedence)
		{
			Token @operator = tokens.Dequeue();

			if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment))
			{
				ParseNode.Expression right_expression = ParseExpression(tokens, current_precedence);
				left_expression = new ParseNode.Assignment(left_expression, right_expression);
			}
			else if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.ConditionalTrue))
			{
				ParseNode.Expression then_expression = ParseExpression(tokens, 0);

				Expect(tokens.Dequeue(), TokenType.BinaryOperator, Syntax.BinaryOperator.ConditionalFalse);

				ParseNode.Expression else_expression = ParseExpression(tokens, current_precedence);
				left_expression = new ParseNode.Conditional(left_expression, then_expression, else_expression);
			}
			else
			{
				ParseNode.Expression right_expression = ParseExpression(tokens, current_precedence + 1);
				left_expression = new ParseNode.BinaryExpression((Syntax.BinaryOperator)@operator.Value, left_expression, right_expression);
			}
		}

		return left_expression;
	}
	
	/// <summary>
	/// Factor ::= Constant | Identifier | FunctionCall | "(" { TypeSpecifier }+ ")" Factor | UnaryOperator Factor | "(" Expression ")"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.Expression ParseFactor(LinkedList<Token> tokens)
	{
		// Parse constants
		if (Match(tokens.Peek(), TokenType.IntegerConstant)) return ParseConstantExpression(tokens);
		if (Match(tokens.Peek(), TokenType.LongConstant)) return ParseConstantExpression(tokens);
		if (Match(tokens.Peek(), TokenType.UnsignedIntegerConstant)) return ParseConstantExpression(tokens);
		if (Match(tokens.Peek(), TokenType.UnsignedLongConstant)) return ParseConstantExpression(tokens);
		
		// Parse Identifiers and FunctionCalls
		if (Match(tokens.Peek(), TokenType.Identifier)) {
			Token identifier = tokens.Dequeue();
			Token next_token = tokens.Peek();
			tokens.AddFirst(identifier);
			
			// If an identifier is followed by an open parenthesis, it is a function call
			if (Match(next_token, TokenType.OpenParenthesis))
				return ParseFunctionCall(tokens);
			else
				return ParseVariable(tokens);
		}
		
		// Parse unary operations
		if (Match(tokens.Peek(), TokenType.UnaryOperator) || Match(tokens.Peek(), TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction)) return ParseUnaryExpression(tokens);
		
		// Parse expresses and casts
		if (Match(tokens.Peek(), TokenType.OpenParenthesis))
		{
			tokens.Dequeue();
			
			// If the inside of the parentheses is a type, then this is a cast
			if (Match(tokens.Peek(), TokenType.Keyword) && valid_type_specifiers.Contains((Syntax.Keyword)tokens.Peek().Value))
			{
				PrimitiveType type = ParseTypeSpecifier(tokens);
				Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
				// Expression precedence must be high when parsing casts because the cast has higher priority than binary operators
				return new ParseNode.Cast(type, ParseExpression(tokens, Syntax.BinaryOperator.Multiplication.GetPrecedence() + 1));
			}
			
			// Otherwise it is an expression wrapped in parentheses
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
			return expression;
		}

		throw new ParserException($"Expected: constant or variable, got: {tokens.Peek()}", tokens.Peek());
	}

	/// <summary>
	/// FunctionCall ::= Identifier "(" [ ArgumentList ] ")"
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static ParseNode.FunctionCall ParseFunctionCall(LinkedList<Token> tokens)
	{
		ParseNode.Identifier identifier = ParseIdentifier(tokens);
		Expect(tokens.Dequeue(), TokenType.OpenParenthesis);
		List<ParseNode.Expression> arguments = ParseArgumentList(tokens);
		Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

		return new ParseNode.FunctionCall(identifier, arguments);
	}

	/// <summary>
	/// ArgumentList ::= Expression { "," Expression }
	/// </summary>
	/// <param name="tokens"></param>
	/// <returns></returns>
	static List<ParseNode.Expression> ParseArgumentList(LinkedList<Token> tokens)
	{
		List<ParseNode.Expression> arguments = [];

		if (!Match(tokens.Peek(), TokenType.CloseParenthesis)) arguments.Add(ParseExpression(tokens, 0));

		while (Match(tokens.Peek(), TokenType.Comma))
		{
			tokens.Dequeue();
			arguments.Add(ParseExpression(tokens, 0));
		}

		return arguments;
	}
	
	static ParseNode.UnaryExpression ParseUnaryExpression(LinkedList<Token> tokens)
	{
		Token @operator = tokens.Dequeue();

		// Convert binary subtraction operator to negation
		if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction))
		{
			@operator = new Token(@operator.Line, TokenType.UnaryOperator, Syntax.UnaryOperator.Negation);
		}
		Expect(@operator, TokenType.UnaryOperator);

		ParseNode.Expression expression = ParseFactor(tokens);

		return new ParseNode.UnaryExpression((Syntax.UnaryOperator)@operator.Value, expression);
	}
	
	static ParseNode.Constant ParseConstantExpression(LinkedList<Token> tokens) {
		Token token = tokens.Dequeue();
		if (Match(token, TokenType.IntegerConstant) || Match(token, TokenType.LongConstant))
		{
			BigInteger value = (BigInteger)token.Value;
			// Value must be smaller than the max long value
			if (value > long.MaxValue) throw new ParserException($"Integer value: {token.Value} is too big to be represented as an 'int' or 'long'.", token);
			
			if (Match(token, TokenType.IntegerConstant) && value <= int.MaxValue) return new ParseNode.Constant(PrimitiveType.Integer, int.CreateTruncating(value));
			return new ParseNode.Constant(PrimitiveType.Long, long.CreateTruncating(value));
		}
		
		if (Match(token, TokenType.UnsignedIntegerConstant) || Match(token, TokenType.UnsignedLongConstant))
		{
			BigInteger value = (BigInteger)token.Value;
			// Value must be smaller than the max unsigned long value
			if (value > ulong.MaxValue) throw new ParserException($"Integer value: {token.Value} is too big to be represented as a 'uint' or 'ulong'.", token);
			
			if (Match(token, TokenType.UnsignedIntegerConstant) && value <= uint.MaxValue) return new ParseNode.Constant(PrimitiveType.UnsignedInteger, uint.CreateTruncating(value));
			return new ParseNode.Constant(PrimitiveType.UnsignedLong, ulong.CreateTruncating(value));
		}
		
		throw new ParserException($"Invalid constant: {token.Value}", token);
	}

	static ParseNode.Variable ParseVariable(LinkedList<Token> tokens)
	{
		ParseNode.Identifier identifier = ParseIdentifier(tokens);
		return new ParseNode.Variable(identifier);
	}
	
	static ParseNode.Identifier ParseIdentifier(LinkedList<Token> tokens)
	{
		Token token = tokens.Dequeue();
		Expect(token, TokenType.Identifier);
		return new ParseNode.Identifier((string)token.Value);
	}
}
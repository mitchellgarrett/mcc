using System;
using System.Collections.Generic;
using System.Linq;

namespace FTG.Studios.MCC
{
	
	public static class Parser {
		
		public static ParseTree Parse(List<Token> tokens) {
			Queue<Token> stream = new Queue<Token>(tokens);
			ParseNode.Program program = ParseProgram(stream);

			if (stream.Count > 0) throw new ParserException($"Unexpected token at end of file: {stream.First()}", stream.First());
			
			return new ParseTree(program);
		}
		
		/// <summary>
		/// Program ::= Function
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.Program ParseProgram(Queue<Token> tokens)
		{
			ParseNode.Function function = ParseFunction(tokens);
			return new ParseNode.Program(function);
		}
		
		/// <summary>
		/// Function ::= "int" Identifier "(" "void" ")" "{" { BlockItem } "}"
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.Function ParseFunction(Queue<Token> tokens)
		{
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Integer);

			ParseNode.Identifier identifier = ParseIdentifier(tokens);

			Expect(tokens.Dequeue(), TokenType.OpenParenthesis);

			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Void);

			Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
			Expect(tokens.Dequeue(), TokenType.OpenBrace);

			List<ParseNode.BlockItem> body = new List<ParseNode.BlockItem>();
			while (!Match(tokens.Peek(), TokenType.CloseBrace))
			{
				ParseNode.BlockItem item = ParseBlockItem(tokens);
				if (item != null) body.Add(item);
			}

			Expect(tokens.Dequeue(), TokenType.CloseBrace);

			return new ParseNode.Function(identifier, body);
		}
		
		/// <summary>
		/// BlockItem ::= Statement | Declaration
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.BlockItem ParseBlockItem(Queue<Token> tokens)
		{
			if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Integer)) return ParseDeclaration(tokens);
			return ParseStatement(tokens);
		}
		
		/// <summary>
		/// Declaration ::= "int" Identifier [ "=" Expression ] ";"
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.Declaration ParseDeclaration(Queue<Token> tokens)
		{
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Integer);

			ParseNode.Identifier identifier = ParseIdentifier(tokens);

			// Optional initialization
			ParseNode.Expression expression = null;
			if (Match(tokens.Peek(), TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment))
			{
				// Remove '=" from queue
				tokens.Dequeue();
				
				// Parse initialization expression
				expression = ParseExpression(tokens, 0);
			}

			Expect(tokens.Dequeue(), TokenType.Semicolon);

			return new ParseNode.Declaration(identifier, expression);
		}
		
		/// <summary>
		/// Statement ::= "return" Expression ";" | Expression ";" | ";"
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.Statement ParseStatement(Queue<Token> tokens)
		{
			if (Match(tokens.Peek(), TokenType.Semicolon))
			{
				tokens.Dequeue();
				return null;
			}
			if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Return)) return ParseReturnStatement(tokens);
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			return expression;
		}
		
		static ParseNode.ReturnStatement ParseReturnStatement(Queue<Token> tokens) {
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Return);
			
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			
			return new ParseNode.ReturnStatement(expression);
		}
		
		/// <summary>
		/// Expression ::= Factor | Expression BinaryOperator Expression
		/// </summary>
		/// <param name="tokens"></param>
		/// <param name="min_precedence"></param>
		/// <returns></returns>
		static ParseNode.Expression ParseExpression(Queue<Token> tokens, int min_precedence)
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
				else
				{
					ParseNode.Expression right_expression = ParseExpression(tokens, current_precedence + 1);
					left_expression = new ParseNode.BinaryExpression((Syntax.BinaryOperator)@operator.Value, left_expression, right_expression);
				}
			}

			return left_expression;
		}
		
		/// <summary>
		/// Factor ::= Integer | Identifier | UnaryOperator Factor | "(" Expression ")"
		/// </summary>
		/// <param name="tokens"></param>
		/// <returns></returns>
		static ParseNode.Expression ParseFactor(Queue<Token> tokens)
		{
			if (Match(tokens.Peek(), TokenType.IntegerConstant)) return ParseConstantExpression(tokens);

			if (Match(tokens.Peek(), TokenType.Identifier)) return ParseVariable(tokens);
			
			if (Match(tokens.Peek(), TokenType.UnaryOperator) || Match(tokens.Peek(), TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction)) return ParseUnaryExpression(tokens);

			if (Match(tokens.Peek(), TokenType.OpenParenthesis))
			{
				tokens.Dequeue();
				ParseNode.Expression expression = ParseExpression(tokens, 0);
				Expect(tokens.Dequeue(), TokenType.CloseParenthesis);
				return expression;
			}

			throw new ParserException($"Expected: constant or variable, got: {tokens.Peek()}", tokens.Peek());
		}
		
		static ParseNode.UnaryExpression ParseUnaryExpression(Queue<Token> tokens) {
			Token @operator = tokens.Dequeue();
			
			// Convert binary subtraction operator to negation
			if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction)) {
				@operator = new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.Negation);
			}
			Expect(@operator, TokenType.UnaryOperator);
			
			ParseNode.Expression expression = ParseFactor(tokens);
			
			return new ParseNode.UnaryExpression((Syntax.UnaryOperator)@operator.Value, expression);
		}
		
		static ParseNode.Constant ParseConstantExpression(Queue<Token> tokens) {
			Token token = tokens.Dequeue();
			Expect(token, TokenType.IntegerConstant);
			return new ParseNode.Constant((int)token.Value);
		}

		static ParseNode.Variable ParseVariable(Queue<Token> tokens)
		{
			ParseNode.Identifier identifier = ParseIdentifier(tokens);
			return new ParseNode.Variable(identifier);
		}
		
		static ParseNode.Identifier ParseIdentifier(Queue<Token> tokens)
		{
			Token token = tokens.Dequeue();
			Expect(token, TokenType.Identifier);
			return new ParseNode.Identifier((string)token.Value);
		}
		
		static bool Match(Token token, TokenType expected_type) {
			return token.Type == expected_type;
		}
		
		static bool Match(Token token, TokenType expected_type, object expected_value) {
			return token.Type == expected_type && token.Value.Equals(expected_value);
		}
		
		static void Expect(Token token, TokenType expected_type) {
			if (token.Type != expected_type) {
				throw new ParserException($"Expected: {expected_type}, got: {token}", token);
			}
		}
		
		static void Expect(Token token, TokenType expected_type, object expected_value) {
			if (token.Type != expected_type || !token.Value.Equals(expected_value)) {
				throw new ParserException($"Expected: {expected_type}, {expected_value}, got: {token}", token);
			}
		}
	}
}
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
		/// Function ::= "int" Identifier "(" "void" ")" Block
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

			ParseNode.Block body = ParseBlock(tokens);

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
		static ParseNode.Statement ParseStatement(Queue<Token> tokens)
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
		
		static ParseNode.Return ParseReturnStatement(Queue<Token> tokens) {
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.Return);
			
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			
			return new ParseNode.Return(expression);
		}
		
		static ParseNode.If ParseIfStatement(Queue<Token> tokens) {
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

		static ParseNode.While ParseWhileStatement(Queue<Token> tokens)
		{
			Expect(tokens.Dequeue(), TokenType.Keyword, Syntax.Keyword.While);
			Expect(tokens.Dequeue(), TokenType.OpenParenthesis);

			ParseNode.Expression condition = ParseExpression(tokens, 0);

			Expect(tokens.Dequeue(), TokenType.CloseParenthesis);

			ParseNode.Statement body = ParseStatement(tokens);

			return new ParseNode.While(condition, body);
		}
		
		static ParseNode.DoWhile ParseDoWhileStatement(Queue<Token> tokens)
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
		
		static ParseNode.For ParseForStatement(Queue<Token> tokens)
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

		static ParseNode.ForInitialization ParseForInitialization(Queue<Token> tokens)
		{
			// If first token is 'int' then this is a variable declaration
			if (Match(tokens.Peek(), TokenType.Keyword, Syntax.Keyword.Integer)) return ParseDeclaration(tokens);
			// Otherwise it is an expression
			ParseNode.Expression expression = ParseExpression(tokens, 0);
			Expect(tokens.Dequeue(), TokenType.Semicolon);
			return expression;
		}
		
		static ParseNode.Block ParseBlock(Queue<Token> tokens)
		{
			Expect(tokens.Dequeue(), TokenType.OpenBrace);

			List<ParseNode.BlockItem> items = new List<ParseNode.BlockItem>();
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
				else if (Match(@operator, TokenType.BinaryOperator, Syntax.BinaryOperator.ConditionalTrue))
				{
					ParseNode.Expression then_expression = ParseExpression(tokens, 0);
					Console.WriteLine(then_expression);

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
				@operator = new Token(@operator.Line, TokenType.UnaryOperator, Syntax.UnaryOperator.Negation);
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
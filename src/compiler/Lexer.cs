using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FTG.Studios.MCC {

	public static class Lexer
	{

		public static List<Token> Tokenize(string source)
		{
			List<Token> tokens = new List<Token>();
			string lexeme = string.Empty;
			for (int index = 0; index < source.Length; index++)
			{
				char c = source[index];

				if (char.IsWhiteSpace(c))
				{
					if (!string.IsNullOrEmpty(lexeme))
					{
						tokens.Add(BuildToken(lexeme));
						lexeme = string.Empty;
					}
					continue;
				}

				char next = index + 1 < source.Length ? source[index + 1] : '\0';
				
				// Check if current character plus next character is a valid operator
				// Takes care of '==' being parsed into two '=' tokens
				Token token = BuildOperatorToken(c.ToString() + next);
				if (token.IsValid)
				{
					index++;
				}
				else
				{
					token = BuildToken(c);
				}
				
				if (token.IsValid)
				{
					if (!string.IsNullOrEmpty(lexeme))
					{
						Token current_token = BuildToken(lexeme);
						if (current_token.IsValid)
						{
							tokens.Add(current_token);
						}
					}
					tokens.Add(token);
					lexeme = string.Empty;
					continue;
				}

				lexeme += c;
			}

			if (!string.IsNullOrEmpty(lexeme)) tokens.Add(BuildToken(lexeme));

			return tokens;
		}

		static Token BuildToken(char lexeme)
		{
			return lexeme switch
			{
				// Puncuation
				Syntax.semicolon => new Token(TokenType.Semicolon),
				Syntax.open_parenthesis => new Token(TokenType.OpenParenthesis),
				Syntax.close_parenthesis => new Token(TokenType.CloseParenthesis),
				Syntax.open_brace => new Token(TokenType.OpenBrace),
				Syntax.close_brace => new Token(TokenType.CloseBrace),

				// Unary Operators
				Syntax.operator_not => new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.Not),
				Syntax.operator_bitwise_complement => new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.BitwiseComplement),

				// Binary Operators
				Syntax.operator_addition => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Addition),
				Syntax.operator_subtraction => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction),
				Syntax.operator_multiplication => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Multiplication),
				Syntax.operator_division => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Division),
				Syntax.operator_remainder => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Remainder),
				Syntax.operator_assignment => new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment),

				_ => Token.Invalid,
			};
		}

		static Token BuildToken(string lexeme)
		{
			Token operator_token = BuildOperatorToken(lexeme);
			if (operator_token.IsValid) return operator_token;
			
			// Check if keyword
			for (int index = 0; index < Syntax.keywords.Length; index++)
			{
				if (lexeme == Syntax.keywords[index]) return new Token(TokenType.Keyword, (Syntax.Keyword)index);
			}

			// Check if identifier
			if (Regex.IsMatch(lexeme, Syntax.identifier))
			{
				return new Token(TokenType.Identifier, lexeme);
			}

			// Check if integer literal
			if (Regex.IsMatch(lexeme, Syntax.integer_literal))
			{
				return new Token(TokenType.IntegerConstant, int.Parse(lexeme));
			}

			// At this point we know the lexeme is invalid since it wasn't a valid identifier or constant
			throw new LexerException($"Invalid lexeme: \'{lexeme}\'", lexeme);
		}

		static Token BuildOperatorToken(string lexeme)
		{
			// Unary operators
			if (lexeme == Syntax.operator_decrement) return new Token(TokenType.UnaryOperator, Syntax.UnaryOperator.Decrement);

			// Binary operators
			if (lexeme == Syntax.operator_logical_and) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalAnd);
			if (lexeme == Syntax.operator_logical_or) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalOr);
			if (lexeme == Syntax.operator_logical_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalEqual);
			if (lexeme == Syntax.operator_logical_not_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalNotEqual);
			if (lexeme == Syntax.operator_logical_less) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLess);
			if (lexeme == Syntax.operator_logical_greater) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreater);
			if (lexeme == Syntax.operator_logical_less_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLessEqual);
			if (lexeme == Syntax.operator_logical_greater_equal) return new Token(TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreaterEqual);
			
			return Token.Invalid;
		}
	}
}
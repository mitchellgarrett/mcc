using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FTG.Studios.MCC.Lexer;

public static class Lexer
{

	static int current_line;
	
	public static List<Token> Tokenize(string source)
	{
		current_line = 1;
		List<Token> tokens = [];
		string lexeme = string.Empty;
		for (int index = 0; index < source.Length; index++)
		{
			char c = source[index];

			if (c == '\n') current_line++;
			
			if (char.IsWhiteSpace(c))
			{
				if (!string.IsNullOrEmpty(lexeme))
				{
					tokens.Add(BuildToken(lexeme));
					lexeme = string.Empty;
				}
				continue;
			}

			// Check for preprocessor directive and skip rest of line
			if (c == Syntax.preprocessor_directive)
			{
				index++;
				while (index < source.Length - 1 && (c = source[++index]) != '\n')
				{
					lexeme += c;
				}

				current_line++;
				lexeme = string.Empty;
				continue;
			}

			char next = index + 1 < source.Length ? source[index + 1] : '\0';
			
			// Check for single line comment and skip rest of line
			if (c.ToString() + next == Syntax.single_line_comment)
			{
				index++;
				while (index < source.Length - 1 && (c = source[++index]) != '\n')
				{
					lexeme += c;
				}
				// TODO: Propogate comments to final program
				//tokens.Add(new Token(current_line, TokenType.Comment, lexeme));
				
				current_line++;
				lexeme = string.Empty;
				continue;
			}

			// Check for multline comment and skip until end of comment
			if (c.ToString() + next == Syntax.multi_line_comment_begin)
			{
				index++;
				while (index < source.Length - 2 && (c = source[++index]) != '\0' && (source[index].ToString() + source[index + 1]) != Syntax.multi_line_comment_end)
				{
					if (c != '\n') lexeme += c;
					else lexeme += ' ';
				}
				// TODO: Propogate comments to final program
				//tokens.Add(new Token(current_line, TokenType.Comment, lexeme));

				index++;
				current_line++;
				lexeme = string.Empty;
				continue;
			}

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
			Syntax.semicolon => new Token(current_line, TokenType.Semicolon),
			Syntax.comma => new Token(current_line, TokenType.Comma),
			Syntax.open_parenthesis => new Token(current_line, TokenType.OpenParenthesis),
			Syntax.close_parenthesis => new Token(current_line, TokenType.CloseParenthesis),
			Syntax.open_brace => new Token(current_line, TokenType.OpenBrace),
			Syntax.close_brace => new Token(current_line, TokenType.CloseBrace),

			// Unary operators
			Syntax.operator_not => new Token(current_line, TokenType.UnaryOperator, Syntax.UnaryOperator.Not),
			Syntax.operator_bitwise_complement => new Token(current_line, TokenType.UnaryOperator, Syntax.UnaryOperator.BitwiseComplement),

			// Binary operators
			Syntax.operator_addition => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Addition),
			Syntax.operator_subtraction => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Subtraction),
			Syntax.operator_multiplication => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Multiplication),
			Syntax.operator_division => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Division),
			Syntax.operator_remainder => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Remainder),
			Syntax.operator_assignment => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.Assignment),

			// Ternary operators
			Syntax.operator_ternary_true => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.ConditionalTrue),
			Syntax.operator_ternary_false => new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.ConditionalFalse),

			_ => Token.Invalid(current_line),
		};
	}

	static Token BuildToken(string lexeme)
	{
		Token operator_token = BuildOperatorToken(lexeme);
		if (operator_token.IsValid) return operator_token;
		
		// Check if keyword
		for (int index = 0; index < Syntax.keywords.Length; index++)
		{
			if (lexeme == Syntax.keywords[index]) return new Token(current_line, TokenType.Keyword, (Syntax.Keyword)index);
		}

		// Check if identifier
		if (Regex.IsMatch(lexeme, Syntax.identifier))
		{
			return new Token(current_line, TokenType.Identifier, lexeme);
		}

		// Check if integer literal
		if (Regex.IsMatch(lexeme, Syntax.integer_literal))
		{
			return new Token(current_line, TokenType.IntegerConstant, BigInteger.Parse(lexeme));
		}
		
		// Check if long literal
		if (Regex.IsMatch(lexeme, Syntax.long_literal))
		{
			return new Token(current_line, TokenType.LongConstant, BigInteger.Parse(lexeme.TrimEnd('l', 'L')));
		}

		// At this point we know the lexeme is invalid since it wasn't a valid identifier or constant
		throw new LexerException($"Invalid lexeme: \'{lexeme}\'", lexeme);
	}

	static Token BuildOperatorToken(string lexeme)
	{
		// Unary operators
		if (lexeme == Syntax.operator_decrement) return new Token(current_line, TokenType.UnaryOperator, Syntax.UnaryOperator.Decrement);

		// Binary operators
		if (lexeme == Syntax.operator_logical_and) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalAnd);
		if (lexeme == Syntax.operator_logical_or) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalOr);
		if (lexeme == Syntax.operator_logical_equal) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalEqual);
		if (lexeme == Syntax.operator_logical_not_equal) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalNotEqual);
		if (lexeme == Syntax.operator_logical_less) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLess);
		if (lexeme == Syntax.operator_logical_greater) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreater);
		if (lexeme == Syntax.operator_logical_less_equal) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalLessEqual);
		if (lexeme == Syntax.operator_logical_greater_equal) return new Token(current_line, TokenType.BinaryOperator, Syntax.BinaryOperator.LogicalGreaterEqual);
		
		return Token.Invalid(current_line);
	}
}
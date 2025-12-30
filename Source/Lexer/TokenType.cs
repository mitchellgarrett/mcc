namespace FTG.Studios.MCC.Lexer;

public enum TokenType {
	Invalid, Comment, Keyword, Identifier, IntegerConstant, LongConstant, UnsignedIntegerConstant, UnsignedLongConstant, FloatingPointConstant, Semicolon, Comma, OpenParenthesis, CloseParenthesis, OpenBrace, CloseBrace, UnaryOperator, BinaryOperator
}
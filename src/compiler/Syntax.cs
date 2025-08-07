namespace FTG.Studios.MCC
{
	
	public static class Syntax {
		
		// Punctuation
		public const char semicolon = ';';
		public const char open_parenthesis = '(';
		public const char close_parenthesis = ')';
		public const char open_brace = '{';
		public const char close_brace = '}';
		
		// Unary operators
		public const char operator_negation = '-';
		public const char operator_not = '!';
		public const char operator_bitwise_complement = '~';
		public const string operator_decrement = "--";
		
		// Binary operators
		public const char operator_addition = '+';
		public const char operator_subtraction = '-';
		public const char operator_multiplication = '*';
		public const char operator_division = '/';
		public const char operator_remainder = '%';
		public const char operator_assignment = '=';
		public const string operator_logical_and = "&&";
		public const string operator_logical_or = "||";
		public const string operator_logical_equal = "==";
		public const string operator_logical_not_equal = "!=";
		public const string operator_logical_less = "<";
		public const string operator_logical_greater = ">";
		public const string operator_logical_less_equal = "<=";
		public const string operator_logical_greater_equal = ">=";

		// Ternary operators
		public const char operator_ternary_true = '?';
		public const char operator_ternary_false = ':';
		
		public const string identifier = @"[a-zA-Z]\w*\b";
		public const string integer_literal = @"^[0-9]+\b$";

		// TODO: Support comments
		public const string single_line_comment = "//";
		public const string multi_line_comment_begin = "/*";
		public const string multi_line_comment_end = "*/";
		
		public static readonly string[] keywords = new string[] { "void", "return", "int", "if", "else" };
		
		public enum Keyword { Void, Return, Integer, If, Else };
		public enum UnaryOperator { Negation, Not, BitwiseComplement, Decrement };
		public enum BinaryOperator { Addition, Subtraction, Multiplication, Division, Remainder, Assignment, LogicalAnd, LogicalOr, LogicalEqual, LogicalNotEqual, LogicalLess, LogicalGreater, LogicalLessEqual, LogicalGreaterEqual, ConditionalTrue, ConditionalFalse };
		
		static readonly string[] unary_operators = new string[] { operator_negation.ToString(), operator_not.ToString(), operator_bitwise_complement.ToString(), operator_decrement };
		public static string GetOperator(this UnaryOperator op) {
			return unary_operators[(int)op];
		}
		
		static readonly string[] binary_operators = new string[] { operator_addition.ToString(), operator_subtraction.ToString(), operator_multiplication.ToString(), operator_division.ToString(), operator_remainder.ToString(), operator_assignment.ToString(), operator_logical_and, operator_logical_or, operator_logical_equal, operator_logical_not_equal, operator_logical_less, operator_logical_greater, operator_logical_less_equal, operator_logical_greater_equal, operator_ternary_true.ToString(), operator_ternary_false.ToString() };
		public static string GetOperator(this BinaryOperator op) {
			return binary_operators[(int)op];
		}
		
		static readonly int[] operator_precedence = new int[] { 45, 45, 50, 50, 50, 1, 10, 5, 30, 30, 35, 35, 35, 35, 3, -1 };
		public static int GetPrecedence(this BinaryOperator op) {
			return operator_precedence[(int)op];
		}
	}
}
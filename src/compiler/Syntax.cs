namespace FTG.Studios.MCC
{
	
	public static class Syntax {
		
		public const char semicolon = ';';
		public const char open_parenthesis = '(';
		public const char close_parenthesis = ')';
		public const char open_brace = '{';
		public const char close_brace = '}';
		
		// Unary Operators
		public const char operator_negation = '-';
		public const char operator_not = '!';
		public const char operator_bitwise_complement = '~';
		public const string operator_decrement = "--";
		
		// Binary Operators
		public const char operator_addition = '+';
		public const char operator_subtraction = '-';
		public const char operator_multiplication = '*';
		public const char operator_division = '/';
		public const char operator_remainder = '%';
		public const string operator_logical_and = "&&";
		public const string operator_logical_or = "||";
		public const string operator_logical_equal = "==";
		public const string operator_logical_not_equal = "!=";
		public const char operator_logical_less_than = '<';
		public const char operator_logical_greater_than = '>';
		public const string operator_logical_less_than_equal_to = "<=";
		public const string operator_logical_greater_than_equal_to = ">=";
		
		public const string identifier = @"[a-zA-Z]\w*\b";
		public const string integer_literal = @"[0-9]+\b";
		
		public static readonly string[] keywords = new string[] { "void", "return", "int" };
		
		public enum Keyword { Void, Return, Integer };
		public enum UnaryOperator { Negation, Not, BitwiseComplement, Decrement };
		public enum BinaryOperator { Addition, Subtraction, Multiplication, Division, Remainder, LogicalAnd, LogicalOr, LogicalEqual, LogicalNotEqual, LogicalLessThan, LogicalGreatherThan, LogicalLessThanEqualTo, LogicalGreatherThanEqualTo };
		
		static readonly string[] unary_operators = new string[] { operator_negation.ToString(), operator_not.ToString(), operator_bitwise_complement.ToString(), operator_decrement };
		public static string GetOperator(this UnaryOperator op) {
			return unary_operators[(int)op];
		}
		
		static readonly string[] binary_operators = new string[] { operator_addition.ToString(), operator_subtraction.ToString(), operator_multiplication.ToString(), operator_division.ToString(), operator_remainder.ToString(), operator_logical_equal, operator_logical_not_equal, operator_logical_less_than.ToString(), operator_logical_greater_than.ToString(), operator_logical_less_than_equal_to, operator_logical_greater_than_equal_to };
		public static string GetOperator(this BinaryOperator op) {
			return binary_operators[(int)op];
		}
		
		static readonly int[] operator_precedence = new int[] { 4, 4, 5, 5, 5, 1, 0, 2, 2, 3, 3, 3, 3 };
		public static int GetPrecedence(this BinaryOperator op) {
			return operator_precedence[(int)op];
		}
	}
}
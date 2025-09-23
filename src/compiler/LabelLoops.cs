namespace FTG.Studios.MCC
{

	public static partial class SemanticAnalzyer
	{
		static int next_loop_label_index;

		static string GetNextLoopLabel(ParseNode.Statement loop)
		{
			return $"{loop.GetType().Name}{next_loop_label_index++}";
		}

		static void LabelLoopsInProgram(ParseNode.Program program)
		{
			foreach (var function in program.FunctionDeclarations) LabelLoopsInFunctionDeclaration(function);
		}
		
		static void LabelLoopsInFunctionDeclaration(ParseNode.FunctionDeclaration function)
		{
			if (function.Body != null) LabelLoopsInBlock(string.Empty, function.Body);
		}

		static void LabelLoopsInBlock(string label, ParseNode.Block block)
		{
			for (int index = 0; index < block.Items.Count; index++)
			{
				if (block.Items[index] is ParseNode.Statement statement) LabelLoopsInStatement(label, statement);
			}
		}

		static void LabelLoopsInStatement(string label, ParseNode.Statement statement)
		{
			if (statement is ParseNode.Block block)
			{
				LabelLoopsInBlock(label, block);
				return;
			}

			if (statement is ParseNode.If @if)
			{
				LabelLoopsInStatement(label, @if.Then);
				LabelLoopsInStatement(label, @if.Else);
				return;
			}

			if (statement is ParseNode.Break @break)
			{
				if (string.IsNullOrEmpty(label)) throw new SemanticAnalzyerException("Break statement is not inside of a loop.", @break.ToString());
				@break.InternalLabel = $"Break{label}";
				return;
			}

			if (statement is ParseNode.Continue @continue)
			{
				if (string.IsNullOrEmpty(label)) throw new SemanticAnalzyerException("Continue statement is not inside of a loop.", @continue.ToString());
				@continue.InternalLabel = $"Continue{label}"; ;
				return;
			}

			if (statement is ParseNode.While @while)
			{
				string new_label = GetNextLoopLabel(@while);
				@while.InternalLabel = new_label;
				LabelLoopsInStatement(new_label, @while.Body);
				return;
			}

			if (statement is ParseNode.DoWhile do_while)
			{
				string new_label = GetNextLoopLabel(do_while);
				do_while.InternalLabel = new_label;
				LabelLoopsInStatement(new_label, do_while.Body);
				return;
			}

			if (statement is ParseNode.For @for)
			{
				string new_label = GetNextLoopLabel(@for);
				@for.InternalLabel = new_label;
				LabelLoopsInStatement(new_label, @for.Body);
				return;
			}

			if (statement == null || statement is ParseNode.Return || statement is ParseNode.Expression) return;
			
			throw new SemanticAnalzyerException($"Unhandled statement type \"{statement}\"", statement.ToString());
		}
	}
}
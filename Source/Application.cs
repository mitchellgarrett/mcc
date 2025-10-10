using System;
using System.IO;
using System.Collections.Generic;
using FTG.Studios.MCC.Lexer;
using FTG.Studios.MCC.Parser;
using FTG.Studios.MCC.SemanticAnalysis;
using FTG.Studios.MCC.Intermediate;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.CodeGeneration;

class Application
{

	struct CommandLineArguments
	{
		public bool DoLex, DoParse, DoValidate, DoTacky, DoCodegen;
	}

	/// Command Line Arguments
	/// --lex - stop before parsing
	/// --parse - stop before semantic analysis
	/// --validate - stop before intermediate code generation
	/// --tacky - stop before code emission
	/// --codegen - stop before execution
	public static void Main(string[] args)
	{
		CommandLineArguments command_line_arguments = new CommandLineArguments()
		{
			DoLex = true,
			DoParse = true,
			DoValidate = true,
			DoTacky = true,
			DoCodegen = true
		};

		string input_path = null;
		string output_path = null;

		// TODO: Make this cleaner
		foreach (string argument in args)
		{
			if (argument.ToLower() == "--lex")
			{
				command_line_arguments.DoLex = true;
				command_line_arguments.DoParse = command_line_arguments.DoValidate = command_line_arguments.DoTacky = command_line_arguments.DoCodegen = false;
			}
			else if (argument.ToLower() == "--parse")
			{
				command_line_arguments.DoLex = command_line_arguments.DoParse = true;
				command_line_arguments.DoValidate = command_line_arguments.DoTacky = command_line_arguments.DoCodegen = false;
			}
			else if (argument.ToLower() == "--validate")
			{
				command_line_arguments.DoLex = command_line_arguments.DoParse = command_line_arguments.DoValidate = true;
				command_line_arguments.DoTacky = command_line_arguments.DoCodegen = false;
			}
			else if (argument.ToLower() == "--tacky")
			{
				command_line_arguments.DoLex = command_line_arguments.DoParse = command_line_arguments.DoValidate = command_line_arguments.DoTacky = true;
				command_line_arguments.DoCodegen = false;
			}
			else if (argument.ToLower() == "--codegen")
			{
				command_line_arguments.DoLex = command_line_arguments.DoParse = command_line_arguments.DoValidate = command_line_arguments.DoTacky = command_line_arguments.DoCodegen = true;
			}
			else if (string.IsNullOrEmpty(input_path))
			{
				input_path = argument;
			}
			else
			{
				output_path = argument;
			}
		}

		if (string.IsNullOrEmpty(input_path) || string.IsNullOrEmpty(output_path)) System.Environment.Exit(1);

		CompileFile(input_path, output_path, command_line_arguments);
	}

	static void CompileFile(string input_path, string output_path, CommandLineArguments arguments)
	{
		string source = File.ReadAllText(input_path);

		Console.WriteLine("------");
		Console.WriteLine("Source");
		Console.WriteLine("------");
		Console.WriteLine(source);
		Console.WriteLine("------\n");

		if (!arguments.DoLex) return;

		Console.WriteLine("------");
		Console.WriteLine("Tokens");
		Console.WriteLine("------");

		List<Token> tokens = null;
		try
		{
			tokens = Lexer.Tokenize(source);
			foreach (var token in tokens) Console.WriteLine(token);
		}
		catch (LexerException e)
		{
			Console.Error.WriteLine(e.Message);
			Environment.Exit(1);
		}

		if (!arguments.DoParse) return;

		Console.WriteLine("------\n");

		Console.WriteLine("----------");
		Console.WriteLine("Parse Tree");
		Console.WriteLine("----------");

		ParseTree parse_tree = null;
		try
		{
			parse_tree = Parser.Parse(tokens);
			Console.WriteLine(parse_tree);
		}
		catch (ParserException e)
		{
			Console.Error.WriteLine(e.Message);
			Environment.Exit(1);
		}

		if (!arguments.DoValidate) return;

		Console.WriteLine("----------\n");

		Console.WriteLine("-------------------");
		Console.WriteLine("Resolved Parse Tree");
		Console.WriteLine("-------------------");

		SymbolTable symbol_table = null;
		try
		{
			SemanticAnalzyer.ResolveIdentifiers(parse_tree);
			symbol_table = SemanticAnalzyer.CheckTypes(parse_tree);
			SemanticAnalzyer.LabelLoops(parse_tree);
			Console.WriteLine(parse_tree);
		}
		catch (SemanticAnalzyerException e)
		{
			Console.Error.WriteLine(e.Message);
			Environment.Exit(1);
		}

		if (!arguments.DoTacky) return;

		Console.WriteLine("-------------------\n");

		Console.WriteLine("-----------------");
		Console.WriteLine("Intermediate Tree");
		Console.WriteLine("-----------------");

		IntermediateTree intermediate_tree = IntermediateGenerator.Generate(parse_tree, symbol_table);
		Console.WriteLine(intermediate_tree);

		if (!arguments.DoCodegen) return;

		Console.WriteLine("-----------------\n");

		Console.WriteLine("-------------");
		Console.WriteLine("Assembly Tree");
		Console.WriteLine("-------------");

		AssemblyTree assembly_tree = AssemblyGenerator.Generate(intermediate_tree, symbol_table);
		Console.WriteLine(assembly_tree);

		Console.WriteLine("-------------");

		Console.WriteLine("---------");
		Console.WriteLine("Optimizer");
		Console.WriteLine("---------");

		CodeOptimizer.AssignVariables(assembly_tree, symbol_table);
		CodeOptimizer.FixVariableAccesses(assembly_tree);
		Console.WriteLine(assembly_tree);

		Console.WriteLine("--------");

		using StreamWriter output_file = new(output_path);
		CodeEmitter.Emit(assembly_tree, output_file);
	}
}
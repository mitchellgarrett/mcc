using System.IO;
using System.Runtime.InteropServices;
using FTG.Studios.MCC.Assembly;
using FTG.Studios.MCC.SemanticAnalysis;

namespace FTG.Studios.MCC.CodeGeneration;

public static class CodeEmitter {
	
	const string macos_function_prefix = "_";
	const string linux_plt_suffix = "@PLT";
	const string linux_program_footer = ".section .note.GNU-stack,\"\",@progbits";

	static SymbolTable symbol_table;
	
	public static void Emit(AssemblyTree program, SymbolTable symbol_table, StreamWriter file)
	{
		CodeEmitter.symbol_table = symbol_table;
		EmitProgram(program.Program, file);
	}
	
	static void EmitProgram(AssemblyNode.Program program, StreamWriter file) {
		foreach (var function in program.Functions) EmitFunction(function, file);
		
		// Write Linux-specific code at end of program
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) file.WriteLine(linux_program_footer);
	}

	static void EmitFunction(AssemblyNode.Function function, StreamWriter file)
	{
		string identifier = function.Identifier;

		// Add '_' to front of function names if on MacOS
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) identifier = macos_function_prefix + identifier;

		// If on Linux and the identifier isn't locally defined, add the PLT suffix
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !symbol_table.ContainsSymbol(function.Identifier)) identifier += linux_plt_suffix;

		// Function header
		file.WriteLine($"// int {function.Identifier}(void)");
		file.WriteLine($".globl {identifier}");
		file.WriteLine($"{identifier}:");

		// Function prologue
		file.WriteLine("\tpushq %rbp");
		file.WriteLine("\tmovq %rsp, %rbp");

		// Emit the body of the function
		foreach (var instruction in function.Body)
		{
			if (instruction is AssemblyNode.Comment) file.WriteLine();
			if (instruction is AssemblyNode.Call function_call)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) function_call.Identifier = macos_function_prefix + function_call.Identifier;
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !symbol_table.ContainsSymbol(function_call.Identifier)) function_call.Identifier += linux_plt_suffix;
			}

			file.WriteLine('\t' + instruction.Emit().Replace("\n", "\n\t"));
		}

		file.WriteLine();
	}
}
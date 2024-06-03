using System.IO;
using System.Runtime.InteropServices;

namespace FTG.Studios.MCC {
	
	public static class CodeEmitter {
		
		const string macos_function_prefix = "_";
		const string linux_program_footer = ".section .note.GNU-stack,\"\",@progbits";
		
		public static void Emit(AssemblyTree program, StreamWriter file) {
			EmitProgram(program.Program, file);
		}
		
		static void EmitProgram(AssemblyNode.Program program, StreamWriter file) {
			EmitFunction(program.Function, file);
			
			// Write Linux-specific code at end of program
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) file.WriteLine(linux_program_footer);
		}
		
		static void EmitFunction(AssemblyNode.Function function, StreamWriter file) {
			string identifier = function.Identifier;
			
			// Add '_' to front of function names if on MacOS
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) identifier = macos_function_prefix + identifier;
			
			file.WriteLine($".globl {identifier}");
			file.WriteLine($"{identifier}:");
			foreach (AssemblyNode.Instruction instruction in function.Body) {
				file.WriteLine('\t' + instruction.Emit());
			}
		}
	}
}
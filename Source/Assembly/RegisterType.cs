namespace FTG.Studios.MCC {
	
	public enum RegisterType { BP, AX, CX, DX, DI, SI, R8, R9, R10, R11 };

	public static class RegisterTypeExtensions
	{

		static readonly string[] register_names_8 = new string[] { "%rbp", "%al", "%cl", "%dl", "%dil", "%sil", "%r8b", "r9b", "%r10b", "%r11b" };
		static readonly string[] register_names_32 = new string[] { "%ebp", "%eax", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d" };
		static readonly string[] register_names_64 = new string[] { "%rbp", "%rax", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11" };

		public static string Emit8(this RegisterType value)
		{
			return register_names_8[(int)value];
		}

		public static string Emit32(this RegisterType value)
		{
			return register_names_32[(int)value];
		}

		public static string Emit64(this RegisterType value)
		{
			return register_names_64[(int)value];
		}

		public static AssemblyNode.Register ToOperand(this RegisterType type)
		{
			return new AssemblyNode.Register(type);
		}

		public static RegisterType[] FunctionCallOrder = new RegisterType[] { RegisterType.DI, RegisterType.SI, RegisterType.DX, RegisterType.CX, RegisterType.R8, RegisterType.R9 };
	}
}
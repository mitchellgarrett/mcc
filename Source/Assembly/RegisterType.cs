namespace FTG.Studios.MCC.Assembly;

public enum RegisterType { 
	BP, AX, CX, DX, DI, SI, R8, R9, R10, R11, SP, 
	XMM0, XMM1, XMM2, XMM3, XMM4, XMM5, XMM6, XMM7, XMM14, XMM15
};

public static class RegisterTypeExtensions
{

	static readonly string[] register_names_8 = ["%bp", "%al", "%cl", "%dl", "%dil", "%sil", "%r8b", "r9b", "%r10b", "%r11b", "%sp"];
	static readonly string[] register_names_32 = ["%ebp", "%eax", "%ecx", "%edx", "%edi", "%esi", "%r8d", "%r9d", "%r10d", "%r11d", "%esp"];
	static readonly string[] register_names_64 = ["%rbp", "%rax", "%rcx", "%rdx", "%rdi", "%rsi", "%r8", "%r9", "%r10", "%r11", "%rsp", "%xmm0", "%xmm1", "%xmm2", "%xmm3", "%xmm4", "%xmm5", "%xmm6", "%xmm7", "%xmm14", "%xmm15"];

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

	public static readonly RegisterType[] integer_function_call_order = [RegisterType.DI, RegisterType.SI, RegisterType.DX, RegisterType.CX, RegisterType.R8, RegisterType.R9];
	public static readonly RegisterType[] double_function_call_order = [RegisterType.XMM0, RegisterType.XMM1, RegisterType.XMM2, RegisterType.XMM3, RegisterType.XMM4, RegisterType.XMM5, RegisterType.XMM6, RegisterType.XMM7];
}
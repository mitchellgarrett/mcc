namespace FTG.Studios.MCC.Parser;

public enum PrimitiveType { Integer, Long, UnsignedInteger, UnsignedLong, Double };

public static class PrimitiveTypeExtensions
{
	public static int GetSize(this PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Integer => 4,
			PrimitiveType.Long => 8,
			PrimitiveType.UnsignedInteger => 4,
			PrimitiveType.UnsignedLong => 8,
			PrimitiveType.Double => 8,
			_ => throw new System.Exception(),
		};
	}
	
	public static bool IsSigned(this PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Integer => true,
			PrimitiveType.Long => true,
			PrimitiveType.UnsignedInteger => false,
			PrimitiveType.UnsignedLong => false,
			PrimitiveType.Double => true,
			_ => throw new System.Exception(),
		};
	}
	
	public static string ToShortString(this PrimitiveType type)
	{
		return type switch
		{
			PrimitiveType.Integer => "int",
			PrimitiveType.Long => "long",
			PrimitiveType.UnsignedInteger => "uint",
			PrimitiveType.UnsignedLong => "ulong",
			PrimitiveType.Double => "double",
			_ => throw new System.Exception(),
		};
	}
}
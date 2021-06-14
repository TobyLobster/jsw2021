using System;

namespace EncodeData
{
    public static class JSWMessage
    {
#nullable enable
        // ********************************************************************
        public static void Message(string format, object? arg0 = null, object? arg1 = null, object? arg2 = null)
        {
            var name = typeof(JSWMessage).Namespace;
            Console.Write(name + ": ");
			Console.WriteLine(string.Format(format, arg0, arg1, arg2));
        }

        // ********************************************************************
        public static void Error(string format, object? arg0 = null, object? arg1 = null, object? arg2 = null)
        {
            if (string.IsNullOrEmpty(format))
            {
                throw new ArgumentException($"'{nameof(format)}' cannot be null or empty", nameof(format));
            }
            var name = typeof(JSWMessage).Namespace;
            Console.Write(name + ": Error: ");
			Console.WriteLine(string.Format(format, arg0, arg1, arg2));
        }

    }
}

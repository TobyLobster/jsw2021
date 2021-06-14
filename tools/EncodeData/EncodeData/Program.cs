using System;

namespace EncodeData
{

    // ************************************************************************
    class MainClass
    {
        // ********************************************************************
        public static void Main(string[] args)
        {
            Console.WriteLine("Encode Data");

            JSWOptions options = new JSWOptions();
            options.ParseOptions(args);

            var processor = new JSWProcessor();
            processor.Process(options.InputFile, options.OutputFile);
        }
    }
}

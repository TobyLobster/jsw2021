using System;
using System.Collections.Generic;
using NDesk.Options;

namespace EncodeData
{
	// ************************************************************************
	public class JSWOptions
    {
		public string InputFile				= "";
        public string OutputFile			= "";
		public bool show_help				= false;

		// For example:
		// -input definitions.txt
		// -output definitions.a

		// ********************************************************************
		public int ParseOptions(
			string[] args)
        {
			var p = new OptionSet() {
				{ "inputfile=",   "the input definition {filepath}.", v => InputFile = v },
				{ "outputfile=",   "the output assembly {filepath}.", v => OutputFile = v },
				{ "help",     "show this message and exit", v => show_help = v != null },
			};

			List<string> extra;

			try {
				extra = p.Parse(args);
			}
			catch (OptionException e) {
				JSWMessage.Error(e.Message);
				JSWMessage.Message("Try `EncodeData -help' for more information.");
				return -1;	// negative means: Completed with error
			}

			if (show_help)
			{
				ShowHelp(p);
				return 1;	// positive means: Completed successfully
			}

			return 0;		// zero means: Continue processing
        }

		// ********************************************************************
		void ShowHelp(
			OptionSet p)
		{
			JSWMessage.Message("Usage: EncodeData [OPTIONS]");
			JSWMessage.Message("Convert JSW definitions into data for the game.");
			JSWMessage.Message("");
			JSWMessage.Message("Options:");
			p.WriteOptionDescriptions(Console.Out);
		}
    }
}

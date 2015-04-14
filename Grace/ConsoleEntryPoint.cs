using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Grace.Execution;
using Grace.Parsing;
using Grace.Runtime;

namespace Grace
{
    class ConsoleEntryPoint
    {
        static int Main(string[] args)
        {
            ParseNode module;
            string filename = null;
            string mode = "run";
            foreach (string arg in args)
            {
                if (arg == "--parse-tree")
                    mode = "parse-tree";
                else if (arg == "--execution-tree")
                    mode = "execution-tree";
                else if (arg == "--no-run")
                    mode = "no-run";
                else if (arg == "--verbose")
                    Interpreter.ActivateDebuggingMessages();
                else
                    filename = arg;
            }
            if (filename == null) {
                System.Console.Error.WriteLine("Required filename argument missing.");
                return 1;
            }
            GraceObject prelude = null;
            string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string preludePath = Path.Combine(dir, "prelude.grace");
            var interp = new Interpreter();
            using (StreamReader preludeReader = File.OpenText(preludePath))
            {
                Parser parser = new Parser("“prelude” (not your code)",
                        preludeReader.ReadToEnd());
                var pt = parser.Parse() as ObjectParseNode;
                var eMod = new ExecutionTreeTranslator().Translate(pt);
                prelude = eMod.Evaluate(interp);
                interp.Extend(prelude);
                Interpreter.Debug("========== END PRELUDE ==========");
            }
            using (StreamReader reader = File.OpenText(filename))
            {
                Parser parser = new Parser(
                        Path.GetFileNameWithoutExtension(filename),
                        reader.ReadToEnd());
                try
                {
                    //Console.WriteLine("========== PARSING ==========");
                    module = parser.Parse();
                    if (mode == "parse-tree")
                    {
                        module.DebugPrint(System.Console.Out, "");
                        return 0;
                    }
                    //Console.WriteLine("========== TRANSLATING ==========");
                    ExecutionTreeTranslator ett = new ExecutionTreeTranslator();
                    Node eModule = ett.Translate(module as ObjectParseNode);
                    if (mode == "execution-tree")
                    {
                        eModule.DebugPrint(Console.Out, "");
                        return 0;
                    }
                    if (mode == "no-run")
                        return 0;
                    //eModule.DebugPrint(Console.Out, "T>    ");
                    //Console.WriteLine("========== EVALUATING ==========");
                    try
                    {
                        eModule.Evaluate(interp);
                    }
                    catch (GraceExceptionPacketException e)
                    {
                        System.Console.Error.WriteLine("Uncaught exception:");
                        ErrorReporting.WriteException(e.ExceptionPacket);
                        if (e.ExceptionPacket.StackTrace != null)
                        {
                            foreach (var l in e.ExceptionPacket.StackTrace)
                            {
                                System.Console.Error.WriteLine("    from "
                                        + l);
                            }
                        }
                        return 1;
                    }
                }
                catch (StaticErrorException e)
                {
                    System.Console.WriteLine(e.StackTrace);
                    return 1;
                }
            }
            return 0;
        }
    }
}

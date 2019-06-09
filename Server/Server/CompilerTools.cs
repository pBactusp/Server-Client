using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.IO;

namespace Server
{
    public static class CompilerTools
    {
        public static string Compile_FromFile(string sourcePath, string target)
        {
            return Compile_FromString(ReadFile(sourcePath), target);
        }

        public static string Compile_FromString(string input, string target)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();

            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = target;
            CompilerResults results = icc.CompileAssemblyFromSource(parameters, input);


            #region Construct output string and return

            string ret = "";

            if (results.Errors.Count > 0)
            {
                ret = "THE CODE HAS NOT COMPILED" + Environment.NewLine;
                ret += results.Errors.Count + " errors have been found:" + Environment.NewLine;

                for (int i = 0; i < 50; i++)
                    ret += '-';
                ret += Environment.NewLine;

                foreach (CompilerError CompErr in results.Errors)
                {
                    ret += "Line number " + CompErr.Line +
                        ", Error Number: " + CompErr.ErrorNumber +
                        ", '" + CompErr.ErrorText + ";" +
                        Environment.NewLine;

                    for (int i = 0; i < 50; i++)
                        ret += '-';

                    ret += Environment.NewLine;
                }
            }

            return ret;

            #endregion
        }





        public static string ReadFile(string source)
        {
            string ret = "";
            using (StreamReader sr = new StreamReader(source))
                while (!sr.EndOfStream)
                {
                    ret += sr.ReadLine();
                    ret += Environment.NewLine;
                }

            return ret;
        }



    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace RoslynCodeGenTest
{
    class Program
    {
        private static async Task Main()
        {
            //step one walk through the EF core DLL via reflection

            //as I am walking through 
            var testAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(@"C:\Code\EFCoreLibrary\EFCoreLibrary\bin\Debug\net5.0\EFCoreLibrary.dll");
            //Assembly testAssembly = Assembly.Load("EFCoreLibrary");
            var theList = testAssembly.GetTypes();
            //generate the class 

            foreach (var item in theList)
            {
                if (item.IsPublic)
                {
                    Console.WriteLine(item.Name);
                    await GenerateCode.GenerateController(item.Name);
                    await PopulateDB.GenerateTableandField(item);
                }
            }
        }
       
    }
}

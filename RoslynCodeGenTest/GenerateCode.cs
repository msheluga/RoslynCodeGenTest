using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RoslynCodeGenTest
{
    internal class GenerateCode
    {
        internal static async Task<bool> GenerateController(string name)
        {
            var result = false;
            var syntaxFactory = SyntaxFactory.CompilationUnit();

            //add my usings
            // Add System using statement: (using System)
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            //using Microsoft.AspNetCore.Mvc;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.Mvc")));
            //using Microsoft.AspNetCore.OData.Deltas;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.OData.Deltas")));
            //using Microsoft.AspNetCore.OData.Formatter;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.OData.Formatter")));
            //using Microsoft.AspNetCore.OData.Query;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.AspNetCore.OData.Query")));
            //using Microsoft.EntityFrameworkCore;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore")));
            //using ODataBatching8.Models;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("ODataBatching8.Models")));
            //using System.Collections.Generic;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
            //using System.Linq;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            //using System.Threading.Tasks;
            syntaxFactory = syntaxFactory.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")));

            // Create a namespace: (namespace CodeGenerationSample)
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("ODataBatching8.Controllers")).NormalizeWhitespace();

            //  Create a class: (class Order)
            var classDeclaration = SyntaxFactory.ClassDeclaration(name.Pluralize() + "Controller");

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            // Inherit ODataController (public class BooksController : ControllerBase)
            classDeclaration = classDeclaration.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ControllerBase")));

            // Create a IDBContextFactory variable: (IDbContextFactory<BooksContext> dbContextFactory;)
            var variableFactoryDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("IDbContextFactory<BooksContext>"))
                .AddVariables(SyntaxFactory.VariableDeclarator("dbContextFactory"));

            //var es = SyntaxFactory.ParseExpression("new DbContextOptionsBuilder<BooksContext>().UseInMemoryDatabase(databaseName: \"BooksContext\").Options ");
            //SyntaxTrivia space = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");
            //EqualsValueClauseSyntax evc = SyntaxFactory.EqualsValueClause(es).WithLeadingTrivia(space);

            //var secondVariable = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("DbContextOptionsBuilder<BooksContext>"))               
            //    .AddVariables(SyntaxFactory.VariableDeclarator("dbContextFactory")
            //     .WithInitializer(evc));
            

            // Create a field declaration: (private readonly IDbContextFactory<BooksContext> dbContextFactory;)
            var fieldFactoryDeclaration = SyntaxFactory.FieldDeclaration(variableFactoryDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            //var field2FactoryDeclaration = SyntaxFactory.FieldDeclaration(secondVariable)
            //    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            // Create a IDBContextFactory variable: (IDbContextFactory<BooksContext> dbContextFactory;)
            var variableContextDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("BooksContext"))
                .AddVariables(SyntaxFactory.VariableDeclarator("dbContext"));

            // Create a field declaration: (private readonly IDbContextFactory<BooksContext> dbContextFactory;)
            var fieldContextDeclaration = SyntaxFactory.FieldDeclaration(variableContextDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            //method Body for base method           
            MethodDeclarationSyntax controllerMethod = GenerateControllerConstructor(name.Pluralize());

            //Generate the Get Method 
            var enableQueryAttribute = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
               SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("EnableQuery")))
               ).NormalizeWhitespace();

            MethodDeclarationSyntax getMethod = GenerateGetMethod(name.Pluralize(), enableQueryAttribute);

            //GetByID    
            MethodDeclarationSyntax getByIdMethod = GenerateGetByIdMethod(name.Pluralize(), enableQueryAttribute);

            var methods = new List<MemberDeclarationSyntax>();
            methods.Add(fieldFactoryDeclaration);
            //methods.Add(field2FactoryDeclaration);
            methods.Add(fieldContextDeclaration);
            methods.Add(controllerMethod);
            methods.Add(getMethod);
            methods.Add(getByIdMethod);

            classDeclaration = classDeclaration.AddMembers(methods.ToArray());
            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Add the namespace to the compilation unit.
            syntaxFactory = syntaxFactory.AddMembers(@namespace);

            // Normalize and get code as string.
            var code = syntaxFactory
                .NormalizeWhitespace()
                .ToFullString();

            await using var streamWriter = new StreamWriter("c:\\code-gen\\" + name.Pluralize() + "Controller.cs");
            streamWriter.Write(code);
            streamWriter.Close();
            return result;
        }

        private static MethodDeclarationSyntax GenerateControllerConstructor(string name)
        {
            var controllerBody = SyntaxFactory.ParseStatement("this.dbContextFactory = dbContextFactory;");

            var dBContextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("dbContextFactory"))
                .WithType(SyntaxFactory.ParseTypeName("IDbContextFactory<BooksContext>"));

            //create the BookContextFactory  base method
            var controllerMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(""), name+"Controller")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(dBContextParameter)
                .WithBody(SyntaxFactory.Block(controllerBody));
            return controllerMethod;
        }

        private static MethodDeclarationSyntax GenerateGetMethod(string name, AttributeListSyntax enableQueryAttribute)
        {
            var getBodyCode = new StringBuilder();
            getBodyCode.AppendLine("dbContext = this.dbContextFactory.CreateDbContext();");
            getBodyCode.AppendLine("return Ok(dbContext." + name + ");");

            var getBody = (BlockSyntax)SyntaxFactory.ParseStatement("{" + getBodyCode.ToString() + "}");



            //Get Method
            var getMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("IActionResult"), "Get")
                .AddAttributeLists(enableQueryAttribute)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(getBody);
            return getMethod;
        }

        private static MethodDeclarationSyntax GenerateGetByIdMethod(string name, AttributeListSyntax enableQueryAttribute)
        {
            var getByIdCode = new StringBuilder();
            getByIdCode.AppendLine("dbContext = this.dbContextFactory.CreateDbContext();");
            getByIdCode.AppendLine("return Ok(await dbContext." + name + ".Where(x=>x.Id == key).FirstOrDefaultAsync());");

            var getByIdBody = (BlockSyntax)SyntaxFactory.ParseStatement("{" + getByIdCode + "}");

            var fromODataURI = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("FromODataUri"))
                )).NormalizeWhitespace();

            var getByIdParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("key"))
                .WithType(SyntaxFactory.ParseTypeName("Guid"))
                .AddAttributeLists(fromODataURI);


            var getByIdMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task<IActionResult>"), "Get")
                .AddAttributeLists(enableQueryAttribute)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .AddParameterListParameters(getByIdParameter)
                .WithBody(getByIdBody);
            return getByIdMethod;
        }
    }
}
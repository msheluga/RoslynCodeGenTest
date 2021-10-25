﻿using Microsoft.CodeAnalysis;
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
            var classDeclaration = SyntaxFactory.ClassDeclaration("BooksController");

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Inherit ODataController (public class BooksController : ControllerBase)
            classDeclaration = classDeclaration.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ODataControllerBase")));

            // Create a IDBContextFactory variable: (IDbContextFactory<BooksContext> dbContextFactory;)
            var variableFactoryDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("IDbContextFactory<BooksContext>"))
                .AddVariables(SyntaxFactory.VariableDeclarator("dbContextFactory"));

            // Create a field declaration: (private readonly IDbContextFactory<BooksContext> dbContextFactory;)
            var fieldFactoryDeclaration = SyntaxFactory.FieldDeclaration(variableFactoryDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            // Create a IDBContextFactory variable: (IDbContextFactory<BooksContext> dbContextFactory;)
            var variableContextDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("BooksContext"))
                .AddVariables(SyntaxFactory.VariableDeclarator("dbContext"));

            // Create a field declaration: (private readonly IDbContextFactory<BooksContext> dbContextFactory;)
            var fieldContextDeclaration = SyntaxFactory.FieldDeclaration(variableContextDeclaration)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            //method Body for base method           
            var controllerBody = SyntaxFactory.ParseStatement("this.dbContextFactory = dbContextFactory");

            var dBContextParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("dbContextFactory"))
                .WithType(SyntaxFactory.ParseTypeName("IDbContextFactory<BooksContext>"));

            //create the BookContextFactory  base method
            var controllerMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(""), name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))   
                .AddParameterListParameters(dBContextParameter)
                .WithBody(SyntaxFactory.Block(controllerBody));

            //Generate the Get Method 
            var getBodyCode = new StringBuilder();
            getBodyCode.AppendLine("dbContext = this.dbContextFactory.CreateDbContext();");
            getBodyCode.AppendLine("return Ok(dbContext." + name + ");");

            var getBody = (BlockSyntax)SyntaxFactory.ParseStatement("{" + getBodyCode.ToString() + "}");
            
            var enableQueryAttribute = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("EnableQuery")))
                ).NormalizeWhitespace();
            
            //Get Method
            var getMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("IActionResult"), "Get")
                .AddAttributeLists(enableQueryAttribute)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(getBody);

            //GetByID    
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
                .AddParameterListParameters(getByIdParameter)
                .WithBody(getByIdBody);

            var methods = new List<MemberDeclarationSyntax>();
            methods.Add(fieldFactoryDeclaration);
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

            await using var streamWriter = new StreamWriter("c:\\code-gen\\" + name + ".cs");
            streamWriter.Write(code);
            streamWriter.Close();
            return result;
        }
    }
}
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RoslynCodeGenTest.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace RoslynCodeGenTest
{
    internal class PopulateDB
    {        

        internal static async Task<bool> GenerateTableandField(Type entity)
        {
            var result = false;

            using (var context = new BooksContext())
            {
                //first check if the item exists in the table DB 
                var tableExists = context.Tables.Where(x => x.TableName.Equals(entity.Name)).Count();
                if (tableExists > 0)
                {
                    await UpdateDBContext(entity, context);
                }
                else 
                {
                    await InsertEntity(entity, context);
                }
            }


            return result;
        }

        private static async Task<int> InsertEntity(Type entity, BooksContext context)
        {

            //first insert the table and get the Id
            var insert = new Table
            {
                Id = Guid.NewGuid(),
                TableName = entity.Name,
                ControllerName = entity.Name.Pluralize()
            };
            var counter = 1;
            //now that I have the tableId I can insert the fields 
            foreach (var prop in entity.GetProperties())
            {

                var attr = prop.GetCustomAttributes();
                
                    //Console.WriteLine(attr.GetType());
                    
                if (attr.Any(a=>a.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute)))
                {
                    Console.WriteLine(entity.Name + "-" + prop.Name + "-" + "Key");
                    Field insertField = new Field
                    {
                        Id = Guid.NewGuid(),
                        TableId = insert.Id,
                        FieldName = prop.Name,
                        FieldProperties = "Key",
                        FieldDataType = GetDBType(prop.PropertyType.Name),
                        FieldOrder = counter
                    };
                    insert.Fields.Add(insertField);
                }
                else if (attr.Any(a=>a.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute)))
                {
                    Console.WriteLine(entity.Name + "-" + prop.Name + "-" + "Foreign Key");
                    Field insertField = new Field
                    {
                        Id = Guid.NewGuid(),
                        TableId = insert.Id,
                        FieldName = prop.Name,
                        FieldProperties = "ForeignKey",
                        FieldDataType = entity.Name,
                        FieldOrder = counter
                    };
                    insert.Fields.Add(insertField);
                }
                else if (attr.Any(a=>a.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.InversePropertyAttribute)))
                {
                    Console.WriteLine(entity.Name + "-" + prop.Name + "-" + "Bridge table");
                    Field insertField = new Field
                    {
                        Id = Guid.NewGuid(),
                        TableId = insert.Id,
                        FieldName = prop.Name,
                        FieldProperties = "Collection",
                        FieldDataType = entity.Name,
                        FieldOrder = counter
                    };
                    insert.Fields.Add(insertField);
                }
                else
                {
                    Console.WriteLine(entity.Name + "-" + prop.Name);
                    Field insertField = new Field
                    {
                        Id = Guid.NewGuid(),
                        TableId = insert.Id,
                        FieldName = prop.Name,                           
                        FieldDataType = GetDBType(prop.PropertyType.Name),
                        FieldOrder = counter
                    };
                    insert.Fields.Add(insertField);
                }
                    
                counter ++;
                
                
            }
            context.Tables.Add(insert);
            return await context.SaveChangesAsync();
        }

        private static string GetDBType(string dbType)
        {
            switch (dbType)
            {
                case "Guid":
                    return "uniqueIdentifier";
                case "string":
                    return "text";
                case "decimal":
                    return "decimal";

                default:
                    return string.Empty;
            }
        }

        private static async Task<int> UpdateDBContext(Type entity, BooksContext context)
        {
            //find the item 
            var tableItem = context.Tables.Where(x => x.TableName.Equals(entity.Name)).FirstOrDefault();
            tableItem.ControllerName = entity.Name.Pluralize();
            context.Tables.Update(tableItem);
            //update the fields 
            // 1) get all fields existing in the DB
            // 2) get the ones that are not from DB
            // 3) get the ones that are not in the list 
            // 4) update the db

            var existingInDb = context.Fields.Where(x => x.TableId == tableItem.Id).ToList();
            var maxCount = existingInDb.Count();
            //get a list of all the fields by the fieldname
            var updatedFieldNames = entity.GetProperties().Select(x => x.Name).ToList();

            var fieldNamesToInsert = updatedFieldNames.Where(f => existingInDb.All(ef => ef.FieldName != f));
            var fieldNamesToUpDate = updatedFieldNames.Where(f => existingInDb.All(ef => ef.FieldName == f));
            var fieldsToDelete = existingInDb.Where(ef => updatedFieldNames.All(f => f != ef.FieldName));

            var fieldsToInsert = new List<Field>();
            var insertPropList = entity.GetProperties().Where(e => fieldNamesToInsert.Any(f => f == e.Name)).ToList();
            var counter = maxCount + 1;
            foreach (var insertProp in insertPropList)
            {
                foreach (var attr in insertProp.GetCustomAttributes())
                {
                    //Console.WriteLine(attr.GetType());

                    if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + insertProp.Name + "-" + "Key");
                        Field insertField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = insertProp.Name,
                            FieldProperties = "Key",
                            FieldDataType = GetDBType(insertProp.PropertyType.Name),
                            FieldOrder = counter
                        };
                        fieldsToInsert.Add(insertField);
                    }
                    else if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + insertProp.Name + "-" + "Foreign Key");
                        Field insertField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = insertProp.Name,
                            FieldProperties = "ForeignKey",
                            FieldDataType = entity.Name,
                            FieldOrder = counter
                        };
                        fieldsToInsert.Add(insertField);
                    }
                    else if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.InversePropertyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + insertProp.Name + "-" + "Bridge table");
                        Field insertField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = insertProp.Name,
                            FieldProperties = "Collection",
                            FieldDataType = entity.Name,
                            FieldOrder = counter
                        };
                        fieldsToInsert.Add(insertField);
                    }
                    else
                    {
                        Console.WriteLine(entity.Name + "-" + insertProp.Name);
                        Field insertField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = insertProp.Name,
                            FieldProperties = "Key",
                            FieldDataType = GetDBType(insertProp.PropertyType.Name),
                            FieldOrder = counter
                        };
                        fieldsToInsert.Add(insertField);
                    }

                    counter++;
                }
                counter++;
            }
            context.Fields.AddRange(fieldsToInsert);

            var fieldsToUpdate = new List<Field>();
            var updatePropList = entity.GetProperties().Where(e => fieldNamesToUpDate.Any(f => f == e.Name)).ToList();
            foreach (var updateProp in updatePropList)
            {
                foreach (var attr in updateProp.GetCustomAttributes())
                {
                    //Console.WriteLine(attr.GetType());

                    if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.KeyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + updateProp.Name + "-" + "Key");
                        Field updateField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = updateProp.Name,
                            FieldProperties = "Key",
                            FieldDataType = GetDBType(updateProp.PropertyType.Name),
                            FieldOrder = counter
                        };
                        fieldsToUpdate.Add(updateField);
                    }
                    else if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + updateProp.Name + "-" + "Foreign Key");
                        Field updateField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = updateProp.Name,
                            FieldProperties = "ForeignKey",
                            FieldDataType = entity.Name,
                            FieldOrder = counter
                        };
                        fieldsToUpdate.Add(updateField);
                    }
                    else if (attr.GetType() == typeof(System.ComponentModel.DataAnnotations.Schema.InversePropertyAttribute))
                    {
                        Console.WriteLine(entity.Name + "-" + updateProp.Name + "-" + "Bridge table");
                        Field updateField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = updateProp.Name,
                            FieldProperties = "Collection",
                            FieldDataType = entity.Name,
                            FieldOrder = counter
                        };
                        fieldsToUpdate.Add(updateField);
                    }
                    else
                    {
                        Console.WriteLine(entity.Name + "-" + updateProp.Name);
                        Field updateField = new Field
                        {
                            TableId = tableItem.Id,
                            FieldName = updateProp.Name,
                            FieldProperties = "Key",
                            FieldDataType = GetDBType(updateProp.PropertyType.Name),
                            FieldOrder = counter
                        };
                        fieldsToUpdate.Add(updateField);
                    }

                    counter++;
                }
                counter++;
            }
            context.Fields.AddRange(fieldsToUpdate);

            
            foreach (var fieldToDelete in fieldsToDelete)
            {
                context.Fields.Remove(fieldToDelete);
            }

            return await context.SaveChangesAsync();
        }
    }
}
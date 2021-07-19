//===----------------------------------------------------------------------===//
//
//                               Violet Styler
//
//===----------------------------------------------------------------------===//
//
//  Copyright (C) 2021. violet-team. All Rights Reserved.
//
//===----------------------------------------------------------------------===//

using System;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace violet_styler
{
    class Program
    {
        public static string SerializeObject(object toSerialize)
        {
            try
            {
                return JsonConvert.SerializeObject(toSerialize, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            }
            catch
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

                    using (StringWriter textWriter = new StringWriter())
                    {
                        xmlSerializer.Serialize(textWriter, toSerialize);
                        return textWriter.ToString();
                    }
                }
                catch
                {
                    return toSerialize.ToString();
                }
            }
        }

        static void Main(string[] args)
        {
            using (var conn = new MySqlConnection(args[0]))
            {
                conn.Open();

                var myCommand = conn.CreateCommand();
                var transaction = conn.BeginTransaction();

                myCommand.Transaction = transaction;
                myCommand.Connection = conn;

                try
                {
                    myCommand.CommandText = "SELECT * FROM viewreport ORDER BY Id LIMIT 10";
                    var reader = myCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        var result = new Object[reader.FieldCount];
                        reader.GetValues(result);
                        Console.WriteLine(SerializeObject(result));
                    }
                    reader.Close();
                }
                catch (Exception e)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception e1)
                    {
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }
    }
}

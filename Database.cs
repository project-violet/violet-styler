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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace violet_styler
{
    class Database
    {
        string connectionString;

        public Database(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public int Count()
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                var myCommand = conn.CreateCommand();
                var transaction = conn.BeginTransaction();

                myCommand.Transaction = transaction;
                myCommand.Connection = conn;

                try
                {
                    myCommand.CommandText = "SELECT COUNT(*) AS C FROM viewreport";
                    var reader = myCommand.ExecuteReader();
                    reader.Read();
                    var val = Convert.ToInt32(reader["C"].ToString());
                    reader.Close();
                    return val;
                }
                catch (Exception e)
                {
                    Logs.Instance.PushError(e.ToString());
                }
                finally
                {
                    conn.Close();
                }
            }

            return -1;
        }

        public List<UserArticle> LoadData(int offset, int count)
        {
            var results = new List<UserArticle>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                var myCommand = conn.CreateCommand();
                var transaction = conn.BeginTransaction();

                myCommand.Transaction = transaction;
                myCommand.Connection = conn;

                try
                {
                    myCommand.CommandText = $"SELECT * FROM viewreport ORDER BY Id LIMIT {count} OFFSET {offset}";
                    var reader = myCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        var result = new Object[reader.FieldCount];
                        reader.GetValues(result);
                        results.Add(new UserArticle(result));
                    }
                    reader.Close();
                }
                catch (Exception e)
                {
                    Logs.Instance.PushError(e.ToString());
                }
                finally
                {
                    conn.Close();
                }
            }
            return results;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtlPackage
{
    public class PackageTableSqlInserter
    {
        public int NestedLevel { get; set; }
        public string PackageName { get; private set; }
        private SqlConnection destSqlConnection { get; set; }
        public string DestinationMappingTableFqName { get; set; } = "";
        public PackageTableSqlInserter(SqlConnection destSqlConnection, EtlPackageReader etlPackageReader)
        {
            NestedLevel = 1;
            etlPackageReader.RaiseDataFlowEvent += HandleDataFlowEvent;
            this.destSqlConnection = destSqlConnection;
            this.PackageName = etlPackageReader.PackageName;
            //this.destSqlConnection.Open();
        }
        public PackageTableSqlInserter(SqlConnection destSqlConnection)
        {
            NestedLevel = 1;
            //etlPackageReader.RaiseDataFlowEvent += HandleDataFlowEvent;
            this.destSqlConnection = destSqlConnection;
            //this.PackageName = etlPackageReader.PackageName;
            //this.destSqlConnection.Open();
        }
        public void SetEtlPackageReader(EtlPackageReader etlPackageReader)
        {
            etlPackageReader.RaiseDataFlowEvent += HandleDataFlowEvent;
            this.PackageName = etlPackageReader.PackageName;
        }
        // Define what actions to take when the event is raised.
        void HandleDataFlowEvent(object sender, DataFlowEventArgs dataFlowEventArgs)
        {
            SqlCommand SqlCmd = destSqlConnection.CreateCommand();
            SqlCmd.CommandText = $@"
;WITH pkg_tab AS (
        SELECT @packageName AS PackageName
                ,@databaseName AS DatabaseName
                ,@schemaName AS SchemaName
                ,@tableName AS TableName
)
MERGE INTO DQMF.Map.PackageTable AS dest
USING pkg_tab
ON pkg_tab.PackageName = dest.PackageName
AND pkg_tab.DatabaseName = dest.DatabaseName
AND pkg_tab.SchemaName = dest.SchemaName
AND pkg_tab.TableName = dest.TableName
WHEN NOT MATCHED THEN
INSERT (PackageName, DatabaseName, SchemaName, TableName)
VALUES (pkg_tab.PackageName, pkg_tab.DatabaseName, pkg_tab.SchemaName, pkg_tab.TableName);
";
            try
            {
                
                SqlParameter parameterPackageName = new SqlParameter("@packageName", SqlDbType.VarChar);
                SqlParameter parameterDatabaseName = new SqlParameter("@databaseName", SqlDbType.VarChar);
                SqlParameter parameterSchemaName = new SqlParameter("@schemaName", SqlDbType.VarChar);
                SqlParameter parameterTableName = new SqlParameter("@tableName", SqlDbType.VarChar);

                parameterPackageName.SqlValue = PackageName;
                parameterDatabaseName.SqlValue = dataFlowEventArgs.DestinationDatabaseName;
                parameterSchemaName.SqlValue = dataFlowEventArgs.DestinationTableName.Split('.')[0];
                parameterTableName.SqlValue = dataFlowEventArgs.DestinationTableName.Split('.')[1];


                SqlCmd.Parameters.Add(parameterPackageName);
                SqlCmd.Parameters.Add(parameterDatabaseName);
                SqlCmd.Parameters.Add(parameterSchemaName);
                SqlCmd.Parameters.Add(parameterTableName);
                SqlCmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"\n\n\n\n\n\n-----------------------------------------------\n\nunable to insert mapping: {PackageName} -> {dataFlowEventArgs.DestinationTableName} into table: {DestinationMappingTableFqName}\n\n\n\n\n\n{SqlCmd.CommandText}\n\n{SqlCmd.Connection.ConnectionString}\n\n");
                Debug.WriteLine($"{e.Message}");



            }
        }

    }
}
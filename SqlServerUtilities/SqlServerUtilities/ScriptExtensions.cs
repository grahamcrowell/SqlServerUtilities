﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;

namespace SqlServerUtilities
{
    public static class ScriptExtensions
    {
        public static string ToScript(this Database database)
        {
            // if DNE then create else nothing
            return string.Format("USE master\nGO\n\nIF DB_ID('{0}') IS NULL\nBEGIN\n\tDECLARE @sql = 'CREATE DATABASE {0};'\n\tPRINT @sql;\n\tEXEC(@sql);\t\nEND\nELSE \nBEGIN\n\tPRINT '{0} already exists';\nEND\nGO\n\n\n", database.Name);
            //return "CREATE DATABASE abc;";
        }
        public static string ToScript(this Table table)
        {
            // if exists then drop GO CREATE
            return string.Format("USE {0}\nGO\n\n\nIF OBJECT_ID('{1}.{2}','U') IS NOT NULL\nBEGIN\n\tSET @sql = 'DROP TABLE {1}.{2};';\n\tPRINT @sql;\n\tEXEC(@sql);\nEND\nGO\n\n\n", table.Parent.Name, table.Schema, table.Name);
        }
        public static string ToScript(this View view)
        {
            // if DNE then create placeholder GO ALTER
            return string.Format("USE {0}\nGO\nIF OBJECT_ID('{1}.{2}','V') IS NULL \nBEGIN\n\tDECLARE @sql varchar(max);\n\tSET @sql = 'CREATE VIEW {1}.{2} AS SELECT 1 AS one;';\n\tPRINT @sql;\n\tEXEC(@sql);\nEND\nGO\n\n\n", view.Parent.Name, view.Schema, view.Name);
        }
        public static string ToScript(this StoredProcedure proc)
        {
            // if DNE then create placeholder GO ALTER
            return string.Format("USE {0}\nGO\n\n\nIF OBJECT_ID('{1}.{2}','P') IS NULL\nBEGIN\n\tSET @sql = 'CREATE PROCEDURE {1}.{2} AS BEGIN RETURN 0 END;';\n\tPRINT @sql;\n\tEXEC(@sql);\nEND\nGO\n\n\n", proc.Parent.Name, proc.Schema, proc.Name);
        }
        public static string ToScript(this UserDefinedFunction func)
        {
            // if DNE then create placeholder GO ALTER
            return string.Format("USE {0}\nGO\n\n\nIF OBJECT_ID('{1}.{2}','FN') IS NULL\nBEGIN\n\tSET @sql = 'CREATE FUNCTION {1}.{2}() RETURNS INT BEGIN RETURN 0 END;';\n\tPRINT @sql;\n\tEXEC(@sql);\nEND\nGO\n\n\n", func.Parent.Name, func.Schema, func.Name);
;
        }
        public static string ToScript(this Index index)
        {
            throw new NotImplementedException();
            //return "CREATE DATABASE abc;";
        }
    }
}

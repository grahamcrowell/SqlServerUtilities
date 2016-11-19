﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Management.Smo;
using Sequence = Microsoft.SqlServer.Dts.Runtime.Sequence;

namespace EtlPackage
{
    public class EtlPackageReader
    {
        private Application _application;
        private Package _package;

        public EtlPackageReader(string etlPackagePath)
        {
            Console.WriteLine($"EtlPackageReader({etlPackagePath})\n... loading...");
            _application = new Application();
            IDTSEvents idtsEvents = new DefaultEvents();
            _package = _application.LoadPackage(etlPackagePath, idtsEvents);
        }

        public void ReadConnectionManagers()
        {

        }
        public void ReadExecutables()
        {
            Executables executables = _package.Executables;
            ReadExecutables(executables);
        }
        public void ReadExecutables(Executables executables)
        {
            Debug.IndentLevel += 1;
            foreach (Executable executable in executables)
            {
                Debug.Print($"executable type: {executable.ToString()}");
                if (executable is TaskHost)
                {
                    Debug.Print("TaskHost");
                    TaskHost taskHost = executable as TaskHost;
                    if (taskHost.InnerObject is MainPipe)
                    {
                        Debug.Print("MainPipe");
                        ParseDataFlow(taskHost);
                    }

                }
                else if (executable.GetType() == typeof(Sequence))
                {
                    //Console.WriteLine(string.Format("\tSequence"));
                    Sequence sequence = executable as Sequence;
                    //Console.WriteLine(string.Format("\t\tName = {0}", seq.Name));
                    //Console.WriteLine(string.Format(fmt, e.GetType().Name, seq.Name));
                    //logFile.WriteLine(string.Format(fmt, e.GetType().Name, seq.Name));
                    Debug.Print($"executable name: {sequence.Name}");
                    ReadExecutables(sequence.Executables);
                }
                else if (executable.GetType() == typeof(ForEachLoop))
                {
                    //Console.WriteLine(string.Format("\tForEachLoop", e.GetType()));
                    ForEachLoop loop = executable as ForEachLoop;
                    //Console.WriteLine(string.Format("\t\tName = {0}", loop.Name));
                    //Console.WriteLine(string.Format("\t\tGetExecutionPath() = {0}", loop.GetExecutionPath()));
                    //Console.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    //logFile.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    Debug.Print($"executable name: {loop.Name}");
                    ReadExecutables(loop.Executables);
                }
                else if (executable.GetType() == typeof(ForLoop))
                {
                    //Console.WriteLine(string.Format("\tForEachLoop", e.GetType()));
                    ForLoop loop = executable as ForLoop;
                    //Console.WriteLine(string.Format("\t\tName = {0}", loop.Name));
                    //Console.WriteLine(string.Format("\t\tGetExecutionPath() = {0}", loop.GetExecutionPath()));
                    //Console.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    //logFile.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    //string.Format("{0}: {1}", executable.GetType().Name, loop.Name).Print(tabCount, ref LogFile);
                    Debug.Print($"executable name: {loop.Name}");
                    ReadExecutables(loop.Executables);
                }
                else if (executable.GetType() == typeof(Package))
                {
                    //Console.WriteLine(string.Format("\tForEachLoop", e.GetType()));
                    Package loop = executable as Package;
                    //Console.WriteLine(string.Format("\t\tName = {0}", loop.Name));
                    //Console.WriteLine(string.Format("\t\tGetExecutionPath() = {0}", loop.GetExecutionPath()));
                    //Console.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    //logFile.WriteLine(string.Format(fmt, e.GetType().Name, loop.Name));
                    Debug.Print($"executable name: {loop.Name}");
                    ReadExecutables(loop.Executables);
                }
                else
                {
                    //Console.WriteLine(string.Format(fmt, e.GetType(), "UNHANDLED executable type"));
                    //logFile.WriteLine(string.Format(fmt, e.GetType(), "UNHANDLED executable type"));
                    Debug.Print($"{executable.GetType()} executable type is NOT IMPLEMENTED");
                }
            }
            Debug.IndentLevel -= 1;
        }
        private void ParseDataFlow(TaskHost taskHost)
        {
            string sourceQuery = null;
            string destinationTableName = null;
            string destinationDatabaseName = null;
            Debug.Print($"Data Flow Task: {taskHost.Name}");
            Debug.IndentLevel += 1;
            MainPipe mainPipe = taskHost.InnerObject as MainPipe;
            IDTSComponentMetaDataCollection100 metaDataCollection = mainPipe.ComponentMetaDataCollection;
            foreach (IDTSComponentMetaData100 componentMetaData in metaDataCollection)
            {
                string componentMetaDataType = componentMetaData.ContactInfo.Split(';')[0];
                Debug.Print($"component: {componentMetaDataType}");
                Debug.Print($"{nameof(componentMetaData.Name)}: {componentMetaData.Name}");
                Debug.Print($"{nameof(componentMetaData.Description)}: {componentMetaData.Description}");
                IDTSCustomPropertyCollection100 customPropertyCollection = componentMetaData.CustomPropertyCollection;
                if (componentMetaDataType == "OLE DB Destination")
                {
                    if (customPropertyCollection.Count > 0)
                    {
                        Debug.IndentLevel += 1;
                        foreach (IDTSCustomProperty100 customProperty in customPropertyCollection)
                        {
                            string valueStr = customProperty.Value as string;
                            if (customProperty.Name == "OpenRowset" && valueStr.Length > 0)
                            {
                                Debug.Print($"{nameof(customProperty.Name)}: {customProperty.Name}");
                                Debug.Print($"{nameof(customProperty.Value)}: {customProperty.Value}");
                                destinationTableName = valueStr;
                            }
                        }
                        Debug.IndentLevel -= 1;
                    }
                }
                else if (componentMetaDataType == "OLE DB Source")
                {
                    if (customPropertyCollection.Count > 0)
                    {
                        Debug.IndentLevel += 1;
                        foreach (IDTSCustomProperty100 customProperty in customPropertyCollection)
                        {
                            string valueStr = customProperty.Value as string;
                            if (customProperty.Name == "SqlCommand" && valueStr.Length > 0)
                            {
                                Debug.Print($"{nameof(customProperty.Name)}: {customProperty.Name}");
                                Debug.Print($"{nameof(customProperty.Value)}: {customProperty.Value}");
                                sourceQuery = valueStr;
                            }
                        }
                        Debug.IndentLevel -= 1;
                    }
                }
                Debug.IndentLevel -= 1;
            }
            if (sourceQuery == null)
            {
                Debug.Print($"sourceQuery not found");
                return;
            }
            if (destinationTableName == null)
            {
                Debug.Print($"destinationTableName not found");
                return;
            }
            sourceQuery.ToFile($"{Path.Combine(Utilities.Cwd(), destinationTableName + ".sql")}");
        }
    }
}
